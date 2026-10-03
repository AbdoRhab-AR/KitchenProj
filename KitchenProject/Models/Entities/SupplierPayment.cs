using KitchenProject.Models.Enums;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KitchenProject.Models.Entities
{
	public class SupplierPayment : BaseEntity
	{
		[ValidateNever]
		public Supplier supplier { get; set; }
		public Guid SupplierId { get; set; }
		public Guid? PurchaseId { get; set; }

		[Column(TypeName = "decimal(18,2)")]
		public decimal Amount { get; set; }

		[MaxLength(30)]
		public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;

		public string? Notes { get; set; }
	}
}
