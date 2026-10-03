using System.ComponentModel.DataAnnotations;

namespace KitchenProject.Models.Enums
{
	public enum SiteVisitStatus
	{
		[Display(Name = "مؤرشف")]
		Scheduled=1,

		[Display(Name = "اكتمل")]
		Completed=2,

		[Display(Name = "تمت الموافقة")]
		Approved=3,

		[Display(Name = "ملغي")]
		Cancelled=4
	}
}
