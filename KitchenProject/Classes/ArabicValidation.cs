using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using System.ComponentModel.DataAnnotations;

namespace KitchenProject.Classes
{
	// رسائل التحقق الافتراضية في MVC إنجليزية ("The X field is required.")
	// هنا نجعلها عربية لكل النماذج، سواء في السيرفر أو في التحقق الفوري بالمتصفح
	public static class ArabicValidation
	{
		public static void UseArabicValidationMessages(this MvcOptions options)
		{
			var m = options.ModelBindingMessageProvider;
			m.SetValueIsInvalidAccessor(_ => "القيمة المدخلة غير صالحة");
			m.SetAttemptedValueIsInvalidAccessor((_, _) => "القيمة المدخلة غير صالحة");
			m.SetNonPropertyAttemptedValueIsInvalidAccessor(_ => "القيمة المدخلة غير صالحة");
			m.SetUnknownValueIsInvalidAccessor(_ => "القيمة المدخلة غير صالحة");
			m.SetNonPropertyUnknownValueIsInvalidAccessor(() => "القيمة المدخلة غير صالحة");
			m.SetValueMustNotBeNullAccessor(_ => "هذا الحقل مطلوب");
			m.SetMissingBindRequiredValueAccessor(_ => "هذا الحقل مطلوب");
			m.SetMissingKeyOrValueAccessor(() => "هذا الحقل مطلوب");
			m.SetMissingRequestBodyRequiredValueAccessor(() => "هذا الحقل مطلوب");
			m.SetValueMustBeANumberAccessor(_ => "يجب إدخال رقم");
			m.SetNonPropertyValueMustBeANumberAccessor(() => "يجب إدخال رقم");

			// يُضاف بعد مزود DataAnnotations، فيلتقط أيضاً Required الضمني للحقول غير القابلة لـ null
			options.ModelMetadataDetailsProviders.Add(new ArabicMessagesProvider());
		}

		private class ArabicMessagesProvider : IValidationMetadataProvider
		{
			public void CreateValidationMetadata(ValidationMetadataProviderContext context)
			{
				foreach (var attribute in context.ValidationMetadata.ValidatorMetadata.OfType<ValidationAttribute>())
				{
					// الرسائل المكتوبة يدوياً على الخاصية تبقى كما هي
					if (!string.IsNullOrEmpty(attribute.ErrorMessage) || attribute.ErrorMessageResourceType != null)
					{
						continue;
					}

					attribute.ErrorMessage = attribute switch
					{
						RequiredAttribute => "هذا الحقل مطلوب",
						RangeAttribute r => $"القيمة يجب أن تكون بين {r.Minimum} و {r.Maximum}",
						MaxLengthAttribute l => $"الحد الأقصى {l.Length} حرف",
						StringLengthAttribute s => $"الحد الأقصى {s.MaximumLength} حرف",
						MinLengthAttribute l => $"الحد الأدنى {l.Length} حرف",
						EmailAddressAttribute => "البريد الإلكتروني غير صالح",
						PhoneAttribute => "رقم الهاتف غير صالح",
						CompareAttribute => "القيمتان غير متطابقتين",
						RegularExpressionAttribute => "الصيغة غير صحيحة",
						_ => "القيمة المدخلة غير صالحة"
					};
				}
			}
		}
	}
}
