using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KitchenProject.Models.Entities
{
	// بند في الفاتورة (المبدئية أو النهائية)
	public class QuoteItem : BaseEntity
	{
		[ValidateNever]
		public Quote Quote { get; set; }
		public Guid QuoteId { get; set; }

		// اختياري: ربط البند بصنف من قائمة الأصناف
		[ValidateNever]
		public Item? Item { get; set; }
		public Guid? ItemId { get; set; }

		public int SortOrder { get; set; }

		[Required, MaxLength(250)]
		[Display(Name = "البيان")]
		public string Description { get; set; } = string.Empty;

		// الوحدة / م (مثلاً 46.0 متر)
		[Column(TypeName = "decimal(10,2)")]
		[Display(Name = "الوحدة / م")]
		public decimal? Meters { get; set; }

		[Column(TypeName = "decimal(10,2)")]
		[Display(Name = "الكمية")]
		public decimal? Quantity { get; set; }

		[Column(TypeName = "decimal(18,2)")]
		[Display(Name = "ثمن الوحدة")]
		public decimal UnitPrice { get; set; }

		// الإجمالي = الأمتار × الكمية × ثمن الوحدة (الحقل الفارغ يحسب كـ 1)
		[NotMapped]
		public decimal LineTotal => CalcLineTotal(Meters, Quantity, UnitPrice);

		public static decimal CalcLineTotal(decimal? meters, decimal? quantity, decimal unitPrice)
		{
			var m = meters.HasValue && meters.Value > 0 ? meters.Value : 1;
			var q = quantity.HasValue && quantity.Value > 0 ? quantity.Value : 1;
			return Math.Round(m * q * unitPrice, 2);
		}
	}
}
