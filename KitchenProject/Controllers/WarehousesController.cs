using KitchenProject.Classes;
using KitchenProject.Models.Entities;
using KitchenProject.Models.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KitchenProject.Controllers
{
	[ViewLayout("_DashboardLayout")]

	// المخازن: إضافة وتعديل وإيقاف (لا تُحذف لأن الحركات المخزنية مرتبطة بها)
	public class WarehousesController : Controller
	{
		private readonly IUnitOfWork<Warehouse> _warehouse;
		private readonly IUnitOfWork<StockTransaction> _tx;

		public WarehousesController(IUnitOfWork<Warehouse> warehouse, IUnitOfWork<StockTransaction> tx)
		{
			_warehouse = warehouse;
			_tx = tx;
		}

		// GET: Warehouses
		public async Task<IActionResult> Index()
		{
			ViewBag.ActivePage = "Warehouses";

			var warehouses = await _warehouse.Entity.GetAll()
				.OrderByDescending(w => w.IsActive).ThenBy(w => w.Created).ThenBy(w => w.Name)
				.ToListAsync();

			// عدد الخامات التي لها رصيد وقيمة الرصيد التقريبية بتكلفة الحركات
			ViewBag.Stats = (await _tx.Entity.GetAll()
					.GroupBy(t => new { t.WarehouseId, t.ItemId })
					.Select(g => new { g.Key.WarehouseId, Balance = g.Sum(t => t.QuantityIn - t.QuantityOut), Value = g.Sum(t => (t.QuantityIn - t.QuantityOut) * t.UnitCost) })
					.ToListAsync())
				.GroupBy(r => r.WarehouseId)
				.ToDictionary(g => g.Key, g => (Items: g.Count(r => r.Balance > 0), Value: g.Sum(r => r.Value)));

			return View(warehouses);
		}

		// POST: Warehouses/Save  (إضافة إذا كان Id فارغاً، وإلا تعديل)
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Save(Guid? id, string? name, string? location)
		{
			name = name?.Trim();
			location = string.IsNullOrWhiteSpace(location) ? null : location.Trim();

			if (string.IsNullOrEmpty(name) || name.Length > 100)
			{
				TempData["Error"] = "اسم المخزن مطلوب (100 حرف كحد أقصى)";
				return RedirectToAction(nameof(Index));
			}

			if (await _warehouse.Entity.GetAll().AnyAsync(w => w.Name == name && w.Id != id))
			{
				TempData["Error"] = $"يوجد مخزن بنفس الاسم ({name})";
				return RedirectToAction(nameof(Index));
			}

			if (id.HasValue)
			{
				var warehouse = await _warehouse.Entity.GetByIdAsync(id.Value);
				if (warehouse == null)
				{
					return NotFound();
				}

				warehouse.Name = name;
				warehouse.Location = location;
				warehouse.Modified = DateTime.UtcNow;
				TempData["Success"] = $"تم تحديث المخزن {name}";
			}
			else
			{
				_warehouse.Entity.Insert(new Warehouse
				{
					Id = Guid.NewGuid(),
					Created = DateTime.UtcNow,
					Name = name,
					Location = location
				});
				TempData["Success"] = $"تمت إضافة المخزن {name}";
			}

			await _warehouse.SaveAsync();
			return RedirectToAction(nameof(Index));
		}

		// POST: Warehouses/ToggleActive/5
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> ToggleActive(Guid id)
		{
			var warehouse = await _warehouse.Entity.GetByIdAsync(id);
			if (warehouse == null)
			{
				return NotFound();
			}

			if (warehouse.IsActive)
			{
				var hasStock = await _tx.Entity.GetWhere(t => t.WarehouseId == id)
					.GroupBy(t => t.ItemId)
					.AnyAsync(g => g.Sum(t => t.QuantityIn - t.QuantityOut) != 0);

				if (hasStock)
				{
					TempData["Error"] = $"المخزن {warehouse.Name} به أرصدة، حوّلها لمخزن آخر قبل إيقافه";
					return RedirectToAction(nameof(Index));
				}

				if (!await _warehouse.Entity.GetAll().AnyAsync(w => w.IsActive && w.Id != id))
				{
					TempData["Error"] = "لا يمكن إيقاف آخر مخزن فعال";
					return RedirectToAction(nameof(Index));
				}
			}

			warehouse.IsActive = !warehouse.IsActive;
			warehouse.Modified = DateTime.UtcNow;
			await _warehouse.SaveAsync();

			TempData["Success"] = warehouse.IsActive ? $"تم تفعيل المخزن {warehouse.Name}" : $"تم إيقاف المخزن {warehouse.Name}";
			return RedirectToAction(nameof(Index));
		}
	}
}
