using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KitchenProject.Models.Entities
{
	public class PurchaseItem :BaseEntity
	{
		[ValidateNever]
		public Purchase Purchases { get; set; }
		public Guid PurchaseId { get; set; }
		[ValidateNever]
		public Item Items { get; set; }

		public Guid ItemId { get; set; }

		[Range(0.01, 100000000, ErrorMessage = "الكمية يجب أن تكون أكبر من صفر")]
		[Column(TypeName = "decimal(18,2)")]
		public decimal Quantity { get; set; } = 1;

		[Range(0, 100000000, ErrorMessage = "السعر غير صالح")]
		[Column(TypeName = "decimal(18,2)")]
		public decimal UnitPrice { get; set; }

		[NotMapped]
		public decimal Total => Quantity * UnitPrice;

	}
}
