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
    public class SiteVisitsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IUnitOfWork<SiteVisit> _siteVisit;
        private readonly IUnitOfWork<Project> _project;
        private readonly IUnitOfWork<MeasurementItem> _measurement;

        public SiteVisitsController(
            ApplicationDbContext context,
            IUnitOfWork<SiteVisit> siteVisit,
            IUnitOfWork<Project> project,
            IUnitOfWork<MeasurementItem> measurement)
        {
            _context = context;
            _siteVisit = siteVisit;
            _project = project;
            _measurement = measurement;
        }

        // ─── INDEX ────────────────────────────────────────────────────────────
        public async Task<IActionResult> Index(string? searchString, SiteVisitStatus? statusFilter, Guid? projectFilter, int page = 1)
        {
            ViewBag.ActivePage = "SiteVisits";
            ViewBag.SearchString = searchString;
            ViewBag.StatusFilter = statusFilter;
            ViewBag.ProjectFilter = projectFilter;

            var query = _context.SiteVisits
                .Include(v => v.Project).ThenInclude(p => p.Customer)
                .Include(v => v.Engineer).ThenInclude(u => u.UserProfile)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                searchString = searchString.Trim();
                query = query.Where(v =>
                    v.Project.ProjectName.Contains(searchString) ||
                    v.Project.ProjectNo.Contains(searchString) ||
                    v.Project.Customer.Name.Contains(searchString) ||
                    (v.Engineer.UserProfile != null && v.Engineer.UserProfile.DisplayName.Contains(searchString)) ||
                    v.Engineer.UserName.Contains(searchString));
            }

            if (statusFilter.HasValue)
                query = query.Where(v => v.Status == statusFilter.Value);

            if (projectFilter.HasValue)
                query = query.Where(v => v.ProjectId == projectFilter.Value);

            var totalCount = await query.CountAsync();
            int pageSize = 10;

            var visits = await query
                .OrderByDescending(v => v.VisitDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            ViewBag.TotalCount = totalCount;

            // KPI
            ViewBag.TotalVisitsCount = await _context.SiteVisits.CountAsync();
            ViewBag.CompletedCount = await _context.SiteVisits
                .CountAsync(v => v.Status == SiteVisitStatus.Completed || v.Status == SiteVisitStatus.Approved);
            ViewBag.ScheduledCount = await _context.SiteVisits
                .CountAsync(v => v.Status == SiteVisitStatus.Scheduled);

            var projects = await _context.Projects
                .Include(p => p.Customer)
                .OrderByDescending(p => p.Created)
                .Select(p => new { Id = p.Id, Name = $"{p.ProjectNo} - {p.ProjectName}" })
                .ToListAsync();
            ViewBag.ProjectsList = new SelectList(projects, "Id", "Name", projectFilter);

            return View(visits);
        }

        // ─── DETAILS ──────────────────────────────────────────────────────────
        public async Task<IActionResult> Details(Guid? id)
        {
            ViewBag.ActivePage = "SiteVisits";

            if (id == null) return NotFound();

            var visit = await _context.SiteVisits
                .Include(v => v.Project).ThenInclude(p => p.Customer)
                .Include(v => v.Engineer).ThenInclude(u => u.UserProfile)
                .Include(v => v.MeasurementItems.OrderBy(m => m.Created))
                .FirstOrDefaultAsync(v => v.Id == id);

            if (visit == null) return NotFound();

            return View(visit);
        }

        // ─── CREATE ───────────────────────────────────────────────────────────
        public async Task<IActionResult> Create(Guid? projectId)
        {
            ViewBag.ActivePage = "SiteVisits";

            var model = new SiteVisit
            {
                ProjectId = projectId ?? Guid.Empty,
                VisitDate = DateTime.Now,
                Status = SiteVisitStatus.Scheduled
            };

            await PopulateDropdownsAsync(projectId, null);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SiteVisit siteVisit)
        {
            ViewBag.ActivePage = "SiteVisits";

            if (ModelState.IsValid)
            {
                siteVisit.Id = Guid.NewGuid();
                siteVisit.Created = DateTime.UtcNow;
                
                _siteVisit.Entity.Insert(siteVisit);
                await _siteVisit.SaveAsync();

                // حفظ المقاسات التفصيلية المرسلة مع النموذج
                var areaNames = Request.Form["AreaNames"].ToArray();
                var lengths   = Request.Form["Lengths"].ToArray();
                var widths    = Request.Form["Widths"].ToArray();
                var heights   = Request.Form["Heights"].ToArray();
                var mNotes    = Request.Form["MNotes"].ToArray();

                for (int i = 0; i < areaNames.Length; i++)
                {
                    var name = areaNames[i]?.Trim();
                    if (string.IsNullOrEmpty(name)) continue;

                    var item = new MeasurementItem
                    {
                        Id          = Guid.NewGuid(),
                        SiteVisitId = siteVisit.Id,
                        AreaName    = name,
                        Created     = DateTime.UtcNow,
                        Length      = decimal.TryParse(lengths.ElementAtOrDefault(i), out var l) ? l : null,
                        Width       = decimal.TryParse(widths.ElementAtOrDefault(i), out var w) ? w : null,
                        Height      = decimal.TryParse(heights.ElementAtOrDefault(i), out var h) ? h : null,
                        Notes       = mNotes.ElementAtOrDefault(i)?.Trim()
                    };

                    _measurement.Entity.Insert(item);
                }

                if (areaNames.Any(n => !string.IsNullOrWhiteSpace(n)))
                    await _measurement.SaveAsync();

                // Update Project Status
                var project = await _context.Projects.FindAsync(siteVisit.ProjectId);
                if (project != null && project.Status == KitchenProject.Models.Enums.ProjectStatus.New)
                {
                    project.Status = KitchenProject.Models.Enums.ProjectStatus.SiteVisit;
                    _context.Projects.Update(project);
                    await _context.SaveChangesAsync();
                }

                TempData["Success"] = "تم إضافة المعاينة بنجاح";
                return RedirectToAction(nameof(Details), new { id = siteVisit.Id });
            }

            await PopulateDropdownsAsync(siteVisit.ProjectId, siteVisit.EngineerId);
            return View(siteVisit);
        }

        // ─── EDIT ─────────────────────────────────────────────────────────────
        public async Task<IActionResult> Edit(Guid? id)
        {
            ViewBag.ActivePage = "SiteVisits";

            if (id == null) return NotFound();

            var visit = await _siteVisit.Entity.GetByIdAsync(id.Value);
            if (visit == null) return NotFound();

            await PopulateDropdownsAsync(visit.ProjectId, visit.EngineerId);
            return View(visit);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, SiteVisit siteVisit)
        {
            ViewBag.ActivePage = "SiteVisits";

            if (id != siteVisit.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    siteVisit.Modified = DateTime.UtcNow;
                    _siteVisit.Entity.Update(siteVisit);
                    await _siteVisit.SaveAsync();

                    TempData["Success"] = "تم تحديث بيانات المعاينة بنجاح";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (await _siteVisit.Entity.GetByIdAsync(siteVisit.Id) == null)
                        return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Details), new { id = siteVisit.Id });
            }

            await PopulateDropdownsAsync(siteVisit.ProjectId, siteVisit.EngineerId);
            return View(siteVisit);
        }

        // ─── DELETE ───────────────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var visit = await _siteVisit.Entity.GetByIdAsync(id);
            if (visit != null)
            {
                _siteVisit.Entity.Delete(visit);
                await _siteVisit.SaveAsync();
                TempData["Success"] = "تم حذف المعاينة بنجاح";
            }
            else
            {
                TempData["Error"] = "تعذر العثور على المعاينة المراد حذفها";
            }

            return RedirectToAction(nameof(Index));
        }

        // ─── ADD MEASUREMENT (POST — AJAX/Form) ───────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMeasurement(MeasurementItem item)
        {
            // تحقق أن الزيارة موجودة
            var visitExists = await _context.SiteVisits.AnyAsync(v => v.Id == item.SiteVisitId);
            if (!visitExists) return NotFound();

            if (ModelState.IsValid)
            {
                item.Id = Guid.NewGuid();
                item.Created = DateTime.UtcNow;

                _measurement.Entity.Insert(item);
                await _measurement.SaveAsync();

                TempData["Success"] = $"تم إضافة مقاس \"{item.AreaName}\" بنجاح";
            }
            else
            {
                TempData["Error"] = "تحقق من بيانات المقاس — اسم المنطقة مطلوب";
            }

            return RedirectToAction(nameof(Details), new { id = item.SiteVisitId });
        }

        // ─── DELETE MEASUREMENT ───────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMeasurement(Guid id, Guid siteVisitId)
        {
            var item = await _measurement.Entity.GetByIdAsync(id);
            if (item != null)
            {
                _measurement.Entity.Delete(item);
                await _measurement.SaveAsync();
                TempData["Success"] = "تم حذف المقاس بنجاح";
            }

            return RedirectToAction(nameof(Details), new { id = siteVisitId });
        }

        // ─── HELPER ───────────────────────────────────────────────────────────
        private async Task PopulateDropdownsAsync(Guid? projectId, string? engineerId)
        {
            var projects = await _context.Projects
                .Include(p => p.Customer)
                .OrderByDescending(p => p.Created)
                .Select(p => new { Id = p.Id, Name = $"{p.ProjectNo} - {p.ProjectName} ({p.Customer.Name})" })
                .ToListAsync();

            ViewBag.ProjectId = new SelectList(projects, "Id", "Name", projectId);

            var engineers = await _context.Set<ApplicationUser>()
                .Include(u => u.UserProfile)
                .Select(u => new
                {
                    Id = u.Id,
                    Name = u.UserProfile != null && !string.IsNullOrEmpty(u.UserProfile.DisplayName)
                        ? u.UserProfile.DisplayName
                        : (u.UserName ?? u.Email ?? "مستخدم")
                })
                .ToListAsync();

            ViewBag.EngineerId = new SelectList(engineers, "Id", "Name", engineerId);
        }
    }
}
