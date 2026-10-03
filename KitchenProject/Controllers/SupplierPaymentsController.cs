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
    public class SupplierPaymentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IUnitOfWork<SupplierPayment> _payment;

        public SupplierPaymentsController(ApplicationDbContext context, IUnitOfWork<SupplierPayment> payment)
        {
            _context = context;
            _payment = payment;
        }

        public async Task<IActionResult> Index(string? searchString, Guid? supplierFilter, int page = 1)
        {
            ViewBag.ActivePage = "SupplierPayments";
            ViewBag.SearchString = searchString;
            ViewBag.SupplierFilter = supplierFilter;

            var query = _context.SupplierPayments
                .Include(p => p.supplier)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                searchString = searchString.Trim();
                query = query.Where(p => p.supplier.Name.Contains(searchString) || (p.Notes != null && p.Notes.Contains(searchString)));
            }
            if (supplierFilter.HasValue)
                query = query.Where(p => p.SupplierId == supplierFilter.Value);

            int pageSize = 10;
            var totalCount = await query.CountAsync();
            var payments = await query.OrderByDescending(p => p.Created).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            ViewBag.TotalCount = totalCount;
            ViewBag.TotalPaidToSuppliers = await _context.SupplierPayments.SumAsync(p => p.Amount);

            var suppliers = await _context.Suppliers.Where(s => s.IsActive).OrderBy(s => s.Name)
                .Select(s => new { Id = s.Id, Name = s.Name }).ToListAsync();
            ViewBag.SuppliersList = new SelectList(suppliers, "Id", "Name", supplierFilter);

            return View(payments);
        }

        public async Task<IActionResult> Details(Guid? id)
        {
            ViewBag.ActivePage = "SupplierPayments";
            if (id == null) return NotFound();

            var payment = await _context.SupplierPayments
                .Include(p => p.supplier)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (payment == null) return NotFound();

            if (payment.PurchaseId.HasValue)
            {
                var purchase = await _context.Purchases.FindAsync(payment.PurchaseId.Value);
                ViewBag.LinkedPurchase = purchase;
            }
            return View(payment);
        }

        public async Task<IActionResult> Create(Guid? supplierId, Guid? purchaseId)
        {
            ViewBag.ActivePage = "SupplierPayments";
            var model = new SupplierPayment
            {
                SupplierId = supplierId ?? Guid.Empty,
                PurchaseId = purchaseId,
                PaymentMethod = PaymentMethod.Cash
            };
            await PopulateDropdownsAsync(model.SupplierId, model.PurchaseId);
            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SupplierPayment supplierPayment)
        {
            ViewBag.ActivePage = "SupplierPayments";
            ModelState.Remove("supplier");

            if (ModelState.IsValid)
            {
                supplierPayment.Id = Guid.NewGuid();
                supplierPayment.Created = DateTime.UtcNow;

                _payment.Entity.Insert(supplierPayment);

                // Update Purchase.PaidAmount
                if (supplierPayment.PurchaseId.HasValue)
                {
                    var purchase = await _context.Purchases.FindAsync(supplierPayment.PurchaseId.Value);
                    if (purchase != null)
                    {
                        purchase.PaidAmount += supplierPayment.Amount;
                        _context.Purchases.Update(purchase);
                    }
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = $"تم تسجيل دفعة للمورد بقيمة {supplierPayment.Amount:N2} د.ل";
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropdownsAsync(supplierPayment.SupplierId, supplierPayment.PurchaseId);
            return View(supplierPayment);
        }

        public async Task<IActionResult> Edit(Guid? id)
        {
            ViewBag.ActivePage = "SupplierPayments";
            if (id == null) return NotFound();
            var payment = await _payment.Entity.GetByIdAsync(id.Value);
            if (payment == null) return NotFound();
            await PopulateDropdownsAsync(payment.SupplierId, payment.PurchaseId);
            return View(payment);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, SupplierPayment supplierPayment)
        {
            ViewBag.ActivePage = "SupplierPayments";
            if (id != supplierPayment.Id) return NotFound();
            ModelState.Remove("supplier");

            if (ModelState.IsValid)
            {
                try
                {
                    var oldPayment = await _context.SupplierPayments.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
                    if (oldPayment != null)
                    {
                        // Revert old
                        if (oldPayment.PurchaseId.HasValue)
                        {
                            var oldPurchase = await _context.Purchases.FindAsync(oldPayment.PurchaseId.Value);
                            if (oldPurchase != null) { oldPurchase.PaidAmount -= oldPayment.Amount; _context.Purchases.Update(oldPurchase); }
                        }
                        // Apply new
                        if (supplierPayment.PurchaseId.HasValue)
                        {
                            var newPurchase = await _context.Purchases.FindAsync(supplierPayment.PurchaseId.Value);
                            if (newPurchase != null) { newPurchase.PaidAmount += supplierPayment.Amount; _context.Purchases.Update(newPurchase); }
                        }
                    }

                    supplierPayment.Modified = DateTime.UtcNow;
                    _payment.Entity.Update(supplierPayment);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "تم تحديث بيانات الدفعة بنجاح";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (await _payment.Entity.GetByIdAsync(supplierPayment.Id) == null) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropdownsAsync(supplierPayment.SupplierId, supplierPayment.PurchaseId);
            return View(supplierPayment);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var payment = await _payment.Entity.GetByIdAsync(id);
            if (payment != null)
            {
                if (payment.PurchaseId.HasValue)
                {
                    var purchase = await _context.Purchases.FindAsync(payment.PurchaseId.Value);
                    if (purchase != null) { purchase.PaidAmount -= payment.Amount; _context.Purchases.Update(purchase); }
                }
                _payment.Entity.Delete(payment);
                await _context.SaveChangesAsync();
                TempData["Success"] = "تم حذف الدفعة بنجاح";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> GetSupplierPurchases(Guid supplierId)
        {
            var purchases = await _context.Purchases
                .Where(p => p.SupplierId == supplierId && p.Status != "Cancelled")
                .Select(p => new { id = p.Id, text = p.InvoiceNo, remaining = p.Total - p.PaidAmount })
                .ToListAsync();
            return Json(purchases);
        }

        private async Task PopulateDropdownsAsync(Guid? supplierId, Guid? purchaseId)
        {
            var suppliers = await _context.Suppliers.Where(s => s.IsActive).OrderBy(s => s.Name)
                .Select(s => new { Id = s.Id, Name = s.Name }).ToListAsync();
            ViewBag.SupplierId = new SelectList(suppliers, "Id", "Name", supplierId);

            var purchases = supplierId.HasValue
                ? await _context.Purchases.Where(p => p.SupplierId == supplierId && p.Status != "Cancelled")
                    .Select(p => new { Id = p.Id, Name = p.InvoiceNo }).ToListAsync()
                : new List<object> { }.Select(x => new { Id = Guid.Empty, Name = "" }).ToList();
            ViewBag.PurchaseId = new SelectList(purchases, "Id", "Name", purchaseId);

            var paymentMethods = Enum.GetValues(typeof(PaymentMethod)).Cast<PaymentMethod>().Select(e => new
            {
                Id = (int)e,
                Name = e.GetType().GetMember(e.ToString()).First()
                    .GetCustomAttributes(false).OfType<System.ComponentModel.DataAnnotations.DisplayAttribute>()
                    .LastOrDefault()?.Name ?? e.ToString()
            });
            ViewBag.PaymentMethod = new SelectList(paymentMethods, "Id", "Name");
        }
    }
}
