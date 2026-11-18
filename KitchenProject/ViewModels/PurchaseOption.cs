namespace KitchenProject.ViewModels
{
	// فاتورة شراء مستلمة في قائمة اختيار الفاتورة بشاشة الصرف للموردين
	public class PurchaseOption
	{
		public Guid Id { get; set; }
		public Guid SupplierId { get; set; }
		public string PurchaseNo { get; set; } = "";
		public string? InvoiceNo { get; set; }
		public decimal Total { get; set; }
		public decimal Remaining { get; set; }
	}
}
