using KitchenProject.Models.Enums;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KitchenProject.Models.Entities
{
	// حساب في شجرة الحسابات. الحساب الأب (IsPostable = false) للتجميع فقط، والقيود تُسجل على الحسابات الفرعية
	public class Account : BaseEntity
	{
		[Required(ErrorMessage = "رقم الحساب مطلوب")]
		[MaxLength(20)]
		public string Code { get; set; } = "";

		[Required(ErrorMessage = "اسم الحساب مطلوب")]
		[MaxLength(150)]
		public string Name { get; set; } = "";

		public AccountType Type { get; set; }

		[ValidateNever]
		public Account? Parent { get; set; }
		public Guid? ParentId { get; set; }

		[ValidateNever]
		public List<Account> Children { get; set; } = new();

		// يقبل قيوداً مباشرة (حساب نهائي) أم للتجميع فقط
		public bool IsPostable { get; set; } = true;

		public bool IsActive { get; set; } = true;

		// مفتاح حساب النظام: تستعمله القيود التلقائية لإيجاد الحساب حتى لو غُيّر اسمه أو رقمه
		[MaxLength(40)]
		public string? SystemKey { get; set; }

		[MaxLength(300)]
		public string? Notes { get; set; }

		[ValidateNever]
		public ICollection<JournalLine> Lines { get; set; } = new List<JournalLine>();

		[NotMapped]
		public bool IsDebitNature => Type == AccountType.Asset || Type == AccountType.Expense;
	}

	// قيد يومي: إما يدوي أو تلقائي مولّد من مستند (عقد، قبض، مشتريات...) ويُعاد توليده إذا تغير المستند
	public class JournalEntry : BaseEntity
	{
		[MaxLength(30)]
		public string EntryNo { get; set; } = "";

		public DateTime EntryDate { get; set; } = DateTime.Today;

		[MaxLength(500)]
		public string Description { get; set; } = "";

		public bool IsAuto { get; set; }

		// نوع المستند المصدر ومعرفه (للقيود التلقائية)
		[MaxLength(30)]
		public string? SourceType { get; set; }
		public Guid? SourceId { get; set; }

		[MaxLength(100)]
		public string? CreatedBy { get; set; }

		public List<JournalLine> Lines { get; set; } = new();

		[NotMapped]
		public decimal TotalDebit => Lines.Sum(l => l.Debit);
		[NotMapped]
		public decimal TotalCredit => Lines.Sum(l => l.Credit);
	}

	public class JournalLine : BaseEntity
	{
		public Guid JournalEntryId { get; set; }
		public JournalEntry JournalEntry { get; set; } = null!;

		public Guid AccountId { get; set; }
		public Account Account { get; set; } = null!;

		[Column(TypeName = "decimal(18,2)")]
		public decimal Debit { get; set; }

		[Column(TypeName = "decimal(18,2)")]
		public decimal Credit { get; set; }

		[MaxLength(300)]
		public string? Description { get; set; }

		// المشروع المرتبط بالسطر (بدون علاقة إلزامية) لتحليل الربحية بالمشروع
		public Guid? ProjectId { get; set; }
	}

	// إقفال فترة: لا تُعدَّل القيود حتى هذا التاريخ، ويُنشأ قيد إقفال يرحّل ناتج الإيرادات والمصروفات للأرباح المحتجزة
	public class PeriodClose : BaseEntity
	{
		public DateTime ClosedThrough { get; set; }
		public Guid? EntryId { get; set; }

		[Column(TypeName = "decimal(18,2)")]
		public decimal NetResult { get; set; }

		[MaxLength(300)]
		public string? Notes { get; set; }

		[MaxLength(100)]
		public string? ClosedBy { get; set; }
	}
}
