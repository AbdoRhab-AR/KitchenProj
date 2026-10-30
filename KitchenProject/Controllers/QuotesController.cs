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

	public class QuotesController : Controller
	{
		private readonly IUnitOfWork<Quote> _quote;
		private readonly IUnitOfWork<QuoteItem> _item;
		private readonly IUnitOfWork<Project> _project;
		private readonly IUnitOfWork<Contract> _contract;
		private readonly LookupService _lookup;

		public QuotesController(IUnitOfWork<Quote> quote,
								IUnitOfWork<QuoteItem> item,
								IUnitOfWork<Project> project,
								IUnitOfWork<Contract> contract,
								LookupService lookup)
		{
			_quote = quote;
			_item = item;
			_project = project;
			_contract = contract;
			_lookup = lookup;
		}

		// GET: Quotes
		public async Task<IActionResult> Index(string? searchString, DesignStatus? status, Guid? projectId, int page = 1)
		{
			ViewBag.ActivePage = "Quotes";
			ViewBag.SearchString = searchString;
			ViewBag.Status = status;
			ViewBag.ProjectId = projectId;

			var query = _quote.Entity.Include(q => q.Project, q => q.Project.Customer);

			if (projectId.HasValue)
			{
				query = query.Where(q => q.ProjectId == projectId.Value);
				ViewBag.ProjectName = await _project.Entity.GetWhere(p => p.Id == projectId.Value)
					.Select(p => p.ProjectNo + " - " + p.ProjectName).FirstOrDefaultAsync();
			}

			if (!string.IsNullOrWhiteSpace(searchString))
			{
				searchString = searchString.Trim();
				query = query.Where(q => q.QuoteNo.Contains(searchString)
									  || q.Project.ProjectName.Contains(searchString)
									  || q.Project.Customer.Name.Contains(searchString));
			}

			if (status.HasValue)
			{
				query = query.Where(q => q.Status == status.Value);
			}

			var totalCount = await query.CountAsync();
			int pageSize = 10;
			var quotes = await query.OrderByDescending(q => q.Created)
									.Skip((page - 1) * pageSize)
									.Take(pageSize)
									.ToListAsync();

			ViewBag.CurrentPage = page;
			ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
			ViewBag.TotalCount = totalCount;

			// Metrics for Dashboard header cards
			var all = _quote.Entity.GetAll();
			ViewBag.WaitingCount = await all.CountAsync(q => q.Status == DesignStatus.WaitingApproval);
			ViewBag.ApprovedCount = await all.CountAsync(q => q.Status == DesignStatus.Approved);
			ViewBag.ApprovedTotal = await all.Where(q => q.Status == DesignStatus.Approved).SumAsync(q => (decimal?)q.Total) ?? 0;

			ViewBag.StatusList = LookupService.EnumList(status);

			return View(quotes);
		}

		// GET: Quotes/Details/5
		public async Task<IActionResult> Details(Guid? id)
		{
			ViewBag.ActivePage = "Quotes";

			var quote = await LoadFullAsync(id);
			if (quote == null)
			{
				return NotFound();
			}

			ViewBag.HasContract = await _contract.Entity.GetAll().AnyAsync(c => c.QuoteId == quote.Id);
			return View(quote);
		}

		// GET: Quotes/Print/5
		public async Task<IActionResult> Print(Guid? id)
		{
			var quote = await LoadFullAsync(id);
			if (quote == null)
			{
				return NotFound();
			}

			return View(quote);
		}

		// GET: Quotes/Create?projectId=5
		public async Task<IActionResult> Create(Guid? projectId)
		{
			ViewBag.ActivePage = "Quotes";

			var model = new Quote
			{
				QuoteNo = await GenerateQuoteNoAsync(),
				ProjectId = projectId ?? Guid.Empty,
				ValidUntil = DateTime.Today.AddDays(15),
				Status = DesignStatus.Draft,
				Items = { new QuoteItem() }
			};

			await FillSelectListsAsync(model);
			return View(model);
		}

		// POST: Quotes/Create
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(Quote quote)
		{
			ViewBag.ActivePage = "Quotes";
			ValidateQuote(quote);

			if (await _quote.Entity.GetAll().AnyAsync(q => q.QuoteNo == quote.QuoteNo))
			{
				// رقم آخر حجزه مستخدم ثاني أثناء تعبئة النموذج
				quote.QuoteNo = await GenerateQuoteNoAsync();
			}

			if (ModelState.IsValid)
			{
				quote.Id = Guid.NewGuid();
				quote.Created = DateTime.UtcNow;
				foreach (var item in quote.Items)
				{
					item.Id = Guid.NewGuid();
					item.QuoteId = quote.Id;
					item.Created = quote.Created;
				}
				quote.Recalculate();

				_quote.Entity.Insert(quote);
				await ProjectWorkflow.AdvanceAsync(_project, quote.ProjectId, ProjectStatus.Quote);
				if (quote.Status == DesignStatus.Approved)
				{
					await ApplyApprovedTotalAsync(quote);
				}
				await _quote.SaveAsync();

				TempData["Success"] = $"تم إنشاء عرض السعر {quote.QuoteNo} بإجمالي {quote.Total:N2}";
				return RedirectToAction(nameof(Details), new { id = quote.Id });
			}

			await FillSelectListsAsync(quote);
			return View(quote);
		}

		// GET: Quotes/Edit/5
		public async Task<IActionResult> Edit(Guid? id)
		{
			ViewBag.ActivePage = "Quotes";

			var quote = await LoadFullAsync(id);
			if (quote == null)
			{
				return NotFound();
			}

			if (quote.Items.Count == 0)
			{
				quote.Items.Add(new QuoteItem());
			}

			await FillSelectListsAsync(quote);
			return View(quote);
		}

		// POST: Quotes/Edit/5
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(Guid id, Quote quote)
		{
			ViewBag.ActivePage = "Quotes";

			if (id != quote.Id)
			{
				return NotFound();
			}

			ValidateQuote(quote);

			if (ModelState.IsValid)
			{
				quote.Modified = DateTime.UtcNow;
				quote.Recalculate();

				// البنود تُستبدل بالكامل: نحذف القديمة ونضيف المرسلة من النموذج
				var newItems = quote.Items;
				quote.Items = new List<QuoteItem>();

				var oldItems = await _item.Entity.GetWhere(i => i.QuoteId == id).ToListAsync();
				foreach (var old in oldItems)
				{
					_item.Entity.Delete(old);
				}

				_quote.Entity.Update(quote);

				foreach (var item in newItems)
				{
					item.Id = Guid.NewGuid();
					item.QuoteId = quote.Id;
					item.Created = DateTime.UtcNow;
					_item.Entity.Insert(item);
				}

				if (quote.Status == DesignStatus.Approved)
				{
					await ApplyApprovedTotalAsync(quote);
				}

				await _quote.SaveAsync();

				TempData["Success"] = $"تم تحديث عرض السعر {quote.QuoteNo} بنجاح";
				return RedirectToAction(nameof(Details), new { id = quote.Id });
			}

			await FillSelectListsAsync(quote);
			return View(quote);
		}

		// POST: Quotes/SetStatus/5
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> SetStatus(Guid id, DesignStatus status)
		{
			var quote = await _quote.Entity.GetByIdAsync(id);
			if (quote == null)
			{
				return NotFound();
			}

			quote.Status = status;
			quote.Modified = DateTime.UtcNow;
			if (status == DesignStatus.Approved)
			{
				await ApplyApprovedTotalAsync(quote);
			}
			await _quote.SaveAsync();

			TempData["Success"] = $"تم تغيير حالة عرض السعر {quote.QuoteNo} إلى ({status.GetDisplayName()})";
			return RedirectToAction(nameof(Details), new { id });
		}

		// POST: Quotes/Delete/5
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Delete(Guid id)
		{
			var quote = await _quote.Entity.GetByIdAsync(id);
			if (quote == null)
			{
				TempData["Error"] = "تعذر العثور على عرض السعر المراد حذفه";
				return RedirectToAction(nameof(Index));
			}

			if (await _contract.Entity.GetAll().AnyAsync(c => c.QuoteId == id))
			{
				TempData["Error"] = $"لا يمكن حذف عرض السعر {quote.QuoteNo} لأنه مرتبط بعقد";
				return RedirectToAction(nameof(Details), new { id });
			}

			_quote.Entity.Delete(quote);
			await _quote.SaveAsync();
			TempData["Success"] = $"تم حذف عرض السعر {quote.QuoteNo} بنجاح";

			return RedirectToAction("Details", "Projects", new { id = quote.ProjectId });
		}

		//---------------------------------------------------------------------

		private Task<Quote?> LoadFullAsync(Guid? id)
		{
			if (id == null)
			{
				return Task.FromResult<Quote?>(null);
			}

			return _quote.Entity.Include(q => q.Project, q => q.Project.Customer, q => q.Items)
				.FirstOrDefaultAsync(q => q.Id == id)!;
		}

		private Task<string> GenerateQuoteNoAsync() =>
			DocumentNumbers.NextAsync(_quote.Entity.GetAll().Select(q => q.QuoteNo), "QT-");

		// قيمة العرض المعتمد تصبح القيمة النهائية للمشروع
		private async Task ApplyApprovedTotalAsync(Quote quote)
		{
			var project = await _project.Entity.GetByIdAsync(quote.ProjectId);
			if (project != null)
			{
				project.FinalAmount = quote.Total;
				project.Modified = DateTime.UtcNow;
			}
		}

		private void ValidateQuote(Quote quote)
		{
			if (quote.ProjectId == Guid.Empty)
				ModelState.AddModelError(nameof(Quote.ProjectId), "يجب اختيار المشروع");

			// الصفوف الفارغة في جدول البنود تُتجاهل
			quote.Items = quote.Items.Where(i => !string.IsNullOrWhiteSpace(i.Description)).ToList();
			foreach (var key in ModelState.Keys.Where(k => k.StartsWith("Items[")).ToList())
			{
				ModelState.Remove(key);
			}

			if (quote.Items.Count == 0)
				ModelState.AddModelError("", "يجب إضافة بند واحد على الأقل لعرض السعر");

			if (quote.Items.Any(i => i.Quantity <= 0 || i.UnitPrice < 0))
				ModelState.AddModelError("", "تأكد من أن الكميات أكبر من صفر والأسعار غير سالبة");

			if (quote.Tax < 0)
				ModelState.AddModelError(nameof(Quote.Tax), "الضريبة لا يمكن أن تكون سالبة");
		}

		private async Task FillSelectListsAsync(Quote quote)
		{
			ViewBag.Projects = await _lookup.ProjectsAsync(quote.ProjectId);
			ViewBag.StatusList = LookupService.EnumList(quote.Status);
		}
	}
}
