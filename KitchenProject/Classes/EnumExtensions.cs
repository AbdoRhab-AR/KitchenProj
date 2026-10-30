using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace KitchenProject.Classes
{
	public static class EnumExtensions
	{
		// يرجع الاسم العربي من [Display(Name = ...)] أو اسم القيمة إن لم يوجد
		public static string GetDisplayName(this Enum value)
		{
			var member = value.GetType().GetMember(value.ToString()).FirstOrDefault();
			return member?.GetCustomAttribute<DisplayAttribute>()?.GetName() ?? value.ToString();
		}
	}
}
