using KitchenProject.Models.Enums;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KitchenProject.Models.Entities
{
	// حركة مخزنية واحدة: الرصيد = مجموع الوارد - مجموع المنصرف
	public class StockTransaction:BaseEntity
	{
		[ValidateNever]
		public Item Items { get; set; }
		public Guid ItemId { get; set; }
		[ValidateNever]
		public Warehouse Warehouses { get; set; } 
		public Guid WarehouseId { get; set; }
		
		[ValidateNever]
		public Project? Projects { get; set; }
		public Guid? ProjectId { get; set; }

		// فاتورة الشراء التي أنشأت الحركة (حركات الشراء تُدار من شاشة المشتريات)
		[ValidateNever]
		public Purchase? Purchase { get; set; }
		public Guid? PurchaseId { get; set; }

		public StockTransactionType TransactionType { get; set; }

		public DateTime TransactionDate { get; set; } = DateTime.Today;

		[Column(TypeName = "decimal(18,2)")]
		public decimal QuantityIn { get; set; }

		[Column(TypeName = "decimal(18,2)")]
		public decimal QuantityOut { get; set; }

		[Column(TypeName = "decimal(18,2)")]
		public decimal UnitCost { get; set; }

		[NotMapped]
		public decimal TotalCost => (QuantityIn + QuantityOut) * UnitCost;

		[MaxLength(50)]
		public string? ReferenceNo { get; set; }
		public string? Notes { get; set; }
	}
}
