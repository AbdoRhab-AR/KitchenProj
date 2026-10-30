using KitchenProject.Models.Entities;
using KitchenProject.Models.Enums;
using KitchenProject.Models.Interfaces;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace KitchenProject.Classes
{
	// القوائم المنسدلة المشتركة بين الشاشات (المشاريع، المستخدمين)
	public class LookupService
	{
		private readonly IUnitOfWork<Project> _project;
		private readonly IUnitOfWork<ApplicationUser> _user;

		public LookupService(IUnitOfWork<Project> project, IUnitOfWork<ApplicationUser> user)
		{
			_project = project;
			_user = user;
		}

		// المشاريع غير المغلقة/الملغاة، مع إبقاء المشروع المختار ظاهراً حتى لو أُغلق
		public async Task<SelectList> ProjectsAsync(Guid? selected)
		{
			var projects = await _project.Entity.Include(p => p.Customer)
				.Where(p => (p.Status != ProjectStatus.Closed && p.Status != ProjectStatus.Cancelled) || p.Id == selected)
				.OrderByDescending(p => p.Created)
				.Select(p => new { p.Id, Text = p.ProjectNo + " - " + p.ProjectName + " (" + p.Customer.Name + ")" })
				.ToListAsync();

			return new SelectList(projects, "Id", "Text", selected);
		}

		public async Task<SelectList> UsersAsync(string? selected)
		{
			var users = await _user.Entity.Include(u => u.UserProfile)
				.OrderBy(u => u.UserName)
				.Select(u => new { u.Id, Text = u.UserProfile != null ? u.UserProfile.DisplayName : u.UserName })
				.ToListAsync();

			return new SelectList(users, "Id", "Text", selected);
		}

		public static SelectList EnumList<TEnum>(TEnum? selected) where TEnum : struct, Enum
		{
			return new SelectList(
				Enum.GetValues<TEnum>().Select(s => new { Value = s, Text = s.GetDisplayName() }),
				"Value", "Text", selected);
		}

		public static SelectList EnumList<TEnum>(TEnum selected) where TEnum : struct, Enum =>
			EnumList((TEnum?)selected);
	}
}
