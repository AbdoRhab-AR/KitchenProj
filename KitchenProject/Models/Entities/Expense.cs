using KitchenProject.Models.Enums;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KitchenProject.Models.Entities
{
	// مصروف: على مشروع (نقل، تركيب، عمالة...) فيدخل في تكلفته، أو مصروف عام للشركة (إيجار، كهرباء...)
	public class Expense : BaseEntity
	{
		[ValidateNever]
		public Project? Project { get; set; }
		public Guid? ProjectId { get; set; }

		[Required(ErrorMessage = "نوع المصروف مطلوب")]
		[MaxLength(100)]
		public string ExpenseType { get; set; } = "";

		[Range(0.01, 100000000, ErrorMessage = "المبلغ يجب أن يكون أكبر من صفر")]
		[Column(TypeName = "decimal(18,2)")]
		public decimal Amount { get; set; }

		public DateTime ExpenseDate { get; set; } = DateTime.Today;

		public PaymentMethod Payment { get; set; } = PaymentMethod.Cash;

		[MaxLength(500)]
		public string? Description { get; set; }
		public string? AttachmentPath { get; set; }
		public string? Notes { get; set; }

		[NotMapped]
		public IFormFile? Attachment { get; set; }

		// أنواع مقترحة في الشاشة (يمكن كتابة غيرها)
		public static readonly string[] SuggestedTypes =
		{
			"نقل وتوصيل", "تركيب", "عمالة يومية", "أدوات ومستهلكات", "صيانة", "إيجار", "كهرباء ومياه", "رواتب", "دعاية وإعلان", "أخرى"
		};
	}
}
