namespace KitchenProject.ViewModels
{
	public class PartyStatementRow
	{
		public DateTime Date { get; set; }
		public string DocType { get; set; } = "";
		public string DocNo { get; set; } = "";
		public string Description { get; set; } = "";
		public decimal Debit { get; set; }
		public decimal Credit { get; set; }
		public decimal Balance { get; set; }
	}

	public class AgingRow
	{
		public string PartyNo { get; set; } = "";
		public string PartyName { get; set; } = "";
		public string DocNo { get; set; } = "";
		public DateTime DocDate { get; set; }
		public int AgeDays { get; set; }
		public decimal Total { get; set; }
		public decimal Paid { get; set; }
		public decimal Remaining { get; set; }
		public decimal Bucket0To30 => AgeDays <= 30 ? Remaining : 0;
		public decimal Bucket31To60 => AgeDays is > 30 and <= 60 ? Remaining : 0;
		public decimal Bucket61To90 => AgeDays is > 60 and <= 90 ? Remaining : 0;
		public decimal BucketOver90 => AgeDays > 90 ? Remaining : 0;
	}
}
