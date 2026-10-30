using System.ComponentModel.DataAnnotations;

namespace KitchenProject.Models.Enums
{
	public enum SiteVisitStatus
	{
		[Display(Name = "مجدولة")]
		Scheduled=1,

		[Display(Name = "تمت المعاينة")]
		Completed=2,

		[Display(Name = "معتمدة")]
		Approved=3,

		[Display(Name = "ملغاة")]
		Cancelled=4
	}
}
