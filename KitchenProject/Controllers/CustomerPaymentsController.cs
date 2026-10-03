using KitchenProject.Classes;
using KitchenProject.Models;
using KitchenProject.Models.Entities;
using KitchenProject.Models.Enums;
using KitchenProject.Models.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace KitchenProject.Controllers
{
    [ViewLayout("_DashboardLayout")]
    public class CustomerPaymentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IUnitOfWork<CustomerPayment> _payment;
        private readonly IUnitOfWork<Contract> _contract;

        public CustomerPaymentsController(ApplicationDbContext context, IUnitOfWork<CustomerPayment> payment, IUnitOfWork<Contract> contract)
        {
            _context = context;
            _payment = payment;
            _contract = contract;
        }

        // GET: CustomerPayments
        public async Task<IActionResult> Index(string? searchString, Guid? projectFilter, Guid? contractFilter, int page = 1)
        {
            ViewBag.ActivePage = "CustomerPayments";
            ViewBag.SearchString = searchString;
            ViewBag.ProjectFilter = projectFilter;
            ViewBag.ContractFilter = contractFilter;

            var query = _context.CustomerPayments
                .Include(p => p.Project).ThenInclude(pr => pr.Customer)
                .Include(p => p.Contract)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                searchString = searchString.Trim();
                query = query.Where(p =>
                    p.ReceiptNo.Contains(searchString) ||
                    p.Project.ProjectNo.Contains(searchString) ||
                    p.Project.ProjectName.Contains(searchString) ||
                    p.Project.Customer.Name.Contains(searchString));
            }

            if (projectFilter.HasValue)
                query = query.Where(p => p.ProjectId == projectFilter.Value);

            if (contractFilter.HasValue)
                query = query.Where(p => p.ContractId == contractFilter.Value);

            var totalCount = await query.CountAsync();
            int pageSize = 10;
            var payments = await query
                .OrderByDescending(p => p.Created)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            ViewBag.TotalCount = totalCount;

            ViewBag.TotalPaymentsAmount = await _context.CustomerPayments.SumAsync(p => p.Amount);

            var projects = await _context.Projects
                .Include(p => p.Customer)
                .OrderByDescending(p => p.Created)
                .Select(p => new { Id = p.Id, Name = $"{p.ProjectNo} - {p.ProjectName} ({p.Customer.Name})" })
                .ToListAsync();
            ViewBag.ProjectsList = new SelectList(projects, "Id", "Name", projectFilter);

            return View(payments);
        }

        // GET: CustomerPayments/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            ViewBag.ActivePage = "CustomerPayments";

            if (id == null) return NotFound();

            var payment = await _context.CustomerPayments
                .Include(p => p.Project).ThenInclude(pr => pr.Customer)
                .Include(p => p.Contract)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (payment == null) return NotFound();

            return View(payment);
        }

        // GET: CustomerPayments/Create
        public async Task<IActionResult> Create(Guid? projectId, Guid? contractId, decimal? amount = null)
        {
            ViewBag.ActivePage = "CustomerPayments";

            var count = await _context.CustomerPayments.CountAsync();
            var nextNo = $"RCPT-{(count + 1):D4}";

            var model = new CustomerPayment
            {
                ReceiptNo = nextNo,
                ProjectId = projectId ?? Guid.Empty,
                ContractId = contractId,
                Payment = PaymentMethod.Cash,
                Amount = amount ?? 0
            };

            await PopulateDropdownsAsync(model.ProjectId, model.ContractId);
            return View(model);
        }

        // POST: CustomerPayments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CustomerPayment customerPayment)
        {
            ViewBag.ActivePage = "CustomerPayments";

            if (ModelState.IsValid)
            {
                customerPayment.Id = Guid.NewGuid();
                customerPayment.Created = DateTime.UtcNow;

                _payment.Entity.Insert(customerPayment);

                // Update Contract PaidAmount
                if (customerPayment.ContractId.HasValue)
                {
                    var contract = await _contract.Entity.GetByIdAsync(customerPayment.ContractId.Value);
                    if (contract != null)
                    {
                        contract.PaidAmount += customerPayment.Amount;
                        _contract.Entity.Update(contract);
                    }
                }

                await _context.SaveChangesAsync();

                TempData["Success"] = $"تم إضافة دفعة العميل \"{customerPayment.ReceiptNo}\" بنجاح";
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropdownsAsync(customerPayment.ProjectId, customerPayment.ContractId);
            return View(customerPayment);
        }

        // GET: CustomerPayments/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            ViewBag.ActivePage = "CustomerPayments";

            if (id == null) return NotFound();

            var payment = await _payment.Entity.GetByIdAsync(id.Value);
            if (payment == null) return NotFound();

            await PopulateDropdownsAsync(payment.ProjectId, payment.ContractId);
            return View(payment);
        }

        // POST: CustomerPayments/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, CustomerPayment customerPayment)
        {
            ViewBag.ActivePage = "CustomerPayments";

            if (id != customerPayment.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // Update Contract PaidAmount difference
                    var oldPayment = await _context.CustomerPayments.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
                    if (oldPayment != null)
                    {
                        // Remove amount from old contract if changed
                        if (oldPayment.ContractId.HasValue)
                        {
                            var oldContract = await _contract.Entity.GetByIdAsync(oldPayment.ContractId.Value);
                            if (oldContract != null)
                            {
                                oldContract.PaidAmount -= oldPayment.Amount;
                                _contract.Entity.Update(oldContract);
                            }
                        }

                        // Add amount to new contract
                        if (customerPayment.ContractId.HasValue)
                        {
                            var newContract = await _contract.Entity.GetByIdAsync(customerPayment.ContractId.Value);
                            if (newContract != null)
                            {
                                newContract.PaidAmount += customerPayment.Amount;
                                _contract.Entity.Update(newContract);
                            }
                        }
                    }

                    customerPayment.Modified = DateTime.UtcNow;
                    _payment.Entity.Update(customerPayment);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = $"تم تحديث الدفعة \"{customerPayment.ReceiptNo}\" بنجاح";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (await _payment.Entity.GetByIdAsync(customerPayment.Id) == null) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropdownsAsync(customerPayment.ProjectId, customerPayment.ContractId);
            return View(customerPayment);
        }

        // POST: CustomerPayments/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var payment = await _payment.Entity.GetByIdAsync(id);
            if (payment != null)
            {
                // Revert Contract PaidAmount
                if (payment.ContractId.HasValue)
                {
                    var contract = await _contract.Entity.GetByIdAsync(payment.ContractId.Value);
                    if (contract != null)
                    {
                        contract.PaidAmount -= payment.Amount;
                        _contract.Entity.Update(contract);
                    }
                }

                _payment.Entity.Delete(payment);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"تم حذف الدفعة \"{payment.ReceiptNo}\" بنجاح";
            }
            else
            {
                TempData["Error"] = "تعذر العثور على الدفعة المراد حذفها";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> GetProjectContracts(Guid projectId)
        {
            var contracts = await _context.Contracts
                .Where(c => c.ProjectId == projectId)
                .Select(c => new { id = c.Id, text = c.ContractNo, remaining = c.TotalAmount - c.PaidAmount })
                .ToListAsync();

            return Json(contracts);
        }

        private async Task PopulateDropdownsAsync(Guid? projectId, Guid? contractId)
        {
            var projects = await _context.Projects
                .Include(p => p.Customer)
                .OrderByDescending(p => p.Created)
                .Select(p => new { Id = p.Id, Name = $"{p.ProjectNo} - {p.ProjectName} ({p.Customer.Name})" })
                .ToListAsync();

            ViewBag.ProjectId = new SelectList(projects, "Id", "Name", projectId);

            var enumList = Enum.GetValues(typeof(PaymentMethod)).Cast<PaymentMethod>().Select(e => new
            {
                Id = (int)e,
                Name = e.GetType().GetMember(e.ToString()).First().GetCustomAttributes(false).OfType<System.ComponentModel.DataAnnotations.DisplayAttribute>().LastOrDefault()?.Name ?? e.ToString()
            });
            ViewBag.Payment = new SelectList(enumList, "Id", "Name");

            var contracts = await _context.Contracts
                .Where(c => c.ProjectId == projectId)
                .Select(c => new { Id = c.Id, Name = c.ContractNo })
                .ToListAsync();
            ViewBag.ContractId = new SelectList(contracts, "Id", "Name", contractId);
        }
    }
}
