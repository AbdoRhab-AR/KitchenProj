using System.ComponentModel.DataAnnotations;

namespace KitchenProject.ViewModels
{
	// شاشة إعدادات الشركة: تجمع SiteInfo و Contact في نموذج واحد
	public class SettingsViewModel
	{
		[Required(ErrorMessage = "اسم الشركة مطلوب")]
		[StringLength(50, MinimumLength = 3, ErrorMessage = "الاسم يجب أن يكون بين {2} و {1} حرفاً")]
		public string Name { get; set; } = "";

		[Required(ErrorMessage = "النشاط مطلوب")]
		[StringLength(200)]
		public string Activity { get; set; } = "";

		[Required(ErrorMessage = "النبذة مطلوبة")]
		[StringLength(2000)]
		public string About { get; set; } = "";

		[Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
		[EmailAddress(ErrorMessage = "بريد إلكتروني غير صالح")]
		public string Email { get; set; } = "";

		[Required(ErrorMessage = "رقم الهاتف مطلوب")]
		[RegularExpression(@"^\d{14}$", ErrorMessage = "رقم الهاتف يجب أن يكون 14 رقماً (مثال: 00218910997500)")]
		public string Phone { get; set; } = "";

		[StringLength(4000)]
		public string? DefaultQuoteTerms { get; set; }

		public string? LogoUrl { get; set; }
		public string? LetterheadUrl { get; set; }

		public IFormFile? Logo { get; set; }
		public IFormFile? Letterhead { get; set; }

		public bool RemoveLetterhead { get; set; }
	}
}
