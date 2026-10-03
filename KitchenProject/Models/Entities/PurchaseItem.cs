using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
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
		public decimal Quantity { get; set; }

		[Column(TypeName = "decimal(18,2)")]
		public decimal UnitPrice { get; set; }

		[NotMapped]
		public decimal Total => Quantity * UnitPrice;

	}
}
