using KitchenProject.Classes;
using KitchenProject.Models.Entities;
using KitchenProject.Models.Enums;
using KitchenProject.Models.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace KitchenProject.Controllers
{
	[ViewLayout("_DashboardLayout")]
	//[Authorize(Policy = "ProgOrAdminOrEmployeePolicy")]

	public class ProjectsController : Controller
	{
		private readonly IUnitOfWork<Project> _project;
		private readonly IUnitOfWork<Customer> _customer;
		private readonly IUnitOfWork<ApplicationUser> _user;

		public ProjectsController(IUnitOfWork<Project> project,
								  IUnitOfWork<Customer> customer,
								  IUnitOfWork<ApplicationUser> user)
		{
			_project = project;
			_customer = customer;
			_user = user;
		}

		// GET: Projects
		public async Task<IActionResult> Index(string? searchString, ProjectStatus? status, int page = 1)
		{
			ViewBag.ActivePage = "Projects";
			ViewBag.SearchString = searchString;
			ViewBag.Status = status;

			var query = _project.Entity.Include(p => p.Customer);

			if (!string.IsNullOrWhiteSpace(searchString))
			{
				searchString = searchString.Trim();
				query = query.Where(p => p.ProjectNo.Contains(searchString)
									  || p.ProjectName.Contains(searchString)
									  || p.Customer.Name.Contains(searchString)
									  || p.Customer.Phone.Contains(searchString));
			}

			if (status.HasValue)
			{
				query = query.Where(p => p.Status == status.Value);
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

			// Metrics for Dashboard header cards
			var all = _project.Entity.GetAll();
			ViewBag.TotalProjectsCount = await all.CountAsync();
			ViewBag.ActiveProjectsCount = await all.CountAsync(p => p.Status != ProjectStatus.Delivered
																&& p.Status != ProjectStatus.Closed
																&& p.Status != ProjectStatus.Cancelled);
			ViewBag.InProgressCount = await all.CountAsync(p => p.Status == ProjectStatus.InProgress);
			ViewBag.DeliveredCount = await all.CountAsync(p => p.Status == ProjectStatus.Delivered
														   || p.Status == ProjectStatus.Closed);

			ViewBag.StatusList = new SelectList(
				Enum.GetValues<ProjectStatus>().Select(s => new { Value = s, Text = s.GetDisplayName() }),
				"Value", "Text", status);

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

			var project = await _project.Entity.Include(p => p.Customer,
														p => p.SalesUser!,
														p => p.EngineerUser!,
														p => p.DesignerUser!,
														p => p.SiteVisits,
														p => p.Quotes,
														p => p.Contracts,
														p => p.CustomerPayments,
														p => p.Expenses)
				.AsSplitQuery()
				.FirstOrDefaultAsync(p => p.Id == id);

			if (project == null)
			{
				return NotFound();
			}

			return View(project);
		}

		// GET: Projects/Create?customerId=5
		public async Task<IActionResult> Create(Guid? customerId)
		{
			ViewBag.ActivePage = "Projects";

			var model = new Project
			{
				ProjectNo = await GenerateProjectNoAsync(),
				ProjectName = "",
				Status = ProjectStatus.New,
				CustomerId = customerId ?? Guid.Empty
			};

			if (customerId.HasValue)
			{
				var customer = await _customer.Entity.GetByIdAsync(customerId.Value);
				if (customer != null)
				{
					model.Address = customer.Address;
				}
			}

			await FillSelectListsAsync(model);
			return View(model);
		}

		// POST: Projects/Create
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(Project project)
		{
			ViewBag.ActivePage = "Projects";

			if (project.CustomerId == Guid.Empty)
			{
				ModelState.AddModelError(nameof(Project.CustomerId), "يجب اختيار العميل");
			}

			if (await _project.Entity.GetAll().AnyAsync(p => p.ProjectNo == project.ProjectNo))
			{
				// رقم آخر حجزه مستخدم ثاني أثناء تعبئة النموذج
				project.ProjectNo = await GenerateProjectNoAsync();
			}

			if (ModelState.IsValid)
			{
				project.Id = Guid.NewGuid();
				project.Created = DateTime.UtcNow;
				ApplyStatusDates(project);

				_project.Entity.Insert(project);
				await _project.SaveAsync();

				TempData["Success"] = $"تم إنشاء المشروع \"{project.ProjectName}\" بنجاح برقم {project.ProjectNo}";
				return RedirectToAction(nameof(Details), new { id = project.Id });
			}

			await FillSelectListsAsync(project);
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

			await FillSelectListsAsync(project);
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

			if (project.CustomerId == Guid.Empty)
			{
				ModelState.AddModelError(nameof(Project.CustomerId), "يجب اختيار العميل");
			}

			if (ModelState.IsValid)
			{
				try
				{
					project.Modified = DateTime.UtcNow;
					ApplyStatusDates(project);

					_project.Entity.Update(project);
					await _project.SaveAsync();

					TempData["Success"] = $"تم تحديث بيانات المشروع \"{project.ProjectName}\" بنجاح";
				}
				catch (DbUpdateConcurrencyException)
				{
					var exists = await _project.Entity.GetAll().AnyAsync(p => p.Id == project.Id);
					if (!exists)
					{
						return NotFound();
					}
					else
					{
						throw;
					}
				}
				return RedirectToAction(nameof(Details), new { id = project.Id });
			}

			await FillSelectListsAsync(project);
			return View(project);
		}

		// POST: Projects/Delete/5
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Delete(Guid id)
		{
			var project = await _project.Entity.GetByIdAsync(id);
			if (project == null)
			{
				TempData["Error"] = "تعذر العثور على المشروع المراد حذفه";
				return RedirectToAction(nameof(Index));
			}

			// العقود والدفعات والمخزون مربوطة بـ Restrict، فلا نحذف مشروعاً عليه حركات مالية أو مخزنية
			var hasLinkedRecords = await _project.Entity.GetAll()
				.Where(p => p.Id == id)
				.AnyAsync(p => p.Contracts.Any() || p.CustomerPayments.Any()
							|| p.StockTransactions.Any() || p.Expenses.Any() || p.Quotes.Any());

			if (hasLinkedRecords)
			{
				TempData["Error"] = $"لا يمكن حذف المشروع \"{project.ProjectName}\" لوجود عقود أو دفعات أو عروض أسعار أو حركات مخزنية مرتبطة به. يمكنك تغيير حالته إلى (ملغى) بدلاً من الحذف.";
				return RedirectToAction(nameof(Details), new { id });
			}

			_project.Entity.Delete(project);
			await _project.SaveAsync();
			TempData["Success"] = $"تم حذف المشروع \"{project.ProjectName}\" بنجاح";

			return RedirectToAction(nameof(Index));
		}

		//---------------------------------------------------------------------

		private async Task<string> GenerateProjectNoAsync()
		{
			// أعلى رقم موجود + 1 (وليس العدد + 1) حتى لا يتكرر الرقم بعد الحذف
			var numbers = await _project.Entity.GetAll()
				.Where(p => p.ProjectNo.StartsWith("PRJ-"))
				.Select(p => p.ProjectNo.Substring(4))
				.ToListAsync();

			var max = numbers.Select(n => int.TryParse(n, out var x) ? x : 0)
							 .DefaultIfEmpty(0)
							 .Max();

			return $"PRJ-{(max + 1):D4}";
		}

		private static void ApplyStatusDates(Project project)
		{
			if (project.Status == ProjectStatus.InProgress && project.StartDate == null)
				project.StartDate = DateTime.UtcNow;

			if (project.Status == ProjectStatus.Delivered && project.DeliveredAt == null)
				project.DeliveredAt = DateTime.UtcNow;

			if (project.Status == ProjectStatus.Closed && project.ClosedAt == null)
				project.ClosedAt = DateTime.UtcNow;
		}

		private async Task FillSelectListsAsync(Project project)
		{
			var customers = await _customer.Entity.GetAll()
				.OrderBy(c => c.Name)
				.Select(c => new { c.Id, Text = c.CustomerNo + " - " + c.Name })
				.ToListAsync();
			ViewBag.Customers = new SelectList(customers, "Id", "Text", project.CustomerId);

			var users = await _user.Entity.Include(u => u.UserProfile)
				.OrderBy(u => u.UserName)
				.Select(u => new { u.Id, Text = u.UserProfile != null ? u.UserProfile.DisplayName : u.UserName })
				.ToListAsync();
			ViewBag.SalesUsers = new SelectList(users, "Id", "Text", project.SalesUserId);
			ViewBag.EngineerUsers = new SelectList(users, "Id", "Text", project.EngineerUserId);
			ViewBag.DesignerUsers = new SelectList(users, "Id", "Text", project.DesignerUserId);

			ViewBag.StatusList = new SelectList(
				Enum.GetValues<ProjectStatus>().Select(s => new { Value = s, Text = s.GetDisplayName() }),
				"Value", "Text", project.Status);
		}
	}
}
