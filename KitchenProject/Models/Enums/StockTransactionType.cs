using System.ComponentModel.DataAnnotations;

namespace KitchenProject.Models.Enums
{
	public enum StockTransactionType
	{
		[Display(Name = "شراء")]
		Purchase = 1,
		[Display(Name = "اصدار")]
		Issue = 2,
		[Display(Name = "تحويل وارد")]
		TransferIn = 3,
		[Display(Name = "تحويل صادر")]
		TransferOut = 4,
		[Display(Name = "استرجاع")]
		Return = 5,
		[Display(Name = "تعديل")]
		Adjustment = 6
	}
}
