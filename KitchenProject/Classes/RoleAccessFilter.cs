using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace KitchenProject.Classes
{
	// يمنع المستخدم من فتح شاشة غير مسموحة لدوره (تسجيل الدخول نفسه تفرضه FallbackPolicy)
	public class RoleAccessFilter : IAuthorizationFilter
	{
		public void OnAuthorization(AuthorizationFilterContext context)
		{
			if (context.ActionDescriptor.EndpointMetadata.OfType<IAllowAnonymous>().Any())
				return;

			var user = context.HttpContext.User;
			if (user.Identity?.IsAuthenticated != true)
				return; // FallbackPolicy يحوله لصفحة الدخول

			if (context.ActionDescriptor is ControllerActionDescriptor action
				&& !AppRoles.CanAccess(user, action.ControllerName, action.ActionName))
			{
				context.Result = new ForbidResult();
			}
		}
	}
}
