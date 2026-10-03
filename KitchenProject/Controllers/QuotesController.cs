using KitchenProject.Classes;
using KitchenProject.Models;
using KitchenProject.Models.Entities;
using KitchenProject.Models.Enums;
using KitchenProject.Models.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace KitchenProject.Controllers
{
    [ViewLayout("_DashboardLayout")]
    public class QuotesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IUnitOfWork<Quote> _quote;

        // الشروط الافتراضية التي تطبع أسفل الفاتورة
        public const string DefaultTerms =
            "مدة صلاحية الفاتورة اسبوع من تاريخها\n" +
            "الأسعار لا تشمل أعمال السباكة و الرخام وأي أنواع زجاج خارج العلب الخشبية أو تركيب الحوض و الغاز";

        public QuotesController(ApplicationDbContext context, IUnitOfWork<Quote> quote)
        {
            _context = context;
            _quote = quote;
        }

        // GET: Quotes
        public async Task<IActionResult> Index(string? searchString, DesignStatus? statusFilter, Guid? projectFilter, int page = 1)
        {
            ViewBag.ActivePage = "Quotes";
            ViewBag.SearchString = searchString;
            ViewBag.StatusFilter = statusFilter;
            ViewBag.ProjectFilter = projectFilter;

            var query = _context.Quotes
                .Include(q => q.Project).ThenInclude(p => p.Customer)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                searchString = searchString.Trim();
                query = query.Where(q =>
                    q.QuoteNo.Contains(searchString) ||
                    q.Project.ProjectNo.Contains(searchString) ||
                    q.Project.ProjectName.Contains(searchString) ||
                    q.Project.Customer.Name.Contains(searchString));
            }

            if (statusFilter.HasValue)
                query = query.Where(q => q.Status == statusFilter.Value);

            if (projectFilter.HasValue)
                query = query.Where(q => q.ProjectId == projectFilter.Value);

            var totalCount = await query.CountAsync();
            int pageSize = 10;
            var quotes = await query
                .OrderByDescending(q => q.Created)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            ViewBag.TotalCount = totalCount;

            // KPI Metrics
            ViewBag.TotalQuotesCount = await _context.Quotes.CountAsync();
            ViewBag.ApprovedCount = await _context.Quotes.CountAsync(q => q.Status == DesignStatus.Approved);
            ViewBag.PendingCount = await _context.Quotes.CountAsync(q => q.Status == DesignStatus.WaitingApproval || q.Status == DesignStatus.Draft);
            ViewBag.TotalValue = await _context.Quotes.SumAsync(q => q.Total);

            // Projects dropdown
            var projects = await _context.Projects
                .Include(p => p.Customer)
                .OrderByDescending(p => p.Created)
                .Select(p => new { Id = p.Id, Name = $"{p.ProjectNo} - {p.ProjectName}" })
                .ToListAsync();
            ViewBag.ProjectsList = new SelectList(projects, "Id", "Name", projectFilter);

            return View(quotes);
        }

        // GET: Quotes/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            ViewBag.ActivePage = "Quotes";
            if (id == null) return NotFound();

            var quote = await LoadQuoteWithItemsAsync(id.Value);
            if (quote == null) return NotFound();

            ViewBag.ReceivedAmount = await GetReceivedAmountAsync(quote.ProjectId);

            // الفاتورة النهائية المنشأة من هذه الفاتورة المبدئية (إن وجدت)
            ViewBag.FinalQuote = quote.Type == ContractType.Preliminary
                ? await _context.Quotes.AsNoTracking().FirstOrDefaultAsync(q => q.SourceQuoteId == quote.Id)
                : null;

            ViewBag.SourceQuote = quote.SourceQuoteId.HasValue
                ? await _context.Quotes.AsNoTracking().FirstOrDefaultAsync(q => q.Id == quote.SourceQuoteId)
                : null;

            ViewBag.Contract = await _context.Contracts.AsNoTracking().FirstOrDefaultAsync(c => c.QuoteId == quote.Id);

            return View(quote);
        }

        // GET: Quotes/Print/5 — صفحة الفاتورة للطباعة
        public async Task<IActionResult> Print(Guid? id)
        {
            if (id == null) return NotFound();

            var quote = await LoadQuoteWithItemsAsync(id.Value);
            if (quote == null) return NotFound();

            ViewBag.ReceivedAmount = await GetReceivedAmountAsync(quote.ProjectId);
            return View(quote);
        }

        // GET: Quotes/Create
        public async Task<IActionResult> Create(Guid? projectId)
        {
            ViewBag.ActivePage = "Quotes";

            var model = new Quote
            {
                QuoteNo = await NextQuoteNoAsync(),
                ProjectId = projectId ?? Guid.Empty,
                Type = ContractType.Preliminary,
                Status = DesignStatus.Draft,
                ValidUntil = DateTime.Today.AddDays(7),
                Terms = DefaultTerms
            };

            await PopulateDropdownsAsync(projectId);
            return View(model);
        }

        // POST: Quotes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Quote quote)
        {
            ViewBag.ActivePage = "Quotes";

            if (string.IsNullOrWhiteSpace(quote.QuoteNo) ||
                await _context.Quotes.AnyAsync(q => q.QuoteNo == quote.QuoteNo))
            {
                quote.QuoteNo = await NextQuoteNoAsync();
                ModelState.Remove("QuoteNo");
            }

            var items = ReadItemsFromForm();
            ApplyTotals(quote, items);

            if (ModelState.IsValid)
            {
                quote.Id = Guid.NewGuid();
                quote.Created = DateTime.UtcNow;
                foreach (var item in items)
                {
                    item.QuoteId = quote.Id;
                    quote.Items.Add(item);
                }
                _quote.Entity.Insert(quote);
                await _quote.SaveAsync();

                // Update Project Status
                var project = await _context.Projects.FindAsync(quote.ProjectId);
                if (project != null && (project.Status == ProjectStatus.New || project.Status == ProjectStatus.SiteVisit || project.Status == ProjectStatus.Design))
                {
                    project.Status = ProjectStatus.Quote;
                    _context.Projects.Update(project);
                    await _context.SaveChangesAsync();
                }

                TempData["Success"] = $"تم إضافة الفاتورة \"{quote.QuoteNo}\" بنجاح";
                return RedirectToAction(nameof(Details), new { id = quote.Id });
            }

            quote.Items = items;
            await PopulateDropdownsAsync(quote.ProjectId);
            return View(quote);
        }

        // GET: Quotes/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            ViewBag.ActivePage = "Quotes";
            if (id == null) return NotFound();

            var quote = await LoadQuoteWithItemsAsync(id.Value);
            if (quote == null) return NotFound();

            await PopulateDropdownsAsync(quote.ProjectId);
            return View(quote);
        }

        // POST: Quotes/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, Quote quote)
        {
            ViewBag.ActivePage = "Quotes";
            if (id != quote.Id) return NotFound();

            var items = ReadItemsFromForm();
            ApplyTotals(quote, items);

            if (ModelState.IsValid)
            {
                try
                {
                    // استبدال بنود الفاتورة بالبنود المرسلة من النموذج
                    var oldItems = await _context.QuoteItems.Where(i => i.QuoteId == quote.Id).ToListAsync();
                    _context.QuoteItems.RemoveRange(oldItems);
                    foreach (var item in items)
                    {
                        item.QuoteId = quote.Id;
                        _context.QuoteItems.Add(item);
                    }

                    quote.Modified = DateTime.UtcNow;
                    _quote.Entity.Update(quote);
                    await _quote.SaveAsync();
                    TempData["Success"] = $"تم تحديث الفاتورة \"{quote.QuoteNo}\" بنجاح";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Quotes.AnyAsync(q => q.Id == quote.Id))
                        return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Details), new { id = quote.Id });
            }

            quote.Items = items;
            await PopulateDropdownsAsync(quote.ProjectId);
            return View(quote);
        }

        // POST: Quotes/Approve/5 — موافقة الزبون على الفاتورة
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(Guid id)
        {
            var quote = await _context.Quotes.FindAsync(id);
            if (quote == null) return NotFound();

            quote.Status = DesignStatus.Approved;
            quote.Modified = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["Success"] = quote.Type == ContractType.Preliminary
                ? $"تمت الموافقة على الفاتورة المبدئية \"{quote.QuoteNo}\". يمكنك الآن تحويلها إلى فاتورة نهائية."
                : $"تم اعتماد الفاتورة النهائية \"{quote.QuoteNo}\". الخطوة التالية: إنشاء العقد النهائي وتحديد موعد مع الزبون.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Quotes/ConvertToFinal/5 — إنشاء فاتورة نهائية من الفاتورة المبدئية بعد الموافقة
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConvertToFinal(Guid id)
        {
            var source = await LoadQuoteWithItemsAsync(id);
            if (source == null) return NotFound();

            if (source.Type != ContractType.Preliminary || source.Status != DesignStatus.Approved)
            {
                TempData["Error"] = "يجب الموافقة على الفاتورة المبدئية أولاً قبل تحويلها إلى فاتورة نهائية";
                return RedirectToAction(nameof(Details), new { id });
            }

            var existing = await _context.Quotes.AsNoTracking().FirstOrDefaultAsync(q => q.SourceQuoteId == id);
            if (existing != null)
            {
                TempData["Error"] = $"تم إنشاء فاتورة نهائية من هذه الفاتورة مسبقاً ({existing.QuoteNo})";
                return RedirectToAction(nameof(Details), new { id = existing.Id });
            }

            var final = new Quote
            {
                Id = Guid.NewGuid(),
                Created = DateTime.UtcNow,
                QuoteNo = await NextQuoteNoAsync(),
                ProjectId = source.ProjectId,
                Type = ContractType.Final,
                Status = DesignStatus.Draft,
                SourceQuoteId = source.Id,
                ValidUntil = DateTime.Today.AddDays(7),
                Subtotal = source.Subtotal,
                Discount = source.Discount,
                Tax = source.Tax,
                Total = source.Total,
                Notes = source.Notes,
                Terms = source.Terms
            };

            foreach (var item in source.Items.OrderBy(i => i.SortOrder))
            {
                final.Items.Add(new QuoteItem
                {
                    Id = Guid.NewGuid(),
                    Created = DateTime.UtcNow,
                    QuoteId = final.Id,
                    ItemId = item.ItemId,
                    SortOrder = item.SortOrder,
                    Description = item.Description,
                    Meters = item.Meters,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice
                });
            }

            _context.Quotes.Add(final);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"تم إنشاء الفاتورة النهائية \"{final.QuoteNo}\". راجع الأصناف والأسعار ثم احفظ واعتمد الفاتورة.";
            return RedirectToAction(nameof(Edit), new { id = final.Id });
        }

        // POST: Quotes/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var quote = await _quote.Entity.GetByIdAsync(id);
            if (quote == null)
            {
                TempData["Error"] = "تعذر العثور على الفاتورة المراد حذفها";
                return RedirectToAction(nameof(Index));
            }

            if (await _context.Contracts.AnyAsync(c => c.QuoteId == id))
            {
                TempData["Error"] = "لا يمكن حذف الفاتورة لأنها مرتبطة بعقد";
                return RedirectToAction(nameof(Index));
            }

            _quote.Entity.Delete(quote);
            await _quote.SaveAsync();
            TempData["Success"] = $"تم حذف الفاتورة \"{quote.QuoteNo}\" بنجاح";
            return RedirectToAction(nameof(Index));
        }

        //---------------------------------------------------------------------

        private Task<Quote?> LoadQuoteWithItemsAsync(Guid id)
        {
            return _context.Quotes
                .AsNoTracking()
                .Include(q => q.Project).ThenInclude(p => p.Customer)
                .Include(q => q.Items.OrderBy(i => i.SortOrder))
                .FirstOrDefaultAsync(q => q.Id == id);
        }

        private Task<decimal> GetReceivedAmountAsync(Guid projectId)
        {
            return _context.CustomerPayments
                .Where(p => p.ProjectId == projectId)
                .SumAsync(p => p.Amount);
        }

        // رقم الفاتورة التالي بناءً على أكبر رقم مستخدم (وليس العدد) لتفادي التكرار بعد الحذف
        private async Task<string> NextQuoteNoAsync()
        {
            var numbers = await _context.Quotes
                .Where(q => q.QuoteNo.StartsWith("QUO-"))
                .Select(q => q.QuoteNo)
                .ToListAsync();

            var max = numbers
                .Select(n => int.TryParse(n.Substring(4), out var x) ? x : 0)
                .DefaultIfEmpty(0)
                .Max();

            return $"QUO-{(max + 1):D4}";
        }

        // قراءة بنود الفاتورة من حقول النموذج (صف لكل بند)
        private List<QuoteItem> ReadItemsFromForm()
        {
            var descriptions = Request.Form["ItemDescriptions"].ToArray();
            var itemIds = Request.Form["ItemIds"].ToArray();
            var meters = Request.Form["ItemMeters"].ToArray();
            var quantities = Request.Form["ItemQuantities"].ToArray();
            var prices = Request.Form["ItemPrices"].ToArray();

            var items = new List<QuoteItem>();
            for (int i = 0; i < descriptions.Length; i++)
            {
                var description = descriptions[i]?.Trim();
                if (string.IsNullOrEmpty(description)) continue;

                items.Add(new QuoteItem
                {
                    Id = Guid.NewGuid(),
                    Created = DateTime.UtcNow,
                    SortOrder = items.Count + 1,
                    Description = description.Length > 250 ? description[..250] : description,
                    ItemId = Guid.TryParse(itemIds.ElementAtOrDefault(i), out var itemId) ? itemId : null,
                    Meters = ParseDecimal(meters.ElementAtOrDefault(i)),
                    Quantity = ParseDecimal(quantities.ElementAtOrDefault(i)),
                    UnitPrice = ParseDecimal(prices.ElementAtOrDefault(i)) ?? 0
                });
            }
            return items;
        }

        private static decimal? ParseDecimal(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            return decimal.TryParse(value.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var d) ? d : null;
        }

        // الإجمالي يحسب من البنود على الخادم، وإن لم توجد بنود يبقى الإجمالي اليدوي
        private static void ApplyTotals(Quote quote, List<QuoteItem> items)
        {
            if (items.Count > 0)
                quote.Subtotal = items.Sum(i => i.LineTotal);

            quote.Total = quote.Subtotal - quote.Discount + quote.Tax;
        }

        private async Task PopulateDropdownsAsync(Guid? projectId)
        {
            var projects = await _context.Projects
                .Include(p => p.Customer)
                .OrderByDescending(p => p.Created)
                .Select(p => new { Id = p.Id, Name = $"{p.ProjectNo} - {p.ProjectName} ({p.Customer.Name})" })
                .ToListAsync();
            ViewBag.ProjectId = new SelectList(projects, "Id", "Name", projectId);

            // أصناف جاهزة للاختيار السريع في بنود الفاتورة
            ViewBag.CatalogItems = await _context.Items
                .Where(i => i.IsActive)
                .OrderBy(i => i.Name)
                .Select(i => new { id = i.Id, name = i.Name, price = i.SalePrice })
                .ToListAsync();
        }
    }
}
