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
    public class ContractsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IUnitOfWork<Contract> _contract;

        public ContractsController(ApplicationDbContext context, IUnitOfWork<Contract> contract)
        {
            _context = context;
            _contract = contract;
        }

        // GET: Contracts
        public async Task<IActionResult> Index(string? searchString, string? statusFilter, Guid? projectFilter, int page = 1)
        {
            ViewBag.ActivePage = "Contracts";
            ViewBag.SearchString = searchString;
            ViewBag.StatusFilter = statusFilter;
            ViewBag.ProjectFilter = projectFilter;

            var query = _context.Contracts
                .Include(c => c.Projects).ThenInclude(p => p.Customer)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                searchString = searchString.Trim();
                query = query.Where(c =>
                    c.ContractNo.Contains(searchString) ||
                    c.Projects.ProjectNo.Contains(searchString) ||
                    c.Projects.ProjectName.Contains(searchString) ||
                    c.Projects.Customer.Name.Contains(searchString));
            }

            if (!string.IsNullOrWhiteSpace(statusFilter))
                query = query.Where(c => c.Status == statusFilter);

            if (projectFilter.HasValue)
                query = query.Where(c => c.ProjectId == projectFilter.Value);

            var totalCount = await query.CountAsync();
            int pageSize = 10;
            var contracts = await query
                .OrderByDescending(c => c.Created)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            ViewBag.TotalCount = totalCount;

            // KPI Metrics
            ViewBag.TotalContractsCount = await _context.Contracts.CountAsync();
            ViewBag.ActiveCount = await _context.Contracts.CountAsync(c => c.Status == "Active");
            ViewBag.TotalContractValue = await _context.Contracts.SumAsync(c => c.TotalAmount);
            ViewBag.TotalPaidValue = await _context.Contracts.SumAsync(c => c.PaidAmount);

            // Statuses list
            ViewBag.StatusList = new SelectList(new[]
            {
                new { Value = "Draft",     Text = "مسودة" },
                new { Value = "Active",    Text = "ساري" },
                new { Value = "Completed", Text = "مكتمل" },
                new { Value = "Cancelled", Text = "ملغى" }
            }, "Value", "Text", statusFilter);

            // Projects dropdown
            var projects = await _context.Projects
                .Include(p => p.Customer)
                .OrderByDescending(p => p.Created)
                .Select(p => new { Id = p.Id, Name = $"{p.ProjectNo} - {p.ProjectName}" })
                .ToListAsync();
            ViewBag.ProjectsList = new SelectList(projects, "Id", "Name", projectFilter);

            return View(contracts);
        }

        // GET: Contracts/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            ViewBag.ActivePage = "Contracts";

            if (id == null) return NotFound();

            var contract = await _context.Contracts
                .Include(c => c.Projects).ThenInclude(p => p.Customer)
                .Include(c => c.CustomerPayments)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (contract == null) return NotFound();

            return View(contract);
        }

        // GET: Contracts/Create
        public async Task<IActionResult> Create(Guid? projectId, Guid? quoteId)
        {
            ViewBag.ActivePage = "Contracts";

            var count = await _context.Contracts.CountAsync();
            var nextNo = $"CON-{(count + 1):D4}";

            var model = new Contract
            {
                ContractNo = nextNo,
                ProjectId = projectId ?? Guid.Empty,
                Status = "Draft",
                StartDate = DateTime.Now
            };

            // عند القدوم من فاتورة نهائية معتمدة: ربط العقد بها وتعبئة القيمة
            if (quoteId.HasValue)
            {
                var quote = await _context.Quotes.AsNoTracking()
                    .FirstOrDefaultAsync(q => q.Id == quoteId && q.Status == KitchenProject.Models.Enums.DesignStatus.Approved);
                if (quote != null)
                {
                    model.ProjectId = quote.ProjectId;
                    model.QuoteId = quote.Id;
                    model.Type = KitchenProject.Models.Enums.ContractType.Final;
                    model.TotalAmount = quote.Total;
                }
            }

            await PopulateDropdownsAsync(model.ProjectId, "Draft", model.QuoteId);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Contract contract)
        {
            ViewBag.ActivePage = "Contracts";

            if (ModelState.IsValid)
            {
                if (contract.Type == KitchenProject.Models.Enums.ContractType.Final)
                {
                    // Ensure TotalAmount is pulled securely from the Approved Quote
                    var quote = await _context.Quotes.FirstOrDefaultAsync(q => q.Id == contract.QuoteId && q.Status == KitchenProject.Models.Enums.DesignStatus.Approved);
                    if (quote == null)
                    {
                        ModelState.AddModelError("QuoteId", "يجب اختيار عرض سعر معتمد صحيح للعقد النهائي");
                        await PopulateDropdownsAsync(contract.ProjectId, contract.Status, contract.QuoteId);
                        return View(contract);
                    }
                    contract.TotalAmount = quote.Total;
                }
                else
                {
                    // Preliminary contract doesn't require a quote
                    contract.QuoteId = null;
                }

                contract.Id = Guid.NewGuid();
                contract.Created = DateTime.UtcNow;

                // Update project status to Contracted
                var project = await _context.Projects.FindAsync(contract.ProjectId);
                if (project != null)
                {
                    project.Status = KitchenProject.Models.Enums.ProjectStatus.Contracted;
                    
                    if (contract.Type == KitchenProject.Models.Enums.ContractType.Final)
                    {
                        project.FinalAmount = contract.TotalAmount;
                    }
                    
                    _context.Projects.Update(project);
                }

                _contract.Entity.Insert(contract);
                await _contract.SaveAsync();

                TempData["Success"] = $"تم إضافة العقد \"{contract.ContractNo}\" بنجاح";
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropdownsAsync(contract.ProjectId, contract.Status, contract.QuoteId);
            return View(contract);
        }

        [HttpGet]
        public async Task<IActionResult> GetApprovedQuotes(Guid projectId)
        {
            var quotes = await _context.Quotes
                .Where(q => q.ProjectId == projectId && q.Status == KitchenProject.Models.Enums.DesignStatus.Approved)
                .OrderByDescending(q => q.Type)
                .Select(q => new
                {
                    id = q.Id,
                    text = q.QuoteNo + (q.Type == KitchenProject.Models.Enums.ContractType.Final ? " (نهائية)" : " (مبدئية)"),
                    total = q.Total
                })
                .ToListAsync();
            
            return Json(quotes);
        }

        // GET: Contracts/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            ViewBag.ActivePage = "Contracts";

            if (id == null) return NotFound();

            var contract = await _contract.Entity.GetByIdAsync(id.Value);
            if (contract == null) return NotFound();

            await PopulateDropdownsAsync(contract.ProjectId, contract.Status, contract.QuoteId);
            return View(contract);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, Contract contract)
        {
            ViewBag.ActivePage = "Contracts";

            if (id != contract.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    if (contract.Type == KitchenProject.Models.Enums.ContractType.Final)
                    {
                        // Ensure TotalAmount is pulled securely from the Approved Quote
                        var quote = await _context.Quotes.FirstOrDefaultAsync(q => q.Id == contract.QuoteId && q.Status == KitchenProject.Models.Enums.DesignStatus.Approved);
                        if (quote == null)
                        {
                            ModelState.AddModelError("QuoteId", "يجب اختيار عرض سعر معتمد صحيح للعقد النهائي");
                            await PopulateDropdownsAsync(contract.ProjectId, contract.Status, contract.QuoteId);
                            return View(contract);
                        }
                        contract.TotalAmount = quote.Total;
                    }
                    else
                    {
                        contract.QuoteId = null;
                    }

                    contract.Modified = DateTime.UtcNow;

                    // Update project status if not Contracted
                    var project = await _context.Projects.FindAsync(contract.ProjectId);
                    if (project != null)
                    {
                        if (project.Status != KitchenProject.Models.Enums.ProjectStatus.Contracted)
                        {
                            project.Status = KitchenProject.Models.Enums.ProjectStatus.Contracted;
                        }
                        
                        if (contract.Type == KitchenProject.Models.Enums.ContractType.Final)
                        {
                            project.FinalAmount = contract.TotalAmount;
                        }
                        
                        _context.Projects.Update(project);
                    }

                    _contract.Entity.Update(contract);
                    await _contract.SaveAsync();

                    TempData["Success"] = $"تم تحديث العقد \"{contract.ContractNo}\" بنجاح";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (await _contract.Entity.GetByIdAsync(contract.Id) == null)
                        return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropdownsAsync(contract.ProjectId, contract.Status, contract.QuoteId);
            return View(contract);
        }

        // POST: Contracts/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var contract = await _contract.Entity.GetByIdAsync(id);
            if (contract != null)
            {
                _contract.Entity.Delete(contract);
                await _contract.SaveAsync();
                TempData["Success"] = $"تم حذف العقد \"{contract.ContractNo}\" بنجاح";
            }
            else
            {
                TempData["Error"] = "تعذر العثور على العقد المراد حذفه";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateDropdownsAsync(Guid? projectId, string? status, Guid? quoteId = null)
        {
            var projects = await _context.Projects
                .Include(p => p.Customer)
                .OrderByDescending(p => p.Created)
                .Select(p => new { Id = p.Id, Name = $"{p.ProjectNo} - {p.ProjectName} ({p.Customer.Name})" })
                .ToListAsync();

            ViewBag.ProjectId = new SelectList(projects, "Id", "Name", projectId);

            // Fetch Approved Quotes for the selected project
            var quotes = await _context.Quotes
                .Where(q => q.ProjectId == projectId && q.Status == KitchenProject.Models.Enums.DesignStatus.Approved)
                .Select(q => new { Id = q.Id, Name = q.QuoteNo })
                .ToListAsync();
            
            ViewBag.QuoteId = new SelectList(quotes, "Id", "Name", quoteId);

            ViewBag.StatusList = new SelectList(new[]
            {
                new { Value = "Draft",     Text = "مسودة" },
                new { Value = "Active",    Text = "ساري" },
                new { Value = "Completed", Text = "مكتمل" },
                new { Value = "Cancelled", Text = "ملغى" }
            }, "Value", "Text", status);

            var types = Enum.GetValues(typeof(KitchenProject.Models.Enums.ContractType))
                .Cast<KitchenProject.Models.Enums.ContractType>()
                .Select(e => new
                {
                    Id = (int)e,
                    Name = e.GetType().GetMember(e.ToString()).First()
                        .GetCustomAttributes(false)
                        .OfType<System.ComponentModel.DataAnnotations.DisplayAttribute>()
                        .LastOrDefault()?.Name ?? e.ToString()
                });
            ViewBag.TypeList = new SelectList(types, "Id", "Name");
        }
    }
}
