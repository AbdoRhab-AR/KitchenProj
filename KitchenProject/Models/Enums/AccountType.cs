using System.ComponentModel.DataAnnotations;

namespace KitchenProject.Models.Enums
{
	// نوع الحساب في شجرة الحسابات
	public enum AccountType
	{
		[Display(Name = "أصول")]
		Asset = 1,
		[Display(Name = "خصوم")]
		Liability = 2,
		[Display(Name = "حقوق ملكية")]
		Equity = 3,
		[Display(Name = "إيرادات")]
		Revenue = 4,
		[Display(Name = "مصروفات")]
		Expense = 5
	}
}
