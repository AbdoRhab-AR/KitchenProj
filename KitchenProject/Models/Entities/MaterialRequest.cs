using KitchenProject.Models.Enums;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KitchenProject.Models.Entities
{
	// طلب مواد داخلي لمشروع: يطلبه الموظف، يعتمده المدير، ثم يصرفه المخزن أو يحوله لفاتورة شراء
	public class MaterialRequest : BaseEntity
	{
		[MaxLength(30)]
		public string RequestNo { get; set; } = "";

		[ValidateNever]
		public Project Project { get; set; }
		[Required(ErrorMessage = "يجب اختيار المشروع")]
		public Guid ProjectId { get; set; }

		[ValidateNever]
		public Warehouse Warehouse { get; set; }
		[Required(ErrorMessage = "يجب اختيار المخزن")]
		public Guid WarehouseId { get; set; }

		[ValidateNever]
		public ApplicationUser? RequestedBy { get; set; }
		[MaxLength(450)]
		public string? RequestedById { get; set; }

		public DateTime RequestDate { get; set; } = DateTime.Today;

		// التاريخ المطلوب توفر المواد فيه
		public DateTime? NeededBy { get; set; }

		public MaterialRequestStatus Status { get; set; } = MaterialRequestStatus.Pending;

		[MaxLength(1000)]
		public string? Notes { get; set; }

		// قرار المدير (الاعتماد أو سبب الرفض)
		[ValidateNever]
		public ApplicationUser? DecidedBy { get; set; }
		[MaxLength(450)]
		public string? DecidedById { get; set; }
		public DateTime? DecidedAt { get; set; }
		[MaxLength(500)]
		public string? DecisionNotes { get; set; }

		// فاتورة الشراء المنشأة من الطلب عند نقص الرصيد
		[ValidateNever]
		public Purchase? Purchase { get; set; }
		public Guid? PurchaseId { get; set; }

		// رقم إذن الصرف (ISS-) بعد صرف المواد
		[MaxLength(30)]
		public string? IssueRef { get; set; }

		public List<MaterialRequestItem> Items { get; set; } = new();
	}

	public class MaterialRequestItem : BaseEntity
	{
		[ValidateNever]
		public MaterialRequest MaterialRequest { get; set; }
		public Guid MaterialRequestId { get; set; }

		[ValidateNever]
		public Item Item { get; set; }
		public Guid ItemId { get; set; }

		[Range(0.01, 100000000, ErrorMessage = "الكمية يجب أن تكون أكبر من صفر")]
		[Column(TypeName = "decimal(18,2)")]
		public decimal Quantity { get; set; } = 1;

		[MaxLength(250)]
		public string? Notes { get; set; }
	}
}
