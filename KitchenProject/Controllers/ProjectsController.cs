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
    public class ProjectsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IUnitOfWork<Project> _project;
        private readonly IUnitOfWork<Customer> _customer;

        public ProjectsController(
            ApplicationDbContext context,
            IUnitOfWork<Project> project,
            IUnitOfWork<Customer> customer)
        {
            _context = context;
            _project = project;
            _customer = customer;
        }

        // GET: Projects
        public async Task<IActionResult> Index(string? searchString, ProjectStatus? statusFilter, Guid? customerFilter, int page = 1)
        {
            ViewBag.ActivePage = "Projects";
            ViewBag.SearchString = searchString;
            ViewBag.StatusFilter = statusFilter;
            ViewBag.CustomerFilter = customerFilter;

            var query = _project.Entity
                .Include(p => p.Customer)
                .Include(p => p.SalesUser).ThenInclude(u => u.UserProfile)
                .Include(p => p.EngineerUser).ThenInclude(u => u.UserProfile)
                .Include(p => p.DesignerUser).ThenInclude(u => u.UserProfile)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                searchString = searchString.Trim();
                query = query.Where(p => p.ProjectName.Contains(searchString)
                                      || p.ProjectNo.Contains(searchString)
                                      || p.Customer.Name.Contains(searchString)
                                      || p.Customer.Phone.Contains(searchString));
            }

            if (statusFilter.HasValue)
            {
                query = query.Where(p => p.Status == statusFilter.Value);
            }

            if (customerFilter.HasValue)
            {
                query = query.Where(p => p.CustomerId == customerFilter.Value);
            }

            var totalCount = await query.CountAsync();
            int pageSize = 10;
            var projects = await query.OrderByDescending(p => p.Created)
                                      .Skip((page - 1) * pageSize)
                                      .Take(pageSize)
                                      .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            ViewBag.TotalCount = totalCount;

            // KPI Metrics for Cards
            var allProjects = _project.Entity.GetAll();
            ViewBag.TotalProjectsCount = await allProjects.CountAsync();
            ViewBag.InProgressCount = await allProjects.Where(p => p.Status == ProjectStatus.InProgress || p.Status == ProjectStatus.Contracted || p.Status == ProjectStatus.Design || p.Status == ProjectStatus.SiteVisit).CountAsync();
            ViewBag.CompletedCount = await allProjects.Where(p => p.Status == ProjectStatus.Delivered || p.Status == ProjectStatus.Closed).CountAsync();
            ViewBag.TotalEstimatedAmount = await allProjects.SumAsync(p => p.EstimatedAmount);
            ViewBag.TotalFinalAmount = await allProjects.SumAsync(p => p.FinalAmount);

            // Customers dropdown for quick filtering
            var customers = await _customer.Entity.GetAll().OrderBy(c => c.Name).ToListAsync();
            ViewBag.CustomersList = new SelectList(customers, "Id", "Name", customerFilter);

            return View(projects);
        }

        // GET: Projects/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            ViewBag.ActivePage = "Projects";

            if (id == null)
            {
                return NotFound();
            }

            var project = await _project.Entity
                .Include(p => p.Customer)
                .Include(p => p.SalesUser).ThenInclude(u => u.UserProfile)
                .Include(p => p.EngineerUser).ThenInclude(u => u.UserProfile)
                .Include(p => p.DesignerUser).ThenInclude(u => u.UserProfile)
                .Include(p => p.SiteVisits)
                .Include(p => p.Quotes)
                .Include(p => p.Contracts)
                .Include(p => p.CustomerPayments)
                .Include(p => p.Expenses)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null)
            {
                return NotFound();
            }

            return View(project);
        }

        // GET: Projects/Create
        public async Task<IActionResult> Create(Guid? customerId)
        {
            ViewBag.ActivePage = "Projects";

            // Auto-generate project number
            var count = await _project.Entity.GetAll().CountAsync();
            var nextNo = $"PRJ-{(count + 1):D4}";

            var model = new Project
            {
                ProjectNo = nextNo,
                ProjectName = "",
                CustomerId = customerId ?? Guid.Empty,
                Status = ProjectStatus.New,
                StartDate = DateTime.Now
            };

            await PopulateDropdownsAsync(customerId, null, null, null);
            return View(model);
        }

        // POST: Projects/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Project project)
        {
            ViewBag.ActivePage = "Projects";

            if (ModelState.IsValid)
            {
                project.Id = Guid.NewGuid();
                project.Created = DateTime.UtcNow;

                _project.Entity.Insert(project);
                await _project.SaveAsync();

                TempData["Success"] = $"تم إضافة المشروع \"{project.ProjectName}\" بنجاح برقم {project.ProjectNo}";
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropdownsAsync(project.CustomerId, project.SalesUserId, project.EngineerUserId, project.DesignerUserId);
            return View(project);
        }

        // GET: Projects/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            ViewBag.ActivePage = "Projects";

            if (id == null)
            {
                return NotFound();
            }

            var project = await _project.Entity.GetByIdAsync(id.Value);
            if (project == null)
            {
                return NotFound();
            }

            await PopulateDropdownsAsync(project.CustomerId, project.SalesUserId, project.EngineerUserId, project.DesignerUserId);
            return View(project);
        }

        // POST: Projects/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, Project project)
        {
            ViewBag.ActivePage = "Projects";

            if (id != project.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    project.Modified = DateTime.UtcNow;
                    _project.Entity.Update(project);
                    await _project.SaveAsync();

                    TempData["Success"] = $"تم تحديث بيانات المشروع \"{project.ProjectName}\" بنجاح";
                }
                catch (DbUpdateConcurrencyException)
                {
                    var exists = await _project.Entity.GetByIdAsync(project.Id) != null;
                    if (!exists)
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropdownsAsync(project.CustomerId, project.SalesUserId, project.EngineerUserId, project.DesignerUserId);
            return View(project);
        }

        // POST: Projects/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var project = await _project.Entity.GetByIdAsync(id);
            if (project != null)
            {
                try
                {
                    _project.Entity.Delete(project);
                    await _project.SaveAsync();
                    TempData["Success"] = $"تم حذف المشروع \"{project.ProjectName}\" بنجاح";
                }
                catch (DbUpdateException)
                {
                    TempData["Error"] = "لا يمكن حذف المشروع لوجود كيانات أو فواتير مالية مرتبطة به. يرجى أرشفته من حالة المشروع بدلاً من حذفه.";
                }
            }
            else
            {
                TempData["Error"] = "تعذر العثور على المشروع المراد حذفه";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeliverProject(Guid id)
        {
            var project = await _project.Entity.GetAll()
                .Include(p => p.CustomerPayments)
                .Include(p => p.Contracts)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null) return NotFound();

            var totalPaid = project.CustomerPayments.Sum(p => p.Amount);
            var finalContract = project.Contracts.OrderByDescending(c => c.Created)
                .FirstOrDefault(c => c.Type == KitchenProject.Models.Enums.ContractType.Final);
            
            var requiredAmount = finalContract != null ? finalContract.TotalAmount : project.FinalAmount;
            var remaining = requiredAmount - totalPaid;

            if (remaining > 0)
            {
                TempData["Error"] = $"لا يمكن تسليم المشروع. يرجى سداد الدفعة النهائية المتبقية بقيمة {remaining.ToString("N2")} د.ل أولاً.";
                return RedirectToAction("Create", "CustomerPayments", new { projectId = id, amount = remaining });
            }

            project.Status = KitchenProject.Models.Enums.ProjectStatus.Delivered;
            project.DeliveredAt = DateTime.UtcNow;
            project.Modified = DateTime.UtcNow;

            _project.Entity.Update(project);
            await _project.SaveAsync();

            TempData["Success"] = $"تم تسليم المشروع \"{project.ProjectName}\" وتسكير الحساب المالي بنجاح.";
            return RedirectToAction(nameof(Details), new { id = id });
        }

        private async Task PopulateDropdownsAsync(Guid? customerId, string? salesUserId, string? engineerUserId, string? designerUserId)
        {
            var customers = await _customer.Entity.GetAll().OrderBy(c => c.Name).ToListAsync();
            ViewBag.CustomerId = new SelectList(customers, "Id", "Name", customerId);

            var users = await _context.Set<ApplicationUser>()
                .Include(u => u.UserProfile)
                .Select(u => new
                {
                    Id = u.Id,
                    Name = u.UserProfile != null && !string.IsNullOrEmpty(u.UserProfile.DisplayName)
                        ? u.UserProfile.DisplayName
                        : (u.UserName ?? u.Email ?? "مستخدم")
                })
                .ToListAsync();

            ViewBag.SalesUserId = new SelectList(users, "Id", "Name", salesUserId);
            ViewBag.EngineerUserId = new SelectList(users, "Id", "Name", engineerUserId);
            ViewBag.DesignerUserId = new SelectList(users, "Id", "Name", designerUserId);
        }
    }
}
