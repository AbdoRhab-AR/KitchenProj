using System.ComponentModel.DataAnnotations;

namespace KitchenProject.Models.Enums
{
	public enum ProjectStatus
	{
		[Display(Name = "جديد")]
		New = 1,

		[Display(Name = "معاينة")]
		SiteVisit = 2,

		[Display(Name = "تصميم")]
		Design = 3,

		[Display(Name = "عرض سعر")]
		Quote = 4,

		[Display(Name = "تم التعاقد")]
		Contracted = 6,

		[Display(Name = "قيد التنفيذ")]
		InProgress = 7,

		[Display(Name = "تم التسليم")]
		Delivered = 10,

		[Display(Name = "مغلق")]
		Closed = 11,

		[Display(Name = "ملغى")]
		Cancelled = 12
	}
}
