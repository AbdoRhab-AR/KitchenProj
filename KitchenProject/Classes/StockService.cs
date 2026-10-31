using KitchenProject.Models.Entities;
using KitchenProject.Models.Enums;
using KitchenProject.Models.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KitchenProject.Classes
{
	// أرصدة المخزن وتكلفة الخامات، محسوبة من الحركات المخزنية مباشرة
	public class StockService
	{
		private readonly IUnitOfWork<StockTransaction> _tx;

		public StockService(IUnitOfWork<StockTransaction> tx)
		{
			_tx = tx;
		}

		// الرصيد لكل صنف: الوارد - المنصرف
		public async Task<Dictionary<Guid, decimal>> BalancesAsync(Guid? warehouseId = null)
		{
			var query = _tx.Entity.GetAll();
			if (warehouseId.HasValue)
			{
				query = query.Where(t => t.WarehouseId == warehouseId.Value);
			}

			return await query.GroupBy(t => t.ItemId)
				.Select(g => new { g.Key, Balance = g.Sum(t => t.QuantityIn - t.QuantityOut) })
				.ToDictionaryAsync(x => x.Key, x => x.Balance);
		}

		// الرصيد لكل مخزن ثم لكل صنف (لعرض المتوفر فور تغيير المخزن في الشاشة)
		public async Task<Dictionary<Guid, Dictionary<Guid, decimal>>> BalancesByWarehouseAsync()
		{
			var rows = await _tx.Entity.GetAll()
				.GroupBy(t => new { t.WarehouseId, t.ItemId })
				.Select(g => new { g.Key.WarehouseId, g.Key.ItemId, Balance = g.Sum(t => t.QuantityIn - t.QuantityOut) })
				.ToListAsync();

			return rows.GroupBy(r => r.WarehouseId)
				.ToDictionary(g => g.Key, g => g.ToDictionary(r => r.ItemId, r => r.Balance));
		}

		public async Task<decimal> BalanceAsync(Guid itemId, Guid warehouseId)
		{
			return await _tx.Entity.GetWhere(t => t.ItemId == itemId && t.WarehouseId == warehouseId)
				.SumAsync(t => (decimal?)(t.QuantityIn - t.QuantityOut)) ?? 0;
		}

		// متوسط التكلفة المرجح لكل صنف من حركات الشراء (يُستخدم لتكلفة المواد المصروفة للمشاريع)
		public async Task<Dictionary<Guid, decimal>> AverageCostsAsync()
		{
			var rows = await _tx.Entity.GetWhere(t => t.TransactionType == StockTransactionType.Purchase && t.QuantityIn > 0)
				.GroupBy(t => t.ItemId)
				.Select(g => new { g.Key, Qty = g.Sum(t => t.QuantityIn), Value = g.Sum(t => t.QuantityIn * t.UnitCost) })
				.ToListAsync();

			return rows.ToDictionary(r => r.Key, r => r.Qty > 0 ? Math.Round(r.Value / r.Qty, 2) : 0);
		}
	}
}
