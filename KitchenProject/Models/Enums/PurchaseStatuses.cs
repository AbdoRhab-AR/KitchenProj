namespace KitchenProject.Models.Enums
{
	// حالة فاتورة الشراء (تُخزن كنص في Purchase.Status)
	public static class PurchaseStatuses
	{
		public const string Draft = "Draft";
		public const string Received = "Received";
		public const string Cancelled = "Cancelled";

		public static readonly string[] All = { Draft, Received, Cancelled };

		public static string GetName(string? status) => status switch
		{
			Draft => "مسودة",
			Received => "مستلمة بالمخزن",
			Cancelled => "ملغاة",
			_ => status ?? "-"
		};
	}
}
