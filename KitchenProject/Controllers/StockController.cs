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
	//[Authorize(Policy = "ProgOrAdminOrEmployeePolicy")]

	// المخزن: أرصدة الخامات، كشف الحركات، صرف المواد للمشاريع وتسوية الجرد
	public class StockController : Controller
	{
		private readonly IUnitOfWork<StockTransaction> _tx;
		private readonly IUnitOfWork<Item> _item;
		private readonly IUnitOfWork<Project> _project;
		private readonly LookupService _lookup;
		private readonly StockService _stock;

		public StockController(IUnitOfWork<StockTransaction> tx,
							   IUnitOfWork<Item> item,
							   IUnitOfWork<Project> project,
							   LookupService lookup,
							   StockService stock)
		{
			_tx = tx;
			_item = item;
			_project = project;
			_lookup = lookup;
			_stock = stock;
		}

		// GET: Stock  أرصدة الخامات
		public async Task<IActionResult> Index(string? searchString, Guid? warehouseId, bool lowStock = false)
		{
			ViewBag.ActivePage = "Stock";
			ViewBag.SearchString = searchString;
			ViewBag.WarehouseId = warehouseId;
			ViewBag.LowStock = lowStock;

			var balances = await _stock.BalancesAsync(warehouseId);
			var costs = await _stock.AverageCostsAsync();

			var query = _item.Entity.GetWhere(i => i.Type == ItemType.Raw);
			if (!string.IsNullOrWhiteSpace(searchString))
			{
				searchString = searchString.Trim();
				query = query.Where(i => i.Name.Contains(searchString) || i.Code.Contains(searchString));
			}

			var rows = (await query.OrderBy(i => i.Name).ToListAsync())
				.Select(i => new StockRow
				{
					Item = i,
					Balance = balances.GetValueOrDefault(i.Id),
					AverageCost = costs.GetValueOrDefault(i.Id, i.PurchasePrice)
				})
				// الأصناف الموقوفة تظهر فقط إذا بقي لها رصيد
				.Where(r => r.Item.IsActive || r.Balance != 0)
				.ToList();

			ViewBag.StockValue = rows.Sum(r => r.Value);
			ViewBag.LowCount = rows.Count(r => r.IsLow);
			ViewBag.ItemCount = rows.Count(r => r.Balance > 0);

			var monthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
			ViewBag.MonthIssued = await _tx.Entity
				.GetWhere(t => t.TransactionType == StockTransactionType.Issue && t.TransactionDate >= monthStart)
				.SumAsync(t => (decimal?)(t.QuantityOut * t.UnitCost)) ?? 0;

			if (lowStock)
			{
				rows = rows.Where(r => r.IsLow).ToList();
			}

			ViewBag.Warehouses = await _lookup.WarehousesAsync(warehouseId);
			return View(rows);
		}

		// GET: Stock/Movements  كشف الحركات
		public async Task<IActionResult> Movements(Guid? itemId, Guid? projectId, StockTransactionType? type, DateTime? from, DateTime? to, int page = 1)
		{
			ViewBag.ActivePage = "Stock";
			ViewBag.ItemId = itemId;
			ViewBag.ProjectId = projectId;
			ViewBag.Type = type;
			ViewBag.From = from;
			ViewBag.To = to;

			var query = _tx.Entity.Include(t => t.Items, t => t.Warehouses, t => t.Projects!);

			if (itemId.HasValue) query = query.Where(t => t.ItemId == itemId.Value);
			if (projectId.HasValue) query = query.Where(t => t.ProjectId == projectId.Value);
			if (type.HasValue) query = query.Where(t => t.TransactionType == type.Value);
			if (from.HasValue) query = query.Where(t => t.TransactionDate >= from.Value.Date);
			if (to.HasValue) query = query.Where(t => t.TransactionDate <= to.Value.Date);

			var totalCount = await query.CountAsync();
			int pageSize = 20;
			var movements = await query.OrderByDescending(t => t.TransactionDate).ThenByDescending(t => t.Created)
									   .Skip((page - 1) * pageSize)
									   .Take(pageSize)
									   .ToListAsync();

			ViewBag.CurrentPage = page;
			ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
			ViewBag.TotalCount = totalCount;

			ViewBag.InTotal = await query.SumAsync(t => (decimal?)(t.QuantityIn * t.UnitCost)) ?? 0;
			ViewBag.OutTotal = await query.SumAsync(t => (decimal?)(t.QuantityOut * t.UnitCost)) ?? 0;

			ViewBag.Items = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
				await _lookup.ItemOptionsAsync(ItemType.Raw), "Id", "Name", itemId);
			ViewBag.Projects = await _lookup.ProjectsAsync(projectId);
			ViewBag.TypeList = LookupService.EnumList(type);

			return View(movements);
		}

		// GET: Stock/Issue?projectId=5&type=Return
		public async Task<IActionResult> Issue(Guid? projectId, Guid? itemId, StockTransactionType type = StockTransactionType.Issue)
		{
			ViewBag.ActivePage = "Stock";

			var model = new StockIssueViewModel
			{
				ProjectId = projectId,
				WarehouseId = await _lookup.DefaultWarehouseIdAsync(),
				Type = type == StockTransactionType.Return ? StockTransactionType.Return : StockTransactionType.Issue,
				Lines = { new StockIssueLine { ItemId = itemId ?? Guid.Empty } }
			};

			await FillIssueListsAsync(model);
			return View(model);
		}

		// POST: Stock/Issue
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Issue(StockIssueViewModel model)
		{
			ViewBag.ActivePage = "Stock";

			if (model.Type != StockTransactionType.Return)
				model.Type = StockTransactionType.Issue;

			model.Lines = model.Lines.Where(l => l.ItemId != Guid.Empty).ToList();
			foreach (var key in ModelState.Keys.Where(k => k.StartsWith("Lines[")).ToList())
			{
				ModelState.Remove(key);
			}

			if (model.Lines.Count == 0)
				ModelState.AddModelError("", "يجب إضافة صنف واحد على الأقل");
			if (model.Lines.Any(l => l.Quantity <= 0))
				ModelState.AddModelError("", "الكميات يجب أن تكون أكبر من صفر");
			if (model.Lines.GroupBy(l => l.ItemId).Any(g => g.Count() > 1))
				ModelState.AddModelError("", "الصنف مكرر في أكثر من سطر، اجمع الكمية في سطر واحد");

			var ids = model.Lines.Select(l => l.ItemId).ToList();
			var items = await _item.Entity.GetWhere(i => ids.Contains(i.Id) && i.Type == ItemType.Raw)
				.ToDictionaryAsync(i => i.Id);
			if (items.Count != ids.Distinct().Count())
				ModelState.AddModelError("", "الصرف يكون للأصناف الخام فقط");

			if (ModelState.IsValid && model.Type == StockTransactionType.Issue)
			{
				// لا يُصرف أكثر من المتوفر في المخزن
				var balances = await _stock.BalancesAsync(model.WarehouseId);
				foreach (var line in model.Lines)
				{
					var available = balances.GetValueOrDefault(line.ItemId);
					if (line.Quantity > available)
						ModelState.AddModelError("", $"الكمية المطلوبة من ({items[line.ItemId].Name}) أكبر من المتوفر ({available:0.##} {items[line.ItemId].Unit})");
				}
			}

			if (ModelState.IsValid && model.Type == StockTransactionType.Return)
			{
				// المرتجع لا يتجاوز ما صُرف للمشروع من الصنف
				var issued = await _tx.Entity.GetWhere(t => t.ProjectId == model.ProjectId && ids.Contains(t.ItemId))
					.GroupBy(t => t.ItemId)
					.Select(g => new { g.Key, Net = g.Sum(t => t.QuantityOut - t.QuantityIn) })
					.ToDictionaryAsync(x => x.Key, x => x.Net);
				foreach (var line in model.Lines)
				{
					var net = issued.GetValueOrDefault(line.ItemId);
					if (line.Quantity > net)
						ModelState.AddModelError("", $"مرتجع ({items[line.ItemId].Name}) أكبر مما صُرف للمشروع ({net:0.##})");
				}
			}

			if (ModelState.IsValid)
			{
				var prefix = model.Type == StockTransactionType.Issue ? "ISS-" : "RET-";
				var refNo = await DocumentNumbers.NextAsync(
					_tx.Entity.GetWhere(t => t.ReferenceNo != null).Select(t => t.ReferenceNo!), prefix);
				var costs = await _stock.AverageCostsAsync();
				var now = DateTime.UtcNow;

				foreach (var line in model.Lines)
				{
					var isIssue = model.Type == StockTransactionType.Issue;
					_tx.Entity.Insert(new StockTransaction
					{
						Id = Guid.NewGuid(),
						Created = now,
						ItemId = line.ItemId,
						WarehouseId = model.WarehouseId,
						ProjectId = model.ProjectId,
						TransactionType = model.Type,
						TransactionDate = model.Date,
						QuantityIn = isIssue ? 0 : line.Quantity,
						QuantityOut = isIssue ? line.Quantity : 0,
						UnitCost = costs.GetValueOrDefault(line.ItemId, items[line.ItemId].PurchasePrice),
						ReferenceNo = refNo,
						Notes = model.Notes
					});
				}

				if (model.Type == StockTransactionType.Issue)
				{
					// بدء صرف المواد يعني أن المشروع دخل مرحلة التنفيذ
					await ProjectWorkflow.AdvanceAsync(_project, model.ProjectId!.Value, ProjectStatus.InProgress);
				}

				await _tx.SaveAsync();

				TempData["Success"] = model.Type == StockTransactionType.Issue
					? $"تم صرف {model.Lines.Count} صنف للمشروع (إذن رقم {refNo})"
					: $"تم تسجيل مرتجع {model.Lines.Count} صنف من المشروع (رقم {refNo})";
				return RedirectToAction("Details", "Projects", new { id = model.ProjectId });
			}

			await FillIssueListsAsync(model);
			return View(model);
		}

		// GET: Stock/Adjust?itemId=5
		public async Task<IActionResult> Adjust(Guid? itemId)
		{
			ViewBag.ActivePage = "Stock";

			var model = new StockAdjustViewModel
			{
				ItemId = itemId ?? Guid.Empty,
				WarehouseId = await _lookup.DefaultWarehouseIdAsync()
			};
			if (itemId.HasValue)
			{
				model.CountedQuantity = await _stock.BalanceAsync(itemId.Value, model.WarehouseId);
			}

			await FillAdjustListsAsync(model);
			return View(model);
		}

		// POST: Stock/Adjust
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Adjust(StockAdjustViewModel model)
		{
			ViewBag.ActivePage = "Stock";

			var item = await _item.Entity.GetWhere(i => i.Id == model.ItemId && i.Type == ItemType.Raw).FirstOrDefaultAsync();
			if (item == null)
				ModelState.AddModelError(nameof(model.ItemId), "يجب اختيار صنف خام");

			if (ModelState.IsValid)
			{
				var balance = await _stock.BalanceAsync(model.ItemId, model.WarehouseId);
				var diff = model.CountedQuantity - balance;

				if (diff == 0)
				{
					TempData["Success"] = $"رصيد {item!.Name} مطابق للجرد ({balance:0.##})، لم تُسجل أي حركة";
					return RedirectToAction(nameof(Index));
				}

				var costs = await _stock.AverageCostsAsync();
				_tx.Entity.Insert(new StockTransaction
				{
					Id = Guid.NewGuid(),
					Created = DateTime.UtcNow,
					ItemId = model.ItemId,
					WarehouseId = model.WarehouseId,
					TransactionType = StockTransactionType.Adjustment,
					TransactionDate = model.Date,
					QuantityIn = diff > 0 ? diff : 0,
					QuantityOut = diff < 0 ? -diff : 0,
					UnitCost = costs.GetValueOrDefault(model.ItemId, item!.PurchasePrice),
					ReferenceNo = await DocumentNumbers.NextAsync(
						_tx.Entity.GetWhere(t => t.ReferenceNo != null).Select(t => t.ReferenceNo!), "ADJ-"),
					Notes = string.IsNullOrWhiteSpace(model.Notes) ? $"تسوية جرد: الرصيد {balance:0.##} والفعلي {model.CountedQuantity:0.##}" : model.Notes
				});
				await _tx.SaveAsync();

				TempData["Success"] = $"تمت تسوية رصيد {item!.Name} إلى {model.CountedQuantity:0.##} ({(diff > 0 ? "زيادة" : "عجز")} {Math.Abs(diff):0.##})";
				return RedirectToAction(nameof(Index));
			}

			await FillAdjustListsAsync(model);
			return View(model);
		}

		// POST: Stock/DeleteMovement/5  (حركات الشراء تُلغى من فاتورة الشراء نفسها)
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteMovement(Guid id, string? returnUrl)
		{
			var movement = await _tx.Entity.GetByIdAsync(id);
			if (movement == null)
			{
				return NotFound();
			}

			if (movement.PurchaseId.HasValue)
			{
				TempData["Error"] = "هذه الحركة تابعة لفاتورة شراء، ألغِ الفاتورة بدلاً من حذف الحركة";
			}
			else if (movement.QuantityIn > 0
					 && await _stock.BalanceAsync(movement.ItemId, movement.WarehouseId) - movement.QuantityIn < 0)
			{
				TempData["Error"] = "لا يمكن حذف هذه الحركة لأن الكمية الواردة بها صُرفت بالفعل";
			}
			else
			{
				_tx.Entity.Delete(movement);
				await _tx.SaveAsync();
				TempData["Success"] = $"تم حذف الحركة {movement.ReferenceNo}";
			}

			if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
			{
				return Redirect(returnUrl);
			}
			return RedirectToAction(nameof(Movements));
		}

		//---------------------------------------------------------------------

		private async Task FillIssueListsAsync(StockIssueViewModel model)
		{
			ViewBag.Projects = await _lookup.ProjectsAsync(model.ProjectId);
			ViewBag.Warehouses = await _lookup.WarehousesAsync(model.WarehouseId);
			ViewBag.ItemOptions = await _lookup.ItemOptionsAsync(ItemType.Raw);
			ViewBag.Balances = await _stock.BalancesAsync(model.WarehouseId);
		}

		private async Task FillAdjustListsAsync(StockAdjustViewModel model)
		{
			ViewBag.Warehouses = await _lookup.WarehousesAsync(model.WarehouseId);
			ViewBag.ItemOptions = await _lookup.ItemOptionsAsync(ItemType.Raw);
			ViewBag.Balances = await _stock.BalancesAsync(model.WarehouseId);
		}
	}
}
