using KitchenProject.Models.Entities;
using KitchenProject.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace KitchenProject.ViewModels
{
	// إذن صرف خامات لمشروع (أو مرتجع منه) بعدة أصناف
	public class StockIssueViewModel
	{
		[Required(ErrorMessage = "يجب اختيار المشروع")]
		public Guid? ProjectId { get; set; }

		[Required(ErrorMessage = "يجب اختيار المخزن")]
		public Guid WarehouseId { get; set; }

		// Issue = صرف للمشروع، Return = مرتجع من المشروع للمخزن
		public StockTransactionType Type { get; set; } = StockTransactionType.Issue;

		public DateTime Date { get; set; } = DateTime.Today;

		public string? Notes { get; set; }

		public List<StockIssueLine> Lines { get; set; } = new();
	}

	public class StockIssueLine
	{
		public Guid ItemId { get; set; }
		public decimal Quantity { get; set; } = 1;
	}

	// تسوية جرد: إدخال الكمية الفعلية بعد العد وتسجيل الفرق
	public class StockAdjustViewModel
	{
		[Required(ErrorMessage = "يجب اختيار الصنف")]
		public Guid ItemId { get; set; }

		[Required(ErrorMessage = "يجب اختيار المخزن")]
		public Guid WarehouseId { get; set; }

		[Range(0, 100000000, ErrorMessage = "الكمية الفعلية لا يمكن أن تكون سالبة")]
		public decimal CountedQuantity { get; set; }

		public DateTime Date { get; set; } = DateTime.Today;

		public string? Notes { get; set; }
	}

	// سطر في شاشة أرصدة المخزن
	public class StockRow
	{
		public required Item Item { get; set; }
		public decimal Balance { get; set; }
		public decimal AverageCost { get; set; }
		public decimal Value => Balance * AverageCost;
		public bool IsLow => Item.IsActive && Item.MinQuantity > 0 && Balance <= Item.MinQuantity;
	}
}
