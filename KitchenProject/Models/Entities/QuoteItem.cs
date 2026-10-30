using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KitchenProject.Models.Entities
{
	public class QuoteItem : BaseEntity
	{
		[ValidateNever]
		public Quote Quote { get; set; }
		public Guid QuoteId { get; set; }

		[Required(ErrorMessage = "وصف البند مطلوب")]
		[MaxLength(250)]
		public string Description { get; set; } = "";

		[MaxLength(30)]
		public string Unit { get; set; } = "قطعة";

		[Range(0.01, 1000000, ErrorMessage = "الكمية يجب أن تكون أكبر من صفر")]
		[Column(TypeName = "decimal(18,2)")]
		public decimal Quantity { get; set; } = 1;

		[Range(0, 100000000, ErrorMessage = "السعر غير صالح")]
		[Column(TypeName = "decimal(18,2)")]
		public decimal UnitPrice { get; set; }

		[NotMapped]
		public decimal Total => Quantity * UnitPrice;
	}
}
