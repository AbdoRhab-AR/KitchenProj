using System.Security.Claims;
using KitchenProject.Models;
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
		private readonly IUnitOfWork<IdentityRole> _role;
		private readonly IUnitOfWork<IdentityUserRole<string>> _userRole;
		private readonly IUnitOfWork<SiteInfo> _siteInfo;
		private readonly IUnitOfWork<Account> _account;
		private readonly StockService _stock;
		private readonly ApplicationDbContext _db;

		public LookupService(IUnitOfWork<Project> project,
							 IUnitOfWork<ApplicationUser> user,
							 IUnitOfWork<Supplier> supplier,
							 IUnitOfWork<Warehouse> warehouse,
							 IUnitOfWork<Item> item,
							 IUnitOfWork<ItemGroup> group,
							 IUnitOfWork<IdentityRole> role,
							 IUnitOfWork<IdentityUserRole<string>> userRole,
							 IUnitOfWork<SiteInfo> siteInfo,
							 IUnitOfWork<Account> account,
							 StockService stock,
							 ApplicationDbContext db)
		{
			_project = project;
			_user = user;
			_supplier = supplier;
			_warehouse = warehouse;
			_item = item;
			_group = group;
			_role = role;
			_userRole = userRole;
			_siteInfo = siteInfo;
			_account = account;
			_stock = stock;
			_db = db;
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

		// الموظفون الفعالون (بدون حساب المبرمج)، ويمكن تصفيتهم حسب الدور مع إبقاء المختار ظاهراً عند التعديل
		public Task<SelectList> UsersAsync(string? selected, params string[] roles) =>
			UsersAsync(selected, includeSelectedIfIneligible: true, roles);

		public async Task<SelectList> UsersAsync(string? selected, bool includeSelectedIfIneligible, params string[] roles)
		{
			var users = await ProjectUserOptions(EligibleUsers(roles))
				.OrderBy(u => u.UserName)
				.ToListAsync();

			if (includeSelectedIfIneligible && !string.IsNullOrEmpty(selected) && users.All(u => u.Id != selected))
			{
				var current = await ProjectUserOptions(_user.Entity.GetWhere(u => u.Id == selected))
					.FirstOrDefaultAsync();

				if (current != null)
				{
					users.Add(current);
					users = users.OrderBy(u => u.UserName).ToList();
				}
			}

			return new SelectList(users, "Id", "Text", selected);
		}

		// يتحقق أن المستخدم فعال (غير موقوف وليس المبرمج) ويملك أحد الأدوار المطلوبة إن حُددت
		public Task<bool> IsEligibleUserAsync(string? userId, params string[] roles)
		{
			if (string.IsNullOrWhiteSpace(userId))
				return Task.FromResult(false);

			return EligibleUsers(roles).AnyAsync(u => u.Id == userId);
		}

		private IQueryable<ApplicationUser> EligibleUsers(string[]? roles)
		{
			var progUserIds = _userRole.Entity
				.GetWhere(r => r.RoleId == AppRoles.ProgRoleId)
				.Select(r => r.UserId);

			var query = _user.Entity.GetWhere(u =>
				!progUserIds.Contains(u.Id)
				&& (u.LockoutEnd == null || u.LockoutEnd < DateTimeOffset.MaxValue));

			if (roles is { Length: > 0 })
			{
				var roleUserIds = UserIdsInRoles(roles);
				query = query.Where(u => roleUserIds.Contains(u.Id));
			}

			return query;
		}

		private IQueryable<string> UserIdsInRoles(string[] roles)
		{
			IQueryable<string> roleIds;
			if (roles.Length == 1)
			{
				var roleName = roles[0];
				var normalizedName = roleName.ToUpperInvariant();
				roleIds = _role.Entity
					.GetWhere(r => r.NormalizedName == normalizedName || r.Name == roleName)
					.Select(r => r.Id);
			}
			else
			{
				var normalized = roles.Select(r => r.ToUpperInvariant()).ToArray();
				roleIds = _role.Entity
					.GetWhere(r => (r.NormalizedName != null && normalized.Contains(r.NormalizedName))
								|| (r.Name != null && roles.Contains(r.Name)))
					.Select(r => r.Id);
			}

			return _userRole.Entity
				.GetWhere(ur => roleIds.Contains(ur.RoleId))
				.Select(ur => ur.UserId);
		}

		private static IQueryable<UserOption> ProjectUserOptions(IQueryable<ApplicationUser> query) =>
			query.Select(u => new UserOption
			{
				Id = u.Id,
				UserName = u.UserName ?? "",
				Text = u.UserProfile != null && !string.IsNullOrEmpty(u.UserProfile.DisplayName)
					? u.UserProfile.DisplayName
					: (u.UserName ?? "")
			});

		private sealed class UserOption
		{
			public string Id { get; set; } = "";
			public string UserName { get; set; } = "";
			public string Text { get; set; } = "";
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

		// أصناف فعالة من نوع معين، مع بيانات الوحدة والتكلفة والسعر لتعبئة سطور الفواتير تلقائياً
		public Task<List<ItemOption>> ItemOptionsAsync(ItemType type)
		{
			var query = _item.Entity.Include(i => i.Group!)
				.Where(i => i.IsActive && i.Type == type)
				.OrderBy(i => i.Group!.SortOrder).ThenBy(i => i.Name);

			return BuildItemOptionsAsync(query, usePurchasePrice: type == ItemType.Raw, fallbackGroupName: null);
		}

		// جميع الأصناف الفعالة لعروض الأسعار (الوحدات المصنّعة + الإكسسوارات والخامات المخزنية) باعتماد السعر الأعلى تلقائياً
		public Task<List<ItemOption>> QuoteItemOptionsAsync()
		{
			var query = _item.Entity.Include(i => i.Group!)
				.Where(i => i.IsActive)
				.OrderBy(i => i.Group != null ? i.Group.SortOrder : 999)
				.ThenBy(i => i.Code)
				.ThenBy(i => i.Name);

			return BuildItemOptionsAsync(query, usePurchasePrice: false, fallbackGroupName: "خامات ومواد مخزنية");
		}

		private async Task<List<ItemOption>> BuildItemOptionsAsync(IQueryable<Item> query, bool usePurchasePrice, string? fallbackGroupName)
		{
			var avgCosts = await _stock.AverageCostsAsync();
			var balances = await _stock.BalancesAsync();
			var rows = await query
				.Select(i => new
				{
					i.Id,
					i.Code,
					i.Name,
					i.Unit,
					i.Type,
					i.PurchasePrice,
					i.SalePrice,
					GroupName = i.Group != null ? i.Group.Code + " - " + i.Group.Name : fallbackGroupName
				})
				.ToListAsync();

			return rows.Select(i =>
			{
				var cost = Math.Max(i.PurchasePrice, avgCosts.GetValueOrDefault(i.Id));
				return new ItemOption
				{
					Id = i.Id,
					Code = i.Code,
					Name = i.Name,
					Unit = i.Unit,
					Type = i.Type,
					Cost = cost,
					Balance = balances.GetValueOrDefault(i.Id),
					Price = usePurchasePrice ? (i.PurchasePrice > 0 ? i.PurchasePrice : cost) : Math.Max(i.SalePrice, cost),
					GroupName = i.GroupName
				};
			}).ToList();
		}

		public static SelectList EnumList<TEnum>(TEnum? selected) where TEnum : struct, Enum
		{
			return new SelectList(
				Enum.GetValues<TEnum>().Select(s => new { Value = s, Text = s.GetDisplayName() }),
				"Value", "Text", selected);
		}

		public static SelectList EnumList<TEnum>(TEnum selected) where TEnum : struct, Enum =>
			EnumList((TEnum?)selected);

		// عدد الصفوف الافتراضي في الجداول من إعدادات الشركة
		public async Task<int> PageSizeAsync(int fallback = 10)
		{
			var size = await _siteInfo.Entity.GetAll()
				.Select(s => (int?)s.PageSize)
				.FirstOrDefaultAsync();
			return size is >= 5 and <= 200 ? size.Value : fallback;
		}

		// حسابات الخزائن والبنوك (111* و 112*) للاختيار في السندات والمصروفات
		public async Task<SelectList> CashAndBankAccountsAsync(Guid? selected)
		{
			var accounts = await _account.Entity.GetWhere(a => a.IsPostable && (a.IsActive || a.Id == selected)
					&& (a.Code.StartsWith("111") || a.Code.StartsWith("112") || a.SystemKey == "Cash" || a.SystemKey == "Bank"))
				.OrderBy(a => a.Code)
				.Select(a => new { a.Id, Text = a.Code + " - " + a.Name })
				.ToListAsync();
			return new SelectList(accounts, "Id", "Text", selected);
		}

		// حسابات المصروفات للاختيار في شاشة المصروفات
		public async Task<SelectList> ExpenseAccountsAsync(Guid? selected)
		{
			var accounts = await _account.Entity.GetWhere(a => a.IsPostable && (a.IsActive || a.Id == selected)
					&& a.Type == AccountType.Expense)
				.OrderBy(a => a.Code)
				.Select(a => new { a.Id, Text = a.Code + " - " + a.Name })
				.ToListAsync();
			return new SelectList(accounts, "Id", "Text", selected);
		}

		// التنبيهات الحية في جرس الشريط العلوي بحسب صلاحيات المستخدم
		public async Task<List<AppAlert>> GetAlertsAsync(ClaimsPrincipal user)
		{
			var alerts = new List<AppAlert>();
			var today = DateTime.Today;
			var tomorrow = today.AddDays(1);

			// 1. مشاريع تأخر موعد تسليمها
			var overdueProjects = await _db.Projects.AsNoTracking()
				.CountAsync(p => p.Status != ProjectStatus.Delivered
							  && p.Status != ProjectStatus.Closed
							  && p.Status != ProjectStatus.Cancelled
							  && p.ExpectedDeliveryDate != null
							  && p.ExpectedDeliveryDate < today);
			if (overdueProjects > 0)
			{
				alerts.Add(new AppAlert(
					$"{overdueProjects} مشاريع تجاوزت موعد التسليم",
					"يرجى مراجعة حالة التنفيذ والتسليم",
					"Projects", "Index", null,
					"fa-clock text-danger"));
			}

			// 2. معاينات مجدولة اليوم
			if (AppRoles.CanAccess(user, "SiteVisits"))
			{
				var todayVisits = await _db.SiteVisits.AsNoTracking()
					.CountAsync(v => v.Status == SiteVisitStatus.Scheduled
								  && v.VisitDate >= today
								  && v.VisitDate < tomorrow);
				if (todayVisits > 0)
				{
					alerts.Add(new AppAlert(
						$"{todayVisits} معاينات مجدولة اليوم",
						"بانتظار الزيارة الميدانية ورفع المقاسات",
						"SiteVisits", "Index", new() { ["status"] = "Scheduled" },
						"fa-ruler-combined text-primary"));
				}
			}

			// 3. طلبات المواد المعلقة أو المعتمدة بانتظار الصرف
			if (AppRoles.IsFullAccess(user))
			{
				var pendingRequests = await _db.MaterialRequests.AsNoTracking()
					.CountAsync(r => r.Status == MaterialRequestStatus.Pending);
				if (pendingRequests > 0)
				{
					alerts.Add(new AppAlert(
						$"{pendingRequests} طلبات مواد بانتظار اعتمادك",
						"مرفوعة من المهندسين والمصممين",
						"MaterialRequests", "Index", new() { ["status"] = "Pending" },
						"fa-clipboard-check text-warning"));
				}
			}
			else if (AppRoles.CanAccess(user, "MaterialRequests", "Issue"))
			{
				var approvedRequests = await _db.MaterialRequests.AsNoTracking()
					.CountAsync(r => r.Status == MaterialRequestStatus.Approved);
				if (approvedRequests > 0)
				{
					alerts.Add(new AppAlert(
						$"{approvedRequests} طلبات مواد معتمدة بانتظار الصرف",
						"جاهزة للصرف أو تحويل النقص للشراء",
						"MaterialRequests", "Index", new() { ["status"] = "Approved" },
						"fa-dolly text-info"));
				}
			}

			// 4. أصناف مخزنية وصلت لحد النقص (تحت حد الطلب)
			if (AppRoles.CanAccess(user, "Stock"))
			{
				var balances = await _stock.BalancesAsync();
				var watched = await _item.Entity.GetWhere(i => i.IsActive && i.Type == ItemType.Raw && i.MinQuantity > 0)
					.Select(i => new { i.Id, i.MinQuantity })
					.ToListAsync();
				var lowCount = watched.Count(i => balances.GetValueOrDefault(i.Id) <= i.MinQuantity);
				if (lowCount > 0)
				{
					alerts.Add(new AppAlert(
						$"{lowCount} أصناف مخزنية تحت حد الطلب",
						"رصيدها عند أو أقل من الحد الأدنى",
						"Stock", "Index", new() { ["lowStock"] = "true" },
						"fa-boxes-stacked text-danger"));
				}
			}

			// 5. صكوك (شيكات) مستحقة التحصيل أو السداد
			if (AppRoles.CanAccess(user, "Accounting", "Cheques"))
			{
				var dueCustomerCheques = await _db.CustomerPayments.AsNoTracking()
					.CountAsync(p => p.Payment == PaymentMethod.Chek
								  && p.ChequeStatus == ChequeStatus.Pending
								  && p.ChequeDueDate != null
								  && p.ChequeDueDate <= today);
				var dueSupplierCheques = await _db.SupplierPayments.AsNoTracking()
					.CountAsync(p => p.PaymentMethod == PaymentMethod.Chek
								  && p.ChequeStatus == ChequeStatus.Pending
								  && p.ChequeDueDate != null
								  && p.ChequeDueDate <= today);
				var dueCheques = dueCustomerCheques + dueSupplierCheques;
				if (dueCheques > 0)
				{
					alerts.Add(new AppAlert(
						$"{dueCheques} صكوك حان أو فات موعد استحقاقها",
						"بانتظار التحصيل أو السداد في حافظة الصكوك",
						"Accounting", "Cheques", null,
						"fa-money-check-dollar text-danger"));
				}
			}

			// 6. مشاريع متعاقد عليها أو قيد التنفيذ لم يُسجل لها صرف خامات
			if (AppRoles.CanAccess(user, "Reports", "Profit") || AppRoles.CanAccess(user, "Stock", "Issue"))
			{
				var noMaterialsProjects = await _db.Projects.AsNoTracking()
					.CountAsync(p => (p.Status == ProjectStatus.Contracted
								   || p.Status == ProjectStatus.InProgress
								   || p.Status == ProjectStatus.Delivered)
								  && !p.StockTransactions.Any(t => t.QuantityOut > 0));
				if (noMaterialsProjects > 0)
				{
					alerts.Add(new AppAlert(
						$"{noMaterialsProjects} مشاريع بدون صرف خامات",
						"متعاقد عليها أو قيد التنفيذ ولم يُسجل لها صرف خامات",
						"Projects", "Index", null,
						"fa-triangle-exclamation text-warning"));
				}
			}

			return alerts;
		}
	}

	public record AppAlert(
		string Title,
		string Subtitle,
		string Controller,
		string Action,
		Dictionary<string, string?>? RouteValues,
		string Icon);

	public class ItemOption
	{
		public Guid Id { get; set; }
		public string Code { get; set; } = "";
		public string Name { get; set; } = "";
		public string Unit { get; set; } = "";
		public ItemType Type { get; set; }
		public decimal Cost { get; set; }
		public decimal Balance { get; set; }
		public decimal Price { get; set; }
		public string? GroupName { get; set; }
	}
}
