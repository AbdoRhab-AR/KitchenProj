using KitchenProject.Models.Entities;
using KitchenProject.Models.Enums;
using KitchenProject.Models.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace KitchenProject.Classes
{
	// القوائم المنسدلة المشتركة بين الشاشات (المشاريع، المستخدمين، الموردين، المخازن، الأصناف)
	public class LookupService
	{
		private readonly IUnitOfWork<Project> _project;
		private readonly IUnitOfWork<ApplicationUser> _user;
		private readonly IUnitOfWork<Supplier> _supplier;
		private readonly IUnitOfWork<Warehouse> _warehouse;
		private readonly IUnitOfWork<Item> _item;
		private readonly IUnitOfWork<ItemGroup> _group;
		private readonly IUnitOfWork<IdentityUserRole<string>> _userRole;

		public LookupService(IUnitOfWork<Project> project,
							 IUnitOfWork<ApplicationUser> user,
							 IUnitOfWork<Supplier> supplier,
							 IUnitOfWork<Warehouse> warehouse,
							 IUnitOfWork<Item> item,
							 IUnitOfWork<ItemGroup> group,
							 IUnitOfWork<IdentityUserRole<string>> userRole)
		{
			_project = project;
			_user = user;
			_supplier = supplier;
			_warehouse = warehouse;
			_item = item;
			_group = group;
			_userRole = userRole;
		}

		// المشاريع غير المغلقة/الملغاة، مع إبقاء المشروع المختار ظاهراً حتى لو أُغلق
		public async Task<SelectList> ProjectsAsync(Guid? selected)
		{
			var projects = await _project.Entity.Include(p => p.Customer)
				.Where(p => (p.Status != ProjectStatus.Closed && p.Status != ProjectStatus.Cancelled) || p.Id == selected)
				.OrderByDescending(p => p.Created)
				.Select(p => new { p.Id, Text = p.ProjectNo + " - " + p.ProjectName + " (" + p.Customer.Name + ")" })
				.ToListAsync();

			return new SelectList(projects, "Id", "Text", selected);
		}

		// الموظفون الفعالون (بدون حساب المبرمج)، مع إبقاء المختار ظاهراً حتى لو أُوقف
		public async Task<SelectList> UsersAsync(string? selected)
		{
			var progUserIds = _userRole.Entity.GetWhere(r => r.RoleId == AppRoles.ProgRoleId).Select(r => r.UserId);

			var users = await _user.Entity.Include(u => u.UserProfile)
				.Where(u => u.Id == selected
						 || (!progUserIds.Contains(u.Id) && (u.LockoutEnd == null || u.LockoutEnd < DateTimeOffset.MaxValue)))
				.OrderBy(u => u.UserName)
				.Select(u => new { u.Id, Text = u.UserProfile != null ? u.UserProfile.DisplayName : u.UserName })
				.ToListAsync();

			return new SelectList(users, "Id", "Text", selected);
		}

		public async Task<SelectList> SuppliersAsync(Guid? selected)
		{
			var suppliers = await _supplier.Entity.GetWhere(s => s.IsActive || s.Id == selected)
				.OrderBy(s => s.Name)
				.Select(s => new { s.Id, s.Name })
				.ToListAsync();

			return new SelectList(suppliers, "Id", "Name", selected);
		}

		public async Task<SelectList> WarehousesAsync(Guid? selected)
		{
			var warehouses = await _warehouse.Entity.GetWhere(w => w.IsActive || w.Id == selected)
				.OrderBy(w => w.Created).ThenBy(w => w.Name)
				.Select(w => new { w.Id, w.Name })
				.ToListAsync();

			return new SelectList(warehouses, "Id", "Name", selected);
		}

		public Task<Guid> DefaultWarehouseIdAsync() =>
			_warehouse.Entity.GetWhere(w => w.IsActive).OrderBy(w => w.Created).Select(w => w.Id).FirstOrDefaultAsync();

		public async Task<SelectList> GroupsAsync(Guid? selected)
		{
			var groups = await _group.Entity.GetAll()
				.OrderBy(g => g.SortOrder).ThenBy(g => g.Code)
				.Select(g => new { g.Id, Text = g.Code + " - " + g.Name })
				.ToListAsync();

			return new SelectList(groups, "Id", "Text", selected);
		}

		// أصناف فعالة من نوع معين، مع بيانات الوحدة والسعر لتعبئة سطور الفواتير تلقائياً
		public Task<List<ItemOption>> ItemOptionsAsync(ItemType type)
		{
			return _item.Entity.Include(i => i.Group!)
				.Where(i => i.IsActive && i.Type == type)
				.OrderBy(i => i.Group!.SortOrder).ThenBy(i => i.Name)
				.Select(i => new ItemOption
				{
					Id = i.Id,
					Code = i.Code,
					Name = i.Name,
					Unit = i.Unit,
					Price = type == ItemType.Raw ? i.PurchasePrice : i.SalePrice,
					GroupName = i.Group != null ? i.Group.Code + " - " + i.Group.Name : null
				})
				.ToListAsync();
		}

		public static SelectList EnumList<TEnum>(TEnum? selected) where TEnum : struct, Enum
		{
			return new SelectList(
				Enum.GetValues<TEnum>().Select(s => new { Value = s, Text = s.GetDisplayName() }),
				"Value", "Text", selected);
		}

		public static SelectList EnumList<TEnum>(TEnum selected) where TEnum : struct, Enum =>
			EnumList((TEnum?)selected);
	}

	public class ItemOption
	{
		public Guid Id { get; set; }
		public string Code { get; set; } = "";
		public string Name { get; set; } = "";
		public string Unit { get; set; } = "";
		public decimal Price { get; set; }
		public string? GroupName { get; set; }
	}
}
