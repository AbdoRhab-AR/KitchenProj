using KitchenProject.Classes;
using KitchenProject.Models.Entities;
using KitchenProject.Models.Enums;
using KitchenProject.Models.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;

namespace KitchenProject.Controllers
{
	[ViewLayout("_DashboardLayout")]
	//[Authorize(Policy = "ProgOrAdminOrEmployeePolicy")]

	public class SiteVisitsController : Controller
	{
		private readonly IUnitOfWork<SiteVisit> _visit;
		private readonly IUnitOfWork<Project> _project;
		private readonly LookupService _lookup;
		private readonly IWebHostEnvironment _env;

		public SiteVisitsController(IUnitOfWork<SiteVisit> visit,
									IUnitOfWork<Project> project,
									LookupService lookup,
									IWebHostEnvironment env)
		{
			_visit = visit;
			_project = project;
			_lookup = lookup;
			_env = env;
		}

		// GET: SiteVisits
		public async Task<IActionResult> Index(string? searchString, SiteVisitStatus? status, Guid? projectId, int page = 1)
		{
			ViewBag.ActivePage = "SiteVisits";
			ViewBag.SearchString = searchString;
			ViewBag.Status = status;
			ViewBag.ProjectId = projectId;

			var query = _visit.Entity.Include(v => v.Project, v => v.Project.Customer, v => v.Engineer!, v => v.Engineer!.UserProfile!);

			if (projectId.HasValue)
			{
				query = query.Where(v => v.ProjectId == projectId.Value);
				ViewBag.ProjectName = await _project.Entity.GetWhere(p => p.Id == projectId.Value)
					.Select(p => p.ProjectNo + " - " + p.ProjectName).FirstOrDefaultAsync();
			}

			if (!string.IsNullOrWhiteSpace(searchString))
			{
				if (!SearchValidator.TryValidateNameSearch(searchString, out var validName, out var searchError))
				{
					TempData["Error"] = searchError;
					ViewBag.SearchError = searchError;
					query = query.Where(_ => false);
				}
				else
				{
					query = query.Where(v => v.Project.ProjectName.Contains(validName)
										  || v.Project.Customer.Name.Contains(validName)
										  || (v.Engineer != null && v.Engineer.UserProfile != null && v.Engineer.UserProfile.DisplayName.Contains(validName)));
				}
			}

			if (status.HasValue)
			{
				query = query.Where(v => v.Status == status.Value);
			}

			var totalCount = await query.CountAsync();
			int pageSize = await _lookup.PageSizeAsync();
			var visits = await query.OrderByDescending(v => v.VisitDate)
									.Skip((page - 1) * pageSize)
									.Take(pageSize)
									.ToListAsync();

			ViewBag.CurrentPage = page;
			ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
			ViewBag.TotalCount = totalCount;

			// Metrics for Dashboard header cards
			var today = DateTime.Today;
			var all = _visit.Entity.GetAll();
			ViewBag.TodayCount = await all.CountAsync(v => v.VisitDate >= today && v.VisitDate < today.AddDays(1));
			ViewBag.UpcomingCount = await all.CountAsync(v => v.Status == SiteVisitStatus.Scheduled && v.VisitDate >= today);
			ViewBag.CompletedCount = await all.CountAsync(v => v.Status == SiteVisitStatus.Completed || v.Status == SiteVisitStatus.Approved);

			ViewBag.StatusList = LookupService.EnumList(status);

			return View(visits);
		}

		// GET: SiteVisits/Create?projectId=5
		public async Task<IActionResult> Create(Guid? projectId)
		{
			ViewBag.ActivePage = "SiteVisits";

			var model = new SiteVisit
			{
				ProjectId = projectId ?? Guid.Empty,
				VisitDate = DateTime.Today.AddDays(1).AddHours(10),
				Status = SiteVisitStatus.Scheduled
			};

			if (projectId.HasValue)
			{
				// مهندس المشروع هو الافتراضي للمعاينة إن كان مؤهلاً
				var defaultEngineerId = await _project.Entity.GetWhere(p => p.Id == projectId.Value)
					.Select(p => p.EngineerUserId).FirstOrDefaultAsync();
				if (await _lookup.IsEligibleUserAsync(defaultEngineerId, AppRoles.Engineer, AppRoles.Designer))
				{
					model.EngineerId = defaultEngineerId;
				}
			}

			await FillSelectListsAsync(model);
			return View(model);
		}

