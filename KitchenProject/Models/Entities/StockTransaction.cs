using KitchenProject.Models.Enums;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KitchenProject.Models.Entities
{
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
		public StockTransactionType TransactionType { get; set; }
		public decimal QuantityIn { get; set; }
		public decimal QuantityOut { get; set; }

		[Column(TypeName = "decimal(18,2)")]
		public decimal UnitCost { get; set; }

		[MaxLength(50)]
		public string? ReferenceNo { get; set; }
		public string? Notes { get; set; }
	}
}
