using KitchenProject.Classes;
using KitchenProject.Models;
using KitchenProject.Models.Entities;
using KitchenProject.Models.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KitchenProject.Controllers
{
    [ViewLayout("_DashboardLayout")]
    public class WarehousesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IUnitOfWork<Warehouse> _warehouse;

        public WarehousesController(ApplicationDbContext context, IUnitOfWork<Warehouse> warehouse)
        {
            _context = context;
            _warehouse = warehouse;
        }

        public async Task<IActionResult> Index(string? searchString, int page = 1)
        {
            ViewBag.ActivePage = "Warehouses";
            ViewBag.SearchString = searchString;

            var query = _context.Warehouses.AsQueryable();
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                searchString = searchString.Trim();
                query = query.Where(w => w.Name.Contains(searchString) || (w.Location != null && w.Location.Contains(searchString)));
            }

            int pageSize = 10;
            var totalCount = await query.CountAsync();
            var warehouses = await query.OrderByDescending(w => w.Created).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            ViewBag.TotalCount = totalCount;
            ViewBag.TotalWarehouses = await _context.Warehouses.CountAsync();
            ViewBag.ActiveWarehouses = await _context.Warehouses.CountAsync(w => w.IsActive);

            return View(warehouses);
        }

        public async Task<IActionResult> Details(Guid? id)
        {
            ViewBag.ActivePage = "Warehouses";
            if (id == null) return NotFound();

            var warehouse = await _context.Warehouses.FirstOrDefaultAsync(w => w.Id == id);
            if (warehouse == null) return NotFound();

            // Stock summary per item in this warehouse
            var stockSummary = await _context.StockTransactions
                .Where(t => t.WarehouseId == id)
                .GroupBy(t => new { t.ItemId, t.Items.Name, t.Items.Code, t.Items.Unit })
                .Select(g => new
                {
                    ItemId = g.Key.ItemId,
                    ItemName = g.Key.Name,
                    ItemCode = g.Key.Code,
                    Unit = g.Key.Unit,
                    TotalIn = g.Sum(t => t.QuantityIn),
                    TotalOut = g.Sum(t => t.QuantityOut),
                    Balance = g.Sum(t => t.QuantityIn - t.QuantityOut)
                })
                .OrderBy(s => s.ItemName)
                .ToListAsync();

            ViewBag.StockSummary = stockSummary;
            ViewBag.TotalTransactions = await _context.StockTransactions.CountAsync(t => t.WarehouseId == id);
            return View(warehouse);
        }

        public IActionResult Create()
        {
            ViewBag.ActivePage = "Warehouses";
            return View(new Warehouse { Name = "" });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Warehouse warehouse)
        {
            ViewBag.ActivePage = "Warehouses";
            if (ModelState.IsValid)
            {
                warehouse.Id = Guid.NewGuid();
                warehouse.Created = DateTime.UtcNow;
                _warehouse.Entity.Insert(warehouse);
                await _warehouse.SaveAsync();
                TempData["Success"] = $"تم إضافة المخزن \"{warehouse.Name}\" بنجاح";
                return RedirectToAction(nameof(Index));
            }
            return View(warehouse);
        }

        public async Task<IActionResult> Edit(Guid? id)
        {
            ViewBag.ActivePage = "Warehouses";
            if (id == null) return NotFound();
            var warehouse = await _warehouse.Entity.GetByIdAsync(id.Value);
            if (warehouse == null) return NotFound();
            return View(warehouse);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, Warehouse warehouse)
        {
            ViewBag.ActivePage = "Warehouses";
            if (id != warehouse.Id) return NotFound();
            if (ModelState.IsValid)
            {
                try
                {
                    warehouse.Modified = DateTime.UtcNow;
                    _warehouse.Entity.Update(warehouse);
                    await _warehouse.SaveAsync();
                    TempData["Success"] = $"تم تحديث بيانات المخزن \"{warehouse.Name}\" بنجاح";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (await _warehouse.Entity.GetByIdAsync(warehouse.Id) == null) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(warehouse);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var warehouse = await _warehouse.Entity.GetByIdAsync(id);
            if (warehouse != null)
            {
                if (await _context.StockTransactions.AnyAsync(t => t.WarehouseId == id))
                {
                    TempData["Error"] = $"لا يمكن حذف المخزن \"{warehouse.Name}\" لأنه يحتوي على حركات مخزنية مسجلة";
                    return RedirectToAction(nameof(Index));
                }
                _warehouse.Entity.Delete(warehouse);
                await _warehouse.SaveAsync();
                TempData["Success"] = $"تم حذف المخزن \"{warehouse.Name}\" بنجاح";
            }
            else
            {
                TempData["Error"] = "تعذر العثور على المخزن";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
