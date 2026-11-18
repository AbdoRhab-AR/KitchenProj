using System.Security.Claims;
using KitchenProject.Classes;
using KitchenProject.Models.Entities;
using KitchenProject.Models.Enums;
using KitchenProject.Models.Interfaces;
using KitchenProject.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
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
		private readonly IUnitOfWork<MaterialRequest> _materialRequest;
		private readonly IUnitOfWork<StockTransaction> _tx;
		private readonly IUnitOfWork<Expense> _expense;
		private readonly LookupService _lookup;
		private readonly IWebHostEnvironment _env;

		public ProjectsController(IUnitOfWork<Project> project,
								  IUnitOfWork<Customer> customer,
								  IUnitOfWork<Purchase> purchase,
								  IUnitOfWork<MaterialRequest> materialRequest,
								  IUnitOfWork<StockTransaction> tx,
								  IUnitOfWork<Expense> expense,
								  LookupService lookup,
								  IWebHostEnvironment env)
		{
			_project = project;
			_customer = customer;
			_purchase = purchase;
			_materialRequest = materialRequest;
			_tx = tx;
			_expense = expense;
			_lookup = lookup;
			_env = env;
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
				if (!SearchValidator.TryValidateNameSearch(searchString, out var validName, out var searchError))
				{
					TempData["Error"] = searchError;
					ViewBag.SearchError = searchError;
					query = query.Where(_ => false);
				}
				else
				{
					query = query.Where(p => p.ProjectName.Contains(validName)
										  || p.Customer.Name.Contains(validName));
				}
			}

			if (status.HasValue)
			{
				query = query.Where(p => p.Status == status.Value);
			}

			var totalCount = await query.CountAsync();
			int pageSize = await _lookup.PageSizeAsync();
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
			ViewBag.CancelledCount = await all.CountAsync(p => p.Status == ProjectStatus.Cancelled);

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

			// طلبات التوريد (المواد) الخاصة بالمشروع
			// (المهندس والمصمم يرون طلباتهم فقط، كما في شاشة الطلبات)
			var seesAllRequests = AppRoles.CanAccess(User, "MaterialRequests", "Issue") || User.IsInRole(AppRoles.Accountant);
			var me = User.FindFirstValue(ClaimTypes.NameIdentifier);
			ViewBag.MaterialRequests = AppRoles.CanAccess(User, "MaterialRequests")
				? await _materialRequest.Entity.Include(r => r.Items)
					.Where(r => r.ProjectId == project.Id && (seesAllRequests || r.RequestedById == me))
					.OrderByDescending(r => r.Created)
					.ToListAsync()
				: new List<MaterialRequest>();

			ViewBag.ProjectPics = CollectProjectPics(project);

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
														p => p.Quotes,
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

			ViewBag.ProjectPics = CollectProjectPics(project);

			return View(project);
		}

		// POST: Projects/UploadPics
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> UploadPics(Guid id, List<IFormFile>? files)
		{
			var exists = await _project.Entity.GetAll().AnyAsync(p => p.Id == id);
			if (!exists)
			{
				return NotFound();
			}

			var (urls, error) = await FileUploads.SavePicsAsync(files, _env, "project", id);
			if (error != null)
			{
				TempData["Error"] = error;
			}
			else if (urls.Count == 0)
			{
				TempData["Error"] = "اختر صورة واحدة أو أكثر للرفع";
			}
			else
			{
				TempData["Success"] = $"تم رفع {urls.Count} صورة/مخطط وحفظها في مجلد المشروع (wwwroot/pic) بنجاح";
			}

			return RedirectToAction(nameof(Details), new { id });
		}

		// POST: Projects/DeletePic
		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult DeletePic(Guid id, string url)
		{
			FileUploads.DeletePic(url, _env);
			TempData["Success"] = "تم حذف الصورة / المخطط من مجلد المشروع بنجاح";
			return RedirectToAction(nameof(Details), new { id });
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
				CustomerId = customerId ?? Guid.Empty,
				StartDate = DateTime.Today
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

			await ValidateTeamMembersAsync(project);

			if (await _project.Entity.GetAll().AnyAsync(p => p.ProjectNo == project.ProjectNo))
			{
				// رقم آخر حجزه مستخدم ثاني أثناء تعبئة النموذج
				project.ProjectNo = await GenerateProjectNoAsync();
			}

			if (ModelState.IsValid)
			{
				project.Id = Guid.NewGuid();
				project.Created = DateTime.UtcNow;
				project.StartDate ??= DateTime.Today;
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

			await FillSelectListsAsync(project, includeCurrentTeamSelection: true);
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

			var existing = await _project.Entity.GetWhere(p => p.Id == id)
				.Select(p => new { p.Created, p.SalesUserId, p.EngineerUserId, p.DesignerUserId, p.Status, p.CancellationFee, p.CancellationReason })
				.FirstOrDefaultAsync();
			if (existing == null)
			{
				return NotFound();
			}

			if (project.CustomerId == Guid.Empty)
			{
				ModelState.AddModelError(nameof(Project.CustomerId), "يجب اختيار العميل");
			}

			await ValidateTeamMembersAsync(project, existing.SalesUserId, existing.EngineerUserId, existing.DesignerUserId);

			if (ModelState.IsValid)
			{
				try
				{
					project.Created = existing.Created;
					project.Modified = DateTime.UtcNow;
					// الحفاظ على رسوم وسبب الإلغاء إذا لم يكن المشروع ملغى عند فتح النموذج
					if (existing.Status != ProjectStatus.Cancelled)
					{
						project.CancellationFee = existing.CancellationFee;
						project.CancellationReason = string.IsNullOrWhiteSpace(project.CancellationReason)
							? existing.CancellationReason
							: project.CancellationReason.Trim();
					}
					else
					{
						project.CancellationReason = string.IsNullOrWhiteSpace(project.CancellationReason)
							? null
							: project.CancellationReason.Trim();
					}
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

			await FillSelectListsAsync(project, includeCurrentTeamSelection: true);
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
			hasLinkedRecords = hasLinkedRecords || await _materialRequest.Entity.GetAll().AnyAsync(r => r.ProjectId == id);

			if (hasLinkedRecords)
			{
				TempData["Error"] = $"لا يمكن حذف المشروع \"{project.ProjectName}\" لوجود عقود أو دفعات أو عروض أسعار أو حركات مخزنية مرتبطة به. يمكنك تغيير حالته إلى (ملغى) بدلاً من الحذف.";
				return RedirectToAction(nameof(Details), new { id });
			}

			var visitIds = await _project.Entity.GetWhere(p => p.Id == id)
				.SelectMany(p => p.SiteVisits.Select(v => v.Id))
				.ToListAsync();

			_project.Entity.Delete(project);
			await _project.SaveAsync();
			FileUploads.DeleteAllPics(_env, "project", id);
			foreach (var vid in visitIds)
			{
				FileUploads.DeleteAllPics(_env, "visit", vid);
			}
			TempData["Success"] = $"تم حذف المشروع \"{project.ProjectName}\" بنجاح";

			return RedirectToAction(nameof(Index));
		}

		// POST: Projects/Cancel
		// إلغاء المشروع مع إمكانية تسجيل أتعاب المهندس والمصمم كمصروفات و/أو رسوم إلغاء على العميل
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Cancel(CancelProjectViewModel model)
		{
			var project = await _project.Entity.GetByIdAsync(model.ProjectId);
			if (project == null)
			{
				TempData["Error"] = "تعذر العثور على المشروع";
				return RedirectToAction(nameof(Index));
			}

			if (project.Status == ProjectStatus.Cancelled)
			{
				TempData["Error"] = $"المشروع \"{project.ProjectName}\" ملغى بالفعل";
				return RedirectToAction(nameof(Details), new { id = project.Id });
			}

			if (!ModelState.IsValid)
			{
				TempData["Error"] = "تأكد من صحة القيم المدخلة (لا يمكن أن تكون الأتعاب سالبة)";
				return RedirectToAction(nameof(Details), new { id = project.Id });
			}

			var teamNames = await _project.Entity.GetWhere(p => p.Id == project.Id)
				.Select(p => new
				{
					EngineerName = p.EngineerUser != null ? p.EngineerUser.UserName : null,
					DesignerName = p.DesignerUser != null ? p.DesignerUser.UserName : null
				})
				.FirstOrDefaultAsync();

			var reason = string.IsNullOrWhiteSpace(model.CancellationReason) ? null : model.CancellationReason.Trim();
			var customerFee = model.CustomerCancellationFee ?? 0m;

			project.Status = ProjectStatus.Cancelled;
			project.CancellationFee = customerFee;
			project.CancellationReason = reason;
			project.Modified = DateTime.UtcNow;

			var feeParts = new List<string>();
			var teamFees = new (decimal Amount, string ExpenseType, string RoleLabel, string? UserName)[]
			{
				(model.EngineerFee ?? 0m, "أتعاب معاينة مهندس", "أتعاب معاينة المهندس", teamNames?.EngineerName),
				(model.DesignerFee ?? 0m, "أتعاب تصميم", "أتعاب المصمم", teamNames?.DesignerName)
			};

			foreach (var fee in teamFees)
			{
				if (fee.Amount <= 0)
				{
					continue;
				}

				var who = string.IsNullOrWhiteSpace(fee.UserName) ? "" : $" ({fee.UserName})";
				_expense.Entity.Insert(new Expense
				{
					Id = Guid.NewGuid(),
					ProjectId = project.Id,
					ExpenseType = fee.ExpenseType,
					Amount = fee.Amount,
					ExpenseDate = DateTime.Today,
					Payment = model.PaymentMethod,
					Description = $"{fee.RoleLabel}{who} - عند إلغاء المشروع",
					Notes = reason,
					Created = DateTime.UtcNow
				});
				feeParts.Add($"{fee.ExpenseType}: {fee.Amount:N2}");
			}

			if (customerFee > 0)
			{
				feeParts.Add($"رسوم على العميل: {customerFee:N2}");
			}

			await _project.SaveAsync();

			TempData["Success"] = feeParts.Count > 0
				? $"تم إلغاء المشروع \"{project.ProjectName}\" وتسجيل الأتعاب ({string.Join("، ", feeParts)})"
				: $"تم إلغاء المشروع \"{project.ProjectName}\" بدون أتعاب";

			return RedirectToAction(nameof(Details), new { id = project.Id });
		}

		//---------------------------------------------------------------------

		private List<(string Url, string Label)> CollectProjectPics(Project project)
		{
			var list = new List<(string Url, string Label)>();
			foreach (var u in FileUploads.GetPics(_env, "project", project.Id))
			{
				list.Add((u, "تصميم / مخطط المشروع"));
			}
			foreach (var v in project.SiteVisits.OrderByDescending(x => x.VisitDate))
			{
				foreach (var u in FileUploads.GetPics(_env, "visit", v.Id))
				{
					list.Add((u, $"معاينة {v.VisitDate:yyyy/MM/dd}"));
				}
			}
			foreach (var q in project.Quotes.OrderByDescending(x => x.Created))
			{
				foreach (var u in FileUploads.GetPics(_env, "quote", q.Id))
				{
					list.Add((u, $"تصميم عرض {q.QuoteNo}"));
				}
			}
			return list;
		}

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

		private async Task ValidateTeamMembersAsync(Project project,
			string? existingSalesUserId = null,
			string? existingEngineerUserId = null,
			string? existingDesignerUserId = null)
		{
			project.SalesUserId = string.IsNullOrWhiteSpace(project.SalesUserId) ? null : project.SalesUserId;
			project.EngineerUserId = string.IsNullOrWhiteSpace(project.EngineerUserId) ? null : project.EngineerUserId;
			project.DesignerUserId = string.IsNullOrWhiteSpace(project.DesignerUserId) ? null : project.DesignerUserId;

			var checks = new (string? UserId, string? ExistingUserId, string Field, string Role, string Error)[]
			{
				(project.SalesUserId, existingSalesUserId, nameof(Project.SalesUserId), AppRoles.Sales, "المستخدم المختار في خانة المبيعات غير فعال أو لا يملك صلاحية المبيعات"),
				(project.EngineerUserId, existingEngineerUserId, nameof(Project.EngineerUserId), AppRoles.Engineer, "المستخدم المختار في خانة المهندس غير فعال أو لا يملك صلاحية المهندس"),
				(project.DesignerUserId, existingDesignerUserId, nameof(Project.DesignerUserId), AppRoles.Designer, "المستخدم المختار في خانة المصمم غير فعال أو لا يملك صلاحية المصمم")
			};

			foreach (var check in checks)
			{
				if (check.UserId != null && check.UserId != check.ExistingUserId
					&& !await _lookup.IsEligibleUserAsync(check.UserId, check.Role))
				{
					ModelState.AddModelError(check.Field, check.Error);
				}
			}
		}

		private async Task FillSelectListsAsync(Project project, bool includeCurrentTeamSelection = false)
		{
			var customers = await _customer.Entity.GetAll()
				.OrderBy(c => c.Name)
				.Select(c => new { c.Id, c.Name, c.Address, Text = c.Name + " (" + c.CustomerNo + ")" })
				.ToListAsync();
			ViewBag.Customers = new SelectList(customers, "Id", "Text", project.CustomerId);
			ViewBag.CustomerAddresses = customers.ToDictionary(c => c.Id.ToString(), c => c.Address ?? "");

			ViewBag.SalesUsers = await _lookup.UsersAsync(
				project.SalesUserId,
				includeCurrentTeamSelection && ModelState.GetFieldValidationState(nameof(Project.SalesUserId)) != ModelValidationState.Invalid,
				AppRoles.Sales);
			ViewBag.EngineerUsers = await _lookup.UsersAsync(
				project.EngineerUserId,
				includeCurrentTeamSelection && ModelState.GetFieldValidationState(nameof(Project.EngineerUserId)) != ModelValidationState.Invalid,
				AppRoles.Engineer);
			ViewBag.DesignerUsers = await _lookup.UsersAsync(
				project.DesignerUserId,
				includeCurrentTeamSelection && ModelState.GetFieldValidationState(nameof(Project.DesignerUserId)) != ModelValidationState.Invalid,
				AppRoles.Designer);
			ViewBag.StatusList = LookupService.EnumList<ProjectStatus>(project.Status);
		}
	}
}
