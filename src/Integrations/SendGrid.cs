using SendGrid;
using DTOs.API;
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
			var msg = MailHelper.CreateSingleEmail(
				new EmailAddress(fromEmail, fromDisplayName),
				new EmailAddress(toEmailAddress),
				subject,
				"",
				body
				);

			await SendEmail(msg);

		}

		public static async Task SendInvoiceAsync(string toEmailAddress, string body, string subject, string fromEmail, string fromDisplayName, string attachmentName, byte[] file, string invoiceNumber)
		{
			//var apiKey = ConfigurationManager.AppSettings["SendGridSuperAdminAPIKey"];
			//var client = new SendGridClient(apiKey);
			//var from = new EmailAddress(fromEmail, fromDisplayName);
			//var to = new EmailAddress(toEmailAddress);
			//var plainTextContent = "";
			//var htmlContent = body;
			//var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);


			var msg = MailHelper.CreateSingleEmail(
				new EmailAddress(fromEmail, fromDisplayName),
				new EmailAddress(toEmailAddress),
				subject,
				"",
				body
				);

			msg.AddAttachment(attachmentName, Convert.ToBase64String(file));

			var path = "~/Invoices/" + DateTime.Today + "/";
			var directoryPath = HttpContext.Current.Server.MapPath(path);
			Directory.CreateDirectory(directoryPath);
			File.WriteAllBytes(directoryPath + invoiceNumber, file);

			await SendEmail(msg);
			//var client = new SendGridClient(ConfigurationManager.AppSettings["SendGridSuperAdminAPIKey"]);
			//await client.SendEmailAsync(msg);
		}

		public static async Task SendMonthlyReportsAsync(string toEmailAddress, string body, string subject, string fromEmail, string fromDisplayName, List<ReportAttachments> reports)
		{


			var msg = MailHelper.CreateSingleEmail(
				new EmailAddress(fromEmail, fromDisplayName),
				new EmailAddress(toEmailAddress),
				subject,
				"",
				body
				);

			//var apiKey = ConfigurationManager.AppSettings["SendGridSuperAdminAPIKey"];
			//var client = new SendGridClient(apiKey);
			//var from = new EmailAddress(fromEmail, fromDisplayName);
			//var to = new EmailAddress(toEmailAddress);
			//var plainTextContent = "";
			//var htmlContent = body;
			//var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);

			foreach (var report in reports)
			{
				var file = Convert.ToBase64String(report.Content);
				msg.AddAttachment(report.Name, file, "application/pdf", "attachment", null);

				//To avoid circular dependency need to write following code snippet.
				//Alternatively add new project, general purpose services in that project and add reference of new project in
				//existing required projects

				var path = "~/Reports/" + report.ReportMonth + "/";
				var directoryPath = HttpContext.Current.Server.MapPath(path);
				Directory.CreateDirectory(directoryPath);
				File.WriteAllBytes(directoryPath + report.FleetName + " - " + "(" + report.ReportNumber + ") - " + report.Name, report.Content);
			}

			await SendEmail(msg);
		}

		private static async Task SendEmail(SendGridMessage msg)
		{
			try
			{
				var client = new SendGridClient(ConfigurationManager.AppSettings["SendGridSuperAdminAPIKey"]);
				await client.SendEmailAsync(msg);
			}

			catch (Exception ex)
			{
				Log.Warning("Email sending failed. Email Subject {0}. Message details {1}. Error details {2}", msg.Subject, msg, ex);
			}
		}
	}
}
