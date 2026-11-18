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
		private readonly IUnitOfWork<SiteInfo> _siteInfo;
		private readonly IUnitOfWork<Item> _catalogItem;
		private readonly LookupService _lookup;
		private readonly StockService _stock;
		private readonly IWebHostEnvironment _env;

		public QuotesController(IUnitOfWork<Quote> quote,
								IUnitOfWork<QuoteItem> item,
								IUnitOfWork<Project> project,
								IUnitOfWork<Contract> contract,
								IUnitOfWork<SiteInfo> siteInfo,
								IUnitOfWork<Item> catalogItem,
								LookupService lookup,
								StockService stock,
								IWebHostEnvironment env)
		{
			_siteInfo = siteInfo;
			_quote = quote;
			_item = item;
			_project = project;
			_contract = contract;
			_catalogItem = catalogItem;
			_lookup = lookup;
			_stock = stock;
			_env = env;
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
				if (!SearchValidator.TryValidateNameSearch(searchString, out var validName, out var searchError))
				{
					TempData["Error"] = searchError;
					ViewBag.SearchError = searchError;
					query = query.Where(_ => false);
				}
				else
				{
					query = query.Where(q => q.Project.ProjectName.Contains(validName)
										  || q.Project.Customer.Name.Contains(validName));
				}
			}

			if (status.HasValue)
			{
				query = query.Where(q => q.Status == status.Value);
			}

			var totalCount = await query.CountAsync();
			int pageSize = await _lookup.PageSizeAsync();
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
			ViewBag.Balances = await _stock.BalancesAsync();
			ViewBag.Pics = FileUploads.GetPics(_env, "quote", quote.Id);
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
				Notes = await _siteInfo.Entity.GetAll().Select(s => s.DefaultQuoteTerms).FirstOrDefaultAsync(),
				Items = { new QuoteItem() }
			};

			await FillSelectListsAsync(model);
			return View(model);
		}

		// POST: Quotes/Create
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(Quote quote, List<IFormFile>? designFiles)
		{
			ViewBag.ActivePage = "Quotes";
			await ValidateQuoteAsync(quote);

			if (await _quote.Entity.GetAll().AnyAsync(q => q.QuoteNo == quote.QuoteNo))
			{
				// رقم آخر حجزه مستخدم ثاني أثناء تعبئة النموذج
				quote.QuoteNo = await GenerateQuoteNoAsync();
			}

			quote.Id = Guid.NewGuid();
			if (ModelState.IsValid)
			{
				var (_, uploadError) = await FileUploads.SavePicsAsync(designFiles, _env, "quote", quote.Id);
				if (uploadError != null)
				{
					ModelState.AddModelError("", uploadError);
				}
			}

			if (ModelState.IsValid)
			{
				quote.Created = DateTime.UtcNow;
				// فرق tick بين البنود يحفظ ترتيبها عند العرض والطباعة
				var order = 0;
				foreach (var item in quote.Items)
				{
					item.Id = Guid.NewGuid();
					item.QuoteId = quote.Id;
					item.Created = quote.Created.AddTicks(order++);
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

			quote.Items = quote.Items.OrderBy(i => i.Created).ToList();
			if (quote.Items.Count == 0)
			{
				quote.Items.Add(new QuoteItem());
			}

			ViewBag.Pics = FileUploads.GetPics(_env, "quote", quote.Id);
			await FillSelectListsAsync(quote);
			return View(quote);
		}

		// POST: Quotes/Edit/5
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(Guid id, Quote quote, List<IFormFile>? designFiles, List<string>? removePics)
		{
			ViewBag.ActivePage = "Quotes";

			if (id != quote.Id)
			{
				return NotFound();
			}

			await ValidateQuoteAsync(quote);

			if (ModelState.IsValid)
			{
				var (_, uploadError) = await FileUploads.SavePicsAsync(designFiles, _env, "quote", quote.Id);
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

				quote.Created = await _quote.Entity.GetWhere(q => q.Id == id).Select(q => q.Created).FirstOrDefaultAsync();
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

				var now = DateTime.UtcNow;
				var order = 0;
				foreach (var item in newItems)
				{
					item.Id = Guid.NewGuid();
					item.QuoteId = quote.Id;
					item.Created = now.AddTicks(order++);
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

			ViewBag.Pics = FileUploads.GetPics(_env, "quote", quote.Id);
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
			FileUploads.DeleteAllPics(_env, "quote", quote.Id);
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

			return _quote.Entity.Include(q => q.Project, q => q.Project.Customer)
				.Include(q => q.Items).ThenInclude(i => i.Item)
				.AsSplitQuery()
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

		private async Task ValidateQuoteAsync(Quote quote)
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
			else if (quote.Items.Count > 1)
				quote.Items = MergeDuplicateCatalogItems(quote.Items);

			var catalogIds = quote.Items
				.Where(i => i.ItemId.HasValue && i.ItemId.Value != Guid.Empty)
				.Select(i => i.ItemId!.Value)
				.Distinct()
				.ToList();

			if (catalogIds.Count > 0)
			{
				var catalogItems = await _catalogItem.Entity.GetWhere(i => catalogIds.Contains(i.Id))
					.ToDictionaryAsync(i => i.Id);
				var avgCosts = await _stock.AverageCostsAsync();

				foreach (var line in quote.Items)
				{
					if (line.ItemId is not { } itemId || !catalogItems.TryGetValue(itemId, out var catItem))
						continue;

					var cost = Math.Max(catItem.PurchasePrice, avgCosts.GetValueOrDefault(itemId));
					if (cost > 0 && line.UnitPrice < cost)
					{
						ModelState.AddModelError("", $"سعر الوحدة للبند «{catItem.Name}» ({line.UnitPrice:N2}) أقل من سعر تكلفته/شرائه ({cost:N2})");
					}
				}
			}

			if (quote.Tax < 0)
				ModelState.AddModelError(nameof(Quote.Tax), "الضريبة لا يمكن أن تكون سالبة");
		}

		// دمج الأصناف المكررة من الكتالوج بزيادة كمية أول صف بدلاً من تكرار الصفوف
		private static List<QuoteItem> MergeDuplicateCatalogItems(List<QuoteItem> items)
		{
			var result = new List<QuoteItem>(items.Count);
			var firstByItemId = new Dictionary<Guid, QuoteItem>();

			foreach (var item in items)
			{
				if (item.ItemId is { } itemId && itemId != Guid.Empty)
				{
					if (firstByItemId.TryGetValue(itemId, out var first))
					{
						first.Quantity += item.Quantity;
						if (!string.IsNullOrWhiteSpace(item.Notes))
						{
							var extraNote = item.Notes.Trim();
							var existingNote = first.Notes?.Trim() ?? string.Empty;
							if (string.IsNullOrEmpty(existingNote))
							{
								first.Notes = extraNote;
							}
							else if (!existingNote.Contains(extraNote, StringComparison.Ordinal))
							{
								var combined = $"{existingNote} | {extraNote}";
								first.Notes = combined.Length > 250 ? combined[..250] : combined;
							}
						}
						continue;
					}

					firstByItemId[itemId] = item;
				}

				result.Add(item);
			}

			return result;
		}

		private async Task FillSelectListsAsync(Quote quote)
		{
			ViewBag.Projects = await _lookup.ProjectsAsync(quote.ProjectId);
			ViewBag.StatusList = LookupService.EnumList(quote.Status);
			ViewBag.ItemOptions = await _lookup.QuoteItemOptionsAsync();
		}
	}
}
