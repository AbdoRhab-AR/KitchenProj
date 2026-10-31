using KitchenProject.Classes;
using KitchenProject.Models.Entities;
using KitchenProject.Models.Enums;
using KitchenProject.Models.Interfaces;
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

		public ReportsController(IUnitOfWork<Project> project,
								 IUnitOfWork<StockTransaction> tx,
								 IUnitOfWork<Expense> expense,
								 IUnitOfWork<Quote> quote,
								 ProfitService profit)
		{
			_project = project;
			_tx = tx;
			_expense = expense;
			_quote = quote;
			_profit = profit;
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
				searchString = searchString.Trim();
				query = query.Where(p => p.ProjectNo.Contains(searchString) || p.ProjectName.Contains(searchString) || p.Customer.Name.Contains(searchString));
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
