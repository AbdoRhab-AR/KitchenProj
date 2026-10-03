using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace KitchenProject.Models.Entities
{
	public class ApplicationUser : IdentityUser
	{
		[Range(18, 100)]
		public int Age { get; set; }
		public DateTime? LastAccessTime { get; set; }
		public DateTime CreatedDate { get; set; }
		public DateTime? ModifiedDate { get; set; }

		public bool? Approval { get; set; }


		[ValidateNever]
		public UserProfile UserProfile { get; set; }

		public ICollection<Project> SalesProjects { get; set; } = new List<Project>();
		public ICollection<Project> EngineerProjects { get; set; } = new List<Project>();
		public ICollection<Project> DesignerProjects { get; set; } = new List<Project>();
	}
}
