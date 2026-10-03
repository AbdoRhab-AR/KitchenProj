using KitchenProject.Classes;
using KitchenProject.Models.Entities;
using KitchenProject.Models.Interfaces;
using MailKit.Net.Smtp;
using MimeKit;

namespace KitchenProject.Models.Repositories
{
	public class EmailSender : IEmailSender
	{
		private readonly MailSettings _mailSettings;
		private readonly IUnitOfWork<SiteInfo> _siteInfo;
		private readonly IUnitOfWork<Contact> _contact;

		public EmailSender(MailSettings mailSettings,
						   IUnitOfWork<SiteInfo> siteInfo,
						   IUnitOfWork<Contact> contact)
		{
			_mailSettings = mailSettings;
			_siteInfo = siteInfo;
			_contact = contact;
		}

		public async Task SendEmailAsync(Message message)
		{
			var emailMessage = CreateMimeMessage(message);
			await SendAsync(emailMessage);
		}

		private MimeMessage CreateMimeMessage(Message message)
		{
			var emailMessage = new MimeMessage();

			emailMessage.From.Add
				(new MailboxAddress(_mailSettings.DisplayName, _mailSettings.UserName));

			emailMessage.To.AddRange(message.To);
			emailMessage.Subject = message.Subject;

			//----------------------SiteInfo & Contacts------------------------------
			var siteInfo = _siteInfo.Entity.GetAll().FirstOrDefault();
			var contact = _contact.Entity.GetAll().FirstOrDefault();

			message.Content = message.Content.Replace("{SiteName}", siteInfo.Name);
			message.Content = message.Content.Replace("{Mail}", contact.Email);
			message.Content = message.Content.Replace("{Phone}", contact.Phone);
			//------------------------------------------------------------------------

			var bodyBuilder = new BodyBuilder { HtmlBody = message.Content };
			if (message.Attachments != null && message.Attachments.Count > 0)
			{
				byte[] fileBytes;
				foreach (var attachment in message.Attachments)
				{
					using (var ms = new MemoryStream())
					{
						attachment.CopyTo(ms);
						fileBytes = ms.ToArray();
					}
					bodyBuilder.Attachments.Add(attachment.FileName, fileBytes, ContentType.Parse(attachment.ContentType));
				}
			}
			emailMessage.Body = bodyBuilder.ToMessageBody();
			//---------------------------------------------------------------------
			return emailMessage;
		}

		private async Task SendAsync(MimeMessage mailMessage)
		{
			using (var client = new SmtpClient())
			{
				try
				{
					await client.ConnectAsync(_mailSettings.Host, _mailSettings.Port, false);
					await client.AuthenticateAsync(_mailSettings.UserName, _mailSettings.Password);
					await client.SendAsync(mailMessage);
				}
				catch
				{
					throw;
				}
				finally
				{
					await client.DisconnectAsync(true);
					client.Dispose();
				}
			}

		}
	}
}
