using KitchenProject.Classes;

namespace KitchenProject.Models.Interfaces
{
	public interface IEmailSender
	{
		Task SendEmailAsync(Message message);

	}
}
