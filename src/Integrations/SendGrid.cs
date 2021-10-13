using SendGrid;
using SendGrid.Helpers.Mail;
using Serilog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Integrations
{
	public class SendGrid
	{
		public static async Task SendEmailAsync(string toEmailAddress, string body, string subject, string fromEmail, string fromDisplayName)
		{
			try
			{
				var client = new SendGridClient(ConfigurationManager.AppSettings["SendGridSuperAdminAPIKey"]);
				var msg = MailHelper.CreateSingleEmail(
					new EmailAddress(fromEmail, fromDisplayName),
					new EmailAddress(toEmailAddress),
					subject,
					"",
					body
					);

				await client.SendEmailAsync(msg);
			}
			catch (Exception ex)
			{
				Log.Warning("Email sending failed to {0}. Message details {1}. Error details {2}", toEmailAddress, body, ex);
			}
		}
	}

	public class ReportAttachments
	{
		public string Name { get; set; }
		public string ReportMonth { get; set; }
		public string ReportNumber { get; set; }
		public string FleetName { get; set; }
		public byte[] Content { get; set; }
	}
}
