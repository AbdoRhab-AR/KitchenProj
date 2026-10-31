using System.ComponentModel.DataAnnotations;

namespace KitchenProject.Models.Enums
{
	// طلب مواد داخلي: معلق ← (معتمد / مرفوض) ← (طُلب شراؤه) ← مصروف
	public enum MaterialRequestStatus
	{
		[Display(Name = "بانتظار الاعتماد")]
		Pending = 1,
		[Display(Name = "معتمد")]
		Approved = 2,
		[Display(Name = "مرفوض")]
		Rejected = 3,
		[Display(Name = "بانتظار الشراء")]
		Ordered = 4,
		[Display(Name = "تم الصرف")]
		Issued = 5,
		[Display(Name = "ملغى")]
		Cancelled = 6
	}
}
