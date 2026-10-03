using KitchenProject.Classes;
using KitchenProject.Models;
using KitchenProject.Models.Entities;
using KitchenProject.Models.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace KitchenProject.Controllers
{
    [ViewLayout("_DashboardLayout")]
    public class ItemsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IUnitOfWork<Item> _item;

        public ItemsController(ApplicationDbContext context, IUnitOfWork<Item> item)
        {
            _context = context;
            _item = item;
        }

        public async Task<IActionResult> Index(string? searchString, string? categoryFilter, bool? activeFilter, int page = 1)
        {
            ViewBag.ActivePage = "Items";
            ViewBag.SearchString = searchString;
            ViewBag.CategoryFilter = categoryFilter;
            ViewBag.ActiveFilter = activeFilter;

            var query = _context.Items.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                searchString = searchString.Trim();
                query = query.Where(i => i.Name.Contains(searchString) || i.Code.Contains(searchString));
            }
            if (!string.IsNullOrWhiteSpace(categoryFilter))
                query = query.Where(i => i.Category == categoryFilter);
            if (activeFilter.HasValue)
                query = query.Where(i => i.IsActive == activeFilter.Value);

            int pageSize = 10;
            var totalCount = await query.CountAsync();
            var items = await query.OrderBy(i => i.Name).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            ViewBag.TotalCount = totalCount;
            ViewBag.TotalItems = await _context.Items.CountAsync();
            ViewBag.ActiveItems = await _context.Items.CountAsync(i => i.IsActive);

            // Stock balances for displayed items
            var itemIds = items.Select(i => i.Id).ToList();
            var balances = await _context.StockTransactions
                .Where(t => itemIds.Contains(t.ItemId))
                .GroupBy(t => t.ItemId)
                .Select(g => new { ItemId = g.Key, Balance = g.Sum(t => t.QuantityIn - t.QuantityOut) })
                .ToDictionaryAsync(x => x.ItemId, x => x.Balance);
            ViewBag.StockBalances = balances;

            var categories = await _context.Items.Select(i => i.Category).Distinct().Where(c => c != null).ToListAsync();
            ViewBag.CategoryList = new SelectList(categories, categoryFilter);

            return View(items);
        }

        public async Task<IActionResult> Details(Guid? id)
        {
            ViewBag.ActivePage = "Items";
            if (id == null) return NotFound();

            var item = await _context.Items.FirstOrDefaultAsync(i => i.Id == id);
            if (item == null) return NotFound();

            // Stock per warehouse
            var warehouseStock = await _context.StockTransactions
                .Where(t => t.ItemId == id)
                .GroupBy(t => new { t.WarehouseId, t.Warehouses.Name })
                .Select(g => new
                {
                    WarehouseId = g.Key.WarehouseId,
                    WarehouseName = g.Key.Name,
                    In = g.Sum(t => t.QuantityIn),
                    Out = g.Sum(t => t.QuantityOut),
                    Balance = g.Sum(t => t.QuantityIn - t.QuantityOut)
                })
                .ToListAsync();
            ViewBag.WarehouseStock = warehouseStock;

            // Recent transactions
            var recentTx = await _context.StockTransactions
                .Where(t => t.ItemId == id)
                .Include(t => t.Warehouses)
                .OrderByDescending(t => t.Created)
                .Take(10)
                .ToListAsync();
            ViewBag.RecentTransactions = recentTx;

            var totalBalance = await _context.StockTransactions
                .Where(t => t.ItemId == id)
                .SumAsync(t => t.QuantityIn - t.QuantityOut);
            ViewBag.TotalBalance = totalBalance;

            return View(item);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.ActivePage = "Items";
            await PopulateDropdownsAsync(null);
            return View(new Item { Code = "", Name = "", Unit = "وحدة" });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Item item)
        {
            ViewBag.ActivePage = "Items";
            if (ModelState.IsValid)
            {
                item.Id = Guid.NewGuid();
                item.Created = DateTime.UtcNow;
                _item.Entity.Insert(item);
                await _item.SaveAsync();
                TempData["Success"] = $"تم إضافة الصنف \"{item.Name}\" بنجاح";
                return RedirectToAction(nameof(Index));
            }
            await PopulateDropdownsAsync(item.Category);
            return View(item);
        }

        public async Task<IActionResult> Edit(Guid? id)
        {
            ViewBag.ActivePage = "Items";
            if (id == null) return NotFound();
            var item = await _item.Entity.GetByIdAsync(id.Value);
            if (item == null) return NotFound();
            await PopulateDropdownsAsync(item.Category);
            return View(item);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, Item item)
        {
            ViewBag.ActivePage = "Items";
            if (id != item.Id) return NotFound();
            if (ModelState.IsValid)
            {
                try
                {
                    item.Modified = DateTime.UtcNow;
                    _item.Entity.Update(item);
                    await _item.SaveAsync();
                    TempData["Success"] = $"تم تحديث بيانات الصنف \"{item.Name}\" بنجاح";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (await _item.Entity.GetByIdAsync(item.Id) == null) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            await PopulateDropdownsAsync(item.Category);
            return View(item);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var item = await _item.Entity.GetByIdAsync(id);
            if (item != null)
            {
                // Soft delete
                item.IsActive = false;
                item.Modified = DateTime.UtcNow;
                _item.Entity.Update(item);
                await _item.SaveAsync();
                TempData["Success"] = $"تم إيقاف تشغيل الصنف \"{item.Name}\" بنجاح";
            }
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateDropdownsAsync(string? selectedCategory)
        {
            var commonCategories = new List<string> { "خامات", "تجهيزات", "أدوات", "لوازم", "قطع غيار", "بضاعة" };
            var dbCategories = await _context.Items.Select(i => i.Category).Distinct().Where(c => c != null).ToListAsync();
            var allCategories = commonCategories.Union(dbCategories!).Where(c => !string.IsNullOrEmpty(c)).ToList();
            ViewBag.CategoryList = new SelectList(allCategories, selectedCategory);
        }
    }
}
