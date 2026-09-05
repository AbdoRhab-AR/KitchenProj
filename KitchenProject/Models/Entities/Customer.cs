using System.ComponentModel.DataAnnotations;

namespace KitchenProject.Models.Entities
{
	public class Customer:BaseEntity
	{
		//كود لكل عميل
		[StringLength(20)]
		public required string CustomerNo { get; set; }

		[StringLength(30)]
		public required string Name { get; set; }

		[RegularExpression(@"^\d{14}$", ErrorMessage = "The phone number must consist of 14 digits.")]
		public required string Phone { get; set; }

		[MaxLength(14)]
		public string? Phone2 { get; set; }

		[MaxLength(30), EmailAddress]
		public string? Email { get; set; }

		[MaxLength(250)]
		public string? Address { get; set; }

		[MaxLength(100)]
		public string? City { get; set; }

		public string? Notes { get; set; }		

		public ICollection<Project> Projects { get; set; } = new List<Project>();
	}
}
