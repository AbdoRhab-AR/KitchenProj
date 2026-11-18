using KitchenProject.Models;
using KitchenProject.Models.Entities;
using KitchenProject.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace KitchenProject.Classes
{
	public record SyncResult(int Added, int Updated, int Removed)
	{
		public bool Changed => Added + Updated + Removed > 0;
	}

	// صف في شجرة الحسابات مع أرصدته (مجمّعة من الحسابات الفرعية)
	public class AccountNode
	{
		public Account Account { get; set; } = null!;
		public int Depth { get; set; }
		public decimal Debit { get; set; }
		public decimal Credit { get; set; }
		public decimal Balance => Account.IsDebitNature ? Debit - Credit : Credit - Debit;
	}

	// الدورة المالية: تولّد القيود اليومية تلقائياً من المستندات الموجودة (عقود، تحصيلات، مشتريات، مدفوعات موردين، مصروفات، حركات مخزن)
	//
	//  العقد الساري/المنتهي ........ من ح/ العملاء            إلى ح/ إيرادات المبيعات
	//  رسوم إلغاء مشروع ............ من ح/ العملاء            إلى ح/ رسوم الإلغاء
	//  تحصيل من عميل ............... من ح/ الصندوق أو البنك    إلى ح/ العملاء
	//  فاتورة شراء مستلمة .......... من ح/ المخزون            إلى ح/ الموردون
	//  دفعة لمورد .................. من ح/ الموردون           إلى ح/ الصندوق أو البنك
	//  مصروف ....................... من ح/ المصروف            إلى ح/ الصندوق أو البنك
	//  صرف/إرجاع/تسوية مخزنية ...... من ح/ تكلفة الخامات (أو التسويات) إلى ح/ المخزون
	//
	// المزامنة آمنة للتكرار: تضيف الجديد وتحدّث ما تغيّر مصدره وتحذف قيد مستند محذوف، ولا تلمس الفترات المقفلة
	public class AccountingService
	{
		public const string CloseSource = "Close";
		public const string OpeningSource = "Opening";

		private static readonly SemaphoreSlim Gate = new(1, 1);
		private readonly ApplicationDbContext _db;

		public AccountingService(ApplicationDbContext db)
		{
			_db = db;
		}

		private record Desired(string Type, Guid Id, DateTime Date, string Description, Guid? ProjectId, List<(Guid Account, decimal Debit, decimal Credit)> Lines);

		public async Task<DateTime?> ClosedThroughAsync() =>
			await _db.PeriodCloses.MaxAsync(p => (DateTime?)p.ClosedThrough);

		public static string SourceName(string? type) => type switch
		{
			"Contract" => "عقد",
			"Cancellation" => "رسوم إلغاء",
			"Receipt" => "تحصيل عميل",
			"Refund" => "إرجاع لعميل",
			"Purchase" => "فاتورة شراء",
			"PurchaseReturn" => "مرتجع شراء",
			"SupplierPayment" => "دفعة مورد",
			"Expense" => "مصروف",
			"Stock" => "حركة مخزنية",
			OpeningSource => "رصيد افتتاحي",
			CloseSource => "إقفال فترة",
			null => "يدوي",
			_ => type
		};

		private static string Signature(DateTime date, IEnumerable<(Guid Account, decimal Debit, decimal Credit)> lines) =>
			date.Date.ToString("yyyyMMdd") + "|" + string.Join(";", lines
				.Select(l => $"{l.Account:N}:{l.Debit:0.00}:{l.Credit:0.00}")
				.OrderBy(s => s, StringComparer.Ordinal));

		public async Task<SyncResult> SyncAsync()
		{
			await Gate.WaitAsync();
			try
			{
				return await SyncCoreAsync();
			}
			finally
			{
				Gate.Release();
			}
		}

		private async Task<SyncResult> SyncCoreAsync()
		{
			var allAccounts = await _db.Accounts.AsNoTracking().ToListAsync();
			var accounts = allAccounts.Where(a => a.SystemKey != null)
				.ToDictionary(a => a.SystemKey!, a => a.Id);
			var postableIds = allAccounts.Where(a => a.IsPostable).Select(a => a.Id).ToHashSet();
			if (accounts.Count == 0)
			{
				return new SyncResult(0, 0, 0);
			}

			Guid Acc(string key) => accounts.TryGetValue(key, out var id)
				? id
				: throw new InvalidOperationException($"حساب النظام '{key}' غير موجود في شجرة الحسابات");

			Guid MoneyAcc(PaymentMethod method, Guid? chosen) =>
				chosen.HasValue && postableIds.Contains(chosen.Value)
					? chosen.Value
					: Acc(method == PaymentMethod.Cash ? "Cash" : "Bank");

			List<(Guid Account, decimal Debit, decimal Credit)> Pair(Guid debit, Guid credit, decimal amount) =>
				new() { (debit, amount, 0m), (credit, 0m, amount) };

			var closed = await ClosedThroughAsync();
			var desired = new List<Desired>();

			// عقود سارية أو منتهية = إيراد مستحق على العميل
			var contracts = await _db.Contracts.AsNoTracking()
				.Where(c => (c.Status == ContractStatuses.Active || c.Status == ContractStatuses.Completed) && c.TotalAmount > 0)
				.Select(c => new { c.Id, c.ContractNo, c.ProjectId, c.TotalAmount, c.StartDate, c.Created })
				.ToListAsync();
			foreach (var c in contracts)
			{
				desired.Add(new("Contract", c.Id, (c.StartDate ?? c.Created).Date, $"إيراد عقد {c.ContractNo}", c.ProjectId,
					Pair(Acc("AR"), Acc("Sales"), c.TotalAmount)));
			}

			// مشروع ملغى برسوم إلغاء ولا يوجد له عقد فعال
			var cancelled = await _db.Projects.AsNoTracking()
				.Where(p => p.Status == ProjectStatus.Cancelled && p.CancellationFee > 0
					&& !p.Contracts.Any(c => (c.Status == ContractStatuses.Active || c.Status == ContractStatuses.Completed) && c.TotalAmount > 0))
				.Select(p => new { p.Id, p.ProjectNo, p.CancellationFee, p.Created, p.Modified })
				.ToListAsync();
			foreach (var p in cancelled)
			{
				desired.Add(new("Cancellation", p.Id, (p.Modified ?? p.Created).Date, $"رسوم إلغاء المشروع {p.ProjectNo}", p.Id,
					Pair(Acc("AR"), Acc("CancellationFees"), p.CancellationFee)));
			}

			var receipts = await _db.CustomerPayments.AsNoTracking()
				.Select(p => new { p.Id, p.ReceiptNo, p.ProjectId, p.Amount, p.PaymentDate, p.Payment, p.CashAccountId, p.IsRefund, p.ChequeStatus })
				.ToListAsync();
			foreach (var p in receipts.Where(p => p.Amount > 0))
			{
				// صك مرتجع: لا أثر مالي على الصندوق/البنك
				if (p.Payment == PaymentMethod.Chek && p.ChequeStatus == ChequeStatus.Bounced)
				{
					continue;
				}

				if (p.IsRefund)
				{
					var payAcc = p.Payment == PaymentMethod.Chek && p.ChequeStatus == ChequeStatus.Pending
						? Acc("ChequesPayable")
						: MoneyAcc(p.Payment, p.CashAccountId);
					desired.Add(new("Refund", p.Id, p.PaymentDate.Date, $"إرجاع دفعة لعميل - سند {p.ReceiptNo}", p.ProjectId,
						Pair(Acc("AR"), payAcc, p.Amount)));
				}
				else
				{
					var recAcc = p.Payment == PaymentMethod.Chek && p.ChequeStatus == ChequeStatus.Pending
						? Acc("ChequesReceivable")
						: MoneyAcc(p.Payment, p.CashAccountId);
					desired.Add(new("Receipt", p.Id, p.PaymentDate.Date, $"تحصيل من عميل - سند {p.ReceiptNo}", p.ProjectId,
						Pair(recAcc, Acc("AR"), p.Amount)));
				}
			}

			// المشتريات المستلمة: المخزون = الإجمالي - الضريبة، والضريبة (إن وجدت) في حساب ضريبة المدخلات
			var purchases = await _db.Purchases.AsNoTracking()
				.Where(p => p.Status == PurchaseStatuses.Received && p.Total > 0)
				.Select(p => new { p.Id, p.PurchaseNo, p.ProjectId, p.Tax, p.Total, p.PurchaseDate })
				.ToListAsync();
			foreach (var p in purchases)
			{
				if (p.Tax > 0 && accounts.ContainsKey("VATInput") && p.Total > p.Tax)
				{
					desired.Add(new("Purchase", p.Id, p.PurchaseDate.Date, $"فاتورة شراء {p.PurchaseNo}", p.ProjectId,
						new()
						{
							(Acc("Inventory"), p.Total - p.Tax, 0m),
							(Acc("VATInput"), p.Tax, 0m),
							(Acc("AP"), 0m, p.Total)
						}));
				}
				else
				{
					desired.Add(new("Purchase", p.Id, p.PurchaseDate.Date, $"فاتورة شراء {p.PurchaseNo}", p.ProjectId,
						Pair(Acc("Inventory"), Acc("AP"), p.Total)));
				}
			}

			// مرتجعات المشتريات: تخفض الموردين والمخزون (وضريبة المدخلات إن وجدت)
			var returns = await _db.PurchaseReturns.AsNoTracking()
				.Where(r => r.Total > 0)
				.Select(r => new { r.Id, r.ReturnNo, r.Tax, r.Total, r.ReturnDate })
				.ToListAsync();
			foreach (var r in returns)
			{
				if (r.Tax > 0 && accounts.ContainsKey("VATInput") && r.Total > r.Tax)
				{
					desired.Add(new("PurchaseReturn", r.Id, r.ReturnDate.Date, $"مرتجع شراء {r.ReturnNo}", null,
						new()
						{
							(Acc("AP"), r.Total, 0m),
							(Acc("Inventory"), 0m, r.Total - r.Tax),
							(Acc("VATInput"), 0m, r.Tax)
						}));
				}
				else
				{
					desired.Add(new("PurchaseReturn", r.Id, r.ReturnDate.Date, $"مرتجع شراء {r.ReturnNo}", null,
						Pair(Acc("AP"), Acc("Inventory"), r.Total)));
				}
			}

			var supplierPayments = await _db.SupplierPayments.AsNoTracking()
				.Select(p => new { p.Id, p.ReceiptNo, p.Amount, p.PaymentDate, p.PaymentMethod, p.CashAccountId, p.ChequeStatus })
				.ToListAsync();
			foreach (var p in supplierPayments.Where(p => p.Amount > 0))
			{
				if (p.PaymentMethod == PaymentMethod.Chek && p.ChequeStatus == ChequeStatus.Bounced)
				{
					continue;
				}

				var payAcc = p.PaymentMethod == PaymentMethod.Chek && p.ChequeStatus == ChequeStatus.Pending
					? Acc("ChequesPayable")
					: MoneyAcc(p.PaymentMethod, p.CashAccountId);
				desired.Add(new("SupplierPayment", p.Id, p.PaymentDate.Date, $"دفعة لمورد - سند {p.ReceiptNo}", null,
					Pair(Acc("AP"), payAcc, p.Amount)));
			}

			var expenses = await _db.Expenses.AsNoTracking()
				.Select(e => new { e.Id, e.ExpenseType, e.Description, e.ProjectId, e.Amount, e.ExpenseDate, e.Payment, e.AccountId, e.CashAccountId })
				.ToListAsync();
			foreach (var e in expenses.Where(e => e.Amount > 0))
			{
				var expenseAccount = e.AccountId.HasValue && postableIds.Contains(e.AccountId.Value)
					? e.AccountId.Value
					: Acc(e.ProjectId != null ? "ProjectExpenses" : ExpenseKey(e.ExpenseType));
				var text = string.IsNullOrWhiteSpace(e.Description) ? e.ExpenseType : $"{e.ExpenseType} - {e.Description}";
				desired.Add(new("Expense", e.Id, e.ExpenseDate.Date, $"مصروف: {text}", e.ProjectId,
					Pair(expenseAccount, MoneyAcc(e.Payment, e.CashAccountId), e.Amount)));
			}

			// الصرف للمشاريع = تكلفة خامات، والصرف بدون مشروع والتسويات = فروقات مخزون (مرتجعات الشراء لها قيد مستقل)
			var stock = await _db.StockTransactions.AsNoTracking()
				.Where(t => t.PurchaseReturnId == null
					&& (t.TransactionType == StockTransactionType.Issue
						|| t.TransactionType == StockTransactionType.Return
						|| t.TransactionType == StockTransactionType.Adjustment))
				.Select(t => new { t.Id, t.TransactionType, t.ProjectId, t.QuantityIn, t.QuantityOut, t.UnitCost, t.TransactionDate, t.ReferenceNo })
				.ToListAsync();
			foreach (var t in stock)
			{
				var value = Math.Round((t.QuantityOut - t.QuantityIn) * t.UnitCost, 2);
				if (value == 0)
				{
					continue;
				}

				var costAccount = Acc(t.ProjectId != null ? "COGS" : "StockAdjust");
				var label = t.TransactionType switch
				{
					StockTransactionType.Issue => "صرف مخزني",
					StockTransactionType.Return => "إرجاع للمخزن",
					_ => "تسوية مخزنية"
				};
				var text = string.IsNullOrWhiteSpace(t.ReferenceNo) ? label : $"{label} - {t.ReferenceNo}";
				var lines = value > 0
					? Pair(costAccount, Acc("Inventory"), value)
					: Pair(Acc("Inventory"), costAccount, -value);
				desired.Add(new("Stock", t.Id, t.TransactionDate.Date, text, t.ProjectId, lines));
			}

			var existing = await _db.JournalEntries.Include(e => e.Lines)
				.Where(e => e.IsAuto && e.SourceType != CloseSource && e.SourceType != OpeningSource && e.SourceId != null)
				.ToListAsync();
			var byKey = existing.ToDictionary(e => (e.SourceType!, e.SourceId!.Value));

			var nextText = await DocumentNumbers.NextAsync(_db.JournalEntries.Select(e => e.EntryNo), "JV-");
			var next = int.Parse(nextText.Substring(3));

			int added = 0, updated = 0, removed = 0;
			var seen = new HashSet<(string, Guid)>();
			var now = DateTime.UtcNow;

			foreach (var d in desired.OrderBy(d => d.Date).ThenBy(d => d.Type))
			{
				var key = (d.Type, d.Id);
				seen.Add(key);

				// مستند بتاريخ فترة مقفلة يُرحَّل إلى أول يوم مفتوح حتى لا يضيع أثره
				var date = closed.HasValue && d.Date <= closed.Value ? closed.Value.AddDays(1) : d.Date;
				var description = date != d.Date ? $"{d.Description} (بتاريخ {d.Date:yyyy/MM/dd})" : d.Description;

				if (byKey.TryGetValue(key, out var entry))
				{
					if (closed.HasValue && entry.EntryDate.Date <= closed.Value)
					{
						continue; // فترة مقفلة: القيد مجمّد
					}

					var oldSignature = Signature(entry.EntryDate, entry.Lines.Select(l => (l.AccountId, l.Debit, l.Credit)));
					if (oldSignature == Signature(date, d.Lines))
					{
						continue;
					}

					_db.JournalLines.RemoveRange(entry.Lines);
					entry.EntryDate = date;
					entry.Description = description;
					entry.Modified = now;
					AddLines(entry.Id, d, now);
					updated++;
				}
				else
				{
					var id = Guid.NewGuid();
					_db.JournalEntries.Add(new JournalEntry
					{
						Id = id,
						Created = now,
						EntryNo = $"JV-{next++:D4}",
						EntryDate = date,
						Description = description,
						IsAuto = true,
						SourceType = d.Type,
						SourceId = d.Id,
						CreatedBy = "النظام"
					});
					AddLines(id, d, now);
					added++;
				}
			}

			foreach (var entry in existing.Where(e => !seen.Contains((e.SourceType!, e.SourceId!.Value))))
			{
				if (closed.HasValue && entry.EntryDate.Date <= closed.Value)
				{
					continue;
				}

				_db.JournalEntries.Remove(entry);
				removed++;
			}

			if (added + updated + removed > 0)
			{
				await _db.SaveChangesAsync();
			}

			return new SyncResult(added, updated, removed);
		}

		private void AddLines(Guid entryId, Desired d, DateTime now)
		{
			foreach (var l in d.Lines)
			{
				_db.JournalLines.Add(new JournalLine
				{
					Id = Guid.NewGuid(),
					Created = now,
					JournalEntryId = entryId,
					AccountId = l.Account,
					Debit = l.Debit,
					Credit = l.Credit,
					ProjectId = d.ProjectId
				});
			}
		}

		// ربط نوع المصروف العام بحساب المصروف المناسب
		public static string ExpenseKey(string? type) => (type ?? "").Trim() switch
		{
			"رواتب" => "ExpSalaries",
			"إيجار" => "ExpRent",
			"كهرباء ومياه" => "ExpUtilities",
			"دعاية وإعلان" => "ExpMarketing",
			"صيانة" => "ExpMaintenance",
			_ => "ExpOther"
		};

		// مجموع المدين والدائن لكل حساب ضمن فترة (القيود الفرعية فقط، التجميع في BuildTree)
		public async Task<Dictionary<Guid, (decimal Debit, decimal Credit)>> TotalsAsync(
			DateTime? from = null, DateTime? to = null, bool excludeClosing = false, Guid? projectId = null)
		{
			var query = _db.JournalLines.AsNoTracking().AsQueryable();
			if (from.HasValue) query = query.Where(l => l.JournalEntry.EntryDate >= from.Value.Date);
			if (to.HasValue) query = query.Where(l => l.JournalEntry.EntryDate <= to.Value.Date);
			if (excludeClosing) query = query.Where(l => l.JournalEntry.SourceType != CloseSource || l.JournalEntry.SourceType == null);
			if (projectId.HasValue) query = query.Where(l => l.ProjectId == projectId.Value);

			var rows = await query.GroupBy(l => l.AccountId)
				.Select(g => new { g.Key, Debit = g.Sum(l => l.Debit), Credit = g.Sum(l => l.Credit) })
				.ToListAsync();

			return rows.ToDictionary(r => r.Key, r => (r.Debit, r.Credit));
		}

		// الشجرة مرتبة برقم الحساب، وكل حساب أب يجمع أرصدة أبنائه
		public static List<AccountNode> BuildTree(IEnumerable<Account> accounts, Dictionary<Guid, (decimal Debit, decimal Credit)> totals)
		{
			var list = accounts.ToList();
			var children = list.Where(a => a.ParentId != null).ToLookup(a => a.ParentId!.Value);
			var ids = list.Select(a => a.Id).ToHashSet();
			var result = new List<AccountNode>();

			(decimal, decimal) Walk(Account account, int depth)
			{
				var node = new AccountNode { Account = account, Depth = depth };
				result.Add(node);

				var (debit, credit) = totals.TryGetValue(account.Id, out var own) ? own : (0m, 0m);
				foreach (var child in children[account.Id].OrderBy(c => c.Code, StringComparer.Ordinal))
				{
					var (d, c) = Walk(child, depth + 1);
					debit += d;
					credit += c;
				}

				node.Debit = debit;
				node.Credit = credit;
				return (debit, credit);
			}

			foreach (var root in list.Where(a => a.ParentId == null || !ids.Contains(a.ParentId.Value))
									 .OrderBy(a => a.Code, StringComparer.Ordinal))
			{
				Walk(root, 0);
			}

			return result;
		}

		// أبناء الحساب وأحفاده (لكشف حساب الحساب الأب)
		public static HashSet<Guid> WithDescendants(IEnumerable<Account> accounts, Guid id)
		{
			var list = accounts.ToList();
			var result = new HashSet<Guid> { id };
			var queue = new Queue<Guid>();
			queue.Enqueue(id);
			while (queue.Count > 0)
			{
				var current = queue.Dequeue();
				foreach (var child in list.Where(a => a.ParentId == current))
				{
					if (result.Add(child.Id))
					{
						queue.Enqueue(child.Id);
					}
				}
			}

			return result;
		}

		// إقفال الفترة حتى تاريخ: يرحّل أرصدة الإيرادات والمصروفات إلى الأرباح المحتجزة ويقفل التعديل على ما قبله
		public async Task<(bool Ok, string Message)> ClosePeriodAsync(DateTime through, string? notes, string? user)
		{
			through = through.Date;
			var closed = await ClosedThroughAsync();
			if (closed.HasValue && through <= closed.Value)
			{
				return (false, $"الفترة مقفلة حتى {closed.Value:yyyy/MM/dd}، اختر تاريخاً أحدث");
			}
			if (through > DateTime.Today)
			{
				return (false, "لا يمكن إقفال فترة لم تنتهِ بعد");
			}

			await SyncAsync();

			var retained = await _db.Accounts.FirstOrDefaultAsync(a => a.SystemKey == "RetainedEarnings");
			if (retained == null)
			{
				return (false, "حساب الأرباح المحتجزة غير موجود في شجرة الحسابات");
			}

			var totals = await TotalsAsync(to: through);
			var pnl = await _db.Accounts.AsNoTracking()
				.Where(a => a.Type == AccountType.Revenue || a.Type == AccountType.Expense)
				.ToListAsync();

			var now = DateTime.UtcNow;
			var close = new PeriodClose
			{
				Id = Guid.NewGuid(),
				Created = now,
				ClosedThrough = through,
				Notes = notes,
				ClosedBy = user
			};

			var entry = new JournalEntry
			{
				Id = Guid.NewGuid(),
				Created = now,
				EntryDate = through,
				Description = $"قيد إقفال الإيرادات والمصروفات حتى {through:yyyy/MM/dd}",
				IsAuto = true,
				SourceType = CloseSource,
				SourceId = close.Id,
				CreatedBy = user
			};

			decimal profit = 0;
			foreach (var account in pnl)
			{
				var (debit, credit) = totals.TryGetValue(account.Id, out var t) ? t : (0m, 0m);
				var net = debit - credit; // موجب = رصيد مدين
				if (net == 0)
				{
					continue;
				}

				profit -= net;
				entry.Lines.Add(new JournalLine
				{
					Id = Guid.NewGuid(),
					Created = now,
					AccountId = account.Id,
					Debit = net < 0 ? -net : 0,
					Credit = net > 0 ? net : 0,
					Description = "إقفال الحساب"
				});
			}

			if (entry.Lines.Count > 0)
			{
				entry.Lines.Add(new JournalLine
				{
					Id = Guid.NewGuid(),
					Created = now,
					AccountId = retained.Id,
					Debit = profit < 0 ? -profit : 0,
					Credit = profit > 0 ? profit : 0,
					Description = profit >= 0 ? "ترحيل صافي الربح" : "ترحيل صافي الخسارة"
				});

				var nextText = await DocumentNumbers.NextAsync(_db.JournalEntries.Select(e => e.EntryNo), "JV-");
				entry.EntryNo = "JV-" + int.Parse(nextText.Substring(3)).ToString("D4");
				_db.JournalEntries.Add(entry);
				close.EntryId = entry.Id;
			}

			close.NetResult = profit;
			_db.PeriodCloses.Add(close);
			await _db.SaveChangesAsync();

			return (true, $"تم إقفال الفترة حتى {through:yyyy/MM/dd} وترحيل صافي {(profit >= 0 ? "ربح" : "خسارة")} {Math.Abs(profit):N2}");
		}

		// إعادة فتح آخر إقفال فقط: يحذف قيد الإقفال ويعيد فتح التعديل على تلك الفترة
		public async Task<(bool Ok, string Message)> ReopenLastPeriodAsync()
		{
			var last = await _db.PeriodCloses.OrderByDescending(p => p.ClosedThrough).FirstOrDefaultAsync();
			if (last == null)
			{
				return (false, "لا توجد فترات مقفلة");
			}

			if (last.EntryId.HasValue)
			{
				var entry = await _db.JournalEntries.FindAsync(last.EntryId.Value);
				if (entry != null)
				{
					_db.JournalEntries.Remove(entry);
				}
			}

			_db.PeriodCloses.Remove(last);
			await _db.SaveChangesAsync();
			await SyncAsync();
			return (true, $"أُعيد فتح الفترة المقفلة حتى {last.ClosedThrough:yyyy/MM/dd}");
		}
	}
}
