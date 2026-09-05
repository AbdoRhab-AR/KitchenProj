using KitchenProject.Models.Entities;
using KitchenProject.Models.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

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




			var controllerName = ((ControllerBase)context.Controller).ControllerContext.ActionDescriptor.ControllerName;
			var actionName = ((ControllerBase)context.Controller).ControllerContext.ActionDescriptor.ActionName;




			var user = _httpContextAccessor.HttpContext?.User;
			if (user != null)
			{
				//-----ApplicationUser المستخدم--
				{
					var currentUser = await _userManager.GetUserAsync(user);
					if (currentUser != null)
					{
						TimeSpan startTs = new TimeSpan(00, 00, 00);
						TimeSpan endTs = new TimeSpan(23, 59, 59);

						if (DateTime.Now.TimeOfDay < startTs | DateTime.Now.TimeOfDay > endTs)
						{
							await _signInManager.SignOutAsync();
						}
					}
				}
				//---------------------------------------------


				var siteState = _siteState.Entity.GetAll().FirstOrDefault();
				if (siteState != null)
				{
					if (!siteState.State
						 && actionName != "Login" && actionName != "Closing"
						 && ((_signInManager.IsSignedIn(user) && !user.IsInRole("Prog"))
							 || !_signInManager.IsSignedIn(user))
						)
					{
						context.Result = new RedirectToRouteResult(
							  new RouteValueDictionary {{ "Controller", "Home" },
												  { "Action", "Closing" } });

						await next();

						//await _signInManager.SignOutAsync();
					}
					else
					{
						context.Result = null;
						await next();
						//// execute any code after the action executes
					}
				}
				else
				{
					context.Result = null;
					await next();
					//// execute any code after the action executes
				}
			}

		}


	}

}
