using KitchenProject.Models;
using KitchenProject.Models.Entities;
using KitchenProject.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace KitchenProject.Classes
{
	// شجرة الحسابات الافتراضية لشركة تصنيع وتركيب مطابخ، مع ضمان وجود حسابات النظام الأساسية
	public static class AccountingSeeder
	{
		private record Row(string Code, string Name, AccountType Type, string? Parent, bool Postable, string? Key = null);

		private static readonly Row[] Rows =
		{
			new("1", "الأصول", AccountType.Asset, null, false),
			new("11", "الأصول المتداولة", AccountType.Asset, "1", false),
			new("1110", "الصندوق", AccountType.Asset, "11", true, "Cash"),
			new("1120", "البنك", AccountType.Asset, "11", true, "Bank"),
			new("1130", "العملاء (ذمم مدينة)", AccountType.Asset, "11", true, "AR"),
			new("1140", "المخزون", AccountType.Asset, "11", true, "Inventory"),
			new("1150", "سلف وأرصدة مدينة أخرى", AccountType.Asset, "11", true),
			new("1160", "ضريبة القيمة المضافة (مدخلات)", AccountType.Asset, "11", true, "VATInput"),
			new("1170", "شيكات تحت التحصيل", AccountType.Asset, "11", true, "ChequesReceivable"),
			new("12", "الأصول الثابتة", AccountType.Asset, "1", false),
			new("1210", "معدات وآلات", AccountType.Asset, "12", true),
			new("1220", "أثاث وتجهيزات", AccountType.Asset, "12", true),
			new("1230", "سيارات", AccountType.Asset, "12", true),

			new("2", "الخصوم", AccountType.Liability, null, false),
			new("21", "الخصوم المتداولة", AccountType.Liability, "2", false),
			new("2110", "الموردون (ذمم دائنة)", AccountType.Liability, "21", true, "AP"),
			new("2120", "دفعات مقدمة من العملاء", AccountType.Liability, "21", true),
			new("2130", "مصروفات مستحقة", AccountType.Liability, "21", true),
			new("2140", "شيكات مؤجلة الدفع", AccountType.Liability, "21", true, "ChequesPayable"),
			new("22", "الخصوم طويلة الأجل", AccountType.Liability, "2", false),
			new("2210", "قروض", AccountType.Liability, "22", true),

			new("3", "حقوق الملكية", AccountType.Equity, null, false),
			new("3100", "رأس المال", AccountType.Equity, "3", true, "Capital"),
			new("3200", "الأرباح المحتجزة", AccountType.Equity, "3", true, "RetainedEarnings"),
			new("3300", "جاري الشركاء", AccountType.Equity, "3", true),

			new("4", "الإيرادات", AccountType.Revenue, null, false),
			new("4100", "إيرادات مبيعات المطابخ", AccountType.Revenue, "4", true, "Sales"),
			new("4200", "رسوم إلغاء المشاريع", AccountType.Revenue, "4", true, "CancellationFees"),
			new("4900", "إيرادات أخرى", AccountType.Revenue, "4", true),

			new("5", "تكلفة المبيعات", AccountType.Expense, null, false),
			new("5100", "تكلفة الخامات المصروفة للمشاريع", AccountType.Expense, "5", true, "COGS"),
			new("5200", "مصروفات المشاريع المباشرة", AccountType.Expense, "5", true, "ProjectExpenses"),
			new("5300", "فروقات وتسويات المخزون", AccountType.Expense, "5", true, "StockAdjust"),

			new("6", "المصروفات التشغيلية", AccountType.Expense, null, false),
			new("6100", "رواتب وأجور", AccountType.Expense, "6", true, "ExpSalaries"),
			new("6200", "إيجارات", AccountType.Expense, "6", true, "ExpRent"),
			new("6300", "كهرباء ومياه", AccountType.Expense, "6", true, "ExpUtilities"),
			new("6400", "دعاية وإعلان", AccountType.Expense, "6", true, "ExpMarketing"),
			new("6500", "صيانة", AccountType.Expense, "6", true, "ExpMaintenance"),
			new("6900", "مصروفات عمومية أخرى", AccountType.Expense, "6", true, "ExpOther"),
		};

		public static async Task SeedAsync(ApplicationDbContext db)
		{
			var now = DateTime.UtcNow;
			var existing = await db.Accounts.ToListAsync();

			if (existing.Count == 0)
			{
				var byCode = new Dictionary<string, Account>();
				foreach (var row in Rows)
				{
					var account = new Account
					{
						Id = Guid.NewGuid(),
						Created = now,
						Code = row.Code,
						Name = row.Name,
						Type = row.Type,
						ParentId = row.Parent != null ? byCode[row.Parent].Id : null,
						IsPostable = row.Postable,
						SystemKey = row.Key
					};
					byCode[row.Code] = account;
					db.Accounts.Add(account);
				}

				await db.SaveChangesAsync();
				await SyncCatalogItemsAsync(db);
				return;
			}

			var byExistingCode = existing.ToDictionary(a => a.Code, a => a);
			var byKey = existing.Where(a => a.SystemKey != null).ToDictionary(a => a.SystemKey!, a => a);
			bool changed = false;

			foreach (var row in Rows.Where(r => r.Key != null))
			{
				if (byKey.ContainsKey(row.Key!))
				{
					continue;
				}

				if (byExistingCode.TryGetValue(row.Code, out var match))
				{
					match.SystemKey = row.Key;
					changed = true;
					continue;
				}

				Guid? parentId = row.Parent != null && byExistingCode.TryGetValue(row.Parent, out var p) ? p.Id : null;
				var account = new Account
				{
					Id = Guid.NewGuid(),
					Created = now,
					Code = row.Code,
					Name = row.Name,
					Type = row.Type,
					ParentId = parentId,
					IsPostable = row.Postable,
					SystemKey = row.Key
				};
				db.Accounts.Add(account);
				byExistingCode[row.Code] = account;
				changed = true;
			}

			if (changed)
			{
				await db.SaveChangesAsync();
			}

			await SyncCatalogItemsAsync(db);
		}

		// تصحيح نوع الإكسسوارات المخزنية (C1..C4) وضمان ألا يقل سعر البيع عن سعر الشراء في قاعدة البيانات الحالية
		private static async Task SyncCatalogItemsAsync(ApplicationDbContext db)
		{
			var stockGroups = FinishedCatalogSeed.StockGroupIds;
			var items = await db.Items
				.Where(i => (i.ItemGroupId != null && stockGroups.Contains(i.ItemGroupId.Value) && i.Type == ItemType.Finished)
						 || i.PurchasePrice > i.SalePrice)
				.ToListAsync();

			if (items.Count == 0)
			{
				return;
			}

			var now = DateTime.UtcNow;
			foreach (var item in items)
			{
				if (item.ItemGroupId.HasValue && stockGroups.Contains(item.ItemGroupId.Value) && item.Type == ItemType.Finished)
				{
					item.Type = ItemType.Raw;
					item.Modified = now;
				}

				if (item.PurchasePrice > item.SalePrice)
				{
					item.SalePrice = item.PurchasePrice;
					item.Modified = now;
				}
			}

			await db.SaveChangesAsync();
		}
	}
}
