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

	public class ContractsController : Controller
	{
		private readonly IUnitOfWork<Contract> _contract;
		private readonly IUnitOfWork<Quote> _quote;
		private readonly IUnitOfWork<Project> _project;
		private readonly IUnitOfWork<CustomerPayment> _payment;
		private readonly LookupService _lookup;
		private readonly IWebHostEnvironment _env;

		public ContractsController(IUnitOfWork<Contract> contract,
								   IUnitOfWork<Quote> quote,
								   IUnitOfWork<Project> project,
								   IUnitOfWork<CustomerPayment> payment,
								   LookupService lookup,
								   IWebHostEnvironment env)
		{
			_contract = contract;
			_quote = quote;
			_project = project;
			_payment = payment;
			_lookup = lookup;
			_env = env;
		}

		// GET: Contracts
		public async Task<IActionResult> Index(string? searchString, string? status, Guid? projectId, int page = 1)
		{
			ViewBag.ActivePage = "Contracts";
			ViewBag.SearchString = searchString;
			ViewBag.Status = status;
			ViewBag.ProjectId = projectId;

			var query = _contract.Entity.Include(c => c.Projects, c => c.Projects.Customer);

			if (projectId.HasValue)
			{
				query = query.Where(c => c.ProjectId == projectId.Value);
				ViewBag.ProjectName = await _project.Entity.GetWhere(p => p.Id == projectId.Value)
					.Select(p => p.ProjectNo + " - " + p.ProjectName).FirstOrDefaultAsync();
			}

			if (!string.IsNullOrWhiteSpace(searchString))
			{
				searchString = searchString.Trim();
				query = query.Where(c => c.ContractNo.Contains(searchString)
									  || c.Projects.ProjectName.Contains(searchString)
									  || c.Projects.Customer.Name.Contains(searchString));
			}

			if (!string.IsNullOrEmpty(status))
			{
				query = query.Where(c => c.Status == status);
			}

			var totalCount = await query.CountAsync();
			int pageSize = 10;
			var contracts = await query.OrderByDescending(c => c.Created)
									   .Skip((page - 1) * pageSize)
									   .Take(pageSize)
									   .ToListAsync();

			ViewBag.CurrentPage = page;
			ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
			ViewBag.TotalCount = totalCount;

			// Metrics for Dashboard header cards
			var active = _contract.Entity.GetWhere(c => c.Status == ContractStatuses.Active);
			ViewBag.ActiveCount = await active.CountAsync();
			ViewBag.ActiveTotal = await active.SumAsync(c => (decimal?)c.TotalAmount) ?? 0;
			ViewBag.ActiveRemaining = await active.SumAsync(c => (decimal?)(c.TotalAmount - c.PaidAmount)) ?? 0;

			ViewBag.StatusList = StatusList(status);

			return View(contracts);
		}

		// GET: Contracts/Details/5
		public async Task<IActionResult> Details(Guid? id)
		{
			ViewBag.ActivePage = "Contracts";

			var contract = await LoadFullAsync(id);
			if (contract == null)
			{
				return NotFound();
			}

			return View(contract);
		}

		// GET: Contracts/Print/5
		public async Task<IActionResult> Print(Guid? id)
		{
			var contract = await LoadFullAsync(id);
			if (contract == null)
			{
				return NotFound();
			}

			return View(contract);
		}

		// GET: Contracts/Create?projectId=5  أو  ?quoteId=5 (من عرض سعر معتمد)
		public async Task<IActionResult> Create(Guid? projectId, Guid? quoteId)
		{
			ViewBag.ActivePage = "Contracts";

			var model = new Contract
			{
				ContractNo = await GenerateContractNoAsync(),
				ProjectId = projectId ?? Guid.Empty,
				StartDate = DateTime.Today,
				Status = ContractStatuses.Draft
			};

			if (quoteId.HasValue)
			{
				var quote = await _quote.Entity.GetWhere(q => q.Id == quoteId.Value).FirstOrDefaultAsync();
				if (quote != null)
				{
					model.QuoteId = quote.Id;
					model.ProjectId = quote.ProjectId;
					model.TotalAmount = quote.Total;
				}
			}

			if (model.ProjectId != Guid.Empty)
			{
				var project = await _project.Entity.GetWhere(p => p.Id == model.ProjectId).FirstOrDefaultAsync();
				if (project != null)
				{
					model.ExpectedEndDate = project.ExpectedDeliveryDate;
					if (model.TotalAmount == 0)
						model.TotalAmount = project.FinalAmount > 0 ? project.FinalAmount : project.EstimatedAmount;
					model.Specifications = SpecificationsTemplate(project.KitchenType);
				}
			}

			model.Specifications ??= SpecificationsTemplate(null);

			await FillSelectListsAsync(model);
			return View(model);
		}

		// POST: Contracts/Create
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(Contract contract, IFormFile? contractFile)
		{
			ViewBag.ActivePage = "Contracts";
			await ValidateContractAsync(contract);

			if (await _contract.Entity.GetAll().AnyAsync(c => c.ContractNo == contract.ContractNo))
			{
				// رقم آخر حجزه مستخدم ثاني أثناء تعبئة النموذج
				contract.ContractNo = await GenerateContractNoAsync();
			}

			if (ModelState.IsValid && contractFile != null)
			{
				var (url, error) = await FileUploads.SaveAsync(contractFile, _env, "contracts", FileUploads.Documents);
				if (error != null)
					ModelState.AddModelError("", error);
				else
					contract.ContractFilePath = url;
			}

			if (ModelState.IsValid)
			{
				contract.Id = Guid.NewGuid();
				contract.Created = DateTime.UtcNow;
				contract.PaidAmount = 0;

				_contract.Entity.Insert(contract);
				await ApplyToProjectAsync(contract);
				await _contract.SaveAsync();

				TempData["Success"] = $"تم إنشاء العقد {contract.ContractNo} بنجاح";
				return RedirectToAction(nameof(Details), new { id = contract.Id });
			}

			await FillSelectListsAsync(contract);
			return View(contract);
		}

		// GET: Contracts/Edit/5
		public async Task<IActionResult> Edit(Guid? id)
		{
			ViewBag.ActivePage = "Contracts";

			if (id == null)
			{
				return NotFound();
			}

			var contract = await _contract.Entity.GetByIdAsync(id.Value);
			if (contract == null)
			{
				return NotFound();
			}

			await FillSelectListsAsync(contract);
			return View(contract);
		}

		// POST: Contracts/Edit/5
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(Guid id, Contract contract, IFormFile? contractFile, bool removeFile = false)
		{
			ViewBag.ActivePage = "Contracts";

			if (id != contract.Id)
			{
				return NotFound();
			}

			// المدفوع والملف لا يُعدلان من النموذج مباشرة
			var stored = await _contract.Entity.GetWhere(c => c.Id == id)
				.Select(c => new { c.PaidAmount, c.ContractFilePath })
				.FirstOrDefaultAsync();
			if (stored == null)
			{
				return NotFound();
			}
			contract.PaidAmount = stored.PaidAmount;
			contract.ContractFilePath = stored.ContractFilePath;

			await ValidateContractAsync(contract);

			string? oldFileToDelete = null;
			if (ModelState.IsValid && contractFile != null)
			{
				var (url, error) = await FileUploads.SaveAsync(contractFile, _env, "contracts", FileUploads.Documents);
				if (error != null)
				{
					ModelState.AddModelError("", error);
				}
				else
				{
					oldFileToDelete = contract.ContractFilePath;
					contract.ContractFilePath = url;
				}
			}
			else if (removeFile)
			{
				oldFileToDelete = contract.ContractFilePath;
				contract.ContractFilePath = null;
			}

			if (ModelState.IsValid)
			{
				contract.Modified = DateTime.UtcNow;
				_contract.Entity.Update(contract);
				await ApplyToProjectAsync(contract);
				await _contract.SaveAsync();

				FileUploads.Delete(oldFileToDelete, _env);

				TempData["Success"] = $"تم تحديث العقد {contract.ContractNo} بنجاح";
				return RedirectToAction(nameof(Details), new { id = contract.Id });
			}

			await FillSelectListsAsync(contract);
			return View(contract);
		}

		// POST: Contracts/Delete/5
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Delete(Guid id)
		{
			var contract = await _contract.Entity.GetByIdAsync(id);
			if (contract == null)
			{
				TempData["Error"] = "تعذر العثور على العقد المراد حذفه";
				return RedirectToAction(nameof(Index));
			}

			if (await _payment.Entity.GetAll().AnyAsync(p => p.ContractId == id))
			{
				TempData["Error"] = $"لا يمكن حذف العقد {contract.ContractNo} لوجود دفعات مسجلة عليه. يمكنك تغيير حالته إلى (ملغى).";
				return RedirectToAction(nameof(Details), new { id });
			}

			_contract.Entity.Delete(contract);
			await _contract.SaveAsync();
			FileUploads.Delete(contract.ContractFilePath, _env);

			TempData["Success"] = $"تم حذف العقد {contract.ContractNo} بنجاح";
			return RedirectToAction("Details", "Projects", new { id = contract.ProjectId });
		}

		//---------------------------------------------------------------------

		private Task<Contract?> LoadFullAsync(Guid? id)
		{
			if (id == null)
			{
				return Task.FromResult<Contract?>(null);
			}

			return _contract.Entity.Include(c => c.Projects, c => c.Projects.Customer,
											c => c.Projects.DesignerUser!, c => c.Quote!, c => c.CustomerPayments)
				.AsSplitQuery()
				.Include(c => c.Quote!.Items)
				.FirstOrDefaultAsync(c => c.Id == id)!;
		}

		private Task<string> GenerateContractNoAsync() =>
			DocumentNumbers.NextAsync(_contract.Entity.GetAll().Select(c => c.ContractNo), "CT-");

		// العقد يحدد القيمة النهائية للمشروع وموعد التسليم وينقله لمرحلة التعاقد
		private async Task ApplyToProjectAsync(Contract contract)
		{
			if (contract.Status == ContractStatuses.Cancelled)
				return;

			await ProjectWorkflow.AdvanceAsync(_project, contract.ProjectId, ProjectStatus.Contracted);

			var project = await _project.Entity.GetByIdAsync(contract.ProjectId);
			if (project != null)
			{
				project.FinalAmount = contract.TotalAmount;
				if (contract.ExpectedEndDate.HasValue)
					project.ExpectedDeliveryDate = contract.ExpectedEndDate;
			}
		}

		private async Task ValidateContractAsync(Contract contract)
		{
			if (contract.ProjectId == Guid.Empty)
				ModelState.AddModelError(nameof(Contract.ProjectId), "يجب اختيار المشروع");

			if (contract.TotalAmount <= 0)
				ModelState.AddModelError(nameof(Contract.TotalAmount), "قيمة العقد يجب أن تكون أكبر من صفر");

			if (!ContractStatuses.Names.ContainsKey(contract.Status))
				ModelState.AddModelError(nameof(Contract.Status), "حالة غير صالحة");

			if (contract.StartDate.HasValue && contract.ExpectedEndDate.HasValue && contract.ExpectedEndDate < contract.StartDate)
				ModelState.AddModelError(nameof(Contract.ExpectedEndDate), "موعد الانتهاء قبل تاريخ البدء");

			if (contract.QuoteId.HasValue)
			{
				var quote = await _quote.Entity.GetWhere(q => q.Id == contract.QuoteId.Value)
					.Select(q => new { q.ProjectId, q.Status }).FirstOrDefaultAsync();

				if (quote == null || quote.ProjectId != contract.ProjectId)
					ModelState.AddModelError(nameof(Contract.QuoteId), "عرض السعر لا يتبع هذا المشروع");
				else if (quote.Status != DesignStatus.Approved)
					ModelState.AddModelError(nameof(Contract.QuoteId), "لا يمكن ربط العقد إلا بعرض سعر معتمد");
				else if (await _contract.Entity.GetAll().AnyAsync(c => c.QuoteId == contract.QuoteId && c.Id != contract.Id))
					ModelState.AddModelError(nameof(Contract.QuoteId), "يوجد عقد آخر مبني على نفس عرض السعر");
			}
		}

		private async Task FillSelectListsAsync(Contract contract)
		{
			ViewBag.Projects = await _lookup.ProjectsAsync(contract.ProjectId);
			ViewBag.StatusList = StatusList(contract.Status);

			// عروض الأسعار المعتمدة للمشروع المختار
			var quotes = await _quote.Entity.GetWhere(q => q.ProjectId == contract.ProjectId && q.Status == DesignStatus.Approved)
				.Select(q => new { q.Id, Text = q.QuoteNo + " - " + q.Total.ToString() })
				.ToListAsync();
			ViewBag.Quotes = new SelectList(quotes, "Id", "Text", contract.QuoteId);
		}

		private static SelectList StatusList(string? selected) =>
			new SelectList(ContractStatuses.Names.Select(s => new { Value = s.Key, Text = s.Value }), "Value", "Text", selected);

		// قالب مبدئي لكتابة المواصفات في العقد
		private static string SpecificationsTemplate(string? kitchenType) =>
			$"""
			نوع المطبخ: {kitchenType}
			خامة الهيكل (الكبائن):
			خامة ولون الواجهات (الأبواب):
			سطح العمل (الرخام / الجرانيت):
			المقابض والإكسسوارات (المفصلات، السحابات):
			الإضاءة:
			الأجهزة المشمولة:
			ما لا يشمله العقد:
			الضمان:
			""";
	}
}
