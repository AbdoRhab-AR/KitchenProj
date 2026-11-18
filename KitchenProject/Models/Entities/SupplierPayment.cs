using KitchenProject.Models.Enums;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KitchenProject.Models.Entities
{
	public class SupplierPayment : BaseEntity
	{
		[ValidateNever]
		public Supplier supplier { get; set; }
		public Guid SupplierId { get; set; }

		[ValidateNever]
		public Purchase? Purchase { get; set; }
		public Guid? PurchaseId { get; set; }

		[MaxLength(30)]
		public string ReceiptNo { get; set; } = "";

		public DateTime PaymentDate { get; set; } = DateTime.Today;

		[Range(0.01, 100000000, ErrorMessage = "المبلغ يجب أن يكون أكبر من صفر")]
		[Column(TypeName = "decimal(18,2)")]
		public decimal Amount { get; set; }

		public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;

		// رقم الصك أو الحوالة
		[MaxLength(50)]
		public string? ReferenceNo { get; set; }

		public string? Notes { get; set; }

		// حساب الخزنة أو البنك المختار في شجرة الحسابات (اختياري)
		public Guid? CashAccountId { get; set; }

		// متابعة الصكوك المؤجلة
		public ChequeStatus? ChequeStatus { get; set; }
		public DateTime? ChequeDueDate { get; set; }

		// ضوابط الصلاحيات والطباعة
		public bool IsPrinted { get; set; }
		[MaxLength(100)]
		public string? CreatedBy { get; set; }
	}
}