		// POST: SiteVisits/Create
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(SiteVisit visit, List<IFormFile>? attachments)
		{
			ViewBag.ActivePage = "SiteVisits";
			await ValidateAsync(visit);

			visit.Id = Guid.NewGuid();
			if (ModelState.IsValid)
			{
				var (_, uploadError) = await FileUploads.SavePicsAsync(attachments, _env, "visit", visit.Id);
				if (uploadError != null)
				{
					ModelState.AddModelError("", uploadError);
				}
			}

			if (ModelState.IsValid)
			{
				visit.Created = DateTime.UtcNow;
				_visit.Entity.Insert(visit);

				await ProjectWorkflow.AdvanceAsync(_project, visit.ProjectId,
					visit.Status == SiteVisitStatus.Approved ? ProjectStatus.Design : ProjectStatus.SiteVisit);

				await _visit.SaveAsync();

				TempData["Success"] = "تم تسجيل موعد المعاينة بنجاح";
				return RedirectToAction("Details", "Projects", new { id = visit.ProjectId });
			}

			await FillSelectListsAsync(visit);
			return View(visit);
		}

		// GET: SiteVisits/Edit/5
		public async Task<IActionResult> Edit(Guid? id)
		{
			ViewBag.ActivePage = "SiteVisits";

			if (id == null)
			{
				return NotFound();
			}

			var visit = await _visit.Entity.GetByIdAsync(id.Value);
			if (visit == null)
			{
				return NotFound();
			}

			ViewBag.Pics = FileUploads.GetPics(_env, "visit", visit.Id);
			await FillSelectListsAsync(visit, includeCurrentSelection: true);
			return View(visit);
		}

		// POST: SiteVisits/Edit/5
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(Guid id, SiteVisit visit, List<IFormFile>? attachments, List<string>? removePics)
		{
			ViewBag.ActivePage = "SiteVisits";

			if (id != visit.Id)
			{
				return NotFound();
			}

			var existing = await _visit.Entity.GetWhere(v => v.Id == id)
				.Select(v => new { v.Created, v.EngineerId })
				.FirstOrDefaultAsync();
			if (existing == null)
			{
				return NotFound();
			}

			await ValidateAsync(visit, existing.EngineerId);

			if (ModelState.IsValid)
			{
				var (_, uploadError) = await FileUploads.SavePicsAsync(attachments, _env, "visit", visit.Id);
				if (uploadError != null)
				{
					ModelState.AddModelError("", uploadError);
				}
			}

			if (ModelState.IsValid)
			{
				if (removePics != null)
				{
					foreach (var pic in removePics)
					{
						FileUploads.DeletePic(pic, _env);
					}
				}

				visit.Created = existing.Created;
				visit.Modified = DateTime.UtcNow;
				_visit.Entity.Update(visit);

				if (visit.Status == SiteVisitStatus.Approved)
				{
					// اعتماد المقاسات ينقل المشروع لمرحلة التصميم
					await ProjectWorkflow.AdvanceAsync(_project, visit.ProjectId, ProjectStatus.Design);
				}

				await _visit.SaveAsync();

				TempData["Success"] = "تم تحديث بيانات المعاينة بنجاح";
				return RedirectToAction("Details", "Projects", new { id = visit.ProjectId });
			}

			ViewBag.Pics = FileUploads.GetPics(_env, "visit", visit.Id);
			await FillSelectListsAsync(visit, includeCurrentSelection: true);
			return View(visit);
		}

		// POST: SiteVisits/Delete/5
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Delete(Guid id)
		{
			var visit = await _visit.Entity.GetByIdAsync(id);
			if (visit == null)
			{
				TempData["Error"] = "تعذر العثور على المعاينة المراد حذفها";
				return RedirectToAction(nameof(Index));
			}

			_visit.Entity.Delete(visit);
			await _visit.SaveAsync();
			FileUploads.DeleteAllPics(_env, "visit", visit.Id);
			TempData["Success"] = "تم حذف المعاينة بنجاح";

			return RedirectToAction("Details", "Projects", new { id = visit.ProjectId });
		}

		//---------------------------------------------------------------------

		private async Task ValidateAsync(SiteVisit visit, string? existingEngineerId = null)
		{
			if (visit.ProjectId == Guid.Empty)
				ModelState.AddModelError(nameof(SiteVisit.ProjectId), "يجب اختيار المشروع");

			visit.EngineerId = string.IsNullOrWhiteSpace(visit.EngineerId) ? null : visit.EngineerId;

			if (visit.EngineerId != null
				&& visit.EngineerId != existingEngineerId
				&& !await _lookup.IsEligibleUserAsync(visit.EngineerId, AppRoles.Engineer, AppRoles.Designer))
			{
				ModelState.AddModelError(nameof(SiteVisit.EngineerId), "المستخدم المختار غير فعال أو لا يملك صلاحية مهندس أو مصمم");
			}
		}

		private async Task FillSelectListsAsync(SiteVisit visit, bool includeCurrentSelection = false)
		{
			ViewBag.Projects = await _lookup.ProjectsAsync(visit.ProjectId);
			ViewBag.Engineers = await _lookup.UsersAsync(
				visit.EngineerId,
				includeCurrentSelection && ModelState.GetFieldValidationState(nameof(SiteVisit.EngineerId)) != ModelValidationState.Invalid,
				AppRoles.Engineer, AppRoles.Designer);
			ViewBag.StatusList = LookupService.EnumList(visit.Status);
		}
	}
}
