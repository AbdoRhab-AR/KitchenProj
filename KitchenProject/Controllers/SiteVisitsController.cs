using KitchenProject.Classes;
using KitchenProject.Models.Entities;
using KitchenProject.Models.Enums;
using KitchenProject.Models.Interfaces;
using Microsoft.AspNetCore.Mvc;
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

		public SiteVisitsController(IUnitOfWork<SiteVisit> visit,
									IUnitOfWork<Project> project,
									LookupService lookup)
		{
			_visit = visit;
			_project = project;
			_lookup = lookup;
		}

		// GET: SiteVisits
		public async Task<IActionResult> Index(string? searchString, SiteVisitStatus? status, Guid? projectId, int page = 1)
		{
			ViewBag.ActivePage = "SiteVisits";
			ViewBag.SearchString = searchString;
			ViewBag.Status = status;
			ViewBag.ProjectId = projectId;

			var query = _visit.Entity.Include(v => v.Project, v => v.Project.Customer, v => v.Engineer);

			if (projectId.HasValue)
			{
				query = query.Where(v => v.ProjectId == projectId.Value);
				ViewBag.ProjectName = await _project.Entity.GetWhere(p => p.Id == projectId.Value)
					.Select(p => p.ProjectNo + " - " + p.ProjectName).FirstOrDefaultAsync();
			}

			if (!string.IsNullOrWhiteSpace(searchString))
			{
				searchString = searchString.Trim();
				query = query.Where(v => v.Project.ProjectNo.Contains(searchString)
									  || v.Project.ProjectName.Contains(searchString)
									  || v.Project.Customer.Name.Contains(searchString));
			}

			if (status.HasValue)
			{
				query = query.Where(v => v.Status == status.Value);
			}

			var totalCount = await query.CountAsync();
			int pageSize = 10;
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
				EngineerId = "",
				VisitDate = DateTime.Today.AddDays(1).AddHours(10),
				Status = SiteVisitStatus.Scheduled
			};

			if (projectId.HasValue)
			{
				// مهندس المشروع هو الافتراضي للمعاينة
				model.EngineerId = await _project.Entity.GetWhere(p => p.Id == projectId.Value)
					.Select(p => p.EngineerUserId).FirstOrDefaultAsync() ?? "";
			}

			await FillSelectListsAsync(model);
			return View(model);
		}

		// POST: SiteVisits/Create
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(SiteVisit visit)
		{
			ViewBag.ActivePage = "SiteVisits";
			Validate(visit);

			if (ModelState.IsValid)
			{
				visit.Id = Guid.NewGuid();
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

			await FillSelectListsAsync(visit);
			return View(visit);
		}

		// POST: SiteVisits/Edit/5
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(Guid id, SiteVisit visit)
		{
			ViewBag.ActivePage = "SiteVisits";

			if (id != visit.Id)
			{
				return NotFound();
			}

			Validate(visit);

			if (ModelState.IsValid)
			{
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

			await FillSelectListsAsync(visit);
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
			TempData["Success"] = "تم حذف المعاينة بنجاح";

			return RedirectToAction("Details", "Projects", new { id = visit.ProjectId });
		}

		//---------------------------------------------------------------------

		private void Validate(SiteVisit visit)
		{
			if (visit.ProjectId == Guid.Empty)
				ModelState.AddModelError(nameof(SiteVisit.ProjectId), "يجب اختيار المشروع");

			if (string.IsNullOrEmpty(visit.EngineerId))
			{
				ModelState.Remove(nameof(SiteVisit.EngineerId));
				ModelState.AddModelError(nameof(SiteVisit.EngineerId), "يجب اختيار مهندس المعاينة");
			}
		}

		private async Task FillSelectListsAsync(SiteVisit visit)
		{
			ViewBag.Projects = await _lookup.ProjectsAsync(visit.ProjectId);
			ViewBag.Engineers = await _lookup.UsersAsync(visit.EngineerId);
			ViewBag.StatusList = LookupService.EnumList(visit.Status);
		}
	}
}
