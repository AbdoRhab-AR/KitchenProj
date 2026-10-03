using System.ComponentModel.DataAnnotations;

namespace KitchenProject.Models.Enums
{
	public enum DesignStatus
	{
		[Display(Name = "مسودة")]
		Draft = 1,
		
		[Display(Name = "بانتظار الموافقة")]
		WaitingApproval = 2,
		
		[Display(Name = "موافقة")]
		Approved = 3,
		
		[Display(Name = "مرفوض")]
		Rejected = 4
	}
}
