using System.ComponentModel.DataAnnotations;

namespace KitchenProject.Models.Enums
{
	public enum PaymentMethod
	{
		[Display(Name = "كاش")]
		Cash = 1,
		[Display(Name = "حوالة مصرفية")]
		Transfer = 2,
		[Display(Name = "بطاقة مصرفية")]
		Card = 3,
		[Display(Name = "صك مصرفي")]
		Chek = 4,
	}
}
