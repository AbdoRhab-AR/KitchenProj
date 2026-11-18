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

	// التقارير: أرباح المشاريع (المبيعات مقابل التكلفة) وفاتورة التكلفة المطبوعة لكل مشروع
	public class ReportsController : Controller
	{
		private readonly IUnitOfWork<Project> _project;
		private readonly IUnitOfWork<StockTransaction> _tx;
		private readonly IUnitOfWork<Expense> _expense;
		private readonly IUnitOfWork<Quote> _quote;
		private readonly ProfitService _profit;
		private readonly KitchenProject.Models.ApplicationDbContext _db;

		public ReportsController(IUnitOfWork<Project> project,
								 IUnitOfWork<StockTransaction> tx,
								 IUnitOfWork<Expense> expense,
								 IUnitOfWork<Quote> quote,
								 ProfitService profit,
								 KitchenProject.Models.ApplicationDbContext db)
		{
			_project = project;
			_tx = tx;
			_expense = expense;
			_quote = quote;
			_profit = profit;
			_db = db;
		}

		// الحركات المالية: كل ما دخل (تحصيلات العملاء) وخرج (دفعات الموردين والمصروفات) مرتباً بالتاريخ
		private async Task<List<FinancialMove>> MovesAsync(DateTime? from, DateTime? to, string? kind, PaymentMethod? method)
		{
			var moves = new List<FinancialMove>();

			if (string.IsNullOrEmpty(kind) || kind == "receipt")
			{
				var q = _db.CustomerPayments.AsNoTracking()
					.Where(p => !(p.Payment == PaymentMethod.Chek && p.ChequeStatus == ChequeStatus.Bounced));
				if (from.HasValue) q = q.Where(p => p.PaymentDate >= from.Value.Date);
				if (to.HasValue) q = q.Where(p => p.PaymentDate <= to.Value.Date);
				if (method.HasValue) q = q.Where(p => p.Payment == method.Value);
				moves.AddRange((await q.Select(p => new { p.PaymentDate, p.ReceiptNo, Party = p.Project.Customer.Name, p.Project.ProjectNo, p.Notes, p.Payment, p.Amount, p.IsRefund }).ToListAsync())
					.Select(p => p.IsRefund
						? new FinancialMove("سند إرجاع عميل", p.PaymentDate, p.ReceiptNo, p.Party, p.ProjectNo, p.Notes ?? "", p.Payment, 0, p.Amount)
						: new FinancialMove("تحصيل عميل", p.PaymentDate, p.ReceiptNo, p.Party, p.ProjectNo, p.Notes ?? "", p.Payment, p.Amount, 0)));
			}

			if (string.IsNullOrEmpty(kind) || kind == "supplier")
			{
				var q = _db.SupplierPayments.AsNoTracking()
					.Where(p => !(p.PaymentMethod == PaymentMethod.Chek && p.ChequeStatus == ChequeStatus.Bounced));
				if (from.HasValue) q = q.Where(p => p.PaymentDate >= from.Value.Date);
				if (to.HasValue) q = q.Where(p => p.PaymentDate <= to.Value.Date);
				if (method.HasValue) q = q.Where(p => p.PaymentMethod == method.Value);
				moves.AddRange((await q.Select(p => new { p.PaymentDate, p.ReceiptNo, Party = p.supplier.Name, PurchaseNo = p.Purchase != null ? p.Purchase.PurchaseNo : "", p.Notes, p.PaymentMethod, p.Amount }).ToListAsync())
					.Select(p => new FinancialMove("دفعة مورد", p.PaymentDate, p.ReceiptNo, p.Party, p.PurchaseNo, p.Notes ?? "", p.PaymentMethod, 0, p.Amount)));
			}

			if (string.IsNullOrEmpty(kind) || kind == "expense")
			{
				var q = _db.Expenses.AsNoTracking().AsQueryable();
				if (from.HasValue) q = q.Where(e => e.ExpenseDate >= from.Value.Date);
				if (to.HasValue) q = q.Where(e => e.ExpenseDate <= to.Value.Date);
				if (method.HasValue) q = q.Where(e => e.Payment == method.Value);
				moves.AddRange((await q.Select(e => new { e.ExpenseDate, e.ExpenseType, ProjectNo = e.Project != null ? e.Project.ProjectNo : "", e.Description, e.Payment, e.Amount }).ToListAsync())
					.Select(e => new FinancialMove("مصروف", e.ExpenseDate, "", e.ExpenseType, e.ProjectNo, e.Description ?? "", e.Payment, 0, e.Amount)));
			}

			return moves.OrderBy(m => m.Date).ThenBy(m => m.Kind).ToList();
		}

		private void FillMoveFilters(DateTime? from, DateTime? to, string? kind, PaymentMethod? method)
		{
			ViewBag.From = from;
			ViewBag.To = to;
			ViewBag.Kind = kind;
			ViewBag.Method = method;
		}

		// GET: Reports/Transactions
		public async Task<IActionResult> Transactions(DateTime? from, DateTime? to, string? kind, PaymentMethod? method)
		{
			ViewBag.ActivePage = "Transactions";
			FillMoveFilters(from, to, kind, method);
			return View(await MovesAsync(from, to, kind, method));
		}

		// GET: Reports/TransactionsPrint  (A4)
		public async Task<IActionResult> TransactionsPrint(DateTime? from, DateTime? to, string? kind, PaymentMethod? method)
		{
			FillMoveFilters(from, to, kind, method);
			ViewBag.PrintedBy = User.Identity?.Name;
			return View(await MovesAsync(from, to, kind, method));
		}

		// أعمار الديون: العقود المتأخرة عن السداد (عملاء) وفواتير المشتريات غير المسددة (موردين)
		private async Task<List<AgingRow>> BuildAgingAsync(string kind, DateTime asOf)
		{
			asOf = asOf.Date;
			if (kind == "suppliers")
			{
				var purchases = await _db.Purchases.AsNoTracking()
					.Where(p => p.Status == PurchaseStatuses.Received && p.Total > p.PaidAmount)
					.Select(p => new { p.Id, p.Suppliers.SupplierNo, p.Suppliers.Name, p.PurchaseNo, p.PurchaseDate, p.Total, p.PaidAmount })
					.ToListAsync();
				var returnsByPurchase = await _db.PurchaseReturns.AsNoTracking()
					.GroupBy(r => r.PurchaseId)
					.Select(g => new { g.Key, Total = g.Sum(r => r.Total) })
					.ToDictionaryAsync(x => x.Key, x => x.Total);
				return purchases.Select(p =>
				{
					var netTotal = p.Total - returnsByPurchase.GetValueOrDefault(p.Id);
					return new AgingRow
					{
						PartyNo = p.SupplierNo,
						PartyName = p.Name,
						DocNo = p.PurchaseNo,
						DocDate = p.PurchaseDate.Date,
						AgeDays = Math.Max(0, (asOf - p.PurchaseDate.Date).Days),
						Total = netTotal,
						Paid = p.PaidAmount,
						Remaining = netTotal - p.PaidAmount
					};
				}).Where(r => r.Remaining > 0).OrderByDescending(r => r.AgeDays).ToList();
			}

			var contracts = await _db.Contracts.AsNoTracking()
				.Where(c => (c.Status == ContractStatuses.Active || c.Status == ContractStatuses.Completed) && c.TotalAmount > c.PaidAmount)
				.Select(c => new { c.Projects.Customer.CustomerNo, c.Projects.Customer.Name, c.ContractNo, Date = (c.StartDate ?? c.Created).Date, c.TotalAmount, c.PaidAmount })
				.ToListAsync();
			return contracts.Select(c => new AgingRow
			{
				PartyNo = c.CustomerNo,
				PartyName = c.Name,
				DocNo = c.ContractNo,
				DocDate = c.Date,
				AgeDays = Math.Max(0, (asOf - c.Date).Days),
				Total = c.TotalAmount,
				Paid = c.PaidAmount,
				Remaining = c.TotalAmount - c.PaidAmount
			}).OrderByDescending(r => r.AgeDays).ToList();
		}

		// GET: Reports/Aging
		public async Task<IActionResult> Aging(string kind = "customers", DateTime? asOf = null)
		{
			ViewBag.ActivePage = "Aging";
			var date = (asOf ?? DateTime.Today).Date;
			ViewBag.Kind = kind;
			ViewBag.AsOf = date;
			return View(await BuildAgingAsync(kind, date));
		}

		// GET: Reports/AgingPrint  (A4)
		public async Task<IActionResult> AgingPrint(string kind = "customers", DateTime? asOf = null)
		{
			var date = (asOf ?? DateTime.Today).Date;
			ViewBag.Kind = kind;
			ViewBag.AsOf = date;
			ViewBag.PrintedBy = User.Identity?.Name;
			return View(await BuildAgingAsync(kind, date));
		}

		// GET: Reports  ->  تقرير الأرباح
		public IActionResult Index() => RedirectToAction(nameof(Profit));

		// GET: Reports/Profit
		// الفترة تُطبق على تاريخ إنشاء المشروع، والمصروفات العامة تُحسب بتاريخها داخل نفس الفترة
		public async Task<IActionResult> Profit(string? searchString, ProjectStatus? status, DateTime? from, DateTime? to, bool contractedOnly = true)
		{
			ViewBag.ActivePage = "Reports";
			ViewBag.SearchString = searchString;
			ViewBag.Status = status;
			ViewBag.From = from;
			ViewBag.To = to;
			ViewBag.ContractedOnly = contractedOnly;

			var query = _project.Entity.GetWhere(p => p.Status != ProjectStatus.Cancelled);
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
					query = query.Where(p => p.ProjectName.Contains(validName) || p.Customer.Name.Contains(validName));
				}
			}
			if (status.HasValue) query = query.Where(p => p.Status == status.Value);
			if (from.HasValue) query = query.Where(p => p.Created >= from.Value.Date);
			if (to.HasValue) query = query.Where(p => p.Created < to.Value.Date.AddDays(1));
			if (contractedOnly) query = query.Where(p => p.Contracts.Any(c => c.Status != ContractStatuses.Cancelled));

			var rows = (await _profit.RowsAsync(query)).OrderByDescending(r => r.Created).ToList();

			var general = _expense.Entity.GetWhere(e => e.ProjectId == null);
			if (from.HasValue) general = general.Where(e => e.ExpenseDate >= from.Value.Date);
			if (to.HasValue) general = general.Where(e => e.ExpenseDate <= to.Value.Date);
			ViewBag.GeneralExpenses = await general.SumAsync(e => (decimal?)e.Amount) ?? 0;

			ViewBag.StatusList = LookupService.EnumList(status);
			return View(rows);
		}

		// GET: Reports/CostSheet/5  فاتورة التكلفة: ما بيع للعميل مقابل ما صُرف فعلياً على المشروع
		public async Task<IActionResult> CostSheet(Guid? id)
		{
			if (id == null)
			{
				return NotFound();
			}

			var project = await _project.Entity.Include(p => p.Customer, p => p.Contracts, p => p.Expenses)
				.AsSplitQuery()
				.FirstOrDefaultAsync(p => p.Id == id);
			if (project == null)
			{
				return NotFound();
			}

			ViewBag.Profit = await _profit.RowAsync(project.Id);

			// بنود البيع من آخر عرض سعر معتمد (أو آخر عرض إن لم يُعتمد بعد)
			ViewBag.Quote = await _quote.Entity.Include(q => q.Items)
				.Where(q => q.ProjectId == project.Id)
				.OrderByDescending(q => q.Status == DesignStatus.Approved).ThenByDescending(q => q.Created)
				.FirstOrDefaultAsync();

			// الخامات المصروفة مجمعة حسب الصنف (الصرف - المرتجع)
			ViewBag.Materials = (await _tx.Entity.Include(t => t.Items)
					.Where(t => t.ProjectId == project.Id)
					.ToListAsync())
				.GroupBy(t => t.ItemId)
				.Select(g => new CostSheetMaterial
				{
					Code = g.First().Items?.Code ?? "",
					Name = g.First().Items?.Name ?? "",
					Unit = g.First().Items?.Unit ?? "",
					Quantity = g.Sum(t => t.QuantityOut - t.QuantityIn),
					Cost = g.Sum(t => (t.QuantityOut - t.QuantityIn) * t.UnitCost)
				})
				.Where(m => m.Quantity != 0 || m.Cost != 0)
				.OrderByDescending(m => m.Cost)
				.ToList();

			return View(project);
		}
	}

	public class CostSheetMaterial
	{
		public string Code { get; set; } = "";
		public string Name { get; set; } = "";
		public string Unit { get; set; } = "";
		public decimal Quantity { get; set; }
		public decimal Cost { get; set; }
		public decimal UnitCost => Quantity != 0 ? Cost / Quantity : 0;
	}
}

