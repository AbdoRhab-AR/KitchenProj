using KitchenProject.Classes;
using KitchenProject.Models;
using KitchenProject.Models.Entities;
using KitchenProject.Models.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KitchenProject.Controllers
{
	[ViewLayout("_DashboardLayout")]
	//[Authorize(Policy = "ProgOrAdminOrEmployeePolicy")]

	public class CustomersController : Controller
    {
		private readonly IUnitOfWork<Customer> _customer;
		private readonly LookupService _lookup;
		private readonly ApplicationDbContext _db;

		public CustomersController(IUnitOfWork<Customer> customer, LookupService lookup, ApplicationDbContext db)
		{
			_customer = customer;
			_lookup = lookup;
			_db = db;
		}

		// GET: Customers
		public async Task<IActionResult> Index(string? searchString, int page = 1)
        {
            ViewBag.ActivePage = "Customers";
            ViewBag.SearchString = searchString;

            var query = _customer.Entity.Include(c => c.Projects);

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                if (!SearchValidator.TryValidateNameSearch(searchString, out var validName, out var searchError))
                {
                    TempData["Error"] = searchError;
                    ViewBag.SearchError = searchError;
                    query = query.Where(_ => false);
                }
                else
                {
                    query = query.Where(c => c.Name.Contains(validName));
                }
            }

            var totalCount = await query.CountAsync();
            int pageSize = await _lookup.PageSizeAsync();
            var customers = await query.OrderByDescending(c => c.Created)
                                      .Skip((page - 1) * pageSize)
                                      .Take(pageSize)
                                      .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            ViewBag.TotalCount = totalCount;

            // Metrics for Dashboard header cards
            var firstDayOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            ViewBag.TotalCustomersCount = await _customer.Entity.GetAll().CountAsync();
            ViewBag.NewCustomersThisMonth = await _customer.Entity.GetAll()
                .Where(c => c.Created >= firstDayOfMonth)
                .CountAsync();

            var ids = customers.Select(c => c.Id).ToList();
            var contractsByCustomer = await _db.Contracts.AsNoTracking()
                .Where(c => ids.Contains(c.Projects.CustomerId)
                         && (c.Status == Models.Enums.ContractStatuses.Active || c.Status == Models.Enums.ContractStatuses.Completed)
                         && c.TotalAmount > 0)
                .GroupBy(c => c.Projects.CustomerId)
                .Select(g => new { g.Key, Total = g.Sum(c => c.TotalAmount) })
                .ToDictionaryAsync(x => x.Key, x => x.Total);

            var cancelFeesByCustomer = await _db.Projects.AsNoTracking()
                .Where(p => ids.Contains(p.CustomerId)
                         && p.Status == Models.Enums.ProjectStatus.Cancelled
                         && p.CancellationFee > 0
                         && !p.Contracts.Any(c => (c.Status == Models.Enums.ContractStatuses.Active || c.Status == Models.Enums.ContractStatuses.Completed) && c.TotalAmount > 0))
                .GroupBy(p => p.CustomerId)
                .Select(g => new { g.Key, Total = g.Sum(p => p.CancellationFee) })
                .ToDictionaryAsync(x => x.Key, x => x.Total);

            var paidByCustomer = await _db.CustomerPayments.AsNoTracking()
                .Where(p => ids.Contains(p.Project.CustomerId)
                         && !(p.Payment == Models.Enums.PaymentMethod.Chek && p.ChequeStatus == Models.Enums.ChequeStatus.Bounced))
                .GroupBy(p => p.Project.CustomerId)
                .Select(g => new { g.Key, Paid = g.Sum(p => p.IsRefund ? -p.Amount : p.Amount) })
                .ToDictionaryAsync(x => x.Key, x => x.Paid);

            ViewBag.Balances = ids.ToDictionary(
                id => id,
                id => (Billed: contractsByCustomer.GetValueOrDefault(id) + cancelFeesByCustomer.GetValueOrDefault(id),
                       Paid: paidByCustomer.GetValueOrDefault(id)));

            var allContractsTotal = await _db.Contracts.AsNoTracking()
                .Where(c => (c.Status == Models.Enums.ContractStatuses.Active || c.Status == Models.Enums.ContractStatuses.Completed) && c.TotalAmount > 0)
                .SumAsync(c => (decimal?)c.TotalAmount) ?? 0m;
            var allCancelFees = await _db.Projects.AsNoTracking()
                .Where(p => p.Status == Models.Enums.ProjectStatus.Cancelled && p.CancellationFee > 0
                         && !p.Contracts.Any(c => (c.Status == Models.Enums.ContractStatuses.Active || c.Status == Models.Enums.ContractStatuses.Completed) && c.TotalAmount > 0))
                .SumAsync(p => (decimal?)p.CancellationFee) ?? 0m;
            var allCustomerPaid = await _db.CustomerPayments.AsNoTracking()
                .Where(p => !(p.Payment == Models.Enums.PaymentMethod.Chek && p.ChequeStatus == Models.Enums.ChequeStatus.Bounced))
                .SumAsync(p => (decimal?)(p.IsRefund ? -p.Amount : p.Amount)) ?? 0m;
            ViewBag.TotalReceivables = allContractsTotal + allCancelFees - allCustomerPaid;

            return View(customers);
        }

        // GET: Customers/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            ViewBag.ActivePage = "Customers";

            if (id == null)
            {
                return NotFound();
            }

            var customer = await _customer.Entity.Include(c => c.Projects)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (customer == null)
            {
                return NotFound();
            }

            var (_, rows, _) = await BuildStatementAsync(customer.Id, null, null);
            ViewBag.TotalBilled = rows.Sum(r => r.Debit);
            ViewBag.TotalPaid = rows.Sum(r => r.Credit);
            ViewBag.DueBalance = rows.Any() ? rows.Last().Balance : 0m;

            ViewBag.Payments = await _db.CustomerPayments.AsNoTracking()
                .Include(p => p.Project)
                .Include(p => p.Contract)
                .Where(p => p.Project.CustomerId == customer.Id)
                .OrderByDescending(p => p.PaymentDate)
                .ThenByDescending(p => p.Created)
                .ToListAsync();

            return View(customer);
        }

        private async Task<(Customer? Customer, List<ViewModels.PartyStatementRow> Rows, decimal Opening)> BuildStatementAsync(Guid id, DateTime? from, DateTime? to)
        {
            var customer = await _customer.Entity.GetWhere(c => c.Id == id).FirstOrDefaultAsync();
            if (customer == null) return (null, new(), 0);

            var raw = new List<(DateTime Date, string Type, string No, string Desc, decimal Debit, decimal Credit)>();

            var contracts = await _db.Contracts.AsNoTracking()
                .Where(c => c.Projects.CustomerId == id && (c.Status == Models.Enums.ContractStatuses.Active || c.Status == Models.Enums.ContractStatuses.Completed) && c.TotalAmount > 0)
                .Select(c => new { Date = (c.StartDate ?? c.Created).Date, c.ContractNo, c.Projects.ProjectNo, c.TotalAmount })
                .ToListAsync();
            foreach (var c in contracts)
                raw.Add((c.Date, "عقد", c.ContractNo, $"عقد مشروع {c.ProjectNo}", c.TotalAmount, 0));

            var cancelled = await _db.Projects.AsNoTracking()
                .Where(p => p.CustomerId == id && p.Status == Models.Enums.ProjectStatus.Cancelled && p.CancellationFee > 0
                    && !p.Contracts.Any(c => (c.Status == Models.Enums.ContractStatuses.Active || c.Status == Models.Enums.ContractStatuses.Completed) && c.TotalAmount > 0))
                .Select(p => new { Date = (p.Modified ?? p.Created).Date, p.ProjectNo, p.CancellationFee })
                .ToListAsync();
            foreach (var p in cancelled)
                raw.Add((p.Date, "رسوم إلغاء", p.ProjectNo, $"رسوم إلغاء المشروع {p.ProjectNo}", p.CancellationFee, 0));

            var payments = await _db.CustomerPayments.AsNoTracking()
                .Where(p => p.Project.CustomerId == id && !(p.Payment == Models.Enums.PaymentMethod.Chek && p.ChequeStatus == Models.Enums.ChequeStatus.Bounced))
                .Select(p => new { Date = p.PaymentDate.Date, p.ReceiptNo, p.Project.ProjectNo, p.Notes, p.Amount, p.IsRefund })
                .ToListAsync();
            foreach (var p in payments)
                raw.Add((p.Date, p.IsRefund ? "سند إرجاع" : "سند قبض", p.ReceiptNo, p.Notes ?? $"مشروع {p.ProjectNo}", p.IsRefund ? p.Amount : 0, p.IsRefund ? 0 : p.Amount));

            var ordered = raw.OrderBy(r => r.Date).ThenBy(r => r.Debit > 0 ? 0 : 1).ThenBy(r => r.No).ToList();
            decimal opening = 0;
            var rows = new List<ViewModels.PartyStatementRow>();
            decimal balance = 0;
            foreach (var r in ordered)
            {
                if (from.HasValue && r.Date < from.Value.Date)
                {
                    opening += r.Debit - r.Credit;
                    balance = opening;
                    continue;
                }
                if (to.HasValue && r.Date > to.Value.Date) continue;

                balance += r.Debit - r.Credit;
                rows.Add(new ViewModels.PartyStatementRow
                {
                    Date = r.Date, DocType = r.Type, DocNo = r.No, Description = r.Desc,
                    Debit = r.Debit, Credit = r.Credit, Balance = balance
                });
            }

            ViewBag.From = from;
            ViewBag.To = to;
            ViewBag.Opening = opening;
            return (customer, rows, opening);
        }

        // GET: Customers/Statement/5
        public async Task<IActionResult> Statement(Guid id, DateTime? from, DateTime? to)
        {
            ViewBag.ActivePage = "Customers";
            var (customer, rows, _) = await BuildStatementAsync(id, from, to);
            if (customer == null) return NotFound();
            ViewBag.Customer = customer;
            return View(rows);
        }

        // GET: Customers/StatementPrint/5  (A4)
        public async Task<IActionResult> StatementPrint(Guid id, DateTime? from, DateTime? to)
        {
            var (customer, rows, _) = await BuildStatementAsync(id, from, to);
            if (customer == null) return NotFound();
            ViewBag.Customer = customer;
            ViewBag.PrintedBy = User.Identity?.Name;
            return View(rows);
        }

        // GET: Customers/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.ActivePage = "Customers";

            // Generate unique customer number
            var nextNo = await DocumentNumbers.NextAsync(_customer.Entity.GetAll().Select(c => c.CustomerNo), "CUST-");

            var model = new Customer
            {
                CustomerNo = nextNo,
                Name = "",
                Phone = "",
                Address = "",
                City = "",
                Notes = ""
            };

            return View(model);
        }

        // POST: Customers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Customer customer)
        {
            ViewBag.ActivePage = "Customers";

            if (ModelState.IsValid)
            {
                customer.Id = Guid.NewGuid();
                customer.Created = DateTime.UtcNow;

                _customer.Entity.Insert(customer);
                await _customer.SaveAsync();

                TempData["Success"] = $"تم إضافة العميل \"{customer.Name}\" بنجاح برقم {customer.CustomerNo}";
                return RedirectToAction(nameof(Index));
            }

            return View(customer);
        }


        // GET: Customers/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            ViewBag.ActivePage = "Customers";

            if (id == null)
            {
                return NotFound();
            }

            var customer = await _customer.Entity.GetByIdAsync(id.Value);
            if (customer == null)
            {
                return NotFound();
            }

            return View(customer);
        }


        // POST: Customers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, Customer customer)
        {
            ViewBag.ActivePage = "Customers";

            if (id != customer.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    customer.Created = await _customer.Entity.GetWhere(c => c.Id == id).Select(c => c.Created).FirstOrDefaultAsync();
                    customer.Modified = DateTime.UtcNow;
                    _customer.Entity.Update(customer);
                    await _customer.SaveAsync();

                    TempData["Success"] = $"تم تحديث بيانات العميل \"{customer.Name}\" بنجاح";
                }
                catch (DbUpdateConcurrencyException)
                {
                    var exists = await _customer.Entity.GetByIdAsync(customer.Id) != null;
                    if (!exists)
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(customer);
        }


        // POST: Customers/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var customer = await _customer.Entity.GetByIdAsync(id);
            if (customer != null && await _customer.Entity.GetAll().AnyAsync(c => c.Id == id && c.Projects.Any()))
            {
                // المشاريع مربوطة بـ Restrict، فالحذف سيفشل من قاعدة البيانات
                TempData["Error"] = $"لا يمكن حذف العميل \"{customer.Name}\" لوجود مشاريع مسجلة باسمه";
            }
            else if (customer != null)
            {
                _customer.Entity.Delete(customer);
                await _customer.SaveAsync();
                TempData["Success"] = $"تم حذف العميل \"{customer.Name}\" بنجاح";
            }
            else
            {
                TempData["Error"] = "تعذر العثور على العميل المراد حذفه";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
