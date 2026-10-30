using KitchenProject.Classes;
using KitchenProject.Models.Entities;
using KitchenProject.Models.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KitchenProject.Controllers
{
	[ViewLayout("_DashboardLayout")]
	//[Authorize(Policy = "ProgOrAdminOrEmployeePolicy")]

	// بنود الأصناف الجاهزة (A, B, C ...)
	public class ItemGroupsController : Controller
	{
		private readonly IUnitOfWork<ItemGroup> _group;
		private readonly IUnitOfWork<Item> _item;

		public ItemGroupsController(IUnitOfWork<ItemGroup> group, IUnitOfWork<Item> item)
		{
			_group = group;
			_item = item;
		}

		// GET: ItemGroups
		public async Task<IActionResult> Index()
		{
			ViewBag.ActivePage = "Items";

			var groups = await _group.Entity.GetAll()
				.OrderBy(g => g.SortOrder).ThenBy(g => g.Code)
				.ToListAsync();

			ViewBag.ItemCounts = await _item.Entity.GetWhere(i => i.ItemGroupId != null)
				.GroupBy(i => i.ItemGroupId!.Value)
				.Select(g => new { g.Key, Count = g.Count() })
				.ToDictionaryAsync(x => x.Key, x => x.Count);

			return View(groups);
		}

		// POST: ItemGroups/Save  (إضافة بند جديد عندما Id فارغ، وإلا تعديل)
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Save(ItemGroup model)
		{
			model.Code = (model.Code ?? "").Trim().ToUpperInvariant();
			model.Name = (model.Name ?? "").Trim();

			if (string.IsNullOrEmpty(model.Code) || string.IsNullOrEmpty(model.Name))
			{
				TempData["Error"] = "رمز البند واسمه مطلوبان";
				return RedirectToAction(nameof(Index));
			}

			if (await _group.Entity.GetAll().AnyAsync(g => g.Code == model.Code && g.Id != model.Id))
			{
				TempData["Error"] = $"الرمز ({model.Code}) مستخدم لبند آخر";
				return RedirectToAction(nameof(Index));
			}

			if (model.Id == Guid.Empty)
			{
				model.Id = Guid.NewGuid();
				model.Created = DateTime.UtcNow;
				_group.Entity.Insert(model);
				TempData["Success"] = $"تمت إضافة البند ({model.Code}) {model.Name}";
			}
			else
			{
				var group = await _group.Entity.GetByIdAsync(model.Id);
				if (group == null)
				{
					return NotFound();
				}

				group.Code = model.Code;
				group.Name = model.Name;
				group.Description = model.Description;
				group.SortOrder = model.SortOrder;
				group.Modified = DateTime.UtcNow;
				TempData["Success"] = $"تم حفظ البند ({group.Code}) {group.Name}";
			}

			await _group.SaveAsync();
			return RedirectToAction(nameof(Index));
		}

		// POST: ItemGroups/Delete/5
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Delete(Guid id)
		{
			var group = await _group.Entity.GetByIdAsync(id);
			if (group == null)
			{
				return NotFound();
			}

			if (await _item.Entity.GetAll().AnyAsync(i => i.ItemGroupId == id))
			{
				TempData["Error"] = $"لا يمكن حذف البند ({group.Code}) لأن تحته أصنافاً، انقلها لبند آخر أولاً";
				return RedirectToAction(nameof(Index));
			}

			_group.Entity.Delete(group);
			await _group.SaveAsync();
			TempData["Success"] = $"تم حذف البند ({group.Code})";
			return RedirectToAction(nameof(Index));
		}
	}
}
