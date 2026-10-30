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

		public SuppliersController(IUnitOfWork<Supplier> supplier,
								   IUnitOfWork<Purchase> purchase,
								   IUnitOfWork<SupplierPayment> payment)
		{
			_supplier = supplier;
			_purchase = purchase;
			_payment = payment;
		}

		// GET: Suppliers
		public async Task<IActionResult> Index(string? searchString, int page = 1)
		{
			ViewBag.ActivePage = "Suppliers";
			ViewBag.SearchString = searchString;

			var query = _supplier.Entity.GetAll();

			if (!string.IsNullOrWhiteSpace(searchString))
			{
				searchString = searchString.Trim();
				query = query.Where(s => s.Name.Contains(searchString)
									  || s.SupplierNo.Contains(searchString)
									  || (s.Phone != null && s.Phone.Contains(searchString)));
			}

			var totalCount = await query.CountAsync();
			int pageSize = 10;
			var suppliers = await query.OrderBy(s => s.Name)
									   .Skip((page - 1) * pageSize)
									   .Take(pageSize)
									   .ToListAsync();

			ViewBag.CurrentPage = page;
			ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
			ViewBag.TotalCount = totalCount;

			// المستحق لكل مورد = فواتير مستلمة - المدفوع
			var ids = suppliers.Select(s => s.Id).ToList();
			ViewBag.Balances = await _purchase.Entity
				.GetWhere(p => ids.Contains(p.SupplierId) && p.Status == PurchaseStatuses.Received)
				.GroupBy(p => p.SupplierId)
				.Select(g => new { g.Key, Total = g.Sum(p => p.Total), Paid = g.Sum(p => p.PaidAmount) })
				.ToDictionaryAsync(x => x.Key, x => (x.Total, x.Paid));

			var received = _purchase.Entity.GetWhere(p => p.Status == PurchaseStatuses.Received);
			ViewBag.SupplierCount = await _supplier.Entity.GetAll().CountAsync(s => s.IsActive);
			ViewBag.PurchasesTotal = await received.SumAsync(p => (decimal?)p.Total) ?? 0;
			ViewBag.DueTotal = await received.SumAsync(p => (decimal?)(p.Total - p.PaidAmount)) ?? 0;

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

			return View(supplier);
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
