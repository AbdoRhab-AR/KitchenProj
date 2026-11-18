using KitchenProject.Classes;
using System.ComponentModel.DataAnnotations;

namespace KitchenProject.Models.Entities
{
	public class Customer:BaseEntity
	{
		//كود لكل عميل
		[StringLength(20)]
		public required string CustomerNo { get; set; }

		[Required(ErrorMessage = "اسم العميل مطلوب")]
		[StringLength(60, ErrorMessage = "اسم العميل لا يتجاوز 60 حرفاً")]
		[SafeName(strictlyLettersOnly: true)]
		public required string Name { get; set; }

		[RegularExpression(@"^\d{10}$", ErrorMessage = "رقم الهاتف يجب أن يكون 10 رقماً")]
		public required string Phone { get; set; }

		[MaxLength(14)]
		public string? Phone2 { get; set; }

		[MaxLength(30), EmailAddress(ErrorMessage = "البريد الإلكتروني غير صالح")]
		public string? Email { get; set; }

		[Required(ErrorMessage = "العنوان مطلوب")]
		[MaxLength(250)]
		public required string Address { get; set; }

		[Required(ErrorMessage = "المدينة مطلوبة")]
		[MaxLength(100)]
		public required string City { get; set; }

		[Required(ErrorMessage = "الملاحظات مطلوبة")]
		public required string Notes { get; set; }

		public ICollection<Project> Projects { get; set; } = new List<Project>();
	}
}
