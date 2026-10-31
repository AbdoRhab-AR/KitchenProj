using System.ComponentModel.DataAnnotations;

namespace KitchenProject.ViewModels
{
	public class LoginViewModel
	{
		[Required(ErrorMessage = "اسم المستخدم مطلوب")]
		public string UserName { get; set; } = "";

		[Required(ErrorMessage = "كلمة المرور مطلوبة")]
		[DataType(DataType.Password)]
		public string Password { get; set; } = "";

		public bool RememberMe { get; set; }
	}

	public class ChangePasswordViewModel
	{
		[Required(ErrorMessage = "كلمة المرور الحالية مطلوبة")]
		[DataType(DataType.Password)]
		public string CurrentPassword { get; set; } = "";

		[Required(ErrorMessage = "كلمة المرور الجديدة مطلوبة")]
		[StringLength(100, MinimumLength = 6, ErrorMessage = "كلمة المرور 6 أحرف على الأقل")]
		[DataType(DataType.Password)]
		public string NewPassword { get; set; } = "";

		[Compare(nameof(NewPassword), ErrorMessage = "تأكيد كلمة المرور غير مطابق")]
		[DataType(DataType.Password)]
		public string ConfirmPassword { get; set; } = "";
	}

	// إضافة / تعديل مستخدم من شاشة المستخدمين
	public class UserFormViewModel
	{
		public string? Id { get; set; }

		[Required(ErrorMessage = "اسم الدخول مطلوب")]
		[RegularExpression(@"^[a-zA-Z0-9._@-]{3,50}$", ErrorMessage = "اسم الدخول بالإنجليزي (حروف وأرقام و . _ - @) من 3 إلى 50 حرفاً")]
		public string UserName { get; set; } = "";

		[Required(ErrorMessage = "الاسم الظاهر مطلوب")]
		[StringLength(100)]
		public string DisplayName { get; set; } = "";

		[EmailAddress(ErrorMessage = "البريد الإلكتروني غير صالح")]
		public string? Email { get; set; }

		[RegularExpression(@"^\d{9,14}$", ErrorMessage = "رقم الهاتف أرقام فقط (9 إلى 14 رقماً)")]
		public string? PhoneNumber { get; set; }

		[Required(ErrorMessage = "يجب اختيار الدور")]
		public string Role { get; set; } = "";

		// مطلوبة عند الإضافة فقط
		[StringLength(100, MinimumLength = 6, ErrorMessage = "كلمة المرور 6 أحرف على الأقل")]
		[DataType(DataType.Password)]
		public string? Password { get; set; }

		public bool IsActive { get; set; } = true;
	}

	public class UserRow
	{
		public string Id { get; set; } = "";
		public string UserName { get; set; } = "";
		public string DisplayName { get; set; } = "";
		public string? PhoneNumber { get; set; }
		public string? Role { get; set; }
		public bool IsActive { get; set; }
		public DateTime? LastAccessTime { get; set; }
	}
}
