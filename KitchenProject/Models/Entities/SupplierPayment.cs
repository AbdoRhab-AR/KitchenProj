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

		[ValidateNever]
		public Purchase? Purchase { get; set; }
		public Guid? PurchaseId { get; set; }

		public DateTime PaymentDate { get; set; } = DateTime.Today;

		[Range(0.01, 100000000, ErrorMessage = "المبلغ يجب أن يكون أكبر من صفر")]
		[Column(TypeName = "decimal(18,2)")]
		public decimal Amount { get; set; }

		public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;

		public string? Notes { get; set; }
	}
}
