using KitchenProject.Models.Enums;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KitchenProject.Models.Entities
{
	public class Quote:BaseEntity
	{
		[ValidateNever]
		public Project Project { get; set; } 
		public Guid ProjectId { get; set; }

		[MaxLength(30)]
		public required string QuoteNo { get; set; }

		[DisplayFormat(DataFormatString = "{0:dd-MM-yyyy hh:mm:ss tt}")]
		public DateTime? ValidUntil { get; set; }

		[Column(TypeName = "decimal(18,2)")]
		public decimal Subtotal { get; set; }

		// الخصم يُدخل كنسبة مئوية، وقيمته تُحسب في الخادم
		[Range(0, 100, ErrorMessage = "نسبة الخصم بين 0 و 100")]
		[Column(TypeName = "decimal(5,2)")]
		public decimal DiscountPercent { get; set; }

		[Column(TypeName = "decimal(18,2)")]
		public decimal Discount { get; set; }

		[Column(TypeName = "decimal(18,2)")]
		public decimal Tax { get; set; }

		[Column(TypeName = "decimal(18,2)")]
		public decimal Total { get; set; }

		public DesignStatus Status { get; set; } = DesignStatus.Draft;

		public string? Notes { get; set; }

		public List<QuoteItem> Items { get; set; } = new List<QuoteItem>();

		// يعيد حساب الإجمالي من البنود ونسبة الخصم
		public void Recalculate()
		{
			Subtotal = Items.Sum(i => i.Total);
			Discount = Math.Round(Subtotal * DiscountPercent / 100, 2);
			Total = Subtotal - Discount + Tax;
		}
	}
}
