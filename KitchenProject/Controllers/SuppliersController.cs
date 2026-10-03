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
    public class SuppliersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IUnitOfWork<Supplier> _supplier;

        public SuppliersController(ApplicationDbContext context, IUnitOfWork<Supplier> supplier)
        {
            _context = context;
            _supplier = supplier;
        }

        public async Task<IActionResult> Index(string? searchString, bool? activeFilter, int page = 1)
        {
            ViewBag.ActivePage = "Suppliers";
            ViewBag.SearchString = searchString;
            ViewBag.ActiveFilter = activeFilter;

            var query = _context.Suppliers.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                searchString = searchString.Trim();
                query = query.Where(s => s.Name.Contains(searchString) || (s.Phone != null && s.Phone.Contains(searchString)) || s.SupplierNo.Contains(searchString));
            }
            if (activeFilter.HasValue)
                query = query.Where(s => s.IsActive == activeFilter.Value);

            int pageSize = 10;
            var totalCount = await query.CountAsync();
            var suppliers = await query.OrderBy(s => s.Name).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            ViewBag.TotalCount = totalCount;
            ViewBag.TotalSuppliers = await _context.Suppliers.CountAsync();
            ViewBag.ActiveSuppliers = await _context.Suppliers.CountAsync(s => s.IsActive);

            return View(suppliers);
        }

        public async Task<IActionResult> Details(Guid? id)
        {
            ViewBag.ActivePage = "Suppliers";
            if (id == null) return NotFound();

            var supplier = await _context.Suppliers
                .Include(s => s.Purchases)
                .FirstOrDefaultAsync(s => s.Id == id);
            if (supplier == null) return NotFound();

            ViewBag.TotalPurchasesAmount = supplier.Purchases.Sum(p => p.Total);
            ViewBag.TotalPaid = supplier.Purchases.Sum(p => p.PaidAmount);
            ViewBag.TotalRemaining = supplier.Purchases.Sum(p => p.RemainingAmount);
            ViewBag.PurchasesCount = supplier.Purchases.Count;

            return View(supplier);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.ActivePage = "Suppliers";
            var count = await _context.Suppliers.CountAsync();
            return View(new Supplier { SupplierNo = $"SUP-{(count + 1):D4}", Name = "" });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Supplier supplier)
        {
            ViewBag.ActivePage = "Suppliers";
            if (ModelState.IsValid)
            {
                supplier.Id = Guid.NewGuid();
                supplier.Created = DateTime.UtcNow;
                _supplier.Entity.Insert(supplier);
                await _supplier.SaveAsync();
                TempData["Success"] = $"تم إضافة المورد \"{supplier.Name}\" بنجاح";
                return RedirectToAction(nameof(Index));
            }
            return View(supplier);
        }

        public async Task<IActionResult> Edit(Guid? id)
        {
            ViewBag.ActivePage = "Suppliers";
            if (id == null) return NotFound();
            var supplier = await _supplier.Entity.GetByIdAsync(id.Value);
            if (supplier == null) return NotFound();
            return View(supplier);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, Supplier supplier)
        {
            ViewBag.ActivePage = "Suppliers";
            if (id != supplier.Id) return NotFound();
            if (ModelState.IsValid)
            {
                try
                {
                    supplier.Modified = DateTime.UtcNow;
                    _supplier.Entity.Update(supplier);
                    await _supplier.SaveAsync();
                    TempData["Success"] = $"تم تحديث بيانات المورد \"{supplier.Name}\" بنجاح";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (await _supplier.Entity.GetByIdAsync(supplier.Id) == null) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(supplier);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var supplier = await _supplier.Entity.GetByIdAsync(id);
            if (supplier != null)
            {
                if (await _context.Purchases.AnyAsync(p => p.SupplierId == id))
                {
                    TempData["Error"] = $"لا يمكن حذف المورد \"{supplier.Name}\" لأنه مرتبط بأوامر شراء مسجلة";
                    return RedirectToAction(nameof(Index));
                }
                _supplier.Entity.Delete(supplier);
                await _supplier.SaveAsync();
                TempData["Success"] = $"تم حذف المورد \"{supplier.Name}\" بنجاح";
            }
            else
            {
                TempData["Error"] = "تعذر العثور على المورد";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
