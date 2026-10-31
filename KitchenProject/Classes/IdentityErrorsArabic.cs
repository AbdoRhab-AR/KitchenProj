using Microsoft.AspNetCore.Identity;

namespace KitchenProject.Classes
{
	// رسائل أخطاء Identity الشائعة بالعربي (الافتراضية إنجليزية)
	public static class IdentityErrorsArabic
	{
		public static string Translate(IdentityError error) => error.Code switch
		{
			"PasswordMismatch" => "كلمة المرور الحالية غير صحيحة",
			"PasswordTooShort" => "كلمة المرور قصيرة جداً (6 أحرف على الأقل)",
			"PasswordRequiresDigit" => "كلمة المرور يجب أن تحتوي على رقم واحد على الأقل",
			"PasswordRequiresUniqueChars" => "كلمة المرور تحتاج حروفاً مختلفة أكثر",
			"DuplicateUserName" => "اسم الدخول مستخدم لموظف آخر",
			"DuplicateEmail" => "البريد الإلكتروني مستخدم لموظف آخر",
			"InvalidUserName" => "اسم الدخول غير صالح (حروف إنجليزية وأرقام فقط)",
			"InvalidEmail" => "البريد الإلكتروني غير صالح",
			"ConcurrencyFailure" => "تم تعديل هذا المستخدم من شخص آخر، أعد المحاولة",
			_ => error.Description
		};
	}
}
