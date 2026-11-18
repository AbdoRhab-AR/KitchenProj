using KitchenProject.Classes;
using KitchenProject.Models.Entities;
using KitchenProject.Models.Enums;
using KitchenProject.Models.Interfaces;
using KitchenProject.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KitchenProject.Controllers
{
	[ViewLayout("_DashboardLayout")]

	// الصرف المباشر للموردين (إيصالات الصرف) - يمكن ربطه بفاتورة شراء مستلمة أو تسجيله كدفعة مباشرة للمورد
	public class SupplierPaymentsController : Controller
	{
		private readonly IUnitOfWork<SupplierPayment> _payment;
		private readonly IUnitOfWork<Purchase> _purchase;
		private readonly IUnitOfWork<Supplier> _supplier;
		private readonly LookupService _lookup;
		private readonly KitchenProject.Models.ApplicationDbContext _db;

		public SupplierPaymentsController(IUnitOfWork<SupplierPayment> payment,
										  IUnitOfWork<Purchase> purchase,
										  IUnitOfWork<Supplier> supplier,
										  LookupService lookup,
										  KitchenProject.Models.ApplicationDbContext db)
		{
			_payment = payment;
			_purchase = purchase;
			_supplier = supplier;
			_lookup = lookup;
			_db = db;
		}

		// GET: SupplierPayments
		public async Task<IActionResult> Index(string? searchString, PaymentMethod? method, Guid? supplierId, DateTime? from, DateTime? to, int page = 1)
		{
			ViewBag.ActivePage = "SupplierPayments";
			ViewBag.SearchString = searchString;
			ViewBag.Method = method;
			ViewBag.SupplierId = supplierId;
			ViewBag.From = from;
			ViewBag.To = to;

			var query = _payment.Entity.Include(p => p.supplier, p => p.Purchase!);

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
					query = query.Where(p => p.supplier.Name.Contains(validName)
										  || p.ReceiptNo.Contains(validName)
										  || (p.ReferenceNo != null && p.ReferenceNo.Contains(validName)));
				}
			}

			if (method.HasValue) query = query.Where(p => p.PaymentMethod == method.Value);
			if (from.HasValue) query = query.Where(p => p.PaymentDate >= from.Value.Date);
			if (to.HasValue) query = query.Where(p => p.PaymentDate <= to.Value.Date);

			if (supplierId.HasValue)
			{
				query = query.Where(p => p.SupplierId == supplierId.Value);
				ViewBag.SupplierName = await _supplier.Entity.GetWhere(s => s.Id == supplierId.Value)
					.Select(s => s.SupplierNo + " - " + s.Name).FirstOrDefaultAsync();
			}

			var totalCount = await query.CountAsync();
			ViewBag.FilteredTotal = await query.SumAsync(p => (decimal?)p.Amount) ?? 0;

			int pageSize = await _lookup.PageSizeAsync();
			var payments = await query.OrderByDescending(p => p.PaymentDate).ThenByDescending(p => p.Created)
									  .Skip((page - 1) * pageSize)
									  .Take(pageSize)
									  .ToListAsync();

			ViewBag.CurrentPage = page;
			ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
			ViewBag.TotalCount = totalCount;

			// بطاقات الملخص أعلى الشاشة
			var monthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
			ViewBag.MonthTotal = await _payment.Entity
				.GetWhere(p => p.PaymentDate >= monthStart && !(p.PaymentMethod == PaymentMethod.Chek && p.ChequeStatus == ChequeStatus.Bounced))
				.SumAsync(p => (decimal?)p.Amount) ?? 0;
			ViewBag.TodayTotal = await _payment.Entity
				.GetWhere(p => p.PaymentDate == DateTime.Today && !(p.PaymentMethod == PaymentMethod.Chek && p.ChequeStatus == ChequeStatus.Bounced))
				.SumAsync(p => (decimal?)p.Amount) ?? 0;

			var receivedTotal = await _purchase.Entity.GetWhere(p => p.Status == PurchaseStatuses.Received).SumAsync(p => (decimal?)p.Total) ?? 0;
			var allReturns = await _db.PurchaseReturns.AsNoTracking().SumAsync(r => (decimal?)r.Total) ?? 0;
			var allPaid = await _payment.Entity
				.GetWhere(p => !(p.PaymentMethod == PaymentMethod.Chek && p.ChequeStatus == ChequeStatus.Bounced))
				.SumAsync(p => (decimal?)p.Amount) ?? 0;
			ViewBag.Outstanding = (receivedTotal - allReturns) - allPaid;

			ViewBag.MethodList = LookupService.EnumList(method);
			return View(payments);
		}

		// GET: SupplierPayments/Receipt/5  طباعة إيصال / سند صرف للمورد
		public async Task<IActionResult> Receipt(Guid? id)
		{
			var payment = await _payment.Entity.Include(p => p.supplier, p => p.Purchase!, p => p.Purchase!.Projects!)
				.FirstOrDefaultAsync(p => p.Id == id);
			if (payment == null)
			{
				return NotFound();
			}

			if (!payment.IsPrinted)
			{
				payment.IsPrinted = true;
				await _payment.SaveAsync();
			}

			if (payment.Purchase == null)
			{
				var totalPurchases = await _purchase.Entity
					.GetWhere(p => p.SupplierId == payment.SupplierId && p.Status == PurchaseStatuses.Received)
					.SumAsync(p => (decimal?)p.Total) ?? 0;
				var totalReturns = await _db.PurchaseReturns.AsNoTracking()
					.Where(r => r.SupplierId == payment.SupplierId)
					.SumAsync(r => (decimal?)r.Total) ?? 0;
				var netPurchases = totalPurchases - totalReturns;
				var totalPaid = await _payment.Entity
					.GetWhere(p => p.SupplierId == payment.SupplierId && !(p.PaymentMethod == PaymentMethod.Chek && p.ChequeStatus == ChequeStatus.Bounced))
					.SumAsync(p => (decimal?)p.Amount) ?? 0;
				ViewBag.SupplierTotalPurchases = netPurchases;
				ViewBag.SupplierTotalPaid = totalPaid;
				ViewBag.SupplierRemaining = netPurchases - totalPaid;
			}

			return View(payment);
		}

		// GET: SupplierPayments/Create?supplierId=5&purchaseId=5
		public async Task<IActionResult> Create(Guid? supplierId, Guid? purchaseId, string? returnTo)
		{
			ViewBag.ActivePage = "SupplierPayments";
			ViewBag.ReturnTo = returnTo;

			if (purchaseId.HasValue)
			{
				var purchase = await _purchase.Entity.GetWhere(p => p.Id == purchaseId.Value)
					.Select(p => new { p.SupplierId, Remaining = p.Total - p.PaidAmount })
					.FirstOrDefaultAsync();
				if (purchase != null)
				{
					supplierId = purchase.SupplierId;
					var returned = await _db.PurchaseReturns.AsNoTracking()
						.Where(r => r.PurchaseId == purchaseId.Value)
						.SumAsync(r => (decimal?)r.Total) ?? 0;
					ViewBag.SuggestedAmount = Math.Max(0, purchase.Remaining - returned);
				}
			}
			else if (supplierId.HasValue)
			{
				// إذا كانت هناك فاتورة مستلمة واحدة غير مسددة للمورد نقترحها تلقائياً (بعد خصم المرتجعات)
				var candidates = await _purchase.Entity
					.GetWhere(p => p.SupplierId == supplierId.Value && p.Status == PurchaseStatuses.Received && p.Total > p.PaidAmount)
					.Select(p => new { p.Id, Remaining = p.Total - p.PaidAmount })
					.ToListAsync();
				var candIds = candidates.Select(c => c.Id).ToList();
				var returnsMap = await _db.PurchaseReturns.AsNoTracking()
					.Where(r => candIds.Contains(r.PurchaseId))
					.GroupBy(r => r.PurchaseId)
					.Select(g => new { g.Key, Total = g.Sum(r => r.Total) })
					.ToDictionaryAsync(x => x.Key, x => x.Total);
				var openPurchases = candidates
					.Select(c => new { c.Id, NetRemaining = c.Remaining - returnsMap.GetValueOrDefault(c.Id) })
					.Where(c => c.NetRemaining > 0)
					.Take(2)
					.ToList();
				if (openPurchases.Count == 1)
				{
					purchaseId = openPurchases[0].Id;
					ViewBag.SuggestedAmount = openPurchases[0].NetRemaining;
				}
			}

			var model = new SupplierPayment
			{
				ReceiptNo = await GenerateNoAsync(),
				SupplierId = supplierId ?? Guid.Empty,
				PurchaseId = purchaseId
			};

			await FillSelectListsAsync(model);
			return View(model);
		}

		// POST: SupplierPayments/Create
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(SupplierPayment model, string? returnTo)
		{
			ViewBag.ActivePage = "SupplierPayments";
			ViewBag.ReturnTo = returnTo;

			if (model.SupplierId == Guid.Empty)
				ModelState.AddModelError(nameof(SupplierPayment.SupplierId), "يجب اختيار المورد");

			if (model.PurchaseId.HasValue)
			{
				var purchase = await _purchase.Entity.GetWhere(p => p.Id == model.PurchaseId.Value).FirstOrDefaultAsync();
				if (purchase == null || purchase.SupplierId != model.SupplierId)
				{
					ModelState.AddModelError(nameof(SupplierPayment.PurchaseId), "فاتورة الشراء المختارة لا تتبع هذا المورد");
				}
				else if (purchase.Status != PurchaseStatuses.Received)
				{
					ModelState.AddModelError(nameof(SupplierPayment.PurchaseId), "السداد على فاتورة يكون للفواتير المستلمة فقط");
				}
				else
				{
					var returned = await _db.PurchaseReturns.AsNoTracking()
						.Where(r => r.PurchaseId == purchase.Id)
						.SumAsync(r => (decimal?)r.Total) ?? 0;
					var netRemaining = purchase.RemainingAmount - returned;
					if (model.Amount > netRemaining)
					{
						ModelState.AddModelError(nameof(SupplierPayment.Amount), $"المبلغ أكبر من المتبقي على الفاتورة ({Math.Max(0, netRemaining):N2})");
					}
				}
			}

			if (string.IsNullOrWhiteSpace(model.ReceiptNo) || await _payment.Entity.GetAll().AnyAsync(p => p.ReceiptNo == model.ReceiptNo))
			{
				model.ReceiptNo = await GenerateNoAsync();
			}

			if (model.PaymentMethod == PaymentMethod.Chek)
			{
				model.ChequeStatus ??= ChequeStatus.Pending;
			}
			else
			{
				model.ChequeStatus = null;
				model.ChequeDueDate = null;
			}

			if (ModelState.IsValid)
			{
				model.Id = Guid.NewGuid();
				model.Created = DateTime.UtcNow;
				model.CreatedBy = User.Identity?.Name;
				_payment.Entity.Insert(model);
				await _payment.SaveAsync();

				if (model.PurchaseId.HasValue)
				{
					await SyncPurchasePaidAsync(model.PurchaseId.Value);
				}

				TempData["Success"] = $"تم تسجيل إيصال الصرف {model.ReceiptNo} بمبلغ {model.Amount:N2}";
				TempData["PrintSupplierReceiptId"] = model.Id.ToString();

				if (returnTo == "supplier")
				{
					return RedirectToAction("Details", "Suppliers", new { id = model.SupplierId });
				}
				return RedirectToAction(nameof(Index));
			}

			await FillSelectListsAsync(model);
			return View(model);
		}

		// POST: SupplierPayments/Delete/5
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Delete(Guid id, string? returnUrl)
		{
			var payment = await _payment.Entity.GetByIdAsync(id);
			if (payment == null)
			{
				TempData["Error"] = "تعذر العثور على إيصال الصرف المراد حذفه";
				return RedirectToAction(nameof(Index));
			}

			if (!AppRoles.IsFullAccess(User))
			{
				if (payment.IsPrinted)
				{
					TempData["Error"] = "لا يمكن حذف إيصال صرف بعد طباعته، تواصل مع المدير";
					return RedirectToAction(nameof(Index));
				}
				if (!string.IsNullOrEmpty(payment.CreatedBy) && !string.Equals(payment.CreatedBy, User.Identity?.Name, StringComparison.OrdinalIgnoreCase))
				{
					TempData["Error"] = "لا يمكنك حذف إيصال أنشأه مستخدم آخر";
					return RedirectToAction(nameof(Index));
				}
			}

			var purchaseId = payment.PurchaseId;
			_payment.Entity.Delete(payment);
			await _payment.SaveAsync();

			if (purchaseId.HasValue)
			{
				await SyncPurchasePaidAsync(purchaseId.Value);
			}

			TempData["Success"] = $"تم حذف إيصال الصرف {(string.IsNullOrEmpty(payment.ReceiptNo) ? "" : payment.ReceiptNo + " ")}بقيمة {payment.Amount:N2}";
			if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
			{
				return Redirect(returnUrl);
			}
			return RedirectToAction(nameof(Index));
		}

		//---------------------------------------------------------------------

		// المدفوع في الفاتورة = مجموع إيصالاتها (يُعاد حسابه لضمان الدقة)
		private async Task SyncPurchasePaidAsync(Guid purchaseId)
		{
			var purchase = await _purchase.Entity.GetByIdAsync(purchaseId);
			if (purchase == null)
			{
				return;
			}

			purchase.PaidAmount = await _payment.Entity
				.GetWhere(p => p.PurchaseId == purchaseId && !(p.PaymentMethod == PaymentMethod.Chek && p.ChequeStatus == ChequeStatus.Bounced))
				.SumAsync(p => (decimal?)p.Amount) ?? 0;
			purchase.Modified = DateTime.UtcNow;
			await _purchase.SaveAsync();
		}

		private Task<string> GenerateNoAsync() =>
			DocumentNumbers.NextAsync(_payment.Entity.GetAll().Select(p => p.ReceiptNo), "SP-");

		private async Task FillSelectListsAsync(SupplierPayment payment)
		{
			ViewBag.Suppliers = await _lookup.SuppliersAsync(payment.SupplierId == Guid.Empty ? null : payment.SupplierId);
			ViewBag.MethodList = LookupService.EnumList(payment.PaymentMethod);
			ViewBag.CashAccounts = await _lookup.CashAndBankAccountsAsync(payment.CashAccountId);
			ViewBag.ChequeStatuses = LookupService.EnumList(payment.ChequeStatus ?? ChequeStatus.Pending);

			// الفواتير المستلمة غير المسددة بالكامل بعد خصم المرتجعات (أو الفاتورة المختارة حالياً)
			var rawPurchases = await _purchase.Entity
				.GetWhere(p => (p.Status == PurchaseStatuses.Received && p.Total > p.PaidAmount) || p.Id == payment.PurchaseId)
				.OrderByDescending(p => p.PurchaseDate)
				.Select(p => new { p.Id, p.SupplierId, p.PurchaseNo, p.InvoiceNo, p.Total, p.PaidAmount })
				.ToListAsync();

			var returnsByPurchase = await _db.PurchaseReturns.AsNoTracking()
				.GroupBy(r => r.PurchaseId)
				.Select(g => new { g.Key, Total = g.Sum(r => r.Total) })
				.ToDictionaryAsync(x => x.Key, x => x.Total);

			ViewBag.Purchases = rawPurchases
				.Select(p =>
				{
					var netTotal = p.Total - returnsByPurchase.GetValueOrDefault(p.Id);
					return new PurchaseOption
					{
						Id = p.Id,
						SupplierId = p.SupplierId,
						PurchaseNo = p.PurchaseNo,
						InvoiceNo = p.InvoiceNo,
						Total = netTotal,
						Remaining = netTotal - p.PaidAmount
					};
				})
				.Where(p => p.Remaining > 0 || p.Id == payment.PurchaseId)
				.ToList();

			var purchasesBySupplier = await _purchase.Entity
				.GetWhere(p => p.Status == PurchaseStatuses.Received)
				.GroupBy(p => p.SupplierId)
				.Select(g => new { g.Key, Total = g.Sum(p => p.Total) })
				.ToDictionaryAsync(x => x.Key, x => x.Total);

			var returnsBySupplier = await _db.PurchaseReturns.AsNoTracking()
				.GroupBy(r => r.SupplierId)
				.Select(g => new { g.Key, Total = g.Sum(r => r.Total) })
				.ToDictionaryAsync(x => x.Key, x => x.Total);

			var paidBySupplier = await _payment.Entity
				.GetWhere(p => !(p.PaymentMethod == PaymentMethod.Chek && p.ChequeStatus == ChequeStatus.Bounced))
				.GroupBy(p => p.SupplierId)
				.Select(g => new { g.Key, Paid = g.Sum(p => p.Amount) })
				.ToDictionaryAsync(x => x.Key, x => x.Paid);

			ViewBag.SupplierDues = purchasesBySupplier.Keys.Union(returnsBySupplier.Keys).Union(paidBySupplier.Keys)
				.ToDictionary(
					id => id.ToString(),
					id => (purchasesBySupplier.GetValueOrDefault(id) - returnsBySupplier.GetValueOrDefault(id)) - paidBySupplier.GetValueOrDefault(id));
		}
	}
}
