using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace KitchenProject.Classes
{
	// يخفي تلقائياً في كل الشاشات أي رابط/نموذج/زر يوصل لإجراء غير مسموح لدور المستخدم (Classes/AppRoles)
	// - الأزرار والنماذج: تُحذف من الصفحة
	// - الروابط النصية (مثل اسم مشروع في جدول): يبقى النص بدون رابط
	// ولعنصر لا يحمل asp-action (مثل زر يفتح نافذة حذف): asp-permission="Delete" أو "Controller/Action"
	[HtmlTargetElement("a", Attributes = "asp-action")]
	[HtmlTargetElement("form", Attributes = "asp-action")]
	[HtmlTargetElement("button", Attributes = "asp-action")]
	[HtmlTargetElement(Attributes = "asp-permission")]
	public class PermissionTagHelper : TagHelper
	{
		// يعمل بعد AnchorTagHelper حتى نستطيع حذف href الذي يولده
		public override int Order => 1000;

		[HtmlAttributeName("asp-action")]
		public string? Action { get; set; }

		[HtmlAttributeName("asp-controller")]
		public string? Controller { get; set; }

		[HtmlAttributeName("asp-permission")]
		public string? Permission { get; set; }

		[ViewContext]
		[HtmlAttributeNotBound]
		public ViewContext ViewContext { get; set; } = default!;

		public override void Process(TagHelperContext context, TagHelperOutput output)
		{
			var user = ViewContext.HttpContext.User;
			if (user.Identity?.IsAuthenticated != true)
				return; // صفحة الدخول وما شابهها

			var controller = Controller ?? ViewContext.RouteData.Values["controller"]?.ToString();
			var action = Action ?? "Index";
			if (!string.IsNullOrEmpty(Permission))
			{
				var parts = Permission.Split('/');
				(controller, action) = parts.Length == 2 ? (parts[0], parts[1]) : (controller, parts[0]);
			}

			if (string.IsNullOrEmpty(controller) || AppRoles.CanAccess(user, controller, action))
				return;

			var css = output.Attributes["class"]?.Value?.ToString() ?? "";
			var isPlainLink = output.TagName == "a" && !css.Contains("btn") && !css.Contains("dropdown-item") && !css.Contains("page-link");
			if (isPlainLink)
			{
				output.TagName = "span";
				output.Attributes.RemoveAll("href");
				output.Attributes.RemoveAll("target");
				return;
			}

			output.SuppressOutput();
		}
	}
}
