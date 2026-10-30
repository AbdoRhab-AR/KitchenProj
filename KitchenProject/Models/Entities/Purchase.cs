using KitchenProject.Models.Enums;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KitchenProject.Models.Entities
{
	// فاتورة شراء خامات من مورد، تدخل المخزن عند استلامها
	public class Purchase : BaseEntity
	{
		[MaxLength(30)]
		public string PurchaseNo { get; set; } = "";

		[ValidateNever]
		public Supplier Suppliers { get; set; }
		public Guid SupplierId { get; set; }
		[ValidateNever]
		public Warehouse Warehouses { get; set; }
		public Guid WarehouseId { get; set; }

		// اختياري: مشتريات خاصة بمشروع معين
		[ValidateNever]
		public Project? Projects { get; set; }
		public Guid? ProjectId { get; set; }

		// عند الاستلام: تُصرف الكمية مباشرة على المشروع بدلاً من بقائها في المخزن
		public bool DirectToProject { get; set; }

		// رقم فاتورة المورد
		[MaxLength(50)]
		public string? InvoiceNo { get; set; } 

		public DateTime PurchaseDate { get; set; } = DateTime.Today;

		[Column(TypeName = "decimal(18,2)")]
		public decimal Subtotal { get; set; }

		[Range(0, 100000000, ErrorMessage = "الخصم غير صالح")]
		[Column(TypeName = "decimal(18,2)")]
		public decimal Discount { get; set; }

		[Range(0, 100000000, ErrorMessage = "الضريبة غير صالحة")]
		[Column(TypeName = "decimal(18,2)")]
		public decimal Tax { get; set; }

		[Column(TypeName = "decimal(18,2)")]
		public decimal Total { get; set; }

		[Column(TypeName = "decimal(18,2)")]
		public decimal PaidAmount { get; set; }

		[NotMapped]
		public decimal RemainingAmount => Total - PaidAmount;

		[MaxLength(30)]
		public string Status { get; set; } = PurchaseStatuses.Draft;

		public string? Notes { get; set; }
		public List<PurchaseItem> Items { get; set; } = new List<PurchaseItem>();
		public ICollection<SupplierPayment> Payments { get; set; } = new List<SupplierPayment>();

		public void Recalculate()
		{
			Subtotal = Items.Sum(i => i.Total);
			Total = Subtotal - Discount + Tax;
		}
	}
}
