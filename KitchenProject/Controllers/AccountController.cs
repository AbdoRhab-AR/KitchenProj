using KitchenProject.Classes;
using KitchenProject.Models.Entities;
using KitchenProject.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace KitchenProject.Controllers
{
	// تسجيل الدخول والخروج وتغيير كلمة المرور
	public class AccountController : Controller
	{
		private readonly UserManager<ApplicationUser> _userManager;
		private readonly SignInManager<ApplicationUser> _signInManager;

		public AccountController(UserManager<ApplicationUser> userManager,
								 SignInManager<ApplicationUser> signInManager)
		{
			_userManager = userManager;
			_signInManager = signInManager;
		}

		// GET: Account/Login
		[AllowAnonymous]
		public IActionResult Login(string? returnUrl)
		{
			if (_signInManager.IsSignedIn(User))
			{
				return RedirectToLocal(returnUrl);
			}

			ViewBag.ReturnUrl = returnUrl;
			return View(new LoginViewModel());
		}

		// POST: Account/Login
		[AllowAnonymous]
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl)
		{
			ViewBag.ReturnUrl = returnUrl;

			if (!ModelState.IsValid)
			{
				return View(model);
			}

			// الدخول باسم المستخدم أو البريد الإلكتروني
			var input = model.UserName.Trim();
			var user = await _userManager.FindByNameAsync(input) ?? await _userManager.FindByEmailAsync(input);
			if (user == null)
			{
				ModelState.AddModelError("", "اسم المستخدم أو كلمة المرور غير صحيحة");
				return View(model);
			}

			var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: true);

			if (result.Succeeded)
			{
				user.LastAccessTime = DateTime.UtcNow;
				await _userManager.UpdateAsync(user);
				return RedirectToLocal(returnUrl);
			}

			if (result.IsLockedOut)
			{
				ModelState.AddModelError("", user.LockoutEnd == DateTimeOffset.MaxValue
					? "هذا الحساب موقوف، تواصل مع مدير النظام"
					: "تم قفل الحساب مؤقتاً بسبب محاولات دخول خاطئة متكررة، حاول بعد 15 دقيقة");
			}
			else if (result.IsNotAllowed)
			{
				ModelState.AddModelError("", "هذا الحساب غير مفعل، تواصل مع مدير النظام");
			}
			else
			{
				ModelState.AddModelError("", "اسم المستخدم أو كلمة المرور غير صحيحة");
			}

			return View(model);
		}

		// POST: Account/Logout
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Logout()
		{
			await _signInManager.SignOutAsync();
			return RedirectToAction(nameof(Login));
		}

		// GET: Account/AccessDenied
		[ViewLayout("_DashboardLayout")]
		public IActionResult AccessDenied()
		{
			return View();
		}

		// GET: Account/ChangePassword
		[ViewLayout("_DashboardLayout")]
		public IActionResult ChangePassword()
		{
			return View(new ChangePasswordViewModel());
		}

		// POST: Account/ChangePassword
		[ViewLayout("_DashboardLayout")]
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
		{
			if (!ModelState.IsValid)
			{
				return View(model);
			}

			var user = await _userManager.GetUserAsync(User);
			if (user == null)
			{
				return RedirectToAction(nameof(Login));
			}

			var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
			if (!result.Succeeded)
			{
				foreach (var error in result.Errors)
				{
					ModelState.AddModelError("", IdentityErrorsArabic.Translate(error));
				}
				return View(model);
			}

			// تحديث الكوكي حتى لا يُطلب الدخول من جديد
			await _signInManager.RefreshSignInAsync(user);
			TempData["Success"] = "تم تغيير كلمة المرور";
			return RedirectToAction(nameof(ChangePassword));
		}

		private IActionResult RedirectToLocal(string? returnUrl)
		{
			if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
			{
				return Redirect(returnUrl);
			}
			return RedirectToAction("Index", "Projects");
		}
	}
}
