using System.Security.Claims;

namespace KitchenProject.Classes
{
	// أدوار النظام والشاشات (الكونترولرات) المسموحة لكل دور
	// المبرمج والمدير يدخلون كل الشاشات، وباقي الأدوار حسب الجدول أدناه
	public static class AppRoles
	{
		public const string Prog = "Prog";
		public const string ProgRoleId = "109400b7-ee44-4b6c-bef8-0cc59de1543e"; // مبذور في ModelBuilderExtensions
		public const string Admin = "Admin";
		public const string Sales = "Sales";
		public const string Engineer = "Engineer";
		public const string Designer = "Designer";
		public const string Warehouse = "Warehouse";
		public const string Accountant = "Accountant";

		// الأدوار التي يمكن إسنادها من شاشة المستخدمين (المبرمج مخفي)
		public static readonly string[] Assignable = { Admin, Sales, Engineer, Designer, Warehouse, Accountant };

		public static readonly Dictionary<string, string> Names = new()
		{
			[Prog] = "المبرمج",
			[Admin] = "المدير",
			[Sales] = "المبيعات",
			[Engineer] = "مهندس",
			[Designer] = "مصمم",
			[Warehouse] = "أمين المخزن",
			[Accountant] = "المحاسب"
		};

		public static string GetName(string? role) =>
			role != null && Names.TryGetValue(role, out var name) ? name : role ?? "-";

		// شاشات مفتوحة لكل مستخدم مسجل
		private static readonly string[] Common = { "Home", "Account" };

		// إجراءات الاطلاع فقط (عرض وطباعة)، يرمز لها بـ "view" في الجدول أدناه
		private static readonly string[] ViewActions = { "Index", "Details", "Print", "Receipt", "SpecSheet", "Movements", "Profit", "CostSheet" };

		// المشاريع والعملاء محور النظام: يطلع عليها الجميع، والتعديل للمدير والمبيعات
		private static readonly string[] EveryoneViews = { "Customers:view", "Projects:view" };

		// "Controller" = كل الشاشة (اطلاع وتعديل)
		// "Controller:view" = اطلاع فقط، و "Controller:view|Action" = اطلاع + إجراءات محددة
		private static readonly Dictionary<string, string[]> Screens = new()
		{
			[Sales] = new[] { "Customers", "Projects", "SiteVisits", "Quotes", "Contracts", "Payments" },
			[Engineer] = new[] { "SiteVisits", "Quotes:view", "Contracts:view", "Items:view", "Stock:view|Issue", "MaterialRequests:view|Create|Cancel" },
			[Designer] = new[] { "SiteVisits", "Quotes", "Contracts:view", "Items:view", "MaterialRequests:view|Create|Cancel" },
			[Warehouse] = new[] { "Items", "ItemGroups", "Stock", "Purchases", "Suppliers", "Warehouses", "MaterialRequests:view|Create|Cancel|Issue|ToPurchase" },
			[Accountant] = new[] { "Payments", "Expenses", "Reports", "Contracts:view", "Quotes:view", "Purchases:view|AddPayment|DeletePayment", "Suppliers:view", "Items:view", "Stock:view", "MaterialRequests:view" }
		};

		public static bool IsFullAccess(ClaimsPrincipal user) => user.IsInRole(Prog) || user.IsInRole(Admin);

		// action = null يعني: هل يرى الشاشة أصلاً (لإظهار الرابط في القائمة)
		public static bool CanAccess(ClaimsPrincipal user, string controller, string? action = null)
		{
			if (user.Identity?.IsAuthenticated != true)
				return false;
			if (IsFullAccess(user) || Common.Contains(controller, StringComparer.OrdinalIgnoreCase))
				return true;

			return Screens.Where(s => user.IsInRole(s.Key))
				.SelectMany(s => s.Value)
				.Concat(EveryoneViews)
				.Any(entry => Allows(entry, controller, action));
		}

		private static bool Allows(string entry, string controller, string? action)
		{
			var parts = entry.Split(':');
			if (!parts[0].Equals(controller, StringComparison.OrdinalIgnoreCase))
				return false;
			if (parts.Length == 1 || action == null)
				return true;

			var allowed = parts[1].Split('|').SelectMany(a => a == "view" ? ViewActions : new[] { a });
			return allowed.Contains(action, StringComparer.OrdinalIgnoreCase);
		}

		// ثوابت ثابتة لبذر الأدوار في قاعدة البيانات (HasData يحتاج قيماً لا تتغير)
		public static readonly (string Id, string Name, string Stamp)[] Seed =
		{
			("2f1d6c3a-0b7e-4a51-9d0e-1a2b3c4d5e01", Admin,      "a7c1e3f5-1111-4a00-8000-000000000001"),
			("2f1d6c3a-0b7e-4a51-9d0e-1a2b3c4d5e02", Sales,      "a7c1e3f5-1111-4a00-8000-000000000002"),
			("2f1d6c3a-0b7e-4a51-9d0e-1a2b3c4d5e03", Engineer,   "a7c1e3f5-1111-4a00-8000-000000000003"),
			("2f1d6c3a-0b7e-4a51-9d0e-1a2b3c4d5e04", Designer,   "a7c1e3f5-1111-4a00-8000-000000000004"),
			("2f1d6c3a-0b7e-4a51-9d0e-1a2b3c4d5e05", Warehouse,  "a7c1e3f5-1111-4a00-8000-000000000005"),
			("2f1d6c3a-0b7e-4a51-9d0e-1a2b3c4d5e06", Accountant, "a7c1e3f5-1111-4a00-8000-000000000006")
		};
	}
}
