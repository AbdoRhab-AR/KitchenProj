using System.ComponentModel.DataAnnotations;

namespace KitchenProject.Models.Enums
{
	// المخزني (خام / إكسسوار) يدخل المخزن من الموردين ويُصرف للمشاريع، والوحدة المصنّعة / الخدمة تُباع في عروض الأسعار
	public enum ItemType
	{
		[Display(Name = "مخزني (خام / إكسسوار)")]
		Raw = 1,
		[Display(Name = "وحدة مصنّعة / خدمة")]
		Finished = 2
	}
}
