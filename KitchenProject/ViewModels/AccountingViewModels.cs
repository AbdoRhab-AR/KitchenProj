namespace KitchenProject.ViewModels
{
	public class JournalLineVm
	{
		public Guid AccountId { get; set; }
		public decimal Debit { get; set; }
		public decimal Credit { get; set; }
		public string? Description { get; set; }
	}

	public class JournalFormVm
	{
		public Guid? Id { get; set; }
		public string? EntryNo { get; set; }
		public DateTime EntryDate { get; set; } = DateTime.Today;
		public string Description { get; set; } = "";
		public List<JournalLineVm> Lines { get; set; } = new();
	}

	public class OpeningBalanceLineVm
	{
		public Guid AccountId { get; set; }
		public string Code { get; set; } = "";
		public string Name { get; set; } = "";
		public string TypeName { get; set; } = "";
		public bool IsDebitNature { get; set; }
		public decimal Debit { get; set; }
		public decimal Credit { get; set; }
	}

	public class OpeningBalancesVm
	{
		public DateTime EntryDate { get; set; } = new DateTime(DateTime.Today.Year, 1, 1);
		public string Description { get; set; } = "الأرصدة الافتتاحية";
		public List<OpeningBalanceLineVm> Lines { get; set; } = new();
	}

	public class ChequeRowVm
	{
		public bool IsCustomer { get; set; }
		public Guid Id { get; set; }
		public string ReceiptNo { get; set; } = "";
		public DateTime PaymentDate { get; set; }
		public DateTime? DueDate { get; set; }
		public string? ReferenceNo { get; set; }
		public string PartyName { get; set; } = "";
		public string LinkText { get; set; } = "";
		public decimal Amount { get; set; }
		public Models.Enums.ChequeStatus Status { get; set; }
	}

	public class LedgerRow
	{
		public DateTime Date { get; set; }
		public Guid EntryId { get; set; }
		public string EntryNo { get; set; } = "";
		public string Description { get; set; } = "";
		public string AccountName { get; set; } = "";
		public decimal Debit { get; set; }
		public decimal Credit { get; set; }
		public decimal Balance { get; set; }
	}
}
