using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace KitchenProject.Models.Entities
{
	public class Contact : BaseEntity 
	{
		//[RegularExpression(@"^\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*$", ErrorMessage = "Invalid Email Address")]
		//[RegularExpression(@"^[a-zA-Z0-9._%+-]+@gmail\.com$", ErrorMessage = "البريد الإلكتروني يجب أن يكون من Gmail فقط")]
		//[EmailAddress]
		//[DataType(DataType.EmailAddress)]

		[Remote(action: "EmailIsValid", controller: "Admin", ErrorMessage = "Invalid Email Address")]
		public required string Email { get; set; }

		//[RegularExpression(@"^(092|094|091)\d{7}$", ErrorMessage = "The phone number must start with 092, 094, or 091 and consist of 10 digits.")]
		[RegularExpression(@"^\d{10}$", ErrorMessage = "يجب أن يتكون رقم الهاتف من 10 أرقام.")]
		public required string Phone { get; set; }
		public required string Facebook { get; set; }
		public required string X { get; set; }
		public required string Instagram { get; set; }
	}
}
