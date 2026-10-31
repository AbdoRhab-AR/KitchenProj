using KitchenProject.Classes;
using KitchenProject.Models.Entities;
using KitchenProject.Models.Interfaces;
using KitchenProject.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace KitchenProject.Controllers
{
	[ViewLayout("_DashboardLayout")]

	// إدارة الموظفين وصلاحياتهم - للمدير والمبرمج فقط (انظر AppRoles)
	public class UsersController : Controller
	{
		private readonly UserManager<ApplicationUser> _userManager;
		private readonly IUnitOfWork<UserProfile> _profile;

		public UsersController(UserManager<ApplicationUser> userManager, IUnitOfWork<UserProfile> profile)
		{
			_userManager = userManager;
			_profile = profile;
		}

		// GET: Users
		public async Task<IActionResult> Index(string? searchString)
		{
			ViewBag.ActivePage = "Users";
			ViewBag.SearchString = searchString;

			var users = await _userManager.Users.Include(u => u.UserProfile).OrderBy(u => u.UserName).ToListAsync();
			var rows = new List<UserRow>();

			foreach (var user in users)
			{
				var roles = await _userManager.GetRolesAsync(user);

				// حساب المبرمج لا يظهر إلا للمبرمج نفسه
				if (roles.Contains(AppRoles.Prog) && !User.IsInRole(AppRoles.Prog))
					continue;

				rows.Add(new UserRow
				{
					Id = user.Id,
					UserName = user.UserName ?? "",
					DisplayName = user.UserProfile?.DisplayName ?? user.UserName ?? "",
					PhoneNumber = user.PhoneNumber,
					Role = roles.FirstOrDefault(),
					IsActive = !IsDisabled(user),
					LastAccessTime = user.LastAccessTime
				});
			}

			if (!string.IsNullOrWhiteSpace(searchString))
			{
				var s = searchString.Trim();
				rows = rows.Where(r => r.UserName.Contains(s, StringComparison.OrdinalIgnoreCase)
									|| r.DisplayName.Contains(s)
									|| (r.PhoneNumber ?? "").Contains(s)).ToList();
			}

			return View(rows);
		}

		// GET: Users/Create
		public IActionResult Create()
		{
			ViewBag.ActivePage = "Users";
			FillRoles(null);
			return View(new UserFormViewModel { Role = AppRoles.Sales });
		}

		// POST: Users/Create
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(UserFormViewModel model)
		{
			ViewBag.ActivePage = "Users";
			ValidateRole(model.Role);

			if (string.IsNullOrEmpty(model.Password))
				ModelState.AddModelError(nameof(model.Password), "كلمة المرور مطلوبة للمستخدم الجديد");

			if (ModelState.IsValid)
			{
				var user = new ApplicationUser
				{
					UserName = model.UserName.Trim(),
					Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim(),
					PhoneNumber = model.PhoneNumber,
					EmailConfirmed = true, // المدير هو من ينشئ الحساب، فلا حاجة لتأكيد البريد
					LockoutEnabled = true,
					CreatedDate = DateTime.UtcNow
				};

				var result = await _userManager.CreateAsync(user, model.Password!);
				if (result.Succeeded)
				{
					await _userManager.AddToRoleAsync(user, model.Role);
					if (!model.IsActive)
					{
						await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
					}

					_profile.Entity.Insert(new UserProfile
					{
						Id = Guid.NewGuid(),
						Created = DateTime.UtcNow,
						UserId = user.Id,
						DisplayName = model.DisplayName.Trim()
					});
					await _profile.SaveAsync();

					TempData["Success"] = $"تمت إضافة المستخدم {model.DisplayName} ({AppRoles.GetName(model.Role)})";
					return RedirectToAction(nameof(Index));
				}

				AddErrors(result);
			}

			FillRoles(model.Role);
			return View(model);
		}

		// GET: Users/Edit/5
		public async Task<IActionResult> Edit(string id)
		{
			ViewBag.ActivePage = "Users";

			var user = await LoadEditableAsync(id);
			if (user == null)
			{
				return NotFound();
			}

			var model = new UserFormViewModel
			{
				Id = user.Id,
				UserName = user.UserName ?? "",
				DisplayName = user.UserProfile?.DisplayName ?? user.UserName ?? "",
				Email = user.Email,
				PhoneNumber = user.PhoneNumber,
				Role = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? "",
				IsActive = !IsDisabled(user)
			};

			FillRoles(model.Role);
			return View(model);
		}

		// POST: Users/Edit/5
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(string id, UserFormViewModel model)
		{
			ViewBag.ActivePage = "Users";

			var user = await LoadEditableAsync(id);
			if (user == null)
			{
				return NotFound();
			}

			var currentRoles = await _userManager.GetRolesAsync(user);
			var isProg = currentRoles.Contains(AppRoles.Prog);
			if (!isProg)
			{
				ValidateRole(model.Role);
			}

			var isSelf = user.Id == _userManager.GetUserId(User);
			if (isSelf && !model.IsActive)
				ModelState.AddModelError(nameof(model.IsActive), "لا يمكنك إيقاف حسابك");
			if (isSelf && !currentRoles.Contains(model.Role) && !isProg)
				ModelState.AddModelError(nameof(model.Role), "لا يمكنك تغيير دورك بنفسك");

			if (ModelState.IsValid)
			{
				user.UserName = model.UserName.Trim();
				user.Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim();
				user.PhoneNumber = model.PhoneNumber;
				user.ModifiedDate = DateTime.UtcNow;

				var result = await _userManager.UpdateAsync(user);
				if (result.Succeeded && !string.IsNullOrEmpty(model.Password))
				{
					// إعادة تعيين كلمة المرور من المدير
					var token = await _userManager.GeneratePasswordResetTokenAsync(user);
					result = await _userManager.ResetPasswordAsync(user, token, model.Password);
				}

				if (result.Succeeded)
				{
					if (!isProg && !currentRoles.Contains(model.Role))
					{
						await _userManager.RemoveFromRolesAsync(user, currentRoles);
						await _userManager.AddToRoleAsync(user, model.Role);
					}

					await SetActiveAsync(user, model.IsActive);

					// الملف الشخصي محمّل ومتتبع مع المستخدم، فنعدّله مباشرة
					if (user.UserProfile == null)
					{
						_profile.Entity.Insert(new UserProfile { Id = Guid.NewGuid(), Created = DateTime.UtcNow, UserId = user.Id, DisplayName = model.DisplayName.Trim() });
					}
					else
					{
						user.UserProfile.DisplayName = model.DisplayName.Trim();
						user.UserProfile.Modified = DateTime.UtcNow;
					}
					await _profile.SaveAsync();

					TempData["Success"] = $"تم تحديث بيانات {model.DisplayName}";
					return RedirectToAction(nameof(Index));
				}

				AddErrors(result);
			}

			model.Id = user.Id;
			FillRoles(model.Role);
			return View(model);
		}

		// POST: Users/ToggleActive/5  إيقاف / تفعيل سريع من القائمة
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> ToggleActive(string id)
		{
			var user = await LoadEditableAsync(id);
			if (user == null)
			{
				return NotFound();
			}

			if (user.Id == _userManager.GetUserId(User))
			{
				TempData["Error"] = "لا يمكنك إيقاف حسابك";
				return RedirectToAction(nameof(Index));
			}

			var activate = IsDisabled(user);
			await SetActiveAsync(user, activate);

			TempData["Success"] = activate ? $"تم تفعيل حساب {user.UserName}" : $"تم إيقاف حساب {user.UserName}";
			return RedirectToAction(nameof(Index));
		}

		//---------------------------------------------------------------------

		// الإيقاف = قفل الحساب بلا نهاية، ويُغيّر ختم الأمان حتى يخرج من أي جلسة مفتوحة
		private async Task SetActiveAsync(ApplicationUser user, bool active)
		{
			if (active == !IsDisabled(user))
				return;

			if (active)
			{
				await _userManager.SetLockoutEndDateAsync(user, null);
				await _userManager.ResetAccessFailedCountAsync(user);
			}
			else
			{
				await _userManager.SetLockoutEnabledAsync(user, true);
				await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
				await _userManager.UpdateSecurityStampAsync(user);
			}
		}

		private static bool IsDisabled(ApplicationUser user) => user.LockoutEnd == DateTimeOffset.MaxValue;

		// المدير لا يعدّل حساب المبرمج
		private async Task<ApplicationUser?> LoadEditableAsync(string? id)
		{
			if (string.IsNullOrEmpty(id))
				return null;

			var user = await _userManager.Users.Include(u => u.UserProfile).FirstOrDefaultAsync(u => u.Id == id);
			if (user == null)
				return null;

			if (!User.IsInRole(AppRoles.Prog) && await _userManager.IsInRoleAsync(user, AppRoles.Prog))
				return null;

			return user;
		}

		private void ValidateRole(string role)
		{
			if (!AppRoles.Assignable.Contains(role))
				ModelState.AddModelError(nameof(UserFormViewModel.Role), "يجب اختيار الدور");
		}

		private void AddErrors(IdentityResult result)
		{
			foreach (var error in result.Errors)
			{
				ModelState.AddModelError("", IdentityErrorsArabic.Translate(error));
			}
		}

		private void FillRoles(string? selected)
		{
			ViewBag.Roles = new SelectList(AppRoles.Assignable.Select(r => new { Value = r, Text = AppRoles.GetName(r) }), "Value", "Text", selected);
		}
	}
}
