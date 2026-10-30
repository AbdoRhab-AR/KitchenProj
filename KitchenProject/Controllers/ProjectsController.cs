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
		private readonly IUnitOfWork<Purchase> _purchase;
		private readonly IUnitOfWork<StockTransaction> _tx;
		private readonly LookupService _lookup;

		public ProjectsController(IUnitOfWork<Project> project,
								  IUnitOfWork<Customer> customer,
								  IUnitOfWork<Purchase> purchase,
								  IUnitOfWork<StockTransaction> tx,
								  LookupService lookup)
		{
			_project = project;
			_customer = customer;
			_purchase = purchase;
			_tx = tx;
			_lookup = lookup;
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

			ViewBag.StatusList = LookupService.EnumList(status);

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

			// المواد المصروفة للمشروع (صرف - مرتجع) ومشترياته المباشرة
			ViewBag.Materials = await _tx.Entity.Include(t => t.Items)
				.Where(t => t.ProjectId == project.Id)
				.OrderByDescending(t => t.TransactionDate).ThenByDescending(t => t.Created)
				.ToListAsync();

			ViewBag.Purchases = await _purchase.Entity.Include(p => p.Suppliers)
				.Where(p => p.ProjectId == project.Id && p.Status != PurchaseStatuses.Cancelled)
				.OrderByDescending(p => p.PurchaseDate)
				.ToListAsync();

			return View(project);
		}

		// GET: Projects/SpecSheet/5
		// ورقة المواصفات التي يوقعها المصمم والعميل (المقاسات + المواصفات المتفق عليها)
		public async Task<IActionResult> SpecSheet(Guid? id)
		{
			if (id == null)
			{
				return NotFound();
			}

			var project = await _project.Entity.Include(p => p.Customer,
														p => p.DesignerUser!,
														p => p.EngineerUser!,
														p => p.SiteVisits,
														p => p.Contracts)
				.AsSplitQuery()
				.FirstOrDefaultAsync(p => p.Id == id);

			if (project == null)
			{
				return NotFound();
			}

			// المعاينة المعتمدة أولاً، ثم الأحدث غير الملغاة
			ViewBag.Visit = project.SiteVisits
				.Where(v => v.Status != SiteVisitStatus.Cancelled)
				.OrderByDescending(v => v.Status == SiteVisitStatus.Approved)
				.ThenByDescending(v => v.VisitDate)
				.FirstOrDefault();

			ViewBag.Specifications = project.Contracts
				.Where(c => c.Status != ContractStatuses.Cancelled && !string.IsNullOrWhiteSpace(c.Specifications))
				.OrderByDescending(c => c.Created)
				.Select(c => c.Specifications)
				.FirstOrDefault();

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
			hasLinkedRecords = hasLinkedRecords || await _purchase.Entity.GetAll().AnyAsync(p => p.ProjectId == id);

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

		private Task<string> GenerateProjectNoAsync() =>
			DocumentNumbers.NextAsync(_project.Entity.GetAll().Select(p => p.ProjectNo), "PRJ-");

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

			ViewBag.SalesUsers = await _lookup.UsersAsync(project.SalesUserId);
			ViewBag.EngineerUsers = await _lookup.UsersAsync(project.EngineerUserId);
			ViewBag.DesignerUsers = await _lookup.UsersAsync(project.DesignerUserId);
			ViewBag.StatusList = LookupService.EnumList<ProjectStatus>(project.Status);
		}
	}
}
