using KitchenProject.Models.Entities;
using KitchenProject.Models.Enums;

namespace KitchenProject.ViewModels
{
	// لوحة التحكم: كل قسم يُملأ فقط إذا كان دور المستخدم يسمح بالشاشة المرتبطة به
	public class DashboardViewModel
	{
		public Dictionary<ProjectStatus, int> ProjectsByStatus { get; set; } = new();
		public int ActiveProjects { get; set; }
		public List<Project> RecentProjects { get; set; } = new();
		public List<Project> MyProjects { get; set; } = new();
		public List<SiteVisit> UpcomingVisits { get; set; } = new();

		// المالية
		public bool ShowFinance { get; set; }
		public decimal MonthCollections { get; set; }
		public decimal MonthExpenses { get; set; }
		public decimal Outstanding { get; set; }
		public decimal MonthContracts { get; set; }

		// المخزن
		public bool ShowStock { get; set; }
		public int LowStockCount { get; set; }
		public decimal MonthMaterialsIssued { get; set; }
		public int DraftPurchases { get; set; }

		// طلبات المواد: للمدير = بانتظار الاعتماد، للمخزن = معتمدة بانتظار الصرف، لغيرهم = طلباته المفتوحة
		public bool ShowRequests { get; set; }
		public int RequestsCount { get; set; }
		public string RequestsLabel { get; set; } = "";
		public MaterialRequestStatus? RequestsStatus { get; set; }
	}
}
