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
		private readonly IUnitOfWork<Quote> _quote;
		private readonly LookupService _lookup;
		private readonly StockService _stock;

		public StockController(IUnitOfWork<StockTransaction> tx,
							   IUnitOfWork<Item> item,
							   IUnitOfWork<Project> project,
							   IUnitOfWork<Quote> quote,
							   LookupService lookup,
							   StockService stock)
		{
			_tx = tx;
			_item = item;
			_project = project;
			_quote = quote;
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
				if (!SearchValidator.TryValidateNameSearch(searchString, out var validName, out var searchError))
				{
					TempData["Error"] = searchError;
					ViewBag.SearchError = searchError;
					query = query.Where(_ => false);
				}
				else
				{
					query = query.Where(i => i.Name.Contains(validName));
				}
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
		public async Task<IActionResult> Movements(Guid? itemId, Guid? projectId, Guid? warehouseId, StockTransactionType? type, DateTime? from, DateTime? to, int page = 1)
		{
			ViewBag.ActivePage = "Stock";
			ViewBag.ItemId = itemId;
			ViewBag.ProjectId = projectId;
			ViewBag.WarehouseId = warehouseId;
			ViewBag.Type = type;
			ViewBag.From = from;
			ViewBag.To = to;

			var query = _tx.Entity.Include(t => t.Items, t => t.Warehouses, t => t.Projects!);

			if (itemId.HasValue) query = query.Where(t => t.ItemId == itemId.Value);
			if (projectId.HasValue) query = query.Where(t => t.ProjectId == projectId.Value);
			if (warehouseId.HasValue) query = query.Where(t => t.WarehouseId == warehouseId.Value);
			if (type.HasValue) query = query.Where(t => t.TransactionType == type.Value);
			if (from.HasValue) query = query.Where(t => t.TransactionDate >= from.Value.Date);
			if (to.HasValue) query = query.Where(t => t.TransactionDate <= to.Value.Date);

			var totalCount = await query.CountAsync();
			int pageSize = await _lookup.PageSizeAsync();
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
			ViewBag.Warehouses = await _lookup.WarehousesAsync(warehouseId);
			ViewBag.Projects = await _lookup.ProjectsAsync(projectId);
			ViewBag.TypeList = LookupService.EnumList(type);

			return View(movements);
		}

		// GET: Stock/Issue?projectId=5&type=Return  أو  ?projectId=5&quoteId=5 / &fromQuote=true
		public async Task<IActionResult> Issue(Guid? projectId, Guid? itemId, Guid? quoteId = null, bool fromQuote = false, StockTransactionType type = StockTransactionType.Issue)
		{
			ViewBag.ActivePage = "Stock";

			var model = new StockIssueViewModel
			{
				ProjectId = projectId,
				WarehouseId = await _lookup.DefaultWarehouseIdAsync(),
				Type = type == StockTransactionType.Return ? StockTransactionType.Return : StockTransactionType.Issue
			};

			if (model.Type == StockTransactionType.Issue && (quoteId.HasValue || (fromQuote && projectId.HasValue)))
			{
				var quoteQuery = _quote.Entity.Include(q => q.Items);
				var quote = quoteId.HasValue
					? await quoteQuery.FirstOrDefaultAsync(q => q.Id == quoteId.Value)
					: await quoteQuery.Where(q => q.ProjectId == projectId!.Value && q.Status != DesignStatus.Rejected)
						.OrderByDescending(q => q.Status == DesignStatus.Approved)
						.ThenByDescending(q => q.Created)
						.FirstOrDefaultAsync();

				if (quote != null)
				{
					model.ProjectId = quote.ProjectId;
					model.Notes = $"صرف أصناف عرض السعر {quote.QuoteNo}";
					var qItemIds = quote.Items.Where(i => i.ItemId.HasValue).Select(i => i.ItemId!.Value).Distinct().ToList();
					var rawIds = (await _item.Entity.GetWhere(i => qItemIds.Contains(i.Id) && i.Type == ItemType.Raw).Select(i => i.Id).ToListAsync()).ToHashSet();

					foreach (var g in quote.Items.Where(i => i.ItemId.HasValue && rawIds.Contains(i.ItemId.Value)).GroupBy(i => i.ItemId!.Value))
					{
						model.Lines.Add(new StockIssueLine { ItemId = g.Key, Quantity = g.Sum(x => x.Quantity) });
					}

					if (model.Lines.Count == 0)
					{
						TempData["Error"] = $"عرض السعر {quote.QuoteNo} لا يحتوي على أصناف مخزنية مسجلة، يمكنك اختيار الخامات يدوياً أدناه";
					}
				}
			}

			if (model.Lines.Count == 0)
			{
				model.Lines.Add(new StockIssueLine { ItemId = itemId ?? Guid.Empty });
			}

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

		// ------------------------------------------------------------------ جرد المخزن كاملاً

		private async Task<(Guid WarehouseId, List<StockRow> Rows)> StocktakeRowsAsync(Guid? warehouseId)
		{
			var wid = warehouseId ?? await _lookup.DefaultWarehouseIdAsync();
			var balances = await _stock.BalancesAsync(wid);
			var costs = await _stock.AverageCostsAsync();

			var rows = (await _item.Entity.GetWhere(i => i.Type == ItemType.Raw).OrderBy(i => i.Name).ToListAsync())
				.Select(i => new StockRow
				{
					Item = i,
					Balance = balances.GetValueOrDefault(i.Id),
					AverageCost = costs.GetValueOrDefault(i.Id, i.PurchasePrice)
				})
				.Where(r => r.Item.IsActive || r.Balance != 0)
				.ToList();

			return (wid, rows);
		}

		// GET: Stock/Stocktake  جرد المخزن: عدّ كل الأصناف وتسوية الفروقات دفعة واحدة
		public async Task<IActionResult> Stocktake(Guid? warehouseId)
		{
			ViewBag.ActivePage = "Stocktake";
			var (wid, rows) = await StocktakeRowsAsync(warehouseId);
			ViewBag.WarehouseId = wid;
			ViewBag.Warehouses = await _lookup.WarehousesAsync(wid);
			return View(rows);
		}

		// POST: Stock/Stocktake   الأصناف التي تُترك كميتها فارغة لا تُجرد ولا تتأثر
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Stocktake(Guid warehouseId, DateTime date, string? notes, [Bind(Prefix = "counts")] Dictionary<Guid, decimal?> counts)
		{
			counts = counts.Where(c => c.Value.HasValue).ToDictionary(c => c.Key, c => c.Value);
			if (counts.Count == 0)
			{
				TempData["Error"] = "أدخل الكمية الفعلية لصنف واحد على الأقل";
				return RedirectToAction(nameof(Stocktake), new { warehouseId });
			}
			if (counts.Any(c => c.Value < 0))
			{
				TempData["Error"] = "الكميات لا تكون سالبة";
				return RedirectToAction(nameof(Stocktake), new { warehouseId });
			}

			var (_, rows) = await StocktakeRowsAsync(warehouseId);
			var refNo = await DocumentNumbers.NextAsync(
				_tx.Entity.GetWhere(t => t.ReferenceNo != null).Select(t => t.ReferenceNo!), "CNT-");

			int matched = 0, surplus = 0, shortage = 0;
			decimal surplusValue = 0, shortageValue = 0;
			var now = DateTime.UtcNow;

			foreach (var row in rows.Where(r => counts.ContainsKey(r.Item.Id)))
			{
				var counted = counts[row.Item.Id]!.Value;
				var diff = counted - row.Balance;
				if (diff == 0)
				{
					matched++;
					continue;
				}

				_tx.Entity.Insert(new StockTransaction
				{
					Id = Guid.NewGuid(),
					Created = now,
					ItemId = row.Item.Id,
					WarehouseId = warehouseId,
					TransactionType = StockTransactionType.Adjustment,
					TransactionDate = date.Date,
					QuantityIn = diff > 0 ? diff : 0,
					QuantityOut = diff < 0 ? -diff : 0,
					UnitCost = row.AverageCost,
					ReferenceNo = refNo,
					Notes = $"جرد {refNo}: الرصيد {row.Balance:0.##} والفعلي {counted:0.##}" + (string.IsNullOrWhiteSpace(notes) ? "" : " - " + notes.Trim())
				});

				if (diff > 0) { surplus++; surplusValue += diff * row.AverageCost; }
				else { shortage++; shortageValue += -diff * row.AverageCost; }
			}

			if (surplus + shortage > 0)
			{
				await _tx.SaveAsync();
			}

			TempData["Success"] = surplus + shortage == 0
				? $"تم جرد {matched} صنف وكلها مطابقة، لم تُسجل أي تسوية"
				: $"جرد {refNo}: {matched} مطابق، {surplus} زيادة (قيمة {surplusValue:N2})، {shortage} عجز (قيمة {shortageValue:N2})";
			return RedirectToAction(nameof(Index), new { warehouseId });
		}

		// GET: Stock/StocktakeSheet  ورقة جرد مطبوعة للعد اليدوي (A4)
		public async Task<IActionResult> StocktakeSheet(Guid? warehouseId, bool showBalance = false)
		{
			var (wid, rows) = await StocktakeRowsAsync(warehouseId);
			var warehouses = await _lookup.WarehousesAsync(wid);
			ViewBag.WarehouseName = warehouses.FirstOrDefault(w => w.Value == wid.ToString())?.Text ?? "";
			ViewBag.ShowBalance = showBalance;
			return View(rows);
		}

		// GET: Stock/Transfer?itemId=5
		public async Task<IActionResult> Transfer(Guid? itemId)
		{
			ViewBag.ActivePage = "Stock";

			var model = new StockTransferViewModel
			{
				FromWarehouseId = await _lookup.DefaultWarehouseIdAsync(),
				Lines = { new StockIssueLine { ItemId = itemId ?? Guid.Empty } }
			};

			await FillTransferListsAsync(model);
			return View(model);
		}

		// POST: Stock/Transfer
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Transfer(StockTransferViewModel model)
		{
			ViewBag.ActivePage = "Stock";

			model.Lines = model.Lines.Where(l => l.ItemId != Guid.Empty).ToList();
			foreach (var key in ModelState.Keys.Where(k => k.StartsWith("Lines[")).ToList())
			{
				ModelState.Remove(key);
			}

			if (model.FromWarehouseId == model.ToWarehouseId)
				ModelState.AddModelError(nameof(model.ToWarehouseId), "لا يمكن التحويل إلى نفس المخزن");
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
				ModelState.AddModelError("", "التحويل يكون للأصناف الخام فقط");

			if (ModelState.IsValid)
			{
				var balances = await _stock.BalancesAsync(model.FromWarehouseId);
				foreach (var line in model.Lines)
				{
					var available = balances.GetValueOrDefault(line.ItemId);
					if (line.Quantity > available)
						ModelState.AddModelError("", $"الكمية المحوّلة من ({items[line.ItemId].Name}) أكبر من المتوفر في المخزن ({available:0.##} {items[line.ItemId].Unit})");
				}
			}

			if (ModelState.IsValid)
			{
				var refNo = await DocumentNumbers.NextAsync(
					_tx.Entity.GetWhere(t => t.ReferenceNo != null).Select(t => t.ReferenceNo!), "TRF-");
				var costs = await _stock.AverageCostsAsync();
				var now = DateTime.UtcNow;

				foreach (var line in model.Lines)
				{
					var cost = costs.GetValueOrDefault(line.ItemId, items[line.ItemId].PurchasePrice);
					_tx.Entity.Insert(NewTransferSide(line, model, model.FromWarehouseId, StockTransactionType.TransferOut, cost, refNo, now));
					_tx.Entity.Insert(NewTransferSide(line, model, model.ToWarehouseId, StockTransactionType.TransferIn, cost, refNo, now));
				}

				await _tx.SaveAsync();

				TempData["Success"] = $"تم تحويل {model.Lines.Count} صنف بين المخازن (رقم {refNo})";
				return RedirectToAction(nameof(Movements), new { type = StockTransactionType.TransferIn });
			}

			await FillTransferListsAsync(model);
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

			// التحويل حركتان (صادر ووارد) تُحذفان معاً حتى لا يختل رصيد أحد المخزنين
			var related = new List<StockTransaction> { movement };
			var isTransfer = movement.TransactionType is StockTransactionType.TransferIn or StockTransactionType.TransferOut;
			if (isTransfer && movement.ReferenceNo != null)
			{
				related = await _tx.Entity.GetWhere(t => t.ReferenceNo == movement.ReferenceNo && t.ItemId == movement.ItemId
						&& (t.TransactionType == StockTransactionType.TransferIn || t.TransactionType == StockTransactionType.TransferOut))
					.ToListAsync();
			}

			var spent = false;
			foreach (var side in related.Where(t => t.QuantityIn > 0))
			{
				if (await _stock.BalanceAsync(side.ItemId, side.WarehouseId) - side.QuantityIn < 0)
					spent = true;
			}

			if (movement.PurchaseId.HasValue)
			{
				TempData["Error"] = "هذه الحركة تابعة لفاتورة شراء، ألغِ الفاتورة بدلاً من حذف الحركة";
			}
			else if (spent)
			{
				TempData["Error"] = "لا يمكن حذف هذه الحركة لأن الكمية الواردة بها صُرفت بالفعل";
			}
			else
			{
				// GetWhere غير متتبع، فنحذف الحركة المتتبعة نفسها ونرفق الباقي
				_tx.Entity.Delete(movement);
				foreach (var side in related.Where(t => t.Id != movement.Id))
				{
					_tx.Entity.Delete(side);
				}
				await _tx.SaveAsync();
				TempData["Success"] = isTransfer
					? $"تم حذف التحويل {movement.ReferenceNo} من المخزنين"
					: $"تم حذف الحركة {movement.ReferenceNo}";
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

		private static StockTransaction NewTransferSide(StockIssueLine line, StockTransferViewModel model, Guid warehouseId,
														StockTransactionType type, decimal cost, string refNo, DateTime now) => new()
		{
			Id = Guid.NewGuid(),
			Created = now,
			ItemId = line.ItemId,
			WarehouseId = warehouseId,
			TransactionType = type,
			TransactionDate = model.Date,
			QuantityIn = type == StockTransactionType.TransferIn ? line.Quantity : 0,
			QuantityOut = type == StockTransactionType.TransferOut ? line.Quantity : 0,
			UnitCost = cost,
			ReferenceNo = refNo,
			Notes = model.Notes
		};

		private async Task FillTransferListsAsync(StockTransferViewModel model)
		{
			ViewBag.FromWarehouses = await _lookup.WarehousesAsync(model.FromWarehouseId);
			ViewBag.ToWarehouses = await _lookup.WarehousesAsync(model.ToWarehouseId);
			ViewBag.ItemOptions = await _lookup.ItemOptionsAsync(ItemType.Raw);
			ViewBag.BalancesByWarehouse = await _stock.BalancesByWarehouseAsync();
		}

		private async Task FillAdjustListsAsync(StockAdjustViewModel model)
		{
			ViewBag.Warehouses = await _lookup.WarehousesAsync(model.WarehouseId);
			ViewBag.ItemOptions = await _lookup.ItemOptionsAsync(ItemType.Raw);
			ViewBag.Balances = await _stock.BalancesAsync(model.WarehouseId);
		}
	}
}
