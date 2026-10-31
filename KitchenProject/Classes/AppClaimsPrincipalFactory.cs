using KitchenProject.Models.Entities;
using KitchenProject.Models.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace KitchenProject.Classes
{
	// يضيف الاسم الظاهر للمستخدم في الكوكي حتى يظهر في أعلى الشاشة بدون استعلام في كل طلب
	public class AppClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>
	{
		public const string DisplayNameClaim = "DisplayName";

		private readonly IUnitOfWork<UserProfile> _profile;

		public AppClaimsPrincipalFactory(UserManager<ApplicationUser> userManager,
										 RoleManager<IdentityRole> roleManager,
										 IOptions<IdentityOptions> options,
										 IUnitOfWork<UserProfile> profile)
			: base(userManager, roleManager, options)
		{
			_profile = profile;
		}

		protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
		{
			var identity = await base.GenerateClaimsAsync(user);

			var displayName = await _profile.Entity.GetWhere(p => p.UserId == user.Id)
				.Select(p => p.DisplayName).FirstOrDefaultAsync();
			identity.AddClaim(new Claim(DisplayNameClaim, string.IsNullOrWhiteSpace(displayName) ? user.UserName ?? "" : displayName));

			return identity;
		}
	}

	public static class UserDisplayExtensions
	{
		public static string DisplayName(this ClaimsPrincipal user) =>
			user.FindFirst(AppClaimsPrincipalFactory.DisplayNameClaim)?.Value ?? user.Identity?.Name ?? "";

		// أول دور للمستخدم بالاسم العربي
		public static string RoleName(this ClaimsPrincipal user)
		{
			var role = user.FindAll(ClaimTypes.Role).Select(c => c.Value).FirstOrDefault();
			return AppRoles.GetName(role);
		}
	}
}
