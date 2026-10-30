using KitchenProject.Models.Enums;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KitchenProject.Models.Entities
{
	public class Item :BaseEntity
	{
		[Required(ErrorMessage = "كود الصنف مطلوب")]
		[MaxLength(30)]
		public required string Code { get; set; }

		[Required(ErrorMessage = "اسم الصنف مطلوب")]
		[MaxLength(150)]
		public required string Name { get; set; } 

		public ItemType Type { get; set; } = ItemType.Raw;

		// للأصناف الجاهزة فقط
		[ValidateNever]
		public ItemGroup? Group { get; set; }
		public Guid? ItemGroupId { get; set; }

		[MaxLength(100)]
		public string? Category { get; set; }

		[MaxLength(30)]
		public string Unit { get; set; } = "قطعة";

		// آخر سعر شراء (للخام)
		[Range(0, 100000000, ErrorMessage = "السعر غير صالح")]
		[Column(TypeName = "decimal(18,2)")]
		public decimal PurchasePrice { get; set; }

		// سعر البيع الافتراضي في عروض الأسعار (للجاهز)
		[Range(0, 100000000, ErrorMessage = "السعر غير صالح")]
		[Column(TypeName = "decimal(18,2)")]
		public decimal SalePrice { get; set; }

		// حد إعادة الطلب: تنبيه عندما يقل الرصيد عنه
		[Range(0, 100000000, ErrorMessage = "الحد الأدنى غير صالح")]
		[Column(TypeName = "decimal(18,2)")]
		public decimal MinQuantity { get; set; }

		public string? Notes { get; set; }

		public bool IsActive { get; set; } = true;
		public ICollection<PurchaseItem> PurchaseItems { get; set; } = new List<PurchaseItem>();
		public ICollection<StockTransaction> StockTransactions { get; set; } = new List<StockTransaction>();
	}
}
