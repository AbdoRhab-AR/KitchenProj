using KitchenProject.Classes;
using KitchenProject.Models.Entities;
using KitchenProject.Models.Enums;
using KitchenProject.Models.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KitchenProject.Controllers
{
	[ViewLayout("_DashboardLayout")]
	//[Authorize(Policy = "ProgOrAdminOrEmployeePolicy")]

	public class SuppliersController : Controller
	{
		private readonly IUnitOfWork<Supplier> _supplier;
		private readonly IUnitOfWork<Purchase> _purchase;
		private readonly IUnitOfWork<SupplierPayment> _payment;
		private readonly LookupService _lookup;
		private readonly Models.ApplicationDbContext _db;

		public SuppliersController(IUnitOfWork<Supplier> supplier,
								   IUnitOfWork<Purchase> purchase,
								   IUnitOfWork<SupplierPayment> payment,
								   LookupService lookup,
								   Models.ApplicationDbContext db)
		{
			_supplier = supplier;
			_purchase = purchase;
			_payment = payment;
			_lookup = lookup;
			_db = db;
		}

		// GET: Suppliers
		public async Task<IActionResult> Index(string? searchString, int page = 1)
		{
			ViewBag.ActivePage = "Suppliers";
			ViewBag.SearchString = searchString;

			var query = _supplier.Entity.GetAll();

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
					query = query.Where(s => s.Name.Contains(validName));
				}
			}

			var totalCount = await query.CountAsync();
			int pageSize = await _lookup.PageSizeAsync();
			var suppliers = await query.OrderBy(s => s.Name)
									   .Skip((page - 1) * pageSize)
									   .Take(pageSize)
									   .ToListAsync();

			ViewBag.CurrentPage = page;
			ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
			ViewBag.TotalCount = totalCount;

			// المستحق لكل مورد = فواتير مستلمة - مرتجعات المشتريات - إجمالي الصرف الفعلي للمورد
			var ids = suppliers.Select(s => s.Id).ToList();
			var purchasesBySupplier = await _purchase.Entity
				.GetWhere(p => ids.Contains(p.SupplierId) && p.Status == PurchaseStatuses.Received)
				.GroupBy(p => p.SupplierId)
				.Select(g => new { g.Key, Total = g.Sum(p => p.Total) })
				.ToDictionaryAsync(x => x.Key, x => x.Total);
			var returnsBySupplier = await _db.PurchaseReturns.AsNoTracking()
				.Where(r => ids.Contains(r.SupplierId))
				.GroupBy(r => r.SupplierId)
				.Select(g => new { g.Key, Total = g.Sum(r => r.Total) })
				.ToDictionaryAsync(x => x.Key, x => x.Total);
			var paymentsBySupplier = await _payment.Entity
				.GetWhere(p => ids.Contains(p.SupplierId) && !(p.PaymentMethod == PaymentMethod.Chek && p.ChequeStatus == ChequeStatus.Bounced))
				.GroupBy(p => p.SupplierId)
				.Select(g => new { g.Key, Paid = g.Sum(p => p.Amount) })
				.ToDictionaryAsync(x => x.Key, x => x.Paid);

			ViewBag.Balances = ids.ToDictionary(
				id => id,
				id => (purchasesBySupplier.GetValueOrDefault(id) - returnsBySupplier.GetValueOrDefault(id), paymentsBySupplier.GetValueOrDefault(id)));

			var received = _purchase.Entity.GetWhere(p => p.Status == PurchaseStatuses.Received);
			ViewBag.SupplierCount = await _supplier.Entity.GetAll().CountAsync(s => s.IsActive);
			var purchasesTotal = await received.SumAsync(p => (decimal?)p.Total) ?? 0;
			var allReturns = await _db.PurchaseReturns.AsNoTracking().SumAsync(r => (decimal?)r.Total) ?? 0;
			var allPaid = await _payment.Entity
				.GetWhere(p => !(p.PaymentMethod == PaymentMethod.Chek && p.ChequeStatus == ChequeStatus.Bounced))
				.SumAsync(p => (decimal?)p.Amount) ?? 0;
			ViewBag.PurchasesTotal = purchasesTotal - allReturns;
			ViewBag.DueTotal = ViewBag.PurchasesTotal - allPaid;

			return View(suppliers);
		}

		// GET: Suppliers/Details/5
		public async Task<IActionResult> Details(Guid? id)
		{
			ViewBag.ActivePage = "Suppliers";

			var supplier = id == null ? null : await _supplier.Entity.GetWhere(s => s.Id == id).FirstOrDefaultAsync();
			if (supplier == null)
			{
				return NotFound();
			}

			ViewBag.Purchases = await _purchase.Entity.Include(p => p.Projects!)
				.Where(p => p.SupplierId == supplier.Id)
				.OrderByDescending(p => p.PurchaseDate).ThenByDescending(p => p.Created)
				.ToListAsync();

			ViewBag.Payments = await _payment.Entity.Include(p => p.Purchase!)
				.Where(p => p.SupplierId == supplier.Id)
				.OrderByDescending(p => p.PaymentDate).ThenByDescending(p => p.Created)
				.ToListAsync();

			ViewBag.ReturnsTotal = await _db.PurchaseReturns.AsNoTracking()
				.Where(r => r.SupplierId == supplier.Id).SumAsync(r => (decimal?)r.Total) ?? 0;

			return View(supplier);
		}

		private async Task<(Supplier? Supplier, List<ViewModels.PartyStatementRow> Rows, decimal Opening)> BuildStatementAsync(Guid id, DateTime? from, DateTime? to)
		{
			var supplier = await _supplier.Entity.GetWhere(s => s.Id == id).FirstOrDefaultAsync();
			if (supplier == null) return (null, new(), 0);

			var raw = new List<(DateTime Date, string Type, string No, string Desc, decimal Debit, decimal Credit)>();

			// له (دائن) = فواتير الشراء المستلمة
			var purchases = await _purchase.Entity.GetWhere(p => p.SupplierId == id && p.Status == PurchaseStatuses.Received)
				.Select(p => new { Date = p.PurchaseDate.Date, p.PurchaseNo, p.InvoiceNo, p.Total })
				.ToListAsync();
			foreach (var p in purchases)
				raw.Add((p.Date, "فاتورة شراء", p.PurchaseNo, string.IsNullOrEmpty(p.InvoiceNo) ? "فاتورة مشتريات" : $"فاتورة مورد {p.InvoiceNo}", 0, p.Total));

			// عليه (مدين) = مرتجعات الشراء + المدفوعات
			var returns = await _db.PurchaseReturns.AsNoTracking().Where(r => r.SupplierId == id)
				.Select(r => new { Date = r.ReturnDate.Date, r.ReturnNo, r.Notes, r.Total })
				.ToListAsync();
			foreach (var r in returns)
				raw.Add((r.Date, "مرتجع شراء", r.ReturnNo, r.Notes ?? "إشعار مرتجع مشتريات", r.Total, 0));

			var payments = await _payment.Entity.GetWhere(p => p.SupplierId == id && !(p.PaymentMethod == PaymentMethod.Chek && p.ChequeStatus == ChequeStatus.Bounced))
				.Select(p => new { Date = p.PaymentDate.Date, p.ReceiptNo, p.Notes, p.Amount })
				.ToListAsync();
			foreach (var p in payments)
				raw.Add((p.Date, "إيصال صرف", p.ReceiptNo, p.Notes ?? "دفعة للمورد", p.Amount, 0));

			// رصيد المورد دائن بطبيعته: يزيد بالفواتير وينقص بالسداد والمرتجع
			var ordered = raw.OrderBy(r => r.Date).ThenBy(r => r.Credit > 0 ? 0 : 1).ThenBy(r => r.No).ToList();
			decimal opening = 0, balance = 0;
			var rows = new List<ViewModels.PartyStatementRow>();
			foreach (var r in ordered)
			{
				if (from.HasValue && r.Date < from.Value.Date)
				{
					opening += r.Credit - r.Debit;
					balance = opening;
					continue;
				}
				if (to.HasValue && r.Date > to.Value.Date) continue;

				balance += r.Credit - r.Debit;
				rows.Add(new ViewModels.PartyStatementRow
				{
					Date = r.Date, DocType = r.Type, DocNo = r.No, Description = r.Desc,
					Debit = r.Debit, Credit = r.Credit, Balance = balance
				});
			}

			ViewBag.From = from;
			ViewBag.To = to;
			ViewBag.Opening = opening;
			return (supplier, rows, opening);
		}

		// GET: Suppliers/Statement/5
		public async Task<IActionResult> Statement(Guid id, DateTime? from, DateTime? to)
		{
			ViewBag.ActivePage = "Suppliers";
			var (supplier, rows, _) = await BuildStatementAsync(id, from, to);
			if (supplier == null) return NotFound();
			ViewBag.Supplier = supplier;
			return View(rows);
		}

		// GET: Suppliers/StatementPrint/5  (A4)
		public async Task<IActionResult> StatementPrint(Guid id, DateTime? from, DateTime? to)
		{
			var (supplier, rows, _) = await BuildStatementAsync(id, from, to);
			if (supplier == null) return NotFound();
			ViewBag.Supplier = supplier;
			ViewBag.PrintedBy = User.Identity?.Name;
			return View(rows);
		}

		// GET: Suppliers/Create
		public async Task<IActionResult> Create()
		{
			ViewBag.ActivePage = "Suppliers";
			return View(new Supplier { SupplierNo = await GenerateNoAsync(), Name = "" });
		}

		// POST: Suppliers/Create
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(Supplier supplier)
		{
			ViewBag.ActivePage = "Suppliers";

			if (await _supplier.Entity.GetAll().AnyAsync(s => s.SupplierNo == supplier.SupplierNo))
			{
				supplier.SupplierNo = await GenerateNoAsync();
			}

			if (ModelState.IsValid)
			{
				supplier.Id = Guid.NewGuid();
				supplier.Created = DateTime.UtcNow;
				_supplier.Entity.Insert(supplier);
				await _supplier.SaveAsync();

				TempData["Success"] = $"تمت إضافة المورد {supplier.Name}";
				return RedirectToAction(nameof(Details), new { id = supplier.Id });
			}

			return View(supplier);
		}

		// GET: Suppliers/Edit/5
		public async Task<IActionResult> Edit(Guid? id)
		{
			ViewBag.ActivePage = "Suppliers";

			var supplier = id == null ? null : await _supplier.Entity.GetByIdAsync(id);
			if (supplier == null)
			{
				return NotFound();
			}

			return View(supplier);
		}

		// POST: Suppliers/Edit/5
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(Guid id, Supplier supplier)
		{
			ViewBag.ActivePage = "Suppliers";

			if (id != supplier.Id)
			{
				return NotFound();
			}

			if (ModelState.IsValid)
			{
				supplier.Created = await _supplier.Entity.GetWhere(s => s.Id == id).Select(s => s.Created).FirstOrDefaultAsync();
				supplier.Modified = DateTime.UtcNow;
				_supplier.Entity.Update(supplier);
				await _supplier.SaveAsync();

				TempData["Success"] = $"تم تحديث بيانات المورد {supplier.Name}";
				return RedirectToAction(nameof(Details), new { id = supplier.Id });
			}

			return View(supplier);
		}

		// POST: Suppliers/Delete/5
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Delete(Guid id)
		{
			var supplier = await _supplier.Entity.GetByIdAsync(id);
			if (supplier == null)
			{
				TempData["Error"] = "تعذر العثور على المورد المراد حذفه";
				return RedirectToAction(nameof(Index));
			}

			if (await _purchase.Entity.GetAll().AnyAsync(p => p.SupplierId == id)
				|| await _payment.Entity.GetAll().AnyAsync(p => p.SupplierId == id))
			{
				TempData["Error"] = $"لا يمكن حذف المورد {supplier.Name} لأن عليه فواتير أو مدفوعات، يمكنك إيقافه من شاشة التعديل";
				return RedirectToAction(nameof(Details), new { id });
			}

			_supplier.Entity.Delete(supplier);
			await _supplier.SaveAsync();
			TempData["Success"] = $"تم حذف المورد {supplier.Name}";
			return RedirectToAction(nameof(Index));
		}

		private Task<string> GenerateNoAsync() =>
			DocumentNumbers.NextAsync(_supplier.Entity.GetAll().Select(s => s.SupplierNo), "SUP-");
	}
}
