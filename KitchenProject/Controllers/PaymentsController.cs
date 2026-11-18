using KitchenProject.Classes;
using KitchenProject.Models.Entities;
using KitchenProject.Models.Enums;
using KitchenProject.Models.Interfaces;
using KitchenProject.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KitchenProject.Controllers
{
	[ViewLayout("_DashboardLayout")]
	//[Authorize(Policy = "ProgOrAdminOrEmployeePolicy")]

	// تحصيلات العملاء (إيصالات القبض) - كل دفعة تحدّث المدفوع في العقد المرتبط بها
	public class PaymentsController : Controller
	{
		private readonly IUnitOfWork<CustomerPayment> _payment;
		private readonly IUnitOfWork<Contract> _contract;
		private readonly IUnitOfWork<Project> _project;
		private readonly LookupService _lookup;

		public PaymentsController(IUnitOfWork<CustomerPayment> payment,
								  IUnitOfWork<Contract> contract,
								  IUnitOfWork<Project> project,
								  LookupService lookup)
		{
			_payment = payment;
			_contract = contract;
			_project = project;
			_lookup = lookup;
		}

		// GET: Payments
		public async Task<IActionResult> Index(string? searchString, PaymentMethod? method, Guid? projectId, DateTime? from, DateTime? to, int page = 1)
		{
			ViewBag.ActivePage = "Payments";
			ViewBag.SearchString = searchString;
			ViewBag.Method = method;
			ViewBag.ProjectId = projectId;
			ViewBag.From = from;
			ViewBag.To = to;

			var query = _payment.Entity.Include(p => p.Project, p => p.Project.Customer, p => p.Contract!);

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
					query = query.Where(p => p.Project.ProjectName.Contains(validName)
										  || p.Project.Customer.Name.Contains(validName));
				}
			}

			if (method.HasValue) query = query.Where(p => p.Payment == method.Value);
			if (from.HasValue) query = query.Where(p => p.PaymentDate >= from.Value.Date);
			if (to.HasValue) query = query.Where(p => p.PaymentDate <= to.Value.Date);

			if (projectId.HasValue)
			{
				query = query.Where(p => p.ProjectId == projectId.Value);
				ViewBag.ProjectName = await _project.Entity.GetWhere(p => p.Id == projectId.Value)
					.Select(p => p.ProjectNo + " - " + p.ProjectName).FirstOrDefaultAsync();
			}

			var totalCount = await query.CountAsync();
			ViewBag.FilteredTotal = await query
				.Where(p => !(p.Payment == PaymentMethod.Chek && p.ChequeStatus == ChequeStatus.Bounced))
				.SumAsync(p => (decimal?)(p.IsRefund ? -p.Amount : p.Amount)) ?? 0;

			int pageSize = await _lookup.PageSizeAsync();
			var payments = await query.OrderByDescending(p => p.PaymentDate).ThenByDescending(p => p.Created)
									  .Skip((page - 1) * pageSize)
									  .Take(pageSize)
									  .ToListAsync();

			ViewBag.CurrentPage = page;
			ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
			ViewBag.TotalCount = totalCount;

			// Metrics for Dashboard header cards
			var monthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
			ViewBag.MonthTotal = await _payment.Entity
				.GetWhere(p => p.PaymentDate >= monthStart && !(p.Payment == PaymentMethod.Chek && p.ChequeStatus == ChequeStatus.Bounced))
				.SumAsync(p => (decimal?)(p.IsRefund ? -p.Amount : p.Amount)) ?? 0;
			ViewBag.TodayTotal = await _payment.Entity
				.GetWhere(p => p.PaymentDate == DateTime.Today && !(p.Payment == PaymentMethod.Chek && p.ChequeStatus == ChequeStatus.Bounced))
				.SumAsync(p => (decimal?)(p.IsRefund ? -p.Amount : p.Amount)) ?? 0;
			ViewBag.Outstanding = await _contract.Entity
				.GetWhere(c => c.Status == ContractStatuses.Active || c.Status == ContractStatuses.Completed)
				.SumAsync(c => (decimal?)(c.TotalAmount - c.PaidAmount)) ?? 0;

			ViewBag.MethodList = LookupService.EnumList(method);
			return View(payments);
		}

		// GET: Payments/Receipt/5  طباعة إيصال القبض
		public async Task<IActionResult> Receipt(Guid? id)
		{
			var payment = await _payment.Entity.Include(p => p.Project, p => p.Project.Customer, p => p.Contract!)
				.FirstOrDefaultAsync(p => p.Id == id);
			if (payment == null)
			{
				return NotFound();
			}

			if (!payment.IsPrinted)
			{
				payment.IsPrinted = true;
				await _payment.SaveAsync();
			}

			return View(payment);
		}

		// GET: Payments/Create?projectId=5  أو  ?contractId=5  أو  ?isRefund=true
		public async Task<IActionResult> Create(Guid? projectId, Guid? contractId, bool isRefund = false)
		{
			ViewBag.ActivePage = "Payments";

			string? defaultNotes = null;
			if (contractId.HasValue)
			{
				var contract = await _contract.Entity.GetWhere(c => c.Id == contractId.Value)
					.Select(c => new { c.ProjectId, Remaining = c.TotalAmount - c.PaidAmount }).FirstOrDefaultAsync();
				if (contract != null)
				{
					projectId = contract.ProjectId;
					if (!isRefund) ViewBag.SuggestedAmount = Math.Max(0, contract.Remaining);
				}
			}
			else if (projectId.HasValue && !isRefund)
			{
				// عقد واحد ساري على المشروع: نختاره تلقائياً
				var contracts = await ProjectContracts(projectId.Value).Select(c => c.Id).Take(2).ToListAsync();
				if (contracts.Count == 1)
				{
					contractId = contracts[0];
				}
				else if (contracts.Count == 0)
				{
					// مشروع ملغى برسوم إلغاء على العميل: نقترح المتبقي من رسوم الإلغاء
					var cancelled = await _project.Entity.GetWhere(p => p.Id == projectId.Value && p.Status == ProjectStatus.Cancelled)
						.Select(p => new { p.CancellationFee })
						.FirstOrDefaultAsync();
					if (cancelled != null && cancelled.CancellationFee > 0)
					{
						var paid = await _payment.Entity
							.GetWhere(p => p.ProjectId == projectId.Value && !(p.Payment == PaymentMethod.Chek && p.ChequeStatus == ChequeStatus.Bounced))
							.SumAsync(p => (decimal?)(p.IsRefund ? -p.Amount : p.Amount)) ?? 0;
						ViewBag.SuggestedAmount = Math.Max(0, cancelled.CancellationFee - paid);
						defaultNotes = "رسوم إلغاء المشروع";
					}
				}
			}

			var model = new CustomerPayment
			{
				ReceiptNo = await GenerateNoAsync(isRefund),
				ProjectId = projectId ?? Guid.Empty,
				ContractId = contractId,
				IsRefund = isRefund,
				Notes = isRefund ? "إرجاع مبلغ للعميل" : defaultNotes
			};

			await FillSelectListsAsync(model);
			return View(model);
		}

		// POST: Payments/Create
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(CustomerPayment model)
		{
			ViewBag.ActivePage = "Payments";

			if (model.ProjectId == Guid.Empty)
				ModelState.AddModelError(nameof(CustomerPayment.ProjectId), "يجب اختيار المشروع");

			if (model.ContractId.HasValue)
			{
				var contract = await _contract.Entity.GetWhere(c => c.Id == model.ContractId.Value).FirstOrDefaultAsync();
				if (contract == null || contract.ProjectId != model.ProjectId)
				{
					ModelState.AddModelError(nameof(CustomerPayment.ContractId), "العقد المختار لا يتبع هذا المشروع");
				}
				else if (!model.IsRefund && contract.Status == ContractStatuses.Cancelled)
				{
					ModelState.AddModelError(nameof(CustomerPayment.ContractId), "لا يمكن التحصيل على عقد ملغى");
				}
				else if (!model.IsRefund && model.Amount > contract.RemainingAmount)
				{
					ModelState.AddModelError(nameof(CustomerPayment.Amount), $"المبلغ أكبر من المتبقي على العقد ({contract.RemainingAmount:N2})");
				}
				else if (model.IsRefund && model.Amount > contract.PaidAmount)
				{
					ModelState.AddModelError(nameof(CustomerPayment.Amount), $"مبلغ الإرجاع أكبر من المدفوع على العقد ({contract.PaidAmount:N2})");
				}
			}

			if (await _payment.Entity.GetAll().AnyAsync(p => p.ReceiptNo == model.ReceiptNo))
			{
				model.ReceiptNo = await GenerateNoAsync(model.IsRefund);
			}

			if (model.Payment == PaymentMethod.Chek)
			{
				model.ChequeStatus ??= ChequeStatus.Pending;
			}
			else
			{
				model.ChequeStatus = null;
				model.ChequeDueDate = null;
			}

			if (ModelState.IsValid)
			{
				model.Id = Guid.NewGuid();
				model.Created = DateTime.UtcNow;
				model.CreatedBy = User.Identity?.Name;
				_payment.Entity.Insert(model);
				await _payment.SaveAsync();

				if (model.ContractId.HasValue)
				{
					await SyncContractPaidAsync(model.ContractId.Value);
				}

				TempData["Success"] = model.IsRefund
					? $"تم تسجيل سند إرجاع للعميل {model.ReceiptNo} بمبلغ {model.Amount:N2}"
					: $"تم تسجيل الإيصال {model.ReceiptNo} بمبلغ {model.Amount:N2}";
				TempData["PrintReceiptId"] = model.Id.ToString();
				return RedirectToAction("Details", "Projects", new { id = model.ProjectId });
			}

			await FillSelectListsAsync(model);
			return View(model);
		}

		// POST: Payments/Delete/5
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Delete(Guid id, string? returnUrl)
		{
			var payment = await _payment.Entity.GetByIdAsync(id);
			if (payment == null)
			{
				TempData["Error"] = "تعذر العثور على الإيصال المراد حذفه";
				return RedirectToAction(nameof(Index));
			}

			if (!AppRoles.IsFullAccess(User))
			{
				if (payment.IsPrinted)
				{
					TempData["Error"] = "لا يمكن حذف إيصال بعد طباعته، تواصل مع المدير";
					return RedirectToAction(nameof(Index));
				}
				if (!string.IsNullOrEmpty(payment.CreatedBy) && !string.Equals(payment.CreatedBy, User.Identity?.Name, StringComparison.OrdinalIgnoreCase))
				{
					TempData["Error"] = "لا يمكنك حذف إيصال أنشأه مستخدم آخر";
					return RedirectToAction(nameof(Index));
				}
			}

			_payment.Entity.Delete(payment);
			await _payment.SaveAsync();

			if (payment.ContractId.HasValue)
			{
				await SyncContractPaidAsync(payment.ContractId.Value);
			}

			TempData["Success"] = $"تم حذف الإيصال {payment.ReceiptNo}";
			if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
			{
				return Redirect(returnUrl);
			}
			return RedirectToAction(nameof(Index));
		}

		//---------------------------------------------------------------------

		private IQueryable<Contract> ProjectContracts(Guid projectId) =>
			_contract.Entity.GetWhere(c => c.ProjectId == projectId && c.Status != ContractStatuses.Cancelled);

		// المدفوع في العقد = مجموع إيصالات القبض مطروحاً منها سندات الإرجاع
		private async Task SyncContractPaidAsync(Guid contractId)
		{
			var contract = await _contract.Entity.GetByIdAsync(contractId);
			if (contract == null)
			{
				return;
			}

			contract.PaidAmount = await _payment.Entity
				.GetWhere(p => p.ContractId == contractId && !(p.Payment == PaymentMethod.Chek && p.ChequeStatus == ChequeStatus.Bounced))
				.SumAsync(p => (decimal?)(p.IsRefund ? -p.Amount : p.Amount)) ?? 0;
			contract.Modified = DateTime.UtcNow;
			await _contract.SaveAsync();
		}

		private Task<string> GenerateNoAsync(bool isRefund = false) =>
			DocumentNumbers.NextAsync(_payment.Entity.GetAll().Select(p => p.ReceiptNo), isRefund ? "RF-" : "RC-");

		private async Task FillSelectListsAsync(CustomerPayment payment)
		{
			ViewBag.Projects = await _lookup.ProjectsAsync(payment.ProjectId == Guid.Empty ? null : payment.ProjectId);
			ViewBag.MethodList = LookupService.EnumList(payment.Payment);
			ViewBag.CashAccounts = await _lookup.CashAndBankAccountsAsync(payment.CashAccountId);
			ViewBag.ChequeStatuses = LookupService.EnumList(payment.ChequeStatus ?? ChequeStatus.Pending);

			// كل العقود غير الملغاة، والنموذج يعرض فقط عقود المشروع المختار
			ViewBag.Contracts = await _contract.Entity.GetWhere(c => c.Status != ContractStatuses.Cancelled || c.Id == payment.ContractId)
				.OrderByDescending(c => c.Created)
				.Select(c => new ContractOption
				{
					Id = c.Id,
					ProjectId = c.ProjectId,
					ContractNo = c.ContractNo,
					Total = c.TotalAmount,
					Remaining = c.TotalAmount - c.PaidAmount
				})
				.ToListAsync();
		}
	}
}
