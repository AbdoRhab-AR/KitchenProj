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

	// كتالوج الأصناف: الخام (مخزون) والجاهز (يُباع في عروض الأسعار ومقسم إلى بنود A/B/C)
	public class ItemsController : Controller
	{
		private readonly IUnitOfWork<Item> _item;
		private readonly IUnitOfWork<StockTransaction> _tx;
		private readonly IUnitOfWork<PurchaseItem> _purchaseItem;
		private readonly IUnitOfWork<QuoteItem> _quoteItem;
		private readonly LookupService _lookup;
		private readonly StockService _stock;

		public ItemsController(IUnitOfWork<Item> item,
							   IUnitOfWork<StockTransaction> tx,
							   IUnitOfWork<PurchaseItem> purchaseItem,
							   IUnitOfWork<QuoteItem> quoteItem,
							   LookupService lookup,
							   StockService stock)
		{
			_item = item;
			_tx = tx;
			_purchaseItem = purchaseItem;
			_quoteItem = quoteItem;
			_lookup = lookup;
			_stock = stock;
		}

		// GET: Items
		public async Task<IActionResult> Index(string? searchString, ItemType? type, Guid? groupId, bool lowStock = false, int page = 1)
		{
			ViewBag.ActivePage = "Items";
			ViewBag.SearchString = searchString;
			ViewBag.Type = type;
			ViewBag.GroupId = groupId;
			ViewBag.LowStock = lowStock;

			var balances = await _stock.BalancesAsync();
			ViewBag.Balances = balances;

			var query = _item.Entity.Include(i => i.Group!);

			if (!string.IsNullOrWhiteSpace(searchString))
			{
				searchString = searchString.Trim();
				query = query.Where(i => i.Name.Contains(searchString) || i.Code.Contains(searchString));
			}

			if (type.HasValue)
			{
				query = query.Where(i => i.Type == type.Value);
			}

			if (groupId.HasValue)
			{
				query = query.Where(i => i.ItemGroupId == groupId.Value);
			}

			var items = await query.OrderBy(i => i.Type).ThenBy(i => i.Group!.SortOrder).ThenBy(i => i.Name).ToListAsync();

			// تحت حد الطلب: الخام الفعال الذي رصيده أقل من أو يساوي الحد الأدنى
			bool IsLow(Item i) => i.Type == ItemType.Raw && i.IsActive && i.MinQuantity > 0
								  && balances.GetValueOrDefault(i.Id) <= i.MinQuantity;
			if (lowStock)
			{
				items = items.Where(IsLow).ToList();
			}

			int pageSize = 15;
			ViewBag.CurrentPage = page;
			ViewBag.TotalPages = (int)Math.Ceiling(items.Count / (double)pageSize);
			ViewBag.TotalCount = items.Count;

			// Metrics for Dashboard header cards
			var all = await _item.Entity.GetAll().ToListAsync();
			ViewBag.RawCount = all.Count(i => i.Type == ItemType.Raw);
			ViewBag.FinishedCount = all.Count(i => i.Type == ItemType.Finished);
			ViewBag.LowCount = all.Count(IsLow);

			ViewBag.TypeList = LookupService.EnumList(type);
			ViewBag.Groups = await _lookup.GroupsAsync(groupId);

			return View(items.Skip((page - 1) * pageSize).Take(pageSize).ToList());
		}

		// GET: Items/Details/5
		public async Task<IActionResult> Details(Guid? id)
		{
			ViewBag.ActivePage = "Items";

			var item = await _item.Entity.Include(i => i.Group!).FirstOrDefaultAsync(i => i.Id == id);
			if (item == null)
			{
				return NotFound();
			}

			if (item.Type == ItemType.Raw)
			{
				ViewBag.Movements = await _tx.Entity.Include(t => t.Warehouses, t => t.Projects!)
					.Where(t => t.ItemId == item.Id)
					.OrderByDescending(t => t.TransactionDate).ThenByDescending(t => t.Created)
					.Take(100)
					.ToListAsync();

				ViewBag.Balance = (await _stock.BalancesAsync()).GetValueOrDefault(item.Id);
				ViewBag.AverageCost = (await _stock.AverageCostsAsync()).GetValueOrDefault(item.Id, item.PurchasePrice);
			}
			else
			{
				ViewBag.QuoteLines = await _quoteItem.Entity.Include(q => q.Quote, q => q.Quote.Project)
					.Where(q => q.ItemId == item.Id)
					.OrderByDescending(q => q.Created)
					.Take(50)
					.ToListAsync();
			}

			return View(item);
		}

		// GET: Items/Create?type=Raw
		public async Task<IActionResult> Create(ItemType type = ItemType.Raw, Guid? groupId = null)
		{
			ViewBag.ActivePage = "Items";

			var model = new Item
			{
				Code = await GenerateCodeAsync(type),
				Name = "",
				Type = type,
				ItemGroupId = type == ItemType.Finished ? groupId : null
			};

			await FillSelectListsAsync(model);
			return View(model);
		}

		// POST: Items/Create
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(Item item)
		{
			ViewBag.ActivePage = "Items";
			await ValidateItemAsync(item);

			if (ModelState.IsValid)
			{
				item.Id = Guid.NewGuid();
				item.Created = DateTime.UtcNow;
				_item.Entity.Insert(item);
				await _item.SaveAsync();

				TempData["Success"] = $"تمت إضافة الصنف {item.Name} ({item.Code})";
				return RedirectToAction(nameof(Index), new { type = item.Type });
			}

			await FillSelectListsAsync(item);
			return View(item);
		}

		// GET: Items/Edit/5
		public async Task<IActionResult> Edit(Guid? id)
		{
			ViewBag.ActivePage = "Items";

			var item = id == null ? null : await _item.Entity.GetByIdAsync(id);
			if (item == null)
			{
				return NotFound();
			}

			await FillSelectListsAsync(item);
			return View(item);
		}

		// POST: Items/Edit/5
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(Guid id, Item item)
		{
			ViewBag.ActivePage = "Items";

			if (id != item.Id)
			{
				return NotFound();
			}

			await ValidateItemAsync(item);

			// الصنف الذي عليه حركات مخزنية لا يتحول إلى جاهز (يفسد الأرصدة)
			var original = await _item.Entity.GetWhere(i => i.Id == id).Select(i => new { i.Type, i.Created }).FirstOrDefaultAsync();
			if (original == null)
			{
				return NotFound();
			}
			if (original.Type != item.Type && await _tx.Entity.GetAll().AnyAsync(t => t.ItemId == id))
			{
				ModelState.AddModelError(nameof(Item.Type), "لا يمكن تغيير نوع صنف عليه حركات مخزنية");
			}

			if (ModelState.IsValid)
			{
				item.Created = original.Created;
				item.Modified = DateTime.UtcNow;
				_item.Entity.Update(item);
				await _item.SaveAsync();

				TempData["Success"] = $"تم تحديث الصنف {item.Name}";
				return RedirectToAction(nameof(Details), new { id = item.Id });
			}

			await FillSelectListsAsync(item);
			return View(item);
		}

		// POST: Items/Delete/5
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Delete(Guid id)
		{
			var item = await _item.Entity.GetByIdAsync(id);
			if (item == null)
			{
				TempData["Error"] = "تعذر العثور على الصنف المراد حذفه";
				return RedirectToAction(nameof(Index));
			}

			var used = await _tx.Entity.GetAll().AnyAsync(t => t.ItemId == id)
					|| await _purchaseItem.Entity.GetAll().AnyAsync(p => p.ItemId == id);
			if (used)
			{
				// له تاريخ حركات: يُوقف بدلاً من الحذف حتى تبقى التقارير صحيحة
				item.IsActive = false;
				item.Modified = DateTime.UtcNow;
				await _item.SaveAsync();
				TempData["Success"] = $"الصنف {item.Name} عليه حركات، لذلك تم إيقافه بدلاً من حذفه";
				return RedirectToAction(nameof(Details), new { id });
			}

			_item.Entity.Delete(item);
			await _item.SaveAsync();
			TempData["Success"] = $"تم حذف الصنف {item.Name}";
			return RedirectToAction(nameof(Index), new { type = item.Type });
		}

		//---------------------------------------------------------------------

		private Task<string> GenerateCodeAsync(ItemType type) =>
			DocumentNumbers.NextAsync(_item.Entity.GetAll().Select(i => i.Code), type == ItemType.Raw ? "RM-" : "FG-");

		private async Task ValidateItemAsync(Item item)
		{
			item.Code = (item.Code ?? "").Trim();

			if (await _item.Entity.GetAll().AnyAsync(i => i.Code == item.Code && i.Id != item.Id))
				ModelState.AddModelError(nameof(Item.Code), "هذا الكود مستخدم لصنف آخر");

			if (item.Type == ItemType.Finished)
			{
				if (item.ItemGroupId == null)
					ModelState.AddModelError(nameof(Item.ItemGroupId), "يجب اختيار البند (A / B / C) للصنف الجاهز");
				item.MinQuantity = 0;
			}
			else
			{
				// البنود للجاهز فقط
				item.ItemGroupId = null;
			}
		}

		private async Task FillSelectListsAsync(Item item)
		{
			ViewBag.TypeList = LookupService.EnumList(item.Type);
			ViewBag.Groups = await _lookup.GroupsAsync(item.ItemGroupId);
		}
	}
}
