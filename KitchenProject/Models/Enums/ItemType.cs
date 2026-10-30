using System.ComponentModel.DataAnnotations;

namespace KitchenProject.Models.Enums
{
	// الخام يدخل المخزن من الموردين ويُصرف للمشاريع، والجاهز يُباع للعميل في عروض الأسعار
	public enum ItemType
	{
		[Display(Name = "خام")]
		Raw = 1,
		[Display(Name = "جاهز")]
		Finished = 2
	}
}
