using KitchenProject.Models.Enums;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KitchenProject.Models.Entities
{
	public class Expense : BaseEntity
	{
		[ValidateNever]
		public Project? Project { get; set; }
		public Guid? ProjectId { get; set; }

		[MaxLength(100)]
		public required  string ExpenseType { get; set; }

		[Column(TypeName = "decimal(18,2)")]
		public decimal Amount { get; set; }

		public PaymentMethod Payment { get; set; } = PaymentMethod.Cash ;

		public string? Description { get; set; }
		public string? AttachmentPath { get; set; }
		public string? Notes { get; set; }
	}
}
