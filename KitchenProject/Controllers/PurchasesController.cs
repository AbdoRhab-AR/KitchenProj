using KitchenProject.Classes;
using KitchenProject.Models;
using KitchenProject.Models.Entities;
using KitchenProject.Models.Enums;
using KitchenProject.Models.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace KitchenProject.Controllers
{
    [ViewLayout("_DashboardLayout")]
    public class PurchasesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IUnitOfWork<Purchase> _purchase;

        public PurchasesController(ApplicationDbContext context, IUnitOfWork<Purchase> purchase)
        {
            _context = context;
            _purchase = purchase;
        }

        // GET: Purchases
        public async Task<IActionResult> Index(string? searchString, string? statusFilter, Guid? supplierFilter, Guid? projectFilter, int page = 1)
        {
            ViewBag.ActivePage = "Purchases";
            ViewBag.SearchString = searchString;
            ViewBag.StatusFilter = statusFilter;
            ViewBag.SupplierFilter = supplierFilter;
            ViewBag.ProjectFilter = projectFilter;

            var query = _context.Purchases
                .Include(p => p.Suppliers)
                .Include(p => p.Warehouses)
                .Include(p => p.Projects)
                .Include(p => p.Items)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                searchString = searchString.Trim();
                query = query.Where(p =>
                    p.InvoiceNo.Contains(searchString) ||
                    p.Suppliers.Name.Contains(searchString));
            }
            if (!string.IsNullOrWhiteSpace(statusFilter))
                query = query.Where(p => p.Status == statusFilter);
            if (supplierFilter.HasValue)
                query = query.Where(p => p.SupplierId == supplierFilter.Value);
            if (projectFilter.HasValue)
                query = query.Where(p => p.ProjectId == projectFilter.Value);

            var totalCount = await query.CountAsync();
            int pageSize = 10;
            var purchases = await query
                .OrderByDescending(p => p.Created)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            ViewBag.TotalCount = totalCount;
            ViewBag.TotalPurchasesAmount = await _context.Purchases.SumAsync(p => p.Total);
            ViewBag.PendingCount = await _context.Purchases.CountAsync(p => p.Status == "Draft" || p.Status == "Ordered");

            var suppliers = await _context.Suppliers.OrderBy(s => s.Name).Select(s => new { Id = s.Id, Name = s.Name }).ToListAsync();
            ViewBag.SuppliersList = new SelectList(suppliers, "Id", "Name", supplierFilter);

            var projects = await _context.Projects.Include(p => p.Customer).OrderByDescending(p => p.Created)
                .Select(p => new { Id = p.Id, Name = $"{p.ProjectNo} - {p.ProjectName}" }).ToListAsync();
            ViewBag.ProjectsList = new SelectList(projects, "Id", "Name", projectFilter);

            var statuses = new List<string> { "Draft", "Ordered", "Received", "Cancelled" };
            ViewBag.StatusList = new SelectList(statuses, statusFilter);

            return View(purchases);
        }

        // GET: Purchases/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            ViewBag.ActivePage = "Purchases";
            if (id == null) return NotFound();

            var purchase = await _context.Purchases
                .Include(p => p.Suppliers)
                .Include(p => p.Warehouses)
                .Include(p => p.Projects).ThenInclude(pr => pr!.Customer)
                .Include(p => p.Items).ThenInclude(i => i.Items)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (purchase == null) return NotFound();
            return View(purchase);
        }

        // GET: Purchases/Create
        public async Task<IActionResult> Create(Guid? projectId)
        {
            ViewBag.ActivePage = "Purchases";
            var count = await _context.Purchases.CountAsync();
            var model = new Purchase
            {
                InvoiceNo = $"PO-{(count + 1):D4}",
                ProjectId = projectId,
                Status = "Draft"
            };
            await PopulateDropdownsAsync(model.SupplierId, model.WarehouseId, model.ProjectId, model.Status);
            return View(model);
        }

        // POST: Purchases/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Purchase purchase)
        {
            ViewBag.ActivePage = "Purchases";

            // Remove navigation properties from validation
            ModelState.Remove("Suppliers");
            ModelState.Remove("Warehouses");
            ModelState.Remove("Projects");

            if (ModelState.IsValid)
            {
                purchase.Id = Guid.NewGuid();
                purchase.Created = DateTime.UtcNow;
                purchase.Total = purchase.Subtotal - purchase.Discount + purchase.Tax;

                // Parse line items from form
                var itemIds = Request.Form["ItemIds"].ToArray();
                var quantities = Request.Form["Quantities"].ToArray();
                var unitPrices = Request.Form["UnitPrices"].ToArray();

                _context.Purchases.Add(purchase);
                await _context.SaveChangesAsync();

                for (int i = 0; i < itemIds.Length; i++)
                {
                    if (!Guid.TryParse(itemIds[i], out var itemId)) continue;
                    if (!decimal.TryParse(quantities[i], out var qty) || qty <= 0) continue;
                    decimal.TryParse(unitPrices[i], out var unitPrice);

                    var lineItem = new PurchaseItem
                    {
                        Id = Guid.NewGuid(),
                        PurchaseId = purchase.Id,
                        ItemId = itemId,
                        Quantity = qty,
                        UnitPrice = unitPrice,
                        Created = DateTime.UtcNow
                    };
                    _context.PurchaseItems.Add(lineItem);

                    // If status is Received, create a stock-in transaction
                    if (purchase.Status == "Received")
                    {
                        var tx = new StockTransaction
                        {
                            Id = Guid.NewGuid(),
                            ItemId = itemId,
                            WarehouseId = purchase.WarehouseId,
                            ProjectId = purchase.ProjectId,
                            TransactionType = StockTransactionType.Purchase,
                            QuantityIn = qty,
                            QuantityOut = 0,
                            UnitCost = unitPrice,
                            ReferenceNo = purchase.InvoiceNo,
                            Notes = $"استلام من أمر الشراء {purchase.InvoiceNo}",
                            Created = DateTime.UtcNow
                        };
                        _context.StockTransactions.Add(tx);
                    }
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = $"تم إضافة أمر الشراء \"{purchase.InvoiceNo}\" بنجاح";
                return RedirectToAction(nameof(Details), new { id = purchase.Id });
            }

            await PopulateDropdownsAsync(purchase.SupplierId, purchase.WarehouseId, purchase.ProjectId, purchase.Status);
            return View(purchase);
        }

        // GET: Purchases/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            ViewBag.ActivePage = "Purchases";
            if (id == null) return NotFound();

            var purchase = await _context.Purchases
                .Include(p => p.Items).ThenInclude(i => i.Items)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (purchase == null) return NotFound();

            await PopulateDropdownsAsync(purchase.SupplierId, purchase.WarehouseId, purchase.ProjectId, purchase.Status);
            return View(purchase);
        }

        // POST: Purchases/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, Purchase purchase)
        {
            ViewBag.ActivePage = "Purchases";
            if (id != purchase.Id) return NotFound();

            ModelState.Remove("Suppliers");
            ModelState.Remove("Warehouses");
            ModelState.Remove("Projects");

            if (ModelState.IsValid)
            {
                try
                {
                    purchase.Modified = DateTime.UtcNow;
                    purchase.Total = purchase.Subtotal - purchase.Discount + purchase.Tax;
                    _context.Purchases.Update(purchase);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = $"تم تحديث أمر الشراء \"{purchase.InvoiceNo}\" بنجاح";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Purchases.AnyAsync(p => p.Id == purchase.Id)) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Details), new { id = purchase.Id });
            }

            await PopulateDropdownsAsync(purchase.SupplierId, purchase.WarehouseId, purchase.ProjectId, purchase.Status);
            return View(purchase);
        }

        // POST: Purchases/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var purchase = await _context.Purchases
                .Include(p => p.Items)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (purchase != null)
            {
                _context.PurchaseItems.RemoveRange(purchase.Items);
                _context.Purchases.Remove(purchase);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"تم حذف أمر الشراء \"{purchase.InvoiceNo}\" بنجاح";
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: Add a single PurchaseItem to existing purchase
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddItem(PurchaseItem item)
        {
            ModelState.Remove("Purchases");
            ModelState.Remove("Items");

            var purchaseExists = await _context.Purchases.AnyAsync(p => p.Id == item.PurchaseId);
            if (!purchaseExists) return NotFound();

            if (item.Quantity > 0)
            {
                item.Id = Guid.NewGuid();
                item.Created = DateTime.UtcNow;
                _context.PurchaseItems.Add(item);

                // Update purchase totals
                var purchase = await _context.Purchases
                    .Include(p => p.Items)
                    .FirstOrDefaultAsync(p => p.Id == item.PurchaseId);
                if (purchase != null)
                {
                    purchase.Subtotal = purchase.Items.Sum(i => i.Quantity * i.UnitPrice) + item.Quantity * item.UnitPrice;
                    purchase.Total = purchase.Subtotal - purchase.Discount + purchase.Tax;
                    _context.Purchases.Update(purchase);
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = "تم إضافة الصنف بنجاح";
            }
            return RedirectToAction(nameof(Details), new { id = item.PurchaseId });
        }

        // POST: Remove a PurchaseItem
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveItem(Guid id, Guid purchaseId)
        {
            var item = await _context.PurchaseItems.FindAsync(id);
            if (item != null)
            {
                _context.PurchaseItems.Remove(item);

                var purchase = await _context.Purchases
                    .Include(p => p.Items)
                    .FirstOrDefaultAsync(p => p.Id == purchaseId);
                if (purchase != null)
                {
                    var remaining = purchase.Items.Where(i => i.Id != id);
                    purchase.Subtotal = remaining.Sum(i => i.Quantity * i.UnitPrice);
                    purchase.Total = purchase.Subtotal - purchase.Discount + purchase.Tax;
                    _context.Purchases.Update(purchase);
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = "تم حذف الصنف من أمر الشراء";
            }
            return RedirectToAction(nameof(Details), new { id = purchaseId });
        }

        // POST: Mark Purchase as Received (creates stock transactions)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkReceived(Guid id)
        {
            var purchase = await _context.Purchases
                .Include(p => p.Items)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (purchase == null) return NotFound();

            if (purchase.Status != "Received")
            {
                purchase.Status = "Received";
                purchase.Modified = DateTime.UtcNow;
                _context.Purchases.Update(purchase);

                foreach (var item in purchase.Items)
                {
                    var tx = new StockTransaction
                    {
                        Id = Guid.NewGuid(),
                        ItemId = item.ItemId,
                        WarehouseId = purchase.WarehouseId,
                        ProjectId = purchase.ProjectId,
                        TransactionType = StockTransactionType.Purchase,
                        QuantityIn = item.Quantity,
                        QuantityOut = 0,
                        UnitCost = item.UnitPrice,
                        ReferenceNo = purchase.InvoiceNo,
                        Notes = $"استلام بضاعة — {purchase.InvoiceNo}",
                        Created = DateTime.UtcNow
                    };
                    _context.StockTransactions.Add(tx);
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = $"تم تأكيد استلام أمر الشراء \"{purchase.InvoiceNo}\" وتحديث المخزن";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpGet]
        public async Task<IActionResult> GetItemsJson(string? q)
        {
            var items = await _context.Items
                .Where(i => i.IsActive && (string.IsNullOrEmpty(q) || i.Name.Contains(q) || i.Code.Contains(q)))
                .Select(i => new { id = i.Id, text = $"{i.Code} - {i.Name}", unit = i.Unit, price = i.PurchasePrice })
                .Take(20)
                .ToListAsync();
            return Json(items);
        }

        private async Task PopulateDropdownsAsync(Guid? supplierId, Guid warehouseId, Guid? projectId, string? status)
        {
            var suppliers = await _context.Suppliers.Where(s => s.IsActive).OrderBy(s => s.Name)
                .Select(s => new { Id = s.Id, Name = s.Name }).ToListAsync();
            ViewBag.SupplierId = new SelectList(suppliers, "Id", "Name", supplierId);

            var warehouses = await _context.Warehouses.Where(w => w.IsActive).OrderBy(w => w.Name)
                .Select(w => new { Id = w.Id, Name = w.Name }).ToListAsync();
            ViewBag.WarehouseId = new SelectList(warehouses, "Id", "Name", warehouseId);

            var projects = await _context.Projects.Include(p => p.Customer).OrderByDescending(p => p.Created)
                .Select(p => new { Id = p.Id, Name = $"{p.ProjectNo} - {p.ProjectName}" }).ToListAsync();
            ViewBag.ProjectId = new SelectList(projects, "Id", "Name", projectId);

            var statuses = new List<string> { "Draft", "Ordered", "Received", "Cancelled" };
            ViewBag.StatusList = new SelectList(statuses, status);

            var items = await _context.Items.Where(i => i.IsActive).OrderBy(i => i.Name)
                .Select(i => new { Id = i.Id, Name = $"{i.Code} - {i.Name}", Unit = i.Unit, Price = i.PurchasePrice })
                .ToListAsync();
            ViewBag.ItemsList = new SelectList(items, "Id", "Name");
            ViewBag.ItemsData = items;
        }
    }
}
