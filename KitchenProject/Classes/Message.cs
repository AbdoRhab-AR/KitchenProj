using MimeKit;

namespace KitchenProject.Classes
{
	public class Message
	{
		public Message(IEnumerable<string> to, string subject, string content, IFormFileCollection attachments)
		{
			To = new List<MailboxAddress>();
			To.AddRange(to.Select(x => new MailboxAddress("", x)));
			//To.AddRange(to.Select(x => new MailboxAddress(x,x)));
			Subject = subject;
			Content = content;
			Attachments = attachments;
		}

		public List<MailboxAddress> To { get; set; }
		public string Subject { get; set; }
		public string Content { get; set; }
		public IFormFileCollection Attachments { get; set; }  // files

		//public IFormCollection Attachment { get; set; } // one file
		//public List<IFormCollection> Attachments { get; set; }  // files
	}
}
