using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KitchenProject.Models.Entities
{
	// مرتجع مشتريات لمورد (إشعار دائن جزئي أو كلي بعد استلام فاتورة الشراء)
	public class PurchaseReturn : BaseEntity
	{
		[MaxLength(30)]
		public string ReturnNo { get; set; } = "";

		[ValidateNever]
		public Purchase Purchase { get; set; } = null!;
		public Guid PurchaseId { get; set; }

		[ValidateNever]
		public Supplier Supplier { get; set; } = null!;
		public Guid SupplierId { get; set; }

		[ValidateNever]
		public Warehouse Warehouse { get; set; } = null!;
		public Guid WarehouseId { get; set; }

		public DateTime ReturnDate { get; set; } = DateTime.Today;

		[Column(TypeName = "decimal(18,2)")]
		public decimal Subtotal { get; set; }

		[Column(TypeName = "decimal(18,2)")]
		public decimal Tax { get; set; }

		[Column(TypeName = "decimal(18,2)")]
		public decimal Total { get; set; }

		public string? Notes { get; set; }

		[MaxLength(100)]
		public string? CreatedBy { get; set; }

		public List<PurchaseReturnItem> Items { get; set; } = new();
	}

	public class PurchaseReturnItem : BaseEntity
	{
		[ValidateNever]
		public PurchaseReturn PurchaseReturn { get; set; } = null!;
		public Guid PurchaseReturnId { get; set; }

		[ValidateNever]
		public Item Item { get; set; } = null!;
		public Guid ItemId { get; set; }

		[Range(0.01, 100000000, ErrorMessage = "الكمية يجب أن تكون أكبر من صفر")]
		[Column(TypeName = "decimal(18,2)")]
		public decimal Quantity { get; set; }

		[Range(0, 100000000, ErrorMessage = "السعر غير صالح")]
		[Column(TypeName = "decimal(18,2)")]
		public decimal UnitPrice { get; set; }

		[NotMapped]
		public decimal Total => Quantity * UnitPrice;
	}
}
