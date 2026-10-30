using System.ComponentModel.DataAnnotations;

namespace KitchenProject.Models.Entities
{
	public class Supplier : BaseEntity
	{
		[MaxLength(30)]
		public required  string SupplierNo { get; set; }

		[Required(ErrorMessage = "اسم المورد مطلوب")]
		[MaxLength(150)]
		public required string Name { get; set; }

		[MaxLength(30)]
		public string? Phone { get; set; }

		[MaxLength(250)]
		public string? Address { get; set; }

		// شروط الدفع المتفق عليها مع المورد (مثلاً: آجل 30 يوم)
		[MaxLength(100)]
		public string? PaymentTerms { get; set; }

		public string? Notes { get; set; }
		public bool IsActive { get; set; } = true;
		public ICollection<Purchase> Purchases { get; set; } = new List<Purchase>();
		public ICollection<SupplierPayment> Payments { get; set; } = new List<SupplierPayment>();
	}
}
