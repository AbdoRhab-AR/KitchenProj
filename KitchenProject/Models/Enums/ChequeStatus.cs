using System.ComponentModel.DataAnnotations;

namespace KitchenProject.Models.Enums
{
	public enum ChequeStatus
	{
		[Display(Name = "تحت التحصيل / مؤجل")]
		Pending = 1,
		[Display(Name = "محصّل / مصروف")]
		Cleared = 2,
		[Display(Name = "مرتجع / مرفوض")]
		Bounced = 3
	}
}
