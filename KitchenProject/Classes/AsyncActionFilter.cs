using KitchenProject.Models.Entities;
using KitchenProject.Models.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace KitchenProject.Classes
{
	public class AsyncActionFilter : IAsyncActionFilter
	{
		private readonly IUnitOfWork<SiteState> _siteState;
		private readonly IHttpContextAccessor _httpContextAccessor;
		private readonly UserManager<ApplicationUser> _userManager;
		private readonly SignInManager<ApplicationUser> _signInManager;

		public AsyncActionFilter(IUnitOfWork<SiteState> siteState,
								  IHttpContextAccessor httpContextAccessor,
								   UserManager<ApplicationUser> userManager,
								 SignInManager<ApplicationUser> signInManager)
		{
			_siteState = siteState;
			_httpContextAccessor = httpContextAccessor;
			_userManager = userManager;
			_signInManager = signInManager;

		}


		public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
		{

			// execute any code before the action executes




			var actionName = ((ControllerBase)context.Controller).ControllerContext.ActionDescriptor.ActionName;

			var user = _httpContextAccessor.HttpContext?.User;

			//-----الموقع مغلق: تحويل الجميع لصفحة الإغلاق ما عدا المبرمج--
			var siteState = await _siteState.Entity.GetAll().FirstOrDefaultAsync();
			if (siteState != null && !siteState.State
				 && actionName != "Login" && actionName != "Closing"
				 && !(user != null && _signInManager.IsSignedIn(user) && user.IsInRole("Prog")))
			{
				// short-circuit: don't call next() so the action never runs
				context.Result = new RedirectToActionResult("Closing", "Home", null);
				return;
			}

			await next();
			//// execute any code after the action executes
		}


	}

}
