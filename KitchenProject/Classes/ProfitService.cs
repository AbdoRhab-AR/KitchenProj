using KitchenProject.Models.Entities;
using KitchenProject.Models.Enums;
using KitchenProject.Models.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KitchenProject.Classes
{
	// ربح المشاريع: قيمة البيع (العقود) مقابل تكلفة الخامات المصروفة ومصروفات المشروع
	// نفس المعادلة تُستخدم في صفحة المشروع وتقرير الأرباح وفاتورة التكلفة ولوحة التحكم
	public class ProfitService
	{
		private readonly IUnitOfWork<Project> _project;
		private readonly IUnitOfWork<StockTransaction> _tx;
		private readonly IUnitOfWork<Expense> _expense;
		private readonly IUnitOfWork<CustomerPayment> _payment;

		public ProfitService(IUnitOfWork<Project> project,
							 IUnitOfWork<StockTransaction> tx,
							 IUnitOfWork<Expense> expense,
							 IUnitOfWork<CustomerPayment> payment)
		{
			_project = project;
			_tx = tx;
			_expense = expense;
			_payment = payment;
		}

		// قيمة البيع: مجموع العقود غير الملغاة، وإلا القيمة النهائية، وإلا التقديرية
		public static decimal SaleValue(decimal contracts, decimal finalAmount, decimal estimated) =>
			contracts > 0 ? contracts : (finalAmount > 0 ? finalAmount : estimated);

		public async Task<List<ProjectProfitRow>> RowsAsync(IQueryable<Project> projects)
		{
			var list = await projects
				.Select(p => new ProjectProfitRow
				{
					ProjectId = p.Id,
					ProjectNo = p.ProjectNo,
					ProjectName = p.ProjectName,
					CustomerName = p.Customer.Name,
					Status = p.Status,
					Created = p.Created,
					ContractsTotal = p.Contracts.Where(c => c.Status != ContractStatuses.Cancelled).Sum(c => (decimal?)c.TotalAmount) ?? 0,
					FinalAmount = p.FinalAmount,
					EstimatedAmount = p.EstimatedAmount
				})
				.ToListAsync();

			if (list.Count == 0)
			{
				return list;
			}

			var ids = list.Select(r => r.ProjectId).ToList();

			var materials = await _tx.Entity.GetWhere(t => t.ProjectId != null && ids.Contains(t.ProjectId.Value))
				.GroupBy(t => t.ProjectId!.Value)
				.Select(g => new { g.Key, Cost = g.Sum(t => (t.QuantityOut - t.QuantityIn) * t.UnitCost) })
				.ToDictionaryAsync(x => x.Key, x => x.Cost);

			var expenses = await _expense.Entity.GetWhere(e => e.ProjectId != null && ids.Contains(e.ProjectId.Value))
				.GroupBy(e => e.ProjectId!.Value)
				.Select(g => new { g.Key, Total = g.Sum(e => e.Amount) })
				.ToDictionaryAsync(x => x.Key, x => x.Total);

			var paid = await _payment.Entity.GetWhere(p => ids.Contains(p.ProjectId))
				.GroupBy(p => p.ProjectId)
				.Select(g => new { g.Key, Total = g.Sum(p => p.Amount) })
				.ToDictionaryAsync(x => x.Key, x => x.Total);

			foreach (var row in list)
			{
				row.MaterialsCost = materials.GetValueOrDefault(row.ProjectId);
				row.Expenses = expenses.GetValueOrDefault(row.ProjectId);
				row.Paid = paid.GetValueOrDefault(row.ProjectId);
			}

			return list;
		}

		public async Task<ProjectProfitRow?> RowAsync(Guid projectId) =>
			(await RowsAsync(_project.Entity.GetWhere(p => p.Id == projectId))).FirstOrDefault();
	}

	public class ProjectProfitRow
	{
		public Guid ProjectId { get; set; }
		public string ProjectNo { get; set; } = "";
		public string ProjectName { get; set; } = "";
		public string CustomerName { get; set; } = "";
		public ProjectStatus Status { get; set; }
		public DateTime Created { get; set; }

		public decimal ContractsTotal { get; set; }
		public decimal FinalAmount { get; set; }
		public decimal EstimatedAmount { get; set; }
		public decimal MaterialsCost { get; set; }
		public decimal Expenses { get; set; }
		public decimal Paid { get; set; }

		public decimal SaleValue => ProfitService.SaleValue(ContractsTotal, FinalAmount, EstimatedAmount);
		public decimal TotalCost => MaterialsCost + Expenses;
		public decimal Profit => SaleValue - TotalCost;
		public decimal? MarginPercent => SaleValue > 0 ? Math.Round(Profit / SaleValue * 100, 1) : null;
		public decimal Remaining => SaleValue - Paid;

		// القيمة مؤكدة فقط بعد التعاقد، قبلها هي تقدير
		public bool IsEstimate => ContractsTotal == 0;
	}
}
