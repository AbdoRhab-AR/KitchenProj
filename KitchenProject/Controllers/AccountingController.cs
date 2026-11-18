using KitchenProject.Classes;
using KitchenProject.Models;
using KitchenProject.Models.Entities;
using KitchenProject.Models.Enums;
using KitchenProject.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace KitchenProject.Controllers
{
	[ViewLayout("_DashboardLayout")]

	// المحاسبة: شجرة الحسابات، القيود اليومية (يدوية وتلقائية)، كشف الحساب، ميزان المراجعة، قائمة الدخل، الميزانية، وإقفال الفترات
	public class AccountingController : Controller
	{
		private readonly ApplicationDbContext _db;
		private readonly AccountingService _accounting;
		private readonly LookupService _lookup;

		public AccountingController(ApplicationDbContext db, AccountingService accounting, LookupService lookup)
		{
			_db = db;
			_accounting = accounting;
			_lookup = lookup;
		}

		private async Task SyncAsync()
		{
			try
			{
				await _accounting.SyncAsync();
			}
			catch (InvalidOperationException ex)
			{
				TempData["Error"] = ex.Message;
			}
		}

		// ------------------------------------------------------------------ شجرة الحسابات

		// GET: Accounting/Accounts
		public async Task<IActionResult> Accounts()
		{
			ViewBag.ActivePage = "Accounts";
			await SyncAsync();

			var accounts = await _db.Accounts.AsNoTracking().ToListAsync();
			var totals = await _accounting.TotalsAsync();
			ViewBag.UsedIds = (await _db.JournalLines.Select(l => l.AccountId).Distinct().ToListAsync()).ToHashSet();
			return View(AccountingService.BuildTree(accounts, totals));
		}

		// GET: Accounting/CreateAccount
		public async Task<IActionResult> CreateAccount(Guid? parentId)
		{
			ViewBag.ActivePage = "Accounts";
			var model = new Account { IsPostable = true };
			if (parentId.HasValue)
			{
				var parent = await _db.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.Id == parentId.Value);
				if (parent != null)
				{
					model.ParentId = parent.Id;
					model.Type = parent.Type;
					model.Code = await NextCodeAsync(parent);
				}
			}

			await FillParentsAsync(model);
			return View("AccountForm", model);
		}

		// POST: Accounting/CreateAccount
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> CreateAccount(Account account)
		{
			ViewBag.ActivePage = "Accounts";
			await ValidateAccountAsync(account, null);

			if (ModelState.IsValid)
			{
				account.Id = Guid.NewGuid();
				account.Created = DateTime.UtcNow;
				account.Code = account.Code.Trim();
				account.Name = account.Name.Trim();
				account.SystemKey = null;
				_db.Accounts.Add(account);
				await _db.SaveChangesAsync();
				TempData["Success"] = $"تم إنشاء الحساب {account.Code} - {account.Name}";
				return RedirectToAction(nameof(Accounts));
			}

			await FillParentsAsync(account);
			return View("AccountForm", account);
		}

		// GET: Accounting/EditAccount/5
		public async Task<IActionResult> EditAccount(Guid id)
		{
			ViewBag.ActivePage = "Accounts";
			var account = await _db.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);
			if (account == null)
			{
				return NotFound();
			}

			await FillParentsAsync(account);
			return View("AccountForm", account);
		}

		// POST: Accounting/EditAccount/5
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> EditAccount(Guid id, Account input)
		{
			ViewBag.ActivePage = "Accounts";
			var account = await _db.Accounts.FirstOrDefaultAsync(a => a.Id == id);
			if (account == null)
			{
				return NotFound();
			}

			input.Id = id;
			input.SystemKey = account.SystemKey;
			// حساب النظام: لا يتغير نوعه ولا موقعه ولا رقمه لأن القيود التلقائية تعتمد عليه
			if (account.SystemKey != null)
			{
				input.Code = account.Code;
				input.Type = account.Type;
				input.ParentId = account.ParentId;
				input.IsPostable = account.IsPostable;
				input.IsActive = true;
				ModelState.Remove(nameof(Account.Code));
			}

			await ValidateAccountAsync(input, account);

			if (ModelState.IsValid)
			{
				account.Code = input.Code.Trim();
				account.Name = input.Name.Trim();
				account.Type = input.Type;
				account.ParentId = input.ParentId;
				account.IsPostable = input.IsPostable;
				account.IsActive = input.IsActive;
				account.Notes = input.Notes;
				account.Modified = DateTime.UtcNow;
				await _db.SaveChangesAsync();
				TempData["Success"] = "تم تحديث الحساب";
				return RedirectToAction(nameof(Accounts));
			}

			await FillParentsAsync(input);
			return View("AccountForm", input);
		}

		// POST: Accounting/DeleteAccount/5
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteAccount(Guid id)
		{
			var account = await _db.Accounts.FirstOrDefaultAsync(a => a.Id == id);
			if (account == null)
			{
				TempData["Error"] = "تعذر العثور على الحساب";
			}
			else if (account.SystemKey != null)
			{
				TempData["Error"] = "هذا حساب أساسي في النظام ولا يمكن حذفه";
			}
			else if (await _db.Accounts.AnyAsync(a => a.ParentId == id))
			{
				TempData["Error"] = "لا يمكن حذف حساب له حسابات فرعية";
			}
			else if (await _db.JournalLines.AnyAsync(l => l.AccountId == id))
			{
				TempData["Error"] = "لا يمكن حذف حساب عليه قيود، يمكنك تعطيله بدلاً من ذلك";
			}
			else
			{
				_db.Accounts.Remove(account);
				await _db.SaveChangesAsync();
				TempData["Success"] = "تم حذف الحساب";
			}

			return RedirectToAction(nameof(Accounts));
		}

		private async Task<string> NextCodeAsync(Account parent)
		{
			var siblings = await _db.Accounts.AsNoTracking()
				.Where(a => a.ParentId == parent.Id).Select(a => a.Code).ToListAsync();
			var max = siblings.Select(c => long.TryParse(c, out var n) ? n : 0).DefaultIfEmpty(0).Max();
			if (max > 0)
			{
				return (max + (siblings.Any(c => c.Length > parent.Code.Length + 1) ? 1 : 10)).ToString();
			}

			return parent.Code.Length >= 4 ? parent.Code + "1" : parent.Code + "1".PadRight(Math.Max(1, 4 - parent.Code.Length), '0');
		}

		private async Task ValidateAccountAsync(Account account, Account? current)
		{
			account.Code = (account.Code ?? "").Trim();

			if (await _db.Accounts.AnyAsync(a => a.Code == account.Code && a.Id != account.Id))
			{
				ModelState.AddModelError(nameof(Account.Code), "رقم الحساب مستخدم لحساب آخر");
			}

			if (account.ParentId.HasValue)
			{
				var all = await _db.Accounts.AsNoTracking().ToListAsync();
				var parent = all.FirstOrDefault(a => a.Id == account.ParentId.Value);
				if (parent == null)
				{
					ModelState.AddModelError(nameof(Account.ParentId), "الحساب الأب غير موجود");
				}
				else
				{
					if (parent.IsPostable && await _db.JournalLines.AnyAsync(l => l.AccountId == parent.Id))
					{
						ModelState.AddModelError(nameof(Account.ParentId), "الحساب الأب عليه قيود ولا يمكن أن تتفرع منه حسابات");
					}
					if (parent.Type != account.Type)
					{
						ModelState.AddModelError(nameof(Account.Type), "نوع الحساب يجب أن يطابق نوع الحساب الأب");
					}
					if (current != null && AccountingService.WithDescendants(all, current.Id).Contains(parent.Id))
					{
						ModelState.AddModelError(nameof(Account.ParentId), "لا يمكن جعل الحساب أباً لنفسه أو لأحد أبنائه");
					}
				}
			}

			if (current != null)
			{
				if (account.IsPostable && await _db.Accounts.AnyAsync(a => a.ParentId == current.Id))
				{
					ModelState.AddModelError(nameof(Account.IsPostable), "الحساب له حسابات فرعية فلا يمكن أن يكون حساباً نهائياً");
				}
				if (!account.IsPostable && await _db.JournalLines.AnyAsync(l => l.AccountId == current.Id))
				{
					ModelState.AddModelError(nameof(Account.IsPostable), "الحساب عليه قيود فلا يمكن تحويله إلى حساب تجميعي");
				}
				if (account.Type != current.Type && await _db.JournalLines.AnyAsync(l => l.AccountId == current.Id))
				{
					ModelState.AddModelError(nameof(Account.Type), "لا يمكن تغيير نوع حساب عليه قيود");
				}
			}
		}

		private async Task FillParentsAsync(Account model)
		{
			var excluded = model.Id != Guid.Empty
				? AccountingService.WithDescendants(await _db.Accounts.AsNoTracking().ToListAsync(), model.Id)
				: new HashSet<Guid>();

			var parents = await _db.Accounts.AsNoTracking()
				.Where(a => !excluded.Contains(a.Id))
				.OrderBy(a => a.Code)
				.Select(a => new { a.Id, Text = a.Code + " - " + a.Name })
				.ToListAsync();
			ViewBag.Parents = new SelectList(parents, "Id", "Text", model.ParentId);
		}

		private async Task<SelectList> PostableAccountsAsync(Guid? selected = null, bool includeGroups = false)
		{
			var query = _db.Accounts.AsNoTracking().Where(a => a.IsActive || a.Id == selected);
			if (!includeGroups)
			{
				query = query.Where(a => a.IsPostable);
			}

			var list = await query.OrderBy(a => a.Code).Select(a => new { a.Id, Text = a.Code + " - " + a.Name }).ToListAsync();
			return new SelectList(list, "Id", "Text", selected);
		}

		// ------------------------------------------------------------------ القيود اليومية

		// GET: Accounting   (قائمة القيود)
		public async Task<IActionResult> Index(string? searchString, string? source, DateTime? from, DateTime? to, Guid? accountId, int page = 1)
		{
			ViewBag.ActivePage = "Journal";
			await SyncAsync();

			var query = _db.JournalEntries.AsNoTracking().Include(e => e.Lines).AsQueryable();
			if (!string.IsNullOrWhiteSpace(searchString))
			{
				var text = searchString.Trim();
				query = query.Where(e => e.Description.Contains(text) || e.EntryNo.Contains(text));
			}
			if (source == "manual") query = query.Where(e => !e.IsAuto);
			else if (source == "auto") query = query.Where(e => e.IsAuto && e.SourceType != AccountingService.CloseSource);
			else if (source == "close") query = query.Where(e => e.SourceType == AccountingService.CloseSource);
			else if (!string.IsNullOrEmpty(source)) query = query.Where(e => e.SourceType == source);
			if (from.HasValue) query = query.Where(e => e.EntryDate >= from.Value.Date);
			if (to.HasValue) query = query.Where(e => e.EntryDate <= to.Value.Date);
			if (accountId.HasValue) query = query.Where(e => e.Lines.Any(l => l.AccountId == accountId.Value));

			var total = await query.CountAsync();
			int pageSize = await _lookup.PageSizeAsync();
			var entries = await query.OrderByDescending(e => e.EntryDate).ThenByDescending(e => e.EntryNo)
				.Skip((page - 1) * pageSize).Take(pageSize).AsSplitQuery().ToListAsync();

			ViewBag.CurrentPage = page;
			ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
			ViewBag.TotalCount = total;
			ViewBag.SearchString = searchString;
			ViewBag.Source = source;
			ViewBag.From = from;
			ViewBag.To = to;
			ViewBag.AccountId = accountId;
			ViewBag.Accounts = await PostableAccountsAsync(accountId, includeGroups: true);
			ViewBag.ClosedThrough = await _accounting.ClosedThroughAsync();
			return View(entries);
		}

		// POST: Accounting/SyncNow
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> SyncNow()
		{
			try
			{
				var result = await _accounting.SyncAsync();
				TempData["Success"] = result.Changed
					? $"تمت مزامنة القيود: {result.Added} جديد، {result.Updated} معدّل، {result.Removed} محذوف"
					: "القيود محدّثة ولا توجد تغييرات";
			}
			catch (InvalidOperationException ex)
			{
				TempData["Error"] = ex.Message;
			}

			return RedirectToAction(nameof(Index));
		}

		// GET: Accounting/JournalDetails/5
		public async Task<IActionResult> JournalDetails(Guid id)
		{
			ViewBag.ActivePage = "Journal";
			var entry = await _db.JournalEntries.AsNoTracking()
				.Include(e => e.Lines).ThenInclude(l => l.Account)
				.FirstOrDefaultAsync(e => e.Id == id);
			if (entry == null)
			{
				return NotFound();
			}

			var projectIds = entry.Lines.Where(l => l.ProjectId != null).Select(l => l.ProjectId!.Value).Distinct().ToList();
			ViewBag.Projects = await _db.Projects.AsNoTracking().Where(p => projectIds.Contains(p.Id))
				.ToDictionaryAsync(p => p.Id, p => p.ProjectNo + " - " + p.ProjectName);
			ViewBag.ClosedThrough = await _accounting.ClosedThroughAsync();
			return View(entry);
		}

		// GET: Accounting/CreateJournal
		public async Task<IActionResult> CreateJournal()
		{
			ViewBag.ActivePage = "Journal";
			var model = new JournalFormVm
			{
				Lines = new() { new JournalLineVm(), new JournalLineVm() }
			};
			await FillJournalFormAsync();
			return View(model);
		}

		// POST: Accounting/CreateJournal
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> CreateJournal(JournalFormVm model)
		{
			ViewBag.ActivePage = "Journal";
			model.Lines = (model.Lines ?? new()).Where(l => l.AccountId != Guid.Empty || l.Debit != 0 || l.Credit != 0).ToList();
			ModelState.Clear();

			var closed = await _accounting.ClosedThroughAsync();
			if (string.IsNullOrWhiteSpace(model.Description))
				ModelState.AddModelError("Description", "بيان القيد مطلوب");
			if (closed.HasValue && model.EntryDate.Date <= closed.Value)
				ModelState.AddModelError("EntryDate", $"الفترة مقفلة حتى {closed.Value:yyyy/MM/dd}");
			if (model.Lines.Count < 2)
				ModelState.AddModelError("", "القيد يحتاج سطرين على الأقل");
			if (model.Lines.Any(l => l.AccountId == Guid.Empty))
				ModelState.AddModelError("", "اختر الحساب في كل سطر");
			if (model.Lines.Any(l => l.Debit < 0 || l.Credit < 0))
				ModelState.AddModelError("", "المبالغ لا تكون سالبة");
			if (model.Lines.Any(l => (l.Debit > 0) == (l.Credit > 0)))
				ModelState.AddModelError("", "كل سطر إما مدين أو دائن (وليس الاثنين أو لا شيء)");

			var debit = model.Lines.Sum(l => l.Debit);
			var credit = model.Lines.Sum(l => l.Credit);
			if (debit != credit)
				ModelState.AddModelError("", $"القيد غير متزن: المدين {debit:N2} والدائن {credit:N2}");

			var ids = model.Lines.Select(l => l.AccountId).Distinct().ToList();
			var valid = await _db.Accounts.Where(a => ids.Contains(a.Id) && a.IsPostable && a.IsActive).CountAsync();
			if (valid != ids.Count)
				ModelState.AddModelError("", "يجب أن تكون كل الحسابات نهائية وفعالة");

			if (ModelState.IsValid)
			{
				var now = DateTime.UtcNow;
				var entry = new JournalEntry
				{
					Id = Guid.NewGuid(),
					Created = now,
					EntryNo = await DocumentNumbers.NextAsync(_db.JournalEntries.Select(e => e.EntryNo), "JV-"),
					EntryDate = model.EntryDate.Date,
					Description = model.Description.Trim(),
					IsAuto = false,
					CreatedBy = User.Identity?.Name
				};
				foreach (var l in model.Lines)
				{
					entry.Lines.Add(new JournalLine
					{
						Id = Guid.NewGuid(),
						Created = now,
						AccountId = l.AccountId,
						Debit = l.Debit,
						Credit = l.Credit,
						Description = l.Description
					});
				}

				_db.JournalEntries.Add(entry);
				await _db.SaveChangesAsync();
				TempData["Success"] = $"تم تسجيل القيد {entry.EntryNo}";
				return RedirectToAction(nameof(JournalDetails), new { id = entry.Id });
			}

			while (model.Lines.Count < 2) model.Lines.Add(new JournalLineVm());
			await FillJournalFormAsync();
			return View(model);
		}

		private async Task FillJournalFormAsync()
		{
			ViewBag.Accounts = await PostableAccountsAsync();
			ViewBag.ClosedThrough = await _accounting.ClosedThroughAsync();
		}

		// GET: Accounting/EditJournal/5
		public async Task<IActionResult> EditJournal(Guid id)
		{
			ViewBag.ActivePage = "Journal";
			var entry = await _db.JournalEntries.AsNoTracking().Include(e => e.Lines).FirstOrDefaultAsync(e => e.Id == id);
			if (entry == null) return NotFound();

			var closed = await _accounting.ClosedThroughAsync();
			if (entry.IsAuto)
			{
				TempData["Error"] = "القيود التلقائية تتبع مستنداتها ولا تُعدل يدوياً";
				return RedirectToAction(nameof(JournalDetails), new { id });
			}
			if (closed.HasValue && entry.EntryDate.Date <= closed.Value)
			{
				TempData["Error"] = "القيد في فترة مقفلة ولا يمكن تعديله";
				return RedirectToAction(nameof(JournalDetails), new { id });
			}

			var model = new JournalFormVm
			{
				Id = entry.Id,
				EntryNo = entry.EntryNo,
				EntryDate = entry.EntryDate,
				Description = entry.Description,
				Lines = entry.Lines.Select(l => new JournalLineVm
				{
					AccountId = l.AccountId,
					Debit = l.Debit,
					Credit = l.Credit,
					Description = l.Description
				}).ToList()
			};
			while (model.Lines.Count < 2) model.Lines.Add(new JournalLineVm());
			await FillJournalFormAsync();
			return View("CreateJournal", model);
		}

		// POST: Accounting/EditJournal/5
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> EditJournal(Guid id, JournalFormVm model)
		{
			ViewBag.ActivePage = "Journal";
			var entry = await _db.JournalEntries.Include(e => e.Lines).FirstOrDefaultAsync(e => e.Id == id);
			if (entry == null) return NotFound();
			if (entry.IsAuto)
			{
				TempData["Error"] = "القيود التلقائية تتبع مستنداتها";
				return RedirectToAction(nameof(JournalDetails), new { id });
			}

			model.Id = id;
			model.EntryNo = entry.EntryNo;
			model.Lines = (model.Lines ?? new()).Where(l => l.AccountId != Guid.Empty || l.Debit != 0 || l.Credit != 0).ToList();
			ModelState.Clear();

			var closed = await _accounting.ClosedThroughAsync();
			if (closed.HasValue && (entry.EntryDate.Date <= closed.Value || model.EntryDate.Date <= closed.Value))
				ModelState.AddModelError("EntryDate", $"الفترة مقفلة حتى {closed.Value:yyyy/MM/dd}");
			if (string.IsNullOrWhiteSpace(model.Description))
				ModelState.AddModelError("Description", "بيان القيد مطلوب");
			if (model.Lines.Count < 2)
				ModelState.AddModelError("", "القيد يحتاج سطرين على الأقل");
			if (model.Lines.Any(l => l.AccountId == Guid.Empty))
				ModelState.AddModelError("", "اختر الحساب في كل سطر");
			if (model.Lines.Any(l => l.Debit < 0 || l.Credit < 0))
				ModelState.AddModelError("", "المبالغ لا تكون سالبة");
			if (model.Lines.Any(l => (l.Debit > 0) == (l.Credit > 0)))
				ModelState.AddModelError("", "كل سطر إما مدين أو دائن");

			var debit = model.Lines.Sum(l => l.Debit);
			var credit = model.Lines.Sum(l => l.Credit);
			if (debit != credit)
				ModelState.AddModelError("", $"القيد غير متزن: المدين {debit:N2} والدائن {credit:N2}");

			var ids = model.Lines.Select(l => l.AccountId).Distinct().ToList();
			var valid = await _db.Accounts.Where(a => ids.Contains(a.Id) && a.IsPostable && a.IsActive).CountAsync();
			if (valid != ids.Count)
				ModelState.AddModelError("", "يجب أن تكون كل الحسابات نهائية وفعالة");

			if (ModelState.IsValid)
			{
				var now = DateTime.UtcNow;
				entry.EntryDate = model.EntryDate.Date;
				entry.Description = model.Description.Trim();
				entry.Modified = now;
				_db.JournalLines.RemoveRange(entry.Lines);
				foreach (var l in model.Lines)
				{
					_db.JournalLines.Add(new JournalLine
					{
						Id = Guid.NewGuid(),
						Created = now,
						JournalEntryId = entry.Id,
						AccountId = l.AccountId,
						Debit = l.Debit,
						Credit = l.Credit,
						Description = l.Description
					});
				}
				await _db.SaveChangesAsync();
				TempData["Success"] = $"تم تعديل القيد {entry.EntryNo}";
				return RedirectToAction(nameof(JournalDetails), new { id = entry.Id });
			}

			while (model.Lines.Count < 2) model.Lines.Add(new JournalLineVm());
			await FillJournalFormAsync();
			return View("CreateJournal", model);
		}

		// GET: Accounting/JournalDetailsPrint/5  (A4)
		public async Task<IActionResult> JournalDetailsPrint(Guid id)
		{
			var entry = await _db.JournalEntries.AsNoTracking()
				.Include(e => e.Lines).ThenInclude(l => l.Account)
				.FirstOrDefaultAsync(e => e.Id == id);
			if (entry == null) return NotFound();

			var projectIds = entry.Lines.Where(l => l.ProjectId != null).Select(l => l.ProjectId!.Value).Distinct().ToList();
			ViewBag.Projects = await _db.Projects.AsNoTracking().Where(p => projectIds.Contains(p.Id))
				.ToDictionaryAsync(p => p.Id, p => p.ProjectNo + " - " + p.ProjectName);
			ViewBag.PrintedBy = User.Identity?.Name;
			return View(entry);
		}

		// POST: Accounting/DeleteJournal/5   (القيود اليدوية فقط، والتلقائية تتبع مستنداتها)
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteJournal(Guid id)
		{
			var entry = await _db.JournalEntries.FirstOrDefaultAsync(e => e.Id == id);
			var closed = await _accounting.ClosedThroughAsync();
			if (entry == null)
			{
				TempData["Error"] = "تعذر العثور على القيد";
			}
			else if (entry.IsAuto)
			{
				TempData["Error"] = "القيود التلقائية تتبع المستند المصدر، عدّل المستند أو احذفه بدلاً من ذلك";
			}
			else if (closed.HasValue && entry.EntryDate.Date <= closed.Value)
			{
				TempData["Error"] = "القيد في فترة مقفلة";
			}
			else
			{
				_db.JournalEntries.Remove(entry);
				await _db.SaveChangesAsync();
				TempData["Success"] = $"تم حذف القيد {entry.EntryNo}";
			}

			return RedirectToAction(nameof(Index));
		}

		// ------------------------------------------------------------------ كشف الحساب (دفتر الأستاذ)

		private async Task<List<LedgerRow>?> BuildLedgerAsync(Guid accountId, DateTime? from, DateTime? to)
		{
			var all = await _db.Accounts.AsNoTracking().ToListAsync();
			var account = all.FirstOrDefault(a => a.Id == accountId);
			if (account == null) return null;

			var ids = AccountingService.WithDescendants(all, account.Id);
			var lines = _db.JournalLines.AsNoTracking().Where(l => ids.Contains(l.AccountId));

			decimal opening = 0;
			if (from.HasValue)
			{
				var before = await lines.Where(l => l.JournalEntry.EntryDate < from.Value.Date)
					.GroupBy(_ => 1).Select(g => new { D = g.Sum(l => l.Debit), C = g.Sum(l => l.Credit) }).FirstOrDefaultAsync();
				opening = before == null ? 0 : (account.IsDebitNature ? before.D - before.C : before.C - before.D);
				lines = lines.Where(l => l.JournalEntry.EntryDate >= from.Value.Date);
			}
			if (to.HasValue)
			{
				lines = lines.Where(l => l.JournalEntry.EntryDate <= to.Value.Date);
			}

			var data = await lines.OrderBy(l => l.JournalEntry.EntryDate).ThenBy(l => l.JournalEntry.EntryNo)
				.Select(l => new
				{
					l.JournalEntry.EntryDate, l.JournalEntryId, l.JournalEntry.EntryNo,
					Description = l.Description ?? l.JournalEntry.Description,
					AccountName = l.Account.Name, l.Debit, l.Credit
				}).ToListAsync();

			var rows = new List<LedgerRow>();
			var balance = opening;
			foreach (var r in data)
			{
				balance += account.IsDebitNature ? r.Debit - r.Credit : r.Credit - r.Debit;
				rows.Add(new LedgerRow
				{
					Date = r.EntryDate, EntryId = r.JournalEntryId, EntryNo = r.EntryNo, Description = r.Description,
					AccountName = r.AccountName, Debit = r.Debit, Credit = r.Credit, Balance = balance
				});
			}

			ViewBag.Account = account;
			ViewBag.Opening = opening;
			ViewBag.IsGroup = ids.Count > 1;
			return rows;
		}

		// GET: Accounting/Ledger?accountId=...
		public async Task<IActionResult> Ledger(Guid? accountId, DateTime? from, DateTime? to)
		{
			ViewBag.ActivePage = "Ledger";
			await SyncAsync();

			ViewBag.AccountId = accountId;
			ViewBag.From = from;
			ViewBag.To = to;
			ViewBag.Accounts = await PostableAccountsAsync(accountId, includeGroups: true);

			if (!accountId.HasValue) return View(new List<LedgerRow>());
			var rows = await BuildLedgerAsync(accountId.Value, from, to);
			return rows == null ? NotFound() : View(rows);
		}

		// GET: Accounting/LedgerPrint  (A4)
		public async Task<IActionResult> LedgerPrint(Guid accountId, DateTime? from, DateTime? to)
		{
			await SyncAsync();
			ViewBag.From = from;
			ViewBag.To = to;
			ViewBag.PrintedBy = User.Identity?.Name;
			var rows = await BuildLedgerAsync(accountId, from, to);
			return rows == null ? NotFound() : View(rows);
		}

		// ------------------------------------------------------------------ ميزان المراجعة

		private async Task<List<TrialRow>> BuildTrialBalanceAsync(DateTime? from, DateTime? to)
		{
			var accounts = await _db.Accounts.AsNoTracking().ToListAsync();
			var before = from.HasValue ? await _accounting.TotalsAsync(to: from.Value.AddDays(-1)) : new();
			var movement = await _accounting.TotalsAsync(from, to);

			return accounts.Where(a => a.IsPostable)
				.Select(a =>
				{
					var (bd, bc) = before.TryGetValue(a.Id, out var b) ? b : (0m, 0m);
					var (md, mc) = movement.TryGetValue(a.Id, out var m) ? m : (0m, 0m);
					return new TrialRow(a, bd - bc, md, mc, bd - bc + md - mc);
				})
				.Where(r => r.Opening != 0 || r.Debit != 0 || r.Credit != 0)
				.OrderBy(r => r.Account.Code, StringComparer.Ordinal)
				.ToList();
		}

		// GET: Accounting/TrialBalance
		public async Task<IActionResult> TrialBalance(DateTime? from, DateTime? to)
		{
			ViewBag.ActivePage = "TrialBalance";
			await SyncAsync();
			ViewBag.From = from;
			ViewBag.To = to;
			return View(await BuildTrialBalanceAsync(from, to));
		}

		// GET: Accounting/TrialBalancePrint  (A4)
		public async Task<IActionResult> TrialBalancePrint(DateTime? from, DateTime? to)
		{
			await SyncAsync();
			ViewBag.From = from;
			ViewBag.To = to;
			ViewBag.PrintedBy = User.Identity?.Name;
			return View(await BuildTrialBalanceAsync(from, to));
		}

		public record TrialRow(Account Account, decimal Opening, decimal Debit, decimal Credit, decimal Closing);

		// ------------------------------------------------------------------ قائمة الدخل

		private async Task<List<AccountNode>> BuildIncomeStatementAsync(DateTime? from, DateTime? to, Guid? projectId)
		{
			from ??= new DateTime(DateTime.Today.Year, 1, 1);
			to ??= DateTime.Today;
			ViewBag.From = from;
			ViewBag.To = to;
			ViewBag.ProjectId = projectId;

			var totals = await _accounting.TotalsAsync(from, to, excludeClosing: true, projectId: projectId);
			var accounts = await _db.Accounts.AsNoTracking().ToListAsync();
			return AccountingService.BuildTree(accounts.Where(a => a.Type == AccountType.Revenue || a.Type == AccountType.Expense), totals);
		}

		// GET: Accounting/IncomeStatement
		public async Task<IActionResult> IncomeStatement(DateTime? from, DateTime? to, Guid? projectId)
		{
			ViewBag.ActivePage = "IncomeStatement";
			await SyncAsync();
			ViewBag.Projects = await _lookup.ProjectsAsync(projectId);
			return View(await BuildIncomeStatementAsync(from, to, projectId));
		}

		// GET: Accounting/IncomeStatementPrint  (A4)
		public async Task<IActionResult> IncomeStatementPrint(DateTime? from, DateTime? to, Guid? projectId)
		{
			await SyncAsync();
			if (projectId.HasValue)
			{
				ViewBag.ProjectName = await _db.Projects.AsNoTracking().Where(p => p.Id == projectId.Value)
					.Select(p => p.ProjectNo + " - " + p.ProjectName).FirstOrDefaultAsync();
			}
			ViewBag.PrintedBy = User.Identity?.Name;
			return View(await BuildIncomeStatementAsync(from, to, projectId));
		}

		// ------------------------------------------------------------------ الميزانية العمومية

		private async Task<List<AccountNode>> BuildBalanceSheetAsync(DateTime? asOf)
		{
			asOf ??= DateTime.Today;
			ViewBag.AsOf = asOf;
			var totals = await _accounting.TotalsAsync(to: asOf);
			var accounts = await _db.Accounts.AsNoTracking().ToListAsync();
			return AccountingService.BuildTree(accounts, totals);
		}

		// GET: Accounting/BalanceSheet
		public async Task<IActionResult> BalanceSheet(DateTime? asOf)
		{
			ViewBag.ActivePage = "BalanceSheet";
			await SyncAsync();
			return View(await BuildBalanceSheetAsync(asOf));
		}

		// GET: Accounting/BalanceSheetPrint  (A4)
		public async Task<IActionResult> BalanceSheetPrint(DateTime? asOf)
		{
			await SyncAsync();
			ViewBag.PrintedBy = User.Identity?.Name;
			return View(await BuildBalanceSheetAsync(asOf));
		}

		// ------------------------------------------------------------------ إقفال الفترات

		// GET: Accounting/Periods
		public async Task<IActionResult> Periods()
		{
			ViewBag.ActivePage = "Periods";
			await SyncAsync();
			ViewBag.CanClose = AppRoles.IsFullAccess(User);
			return View(await _db.PeriodCloses.AsNoTracking().OrderByDescending(p => p.ClosedThrough).ToListAsync());
		}

		// POST: Accounting/ClosePeriod
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> ClosePeriod(DateTime through, string? notes)
		{
			if (!AppRoles.IsFullAccess(User))
			{
				return Forbid();
			}

			var (ok, message) = await _accounting.ClosePeriodAsync(through, notes, User.Identity?.Name);
			TempData[ok ? "Success" : "Error"] = message;
			return RedirectToAction(nameof(Periods));
		}

		// POST: Accounting/ReopenPeriod
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> ReopenPeriod()
		{
			if (!AppRoles.IsFullAccess(User))
			{
				return Forbid();
			}

			var (ok, message) = await _accounting.ReopenLastPeriodAsync();
			TempData[ok ? "Success" : "Error"] = message;
			return RedirectToAction(nameof(Periods));
		}

		// ------------------------------------------------------------------ الأرصدة الافتتاحية

		// GET: Accounting/OpeningBalances
		public async Task<IActionResult> OpeningBalances()
		{
			ViewBag.ActivePage = "OpeningBalances";
			var existing = await _db.JournalEntries.AsNoTracking()
				.Include(e => e.Lines)
				.FirstOrDefaultAsync(e => e.SourceType == AccountingService.OpeningSource);

			var accounts = await _db.Accounts.AsNoTracking()
				.Where(a => a.IsPostable && a.IsActive && (a.Type == AccountType.Asset || a.Type == AccountType.Liability || a.Type == AccountType.Equity))
				.OrderBy(a => a.Code)
				.ToListAsync();

			var existingLines = existing?.Lines.ToDictionary(l => l.AccountId) ?? new Dictionary<Guid, JournalLine>();
			var vm = new OpeningBalancesVm
			{
				EntryDate = existing?.EntryDate ?? new DateTime(DateTime.Today.Year, 1, 1),
				Description = existing?.Description ?? "الأرصدة الافتتاحية",
				Lines = accounts.Select(a =>
				{
					existingLines.TryGetValue(a.Id, out var l);
					return new OpeningBalanceLineVm
					{
						AccountId = a.Id,
						Code = a.Code,
						Name = a.Name,
						TypeName = a.Type.GetDisplayName(),
						IsDebitNature = a.IsDebitNature,
						Debit = l?.Debit ?? 0,
						Credit = l?.Credit ?? 0
					};
				}).ToList()
			};
			ViewBag.HasExisting = existing != null;
			ViewBag.ClosedThrough = await _accounting.ClosedThroughAsync();
			return View(vm);
		}

		// POST: Accounting/OpeningBalances
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> OpeningBalances(OpeningBalancesVm model)
		{
			ViewBag.ActivePage = "OpeningBalances";

			// خانات المدين/الدائن الفارغة تُقرأ صفراً، والتحقق الفعلي للأسطر يتم أدناه
			foreach (var key in ModelState.Keys.Where(k => k.StartsWith("Lines[", StringComparison.Ordinal)).ToList())
				ModelState.Remove(key);

			var closed = await _accounting.ClosedThroughAsync();
			if (closed.HasValue && model.EntryDate.Date <= closed.Value)
			{
				ModelState.AddModelError("EntryDate", $"الفترة مقفلة حتى {closed.Value:yyyy/MM/dd}");
			}

			var nonZero = (model.Lines ?? new()).Where(l => l.Debit > 0 || l.Credit > 0).ToList();
			if (nonZero.Any(l => l.Debit > 0 && l.Credit > 0))
				ModelState.AddModelError("", "لا يمكن إدخال مدين ودائن في نفس الحساب");

			var totalDebit = nonZero.Sum(l => l.Debit);
			var totalCredit = nonZero.Sum(l => l.Credit);
			if (nonZero.Count == 0)
				ModelState.AddModelError("", "أدخل رصيداً واحداً على الأقل");
			else if (totalDebit != totalCredit)
				ModelState.AddModelError("", $"الأرصدة الافتتاحية غير متزنة: المدين {totalDebit:N2} والدائن {totalCredit:N2}");

			if (ModelState.IsValid)
			{
				var now = DateTime.UtcNow;
				var existing = await _db.JournalEntries.Include(e => e.Lines)
					.FirstOrDefaultAsync(e => e.SourceType == AccountingService.OpeningSource);

				if (existing != null)
				{
					if (closed.HasValue && existing.EntryDate.Date <= closed.Value)
					{
						TempData["Error"] = "قيد الأرصدة الافتتاحية في فترة مقفلة";
						return RedirectToAction(nameof(OpeningBalances));
					}
					_db.JournalLines.RemoveRange(existing.Lines);
					existing.EntryDate = model.EntryDate.Date;
					existing.Description = string.IsNullOrWhiteSpace(model.Description) ? "الأرصدة الافتتاحية" : model.Description.Trim();
					existing.Modified = now;
				}
				else
				{
					existing = new JournalEntry
					{
						Id = Guid.NewGuid(),
						Created = now,
						EntryNo = await DocumentNumbers.NextAsync(_db.JournalEntries.Select(e => e.EntryNo), "JV-"),
						EntryDate = model.EntryDate.Date,
						Description = string.IsNullOrWhiteSpace(model.Description) ? "الأرصدة الافتتاحية" : model.Description.Trim(),
						IsAuto = true,
						SourceType = AccountingService.OpeningSource,
						SourceId = Guid.NewGuid(),
						CreatedBy = User.Identity?.Name
					};
					_db.JournalEntries.Add(existing);
				}

				foreach (var l in nonZero)
				{
					_db.JournalLines.Add(new JournalLine
					{
						Id = Guid.NewGuid(),
						Created = now,
						JournalEntryId = existing.Id,
						AccountId = l.AccountId,
						Debit = l.Debit,
						Credit = l.Credit,
						Description = "رصيد افتتاحي"
					});
				}

				await _db.SaveChangesAsync();
				TempData["Success"] = $"تم حفظ قيد الأرصدة الافتتاحية ({existing.EntryNo})";
				return RedirectToAction(nameof(JournalDetails), new { id = existing.Id });
			}

			ViewBag.HasExisting = await _db.JournalEntries.AnyAsync(e => e.SourceType == AccountingService.OpeningSource);
			ViewBag.ClosedThrough = closed;
			return View(model);
		}

		// ------------------------------------------------------------------ متابعة الصكوك / الشيكات

		// GET: Accounting/Cheques
		public async Task<IActionResult> Cheques(ChequeStatus? status, string? kind)
		{
			ViewBag.ActivePage = "Cheques";
			ViewBag.Status = status;
			ViewBag.Kind = kind;

			var list = new List<ChequeRowVm>();

			if (string.IsNullOrEmpty(kind) || kind == "customer")
			{
				var cq = _db.CustomerPayments.AsNoTracking()
					.Include(p => p.Project).ThenInclude(pr => pr.Customer)
					.Where(p => p.Payment == PaymentMethod.Chek);
				if (status.HasValue) cq = cq.Where(p => (p.ChequeStatus ?? ChequeStatus.Cleared) == status.Value);

				list.AddRange((await cq.ToListAsync()).Select(p => new ChequeRowVm
				{
					IsCustomer = true,
					Id = p.Id,
					ReceiptNo = p.ReceiptNo,
					PaymentDate = p.PaymentDate,
					DueDate = p.ChequeDueDate,
					ReferenceNo = p.ReferenceNo,
					PartyName = p.Project.Customer.Name,
					LinkText = p.Project.ProjectNo,
					Amount = p.Amount,
					Status = p.ChequeStatus ?? ChequeStatus.Cleared
				}));
			}

			if (string.IsNullOrEmpty(kind) || kind == "supplier")
			{
				var sq = _db.SupplierPayments.AsNoTracking()
					.Include(p => p.supplier)
					.Include(p => p.Purchase!)
					.Where(p => p.PaymentMethod == PaymentMethod.Chek);
				if (status.HasValue) sq = sq.Where(p => (p.ChequeStatus ?? ChequeStatus.Cleared) == status.Value);

				list.AddRange((await sq.ToListAsync()).Select(p => new ChequeRowVm
				{
					IsCustomer = false,
					Id = p.Id,
					ReceiptNo = p.ReceiptNo,
					PaymentDate = p.PaymentDate,
					DueDate = p.ChequeDueDate,
					ReferenceNo = p.ReferenceNo,
					PartyName = p.supplier.Name,
					LinkText = p.Purchase?.PurchaseNo ?? "",
					Amount = p.Amount,
					Status = p.ChequeStatus ?? ChequeStatus.Cleared
				}));
			}

			return View(list.OrderBy(c => c.DueDate ?? c.PaymentDate).ToList());
		}

		// POST: Accounting/UpdateChequeStatus
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> UpdateChequeStatus(bool isCustomer, Guid id, ChequeStatus newStatus)
		{
			if (isCustomer)
			{
				var p = await _db.CustomerPayments.FindAsync(id);
				if (p == null) return NotFound();
				p.ChequeStatus = newStatus;
				p.Modified = DateTime.UtcNow;
				await _db.SaveChangesAsync();

				if (p.ContractId.HasValue)
				{
					var contract = await _db.Contracts.FindAsync(p.ContractId.Value);
					if (contract != null)
					{
						contract.PaidAmount = await _db.CustomerPayments
							.Where(x => x.ContractId == contract.Id && !(x.Payment == PaymentMethod.Chek && x.ChequeStatus == ChequeStatus.Bounced))
							.SumAsync(x => (decimal?)(x.IsRefund ? -x.Amount : x.Amount)) ?? 0;
						contract.Modified = DateTime.UtcNow;
						await _db.SaveChangesAsync();
					}
				}
			}
			else
			{
				var p = await _db.SupplierPayments.FindAsync(id);
				if (p == null) return NotFound();
				p.ChequeStatus = newStatus;
				p.Modified = DateTime.UtcNow;
				await _db.SaveChangesAsync();

				if (p.PurchaseId.HasValue)
				{
					var purchase = await _db.Purchases.FindAsync(p.PurchaseId.Value);
					if (purchase != null)
					{
						purchase.PaidAmount = await _db.SupplierPayments
							.Where(x => x.PurchaseId == purchase.Id && !(x.PaymentMethod == PaymentMethod.Chek && x.ChequeStatus == ChequeStatus.Bounced))
							.SumAsync(x => (decimal?)x.Amount) ?? 0;
						purchase.Modified = DateTime.UtcNow;
						await _db.SaveChangesAsync();
					}
				}
			}

			await _accounting.SyncAsync();
			TempData["Success"] = $"تم تحديث حالة الصك إلى: {newStatus.GetDisplayName()}";
			return RedirectToAction(nameof(Cheques));
		}
	}
}
