namespace KitchenProject.ViewModels
{
	// عقد في قائمة اختيار العقد بشاشة التحصيل
	public class ContractOption
	{
		public Guid Id { get; set; }
		public Guid ProjectId { get; set; }
		public string ContractNo { get; set; } = "";
		public decimal Total { get; set; }
		public decimal Remaining { get; set; }
	}
}
