using KitchenProject.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace KitchenProject.ViewModels
{
	// نموذج إلغاء المشروع مع أتعاب اختيارية (مصروفات للمهندس والمصمم و/أو رسوم إلغاء على العميل)
	public class CancelProjectViewModel
	{
		[Required]
		public Guid ProjectId { get; set; }

		[Range(0, 100000000, ErrorMessage = "أتعاب المهندس لا يمكن أن تكون سالبة")]
		public decimal? EngineerFee { get; set; }

		[Range(0, 100000000, ErrorMessage = "أتعاب المصمم لا يمكن أن تكون سالبة")]
		public decimal? DesignerFee { get; set; }

		public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;

		[Range(0, 100000000, ErrorMessage = "رسوم الإلغاء على العميل لا يمكن أن تكون سالبة")]
		public decimal? CustomerCancellationFee { get; set; }

		[MaxLength(500)]
		public string? CancellationReason { get; set; }
	}
}
