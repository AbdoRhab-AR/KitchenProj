using KitchenProject.Classes;
using KitchenProject.Models.Entities;
using KitchenProject.Models.Enums;
using KitchenProject.Models.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KitchenProject.Controllers
{
	[ViewLayout("_DashboardLayout")]

	// المصروفات: مصروف على مشروع يدخل في تكلفته وربحه، ومصروف عام (بدون مشروع) يُخصم من صافي ربح الشركة
	public class ExpensesController : Controller
	{
		private readonly IUnitOfWork<Expense> _expense;
		private readonly IUnitOfWork<Project> _project;
		private readonly LookupService _lookup;
		private readonly IWebHostEnvironment _env;

		public ExpensesController(IUnitOfWork<Expense> expense,
								  IUnitOfWork<Project> project,
								  LookupService lookup,
								  IWebHostEnvironment env)
		{
			_expense = expense;
			_project = project;
			_lookup = lookup;
			_env = env;
		}

		// GET: Expenses   scope: project = على المشاريع، general = عامة
		public async Task<IActionResult> Index(string? searchString, string? scope, Guid? projectId, string? type, DateTime? from, DateTime? to, int page = 1)
		{
			ViewBag.ActivePage = "Expenses";
			ViewBag.SearchString = searchString;
			ViewBag.Scope = scope;
			ViewBag.ProjectId = projectId;
			ViewBag.Type = type;
			ViewBag.From = from;
			ViewBag.To = to;

			var query = _expense.Entity.Include(e => e.Project!);

			if (!string.IsNullOrWhiteSpace(searchString))
			{
				searchString = searchString.Trim();
				query = query.Where(e => (e.Description != null && e.Description.Contains(searchString))
									  || e.ExpenseType.Contains(searchString)
									  || (e.Project != null && (e.Project.ProjectNo.Contains(searchString) || e.Project.ProjectName.Contains(searchString))));
			}

			if (scope == "project") query = query.Where(e => e.ProjectId != null);
			if (scope == "general") query = query.Where(e => e.ProjectId == null);
			if (projectId.HasValue) query = query.Where(e => e.ProjectId == projectId.Value);
			if (!string.IsNullOrEmpty(type)) query = query.Where(e => e.ExpenseType == type);
			if (from.HasValue) query = query.Where(e => e.ExpenseDate >= from.Value.Date);
			if (to.HasValue) query = query.Where(e => e.ExpenseDate <= to.Value.Date);

			var totalCount = await query.CountAsync();
			ViewBag.FilteredTotal = await query.SumAsync(e => (decimal?)e.Amount) ?? 0;

			int pageSize = 15;
			var expenses = await query.OrderByDescending(e => e.ExpenseDate).ThenByDescending(e => e.Created)
									  .Skip((page - 1) * pageSize)
									  .Take(pageSize)
									  .ToListAsync();

			ViewBag.CurrentPage = page;
			ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
			ViewBag.TotalCount = totalCount;

			var monthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
			var month = _expense.Entity.GetWhere(e => e.ExpenseDate >= monthStart);
			ViewBag.MonthTotal = await month.SumAsync(e => (decimal?)e.Amount) ?? 0;
			ViewBag.MonthProjects = await month.Where(e => e.ProjectId != null).SumAsync(e => (decimal?)e.Amount) ?? 0;
			ViewBag.MonthGeneral = await month.Where(e => e.ProjectId == null).SumAsync(e => (decimal?)e.Amount) ?? 0;

			ViewBag.Projects = await _lookup.ProjectsAsync(projectId);
			ViewBag.Types = await TypesAsync();
			return View(expenses);
		}

		// GET: Expenses/Create?projectId=5
		public async Task<IActionResult> Create(Guid? projectId)
		{
			ViewBag.ActivePage = "Expenses";
			var model = new Expense { ProjectId = projectId };
			await FillSelectListsAsync(model);
			return View(model);
		}

		// POST: Expenses/Create
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(Expense expense, string? returnTo)
		{
			ViewBag.ActivePage = "Expenses";

			if (ModelState.IsValid && expense.Attachment != null)
			{
				var (url, error) = await FileUploads.SaveAsync(expense.Attachment, _env, "expenses", FileUploads.Documents, maxMb: 10);
				if (error != null) ModelState.AddModelError(nameof(expense.Attachment), error);
				else expense.AttachmentPath = url;
			}

			if (ModelState.IsValid)
			{
				expense.Id = Guid.NewGuid();
				expense.Created = DateTime.UtcNow;
				expense.ExpenseType = expense.ExpenseType.Trim();
				_expense.Entity.Insert(expense);
				await _expense.SaveAsync();

				TempData["Success"] = $"تم تسجيل مصروف ({expense.ExpenseType}) بمبلغ {expense.Amount:N2}";
				if (returnTo == "project" && expense.ProjectId.HasValue)
				{
					return RedirectToAction("Details", "Projects", new { id = expense.ProjectId });
				}
				return RedirectToAction(nameof(Index));
			}

			await FillSelectListsAsync(expense);
			return View(expense);
		}

		// GET: Expenses/Edit/5
		public async Task<IActionResult> Edit(Guid? id)
		{
			ViewBag.ActivePage = "Expenses";

			var expense = id.HasValue ? await _expense.Entity.GetWhere(e => e.Id == id.Value).FirstOrDefaultAsync() : null;
			if (expense == null)
			{
				return NotFound();
			}

			await FillSelectListsAsync(expense);
			return View(expense);
		}

		// POST: Expenses/Edit/5
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(Guid id, Expense model, bool removeAttachment = false)
		{
			ViewBag.ActivePage = "Expenses";

			var expense = await _expense.Entity.GetByIdAsync(id);
			if (expense == null)
			{
				return NotFound();
			}

			string? oldFile = null;
			if (ModelState.IsValid && model.Attachment != null)
			{
				var (url, error) = await FileUploads.SaveAsync(model.Attachment, _env, "expenses", FileUploads.Documents, maxMb: 10);
				if (error != null) ModelState.AddModelError(nameof(model.Attachment), error);
				else { oldFile = expense.AttachmentPath; expense.AttachmentPath = url; }
			}
			else if (removeAttachment)
			{
				oldFile = expense.AttachmentPath;
				expense.AttachmentPath = null;
			}

			if (!ModelState.IsValid)
			{
				model.Id = id;
				model.AttachmentPath = expense.AttachmentPath;
				await FillSelectListsAsync(model);
				return View(model);
			}

			expense.ProjectId = model.ProjectId;
			expense.ExpenseType = model.ExpenseType.Trim();
			expense.Amount = model.Amount;
			expense.ExpenseDate = model.ExpenseDate;
			expense.Payment = model.Payment;
			expense.Description = model.Description;
			expense.Notes = model.Notes;
			expense.Modified = DateTime.UtcNow;
			await _expense.SaveAsync();

			FileUploads.Delete(oldFile, _env);

			TempData["Success"] = "تم تحديث المصروف";
			return RedirectToAction(nameof(Index));
		}

		// POST: Expenses/Delete/5
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Delete(Guid id, string? returnUrl)
		{
			var expense = await _expense.Entity.GetByIdAsync(id);
			if (expense == null)
			{
				TempData["Error"] = "تعذر العثور على المصروف المراد حذفه";
			}
			else
			{
				_expense.Entity.Delete(expense);
				await _expense.SaveAsync();
				FileUploads.Delete(expense.AttachmentPath, _env);
				TempData["Success"] = $"تم حذف المصروف ({expense.ExpenseType}) بمبلغ {expense.Amount:N2}";
			}

			if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
			{
				return Redirect(returnUrl);
			}
			return RedirectToAction(nameof(Index));
		}

		//---------------------------------------------------------------------

		// الأنواع المقترحة + الأنواع التي استُخدمت سابقاً
		private async Task<List<string>> TypesAsync()
		{
			var used = await _expense.Entity.GetAll().Select(e => e.ExpenseType).Distinct().ToListAsync();
			return Expense.SuggestedTypes.Union(used).ToList();
		}

		private async Task FillSelectListsAsync(Expense model)
		{
			ViewBag.Projects = await _lookup.ProjectsAsync(model.ProjectId);
			ViewBag.Types = await TypesAsync();
			ViewBag.MethodList = LookupService.EnumList(model.Payment);
		}
	}
}
