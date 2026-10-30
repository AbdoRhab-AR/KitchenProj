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

	// فواتير شراء الخامات: مسودة ← استلام بالمخزن (تُنشئ حركات وارد) ← سداد المورد
	public class PurchasesController : Controller
	{
		private readonly IUnitOfWork<Purchase> _purchase;
		private readonly IUnitOfWork<PurchaseItem> _line;
		private readonly IUnitOfWork<Item> _item;
		private readonly IUnitOfWork<StockTransaction> _tx;
		private readonly IUnitOfWork<SupplierPayment> _payment;
		private readonly IUnitOfWork<Project> _project;
		private readonly LookupService _lookup;
		private readonly StockService _stock;

		public PurchasesController(IUnitOfWork<Purchase> purchase,
								   IUnitOfWork<PurchaseItem> line,
								   IUnitOfWork<Item> item,
								   IUnitOfWork<StockTransaction> tx,
								   IUnitOfWork<SupplierPayment> payment,
								   IUnitOfWork<Project> project,
								   LookupService lookup,
								   StockService stock)
		{
			_purchase = purchase;
			_line = line;
			_item = item;
			_tx = tx;
			_payment = payment;
			_project = project;
			_lookup = lookup;
			_stock = stock;
		}

		// GET: Purchases
		public async Task<IActionResult> Index(string? searchString, string? status, Guid? supplierId, Guid? projectId, int page = 1)
		{
			ViewBag.ActivePage = "Purchases";
			ViewBag.SearchString = searchString;
			ViewBag.Status = status;
			ViewBag.SupplierId = supplierId;
			ViewBag.ProjectId = projectId;

			var query = _purchase.Entity.Include(p => p.Suppliers, p => p.Projects!);

			if (!string.IsNullOrWhiteSpace(searchString))
			{
				searchString = searchString.Trim();
				query = query.Where(p => p.PurchaseNo.Contains(searchString)
									  || (p.InvoiceNo != null && p.InvoiceNo.Contains(searchString))
									  || p.Suppliers.Name.Contains(searchString));
			}

			if (!string.IsNullOrEmpty(status))
			{
				query = query.Where(p => p.Status == status);
			}

			if (supplierId.HasValue)
			{
				query = query.Where(p => p.SupplierId == supplierId.Value);
			}

			if (projectId.HasValue)
			{
				query = query.Where(p => p.ProjectId == projectId.Value);
				ViewBag.ProjectName = await _project.Entity.GetWhere(p => p.Id == projectId.Value)
					.Select(p => p.ProjectNo + " - " + p.ProjectName).FirstOrDefaultAsync();
			}

			var totalCount = await query.CountAsync();
			int pageSize = 10;
			var purchases = await query.OrderByDescending(p => p.PurchaseDate).ThenByDescending(p => p.Created)
									   .Skip((page - 1) * pageSize)
									   .Take(pageSize)
									   .ToListAsync();

			ViewBag.CurrentPage = page;
			ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
			ViewBag.TotalCount = totalCount;

			// Metrics for Dashboard header cards
			var monthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
			var received = _purchase.Entity.GetWhere(p => p.Status == PurchaseStatuses.Received);
			ViewBag.DraftCount = await _purchase.Entity.GetAll().CountAsync(p => p.Status == PurchaseStatuses.Draft);
			ViewBag.MonthTotal = await received.Where(p => p.PurchaseDate >= monthStart).SumAsync(p => (decimal?)p.Total) ?? 0;
			ViewBag.DueTotal = await received.SumAsync(p => (decimal?)(p.Total - p.PaidAmount)) ?? 0;

			ViewBag.Suppliers = await _lookup.SuppliersAsync(supplierId);

			return View(purchases);
		}

		// GET: Purchases/Details/5
		public async Task<IActionResult> Details(Guid? id)
		{
			ViewBag.ActivePage = "Purchases";

			var purchase = await LoadFullAsync(id);
			if (purchase == null)
			{
				return NotFound();
			}

			ViewBag.PaymentMethods = LookupService.EnumList(PaymentMethod.Cash);
			return View(purchase);
		}

		// GET: Purchases/Create?supplierId=5&projectId=5
		public async Task<IActionResult> Create(Guid? supplierId, Guid? projectId)
		{
			ViewBag.ActivePage = "Purchases";

			var model = new Purchase
			{
				PurchaseNo = await GenerateNoAsync(),
				SupplierId = supplierId ?? Guid.Empty,
				ProjectId = projectId,
				DirectToProject = projectId.HasValue,
				WarehouseId = await _lookup.DefaultWarehouseIdAsync(),
				Items = { new PurchaseItem() }
			};

			await FillSelectListsAsync(model);
			return View(model);
		}

		// POST: Purchases/Create
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(Purchase purchase, bool receive = false)
		{
			ViewBag.ActivePage = "Purchases";
			await ValidatePurchaseAsync(purchase);

			if (await _purchase.Entity.GetAll().AnyAsync(p => p.PurchaseNo == purchase.PurchaseNo))
			{
				purchase.PurchaseNo = await GenerateNoAsync();
			}

			if (ModelState.IsValid)
			{
				purchase.Id = Guid.NewGuid();
				purchase.Created = DateTime.UtcNow;
				purchase.Status = PurchaseStatuses.Draft;
				purchase.PaidAmount = 0;
				foreach (var line in purchase.Items)
				{
					line.Id = Guid.NewGuid();
					line.PurchaseId = purchase.Id;
					line.Created = purchase.Created;
				}
				purchase.Recalculate();
				_purchase.Entity.Insert(purchase);

				if (receive)
				{
					await ReceiveIntoStockAsync(purchase, purchase.Items);
				}

				await _purchase.SaveAsync();

				TempData["Success"] = receive
					? $"تم حفظ فاتورة الشراء {purchase.PurchaseNo} واستلام الأصناف بالمخزن"
					: $"تم حفظ فاتورة الشراء {purchase.PurchaseNo} كمسودة";
				return RedirectToAction(nameof(Details), new { id = purchase.Id });
			}

			await FillSelectListsAsync(purchase);
			return View(purchase);
		}

		// GET: Purchases/Edit/5
		public async Task<IActionResult> Edit(Guid? id)
		{
			ViewBag.ActivePage = "Purchases";

			var purchase = await LoadFullAsync(id);
			if (purchase == null)
			{
				return NotFound();
			}

			if (purchase.Status != PurchaseStatuses.Draft)
			{
				TempData["Error"] = "لا يمكن تعديل فاتورة مستلمة أو ملغاة، ألغِ الاستلام أولاً";
				return RedirectToAction(nameof(Details), new { id });
			}

			if (purchase.Items.Count == 0)
			{
				purchase.Items.Add(new PurchaseItem());
			}

			await FillSelectListsAsync(purchase);
			return View(purchase);
		}

		// POST: Purchases/Edit/5
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(Guid id, Purchase purchase, bool receive = false)
		{
			ViewBag.ActivePage = "Purchases";

			if (id != purchase.Id)
			{
				return NotFound();
			}

			var original = await _purchase.Entity.GetWhere(p => p.Id == id)
				.Select(p => new { p.Status, p.Created, p.PaidAmount }).FirstOrDefaultAsync();
			if (original == null)
			{
				return NotFound();
			}
			if (original.Status != PurchaseStatuses.Draft)
			{
				TempData["Error"] = "لا يمكن تعديل فاتورة مستلمة أو ملغاة";
				return RedirectToAction(nameof(Details), new { id });
			}

			await ValidatePurchaseAsync(purchase);

			if (ModelState.IsValid)
			{
				purchase.Created = original.Created;
				purchase.Modified = DateTime.UtcNow;
				purchase.Status = PurchaseStatuses.Draft;
				purchase.PaidAmount = original.PaidAmount;
				purchase.Recalculate();

				// السطور تُستبدل بالكامل: نحذف القديمة ونضيف المرسلة من النموذج
				var newLines = purchase.Items;
				purchase.Items = new List<PurchaseItem>();

				var oldLines = await _line.Entity.GetWhere(l => l.PurchaseId == id).ToListAsync();
				foreach (var old in oldLines)
				{
					_line.Entity.Delete(old);
				}

				_purchase.Entity.Update(purchase);

				foreach (var line in newLines)
				{
					line.Id = Guid.NewGuid();
					line.PurchaseId = purchase.Id;
					line.Created = DateTime.UtcNow;
					_line.Entity.Insert(line);
				}

				if (receive)
				{
					await ReceiveIntoStockAsync(purchase, newLines);
				}

				await _purchase.SaveAsync();

				TempData["Success"] = receive
					? $"تم تحديث الفاتورة {purchase.PurchaseNo} واستلام الأصناف بالمخزن"
					: $"تم تحديث الفاتورة {purchase.PurchaseNo}";
				return RedirectToAction(nameof(Details), new { id = purchase.Id });
			}

			await FillSelectListsAsync(purchase);
			return View(purchase);
		}

		// POST: Purchases/Receive/5  استلام مسودة بالمخزن
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Receive(Guid id)
		{
			var purchase = await _purchase.Entity.GetByIdAsync(id);
			if (purchase == null)
			{
				return NotFound();
			}

			if (purchase.Status != PurchaseStatuses.Draft)
			{
				TempData["Error"] = "هذه الفاتورة ليست مسودة";
				return RedirectToAction(nameof(Details), new { id });
			}

			var lines = await _line.Entity.GetWhere(l => l.PurchaseId == id).ToListAsync();
			if (lines.Count == 0)
			{
				TempData["Error"] = "لا توجد أصناف في الفاتورة";
				return RedirectToAction(nameof(Details), new { id });
			}

			await ReceiveIntoStockAsync(purchase, lines);
			await _purchase.SaveAsync();

			TempData["Success"] = $"تم استلام أصناف الفاتورة {purchase.PurchaseNo} بالمخزن";
			return RedirectToAction(nameof(Details), new { id });
		}

		// POST: Purchases/Cancel/5  إلغاء الفاتورة وعكس حركاتها المخزنية
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Cancel(Guid id)
		{
			var purchase = await _purchase.Entity.GetByIdAsync(id);
			if (purchase == null)
			{
				return NotFound();
			}

			if (purchase.Status == PurchaseStatuses.Cancelled)
			{
				return RedirectToAction(nameof(Details), new { id });
			}

			if (await _payment.Entity.GetAll().AnyAsync(p => p.PurchaseId == id))
			{
				TempData["Error"] = "لا يمكن إلغاء فاتورة عليها مدفوعات للمورد، احذف المدفوعات أولاً";
				return RedirectToAction(nameof(Details), new { id });
			}

			var movements = await _tx.Entity.GetWhere(t => t.PurchaseId == id).ToListAsync();
			if (movements.Count > 0)
			{
				// لا نسمح بالإلغاء إذا كانت الكمية قد صُرفت من المخزن (يصبح الرصيد سالباً)
				var shortages = new List<string>();
				foreach (var g in movements.GroupBy(m => new { m.ItemId, m.WarehouseId }))
				{
					var net = g.Sum(m => m.QuantityIn - m.QuantityOut);
					var balance = await _stock.BalanceAsync(g.Key.ItemId, g.Key.WarehouseId);
					if (balance - net < 0)
					{
						var name = await _item.Entity.GetWhere(i => i.Id == g.Key.ItemId).Select(i => i.Name).FirstOrDefaultAsync();
						shortages.Add($"{name} (المتوفر {balance:0.##})");
					}
				}

				if (shortages.Count > 0)
				{
					TempData["Error"] = "لا يمكن إلغاء الفاتورة لأن جزءاً من الكمية صُرف بالفعل: " + string.Join("، ", shortages);
					return RedirectToAction(nameof(Details), new { id });
				}

				foreach (var m in movements)
				{
					_tx.Entity.Delete(m);
				}
			}

			purchase.Status = PurchaseStatuses.Cancelled;
			purchase.Modified = DateTime.UtcNow;
			await _purchase.SaveAsync();

			TempData["Success"] = $"تم إلغاء الفاتورة {purchase.PurchaseNo}" + (movements.Count > 0 ? " وعكس حركاتها المخزنية" : "");
			return RedirectToAction(nameof(Details), new { id });
		}

		// POST: Purchases/Delete/5
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Delete(Guid id)
		{
			var purchase = await _purchase.Entity.GetByIdAsync(id);
			if (purchase == null)
			{
				TempData["Error"] = "تعذر العثور على الفاتورة المراد حذفها";
				return RedirectToAction(nameof(Index));
			}

			if (purchase.Status == PurchaseStatuses.Received)
			{
				TempData["Error"] = "لا يمكن حذف فاتورة مستلمة، ألغها أولاً";
				return RedirectToAction(nameof(Details), new { id });
			}

			if (await _payment.Entity.GetAll().AnyAsync(p => p.PurchaseId == id))
			{
				TempData["Error"] = "لا يمكن حذف فاتورة عليها مدفوعات";
				return RedirectToAction(nameof(Details), new { id });
			}

			_purchase.Entity.Delete(purchase);
			await _purchase.SaveAsync();
			TempData["Success"] = $"تم حذف الفاتورة {purchase.PurchaseNo}";
			return RedirectToAction(nameof(Index));
		}

		// POST: Purchases/AddPayment/5  سداد للمورد على هذه الفاتورة
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> AddPayment(Guid id, decimal amount, PaymentMethod paymentMethod, DateTime? paymentDate, string? notes)
		{
			var purchase = await _purchase.Entity.GetByIdAsync(id);
			if (purchase == null)
			{
				return NotFound();
			}

			if (purchase.Status != PurchaseStatuses.Received)
			{
				TempData["Error"] = "السداد يكون على الفواتير المستلمة فقط";
				return RedirectToAction(nameof(Details), new { id });
			}

			if (amount <= 0 || amount > purchase.RemainingAmount)
			{
				TempData["Error"] = $"المبلغ يجب أن يكون أكبر من صفر ولا يتجاوز المتبقي ({purchase.RemainingAmount:N2})";
				return RedirectToAction(nameof(Details), new { id });
			}

			_payment.Entity.Insert(new SupplierPayment
			{
				Id = Guid.NewGuid(),
				Created = DateTime.UtcNow,
				SupplierId = purchase.SupplierId,
				PurchaseId = purchase.Id,
				Amount = amount,
				PaymentMethod = paymentMethod,
				PaymentDate = paymentDate ?? DateTime.Today,
				Notes = notes
			});

			purchase.PaidAmount += amount;
			purchase.Modified = DateTime.UtcNow;
			await _purchase.SaveAsync();

			TempData["Success"] = $"تم تسجيل سداد {amount:N2} للمورد على الفاتورة {purchase.PurchaseNo}";
			return RedirectToAction(nameof(Details), new { id });
		}

		// POST: Purchases/DeletePayment/5
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeletePayment(Guid paymentId)
		{
			var payment = await _payment.Entity.GetByIdAsync(paymentId);
			if (payment == null)
			{
				return NotFound();
			}

			if (payment.PurchaseId.HasValue)
			{
				var purchase = await _purchase.Entity.GetByIdAsync(payment.PurchaseId.Value);
				if (purchase != null)
				{
					purchase.PaidAmount = Math.Max(0, purchase.PaidAmount - payment.Amount);
					purchase.Modified = DateTime.UtcNow;
				}
			}

			_payment.Entity.Delete(payment);
			await _payment.SaveAsync();

			TempData["Success"] = $"تم حذف السداد بقيمة {payment.Amount:N2}";
			return payment.PurchaseId.HasValue
				? RedirectToAction(nameof(Details), new { id = payment.PurchaseId })
				: RedirectToAction("Details", "Suppliers", new { id = payment.SupplierId });
		}

		//---------------------------------------------------------------------

		private Task<Purchase?> LoadFullAsync(Guid? id)
		{
			if (id == null)
			{
				return Task.FromResult<Purchase?>(null);
			}

			return _purchase.Entity.Include(p => p.Suppliers, p => p.Warehouses, p => p.Projects!, p => p.Payments)
				.Include(p => p.Items).ThenInclude(l => l.Items)
				.AsSplitQuery()
				.FirstOrDefaultAsync(p => p.Id == id)!;
		}

		private Task<string> GenerateNoAsync() =>
			DocumentNumbers.NextAsync(_purchase.Entity.GetAll().Select(p => p.PurchaseNo), "PUR-");

		// ينشئ حركات الوارد (وصرفاً مباشراً للمشروع عند الحاجة) ويحدّث آخر سعر شراء للأصناف
		private async Task ReceiveIntoStockAsync(Purchase purchase, IEnumerable<PurchaseItem> lines)
		{
			var now = DateTime.UtcNow;
			var directProject = purchase.DirectToProject ? purchase.ProjectId : null;

			// الخصم يُوزع على تكلفة الأصناف بنسبة قيمتها، والضريبة تُضاف لها
			var factor = purchase.Subtotal > 0 ? (purchase.Subtotal - purchase.Discount + purchase.Tax) / purchase.Subtotal : 1;

			foreach (var line in lines)
			{
				var unitCost = Math.Round(line.UnitPrice * factor, 2);

				_tx.Entity.Insert(new StockTransaction
				{
					Id = Guid.NewGuid(),
					Created = now,
					ItemId = line.ItemId,
					WarehouseId = purchase.WarehouseId,
					PurchaseId = purchase.Id,
					TransactionType = StockTransactionType.Purchase,
					TransactionDate = purchase.PurchaseDate,
					QuantityIn = line.Quantity,
					UnitCost = unitCost,
					ReferenceNo = purchase.PurchaseNo,
					Notes = purchase.InvoiceNo != null ? "فاتورة المورد " + purchase.InvoiceNo : null
				});

				if (directProject.HasValue)
				{
					_tx.Entity.Insert(new StockTransaction
					{
						Id = Guid.NewGuid(),
						Created = now.AddMilliseconds(1),
						ItemId = line.ItemId,
						WarehouseId = purchase.WarehouseId,
						PurchaseId = purchase.Id,
						ProjectId = directProject,
						TransactionType = StockTransactionType.Issue,
						TransactionDate = purchase.PurchaseDate,
						QuantityOut = line.Quantity,
						UnitCost = unitCost,
						ReferenceNo = purchase.PurchaseNo,
						Notes = "صرف مباشر من فاتورة الشراء"
					});
				}

				var item = await _item.Entity.GetByIdAsync(line.ItemId);
				if (item != null)
				{
					item.PurchasePrice = line.UnitPrice;
					item.Modified = now;
				}
			}

			purchase.Status = PurchaseStatuses.Received;
			purchase.Modified = now;
		}

		private async Task ValidatePurchaseAsync(Purchase purchase)
		{
			if (purchase.SupplierId == Guid.Empty)
				ModelState.AddModelError(nameof(Purchase.SupplierId), "يجب اختيار المورد");

			if (purchase.WarehouseId == Guid.Empty)
				ModelState.AddModelError(nameof(Purchase.WarehouseId), "يجب اختيار المخزن");

			if (!purchase.ProjectId.HasValue)
				purchase.DirectToProject = false;

			// الصفوف الفارغة في جدول الأصناف تُتجاهل
			purchase.Items = purchase.Items.Where(i => i.ItemId != Guid.Empty).ToList();
			foreach (var key in ModelState.Keys.Where(k => k.StartsWith("Items[")).ToList())
			{
				ModelState.Remove(key);
			}

			if (purchase.Items.Count == 0)
				ModelState.AddModelError("", "يجب إضافة صنف واحد على الأقل للفاتورة");

			if (purchase.Items.Any(i => i.Quantity <= 0 || i.UnitPrice < 0))
				ModelState.AddModelError("", "تأكد من أن الكميات أكبر من صفر والأسعار غير سالبة");

			var ids = purchase.Items.Select(i => i.ItemId).Distinct().ToList();
			var rawCount = await _item.Entity.GetAll().CountAsync(i => ids.Contains(i.Id) && i.Type == ItemType.Raw);
			if (rawCount != ids.Count)
				ModelState.AddModelError("", "المشتريات تكون للأصناف الخام فقط");

			purchase.Subtotal = purchase.Items.Sum(i => i.Total);
			if (purchase.Discount > purchase.Subtotal)
				ModelState.AddModelError(nameof(Purchase.Discount), "الخصم أكبر من قيمة الفاتورة");
		}

		private async Task FillSelectListsAsync(Purchase purchase)
		{
			ViewBag.Suppliers = await _lookup.SuppliersAsync(purchase.SupplierId);
			ViewBag.Warehouses = await _lookup.WarehousesAsync(purchase.WarehouseId);
			ViewBag.Projects = await _lookup.ProjectsAsync(purchase.ProjectId);
			ViewBag.ItemOptions = await _lookup.ItemOptionsAsync(ItemType.Raw);
		}
	}
}
