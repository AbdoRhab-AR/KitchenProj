using KitchenProject.Models.Enums;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KitchenProject.Models.Entities
{
	public class CustomerPayment :BaseEntity
	{
		[ValidateNever]
		public Project Project { get; set; }
		public required Guid ProjectId { get; set; }

		[ValidateNever]
		public Contract? Contract { get; set; }
		public Guid? ContractId { get; set; }

		[MaxLength(30)]
		public required string ReceiptNo { get; set; }

		[Column(TypeName = "decimal(18,2)")]
		public decimal Amount { get; set; }

		[Required, MaxLength(30)]
		public PaymentMethod Payment { get; set; } = PaymentMethod.Cash;
		public string? Notes { get; set; }
	}
}
