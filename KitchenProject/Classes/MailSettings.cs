using System.ComponentModel.DataAnnotations;

namespace KitchenProject.Classes
{
	public class MailSettings
	{
		public string DisplayName { get; set; }
		public string UserName { get; set; }

		[DataType(DataType.Password)]
		public string Password { get; set; }
		public string Host { get; set; }
		public int Port { get; set; }
	}
}
