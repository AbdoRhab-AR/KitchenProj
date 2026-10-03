using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KitchenProject.Models.Entities
{
	public class Purchase : BaseEntity
	{
		[ValidateNever]
		public Supplier Suppliers { get; set; }
		public Guid SupplierId { get; set; }
		[ValidateNever]
		public Warehouse Warehouses { get; set; }
		public Guid WarehouseId { get; set; }
		[ValidateNever]
		public Project Projects { get; set; }
		public Guid? ProjectId { get; set; }

		[MaxLength(50)]
		public required string InvoiceNo { get; set; } 

		[Column(TypeName = "decimal(18,2)")]
		public decimal Subtotal { get; set; }

		[Column(TypeName = "decimal(18,2)")]
		public decimal Discount { get; set; }

		[Column(TypeName = "decimal(18,2)")]
		public decimal Tax { get; set; }

		[Column(TypeName = "decimal(18,2)")]
		public decimal Total { get; set; }

		[Column(TypeName = "decimal(18,2)")]
		public decimal PaidAmount { get; set; }

		[NotMapped]
		public decimal RemainingAmount => Total - PaidAmount;

		[MaxLength(30)]
		public string Status { get; set; } = "Draft";

		public string? Notes { get; set; }
		public ICollection<PurchaseItem> Items { get; set; } = new List<PurchaseItem>();
	}
}
