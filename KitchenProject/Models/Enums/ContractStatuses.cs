namespace KitchenProject.Models.Enums
{
	// Contract.Status مخزن كنص، وهذه القيم المسموحة مع أسمائها العربية
	public static class ContractStatuses
	{
		public const string Draft = "Draft";
		public const string Active = "Active";
		public const string Completed = "Completed";
		public const string Cancelled = "Cancelled";

		public static readonly Dictionary<string, string> Names = new()
		{
			[Draft] = "مسودة",
			[Active] = "ساري",
			[Completed] = "منتهي",
			[Cancelled] = "ملغى"
		};

		public static string GetName(string status) =>
			Names.TryGetValue(status, out var name) ? name : status;
	}
}
