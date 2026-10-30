using KitchenProject.Models.Enums;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KitchenProject.Models.Entities
{
	// دفعة مستلمة من العميل (إيصال قبض) على مشروع، ويُفضل ربطها بالعقد
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

		public DateTime PaymentDate { get; set; } = DateTime.Today;

		[Range(0.01, 100000000, ErrorMessage = "المبلغ يجب أن يكون أكبر من صفر")]
		[Column(TypeName = "decimal(18,2)")]
		public decimal Amount { get; set; }

		[Required]
		public PaymentMethod Payment { get; set; } = PaymentMethod.Cash;

		// رقم الصك أو الحوالة
		[MaxLength(50)]
		public string? ReferenceNo { get; set; }

		public string? Notes { get; set; }
	}
}
