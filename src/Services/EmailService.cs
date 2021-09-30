using Integrations;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;

namespace Services
{
	public class EmailService
	{
		public static async Task<string> SendEmailOTPAsync(string toEmailAddress)
		{
			var verificationCode = AuthenticationService.GenerateOTP();
			await SendGrid.SendEmailAsync(toEmailAddress, string.Format(@"Your update email verification code is {0}",verificationCode),
				ConfigurationManager.AppSettings["ChangeEmailOTPEmailSubject"].ToString(), 
				ConfigurationManager.AppSettings["ChangeEmailOTPFromEmailAddress"].ToString(),
				ConfigurationManager.AppSettings["ChangeEmailOTPFromDisplayName"].ToString());
			return verificationCode;
		}

		//public static bool SendInvoice(string toEmailAddress, string body, string subject, string fromEmail, string fromDisplayName, string AttachmentName, byte[] invoiceContent)
		//{
		//	try
		//	{
		//		var apiKey = ConfigurationManager.AppSettings["SendGridSuperAdminAPIKey"];
		//		var client = new SendGridClient(apiKey);
		//		var from = new EmailAddress(fromEmail, fromDisplayName);
		//		var to = new EmailAddress(toEmailAddress);
		//		var plainTextContent = "";
		//		var htmlContent = body;
		//		var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);

		//		var file = Convert.ToBase64String(invoiceContent);
		//		msg.AddAttachment(AttachmentName, file);
		//		var task = Task.Run(async () => { await client.SendEmailAsync(msg); });
		//		task.Wait();
		//		return true;
		//	}
		//	catch (Exception ex)
		//	{
		//		Logger.WriteLog(ex);
		//		return false;
		//	}
		//}

		//public static bool SendReports(string toEmailAddress, string body, string subject, string fromEmail, string fromDisplayName, List<ReportAttachments> reports)
		//{
		//	try
		//	{
		//		var apiKey = ConfigurationManager.AppSettings["SendGridSuperAdminAPIKey"];
		//		var client = new SendGridClient(apiKey);
		//		var from = new EmailAddress(fromEmail, fromDisplayName);
		//		var to = new EmailAddress(toEmailAddress);
		//		var plainTextContent = "";
		//		var htmlContent = body;
		//		var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);

		//		foreach (var report in reports)
		//		{
		//			var file = Convert.ToBase64String(report.Content);
		//			msg.AddAttachment(report.Name, file, "application/pdf", "attachment", null);

		//			var path = "~/Reports/" + report.ReportMonth + "/";
		//			var directoryPath = HttpContext.Current.Server.MapPath(path);
		//			Directory.CreateDirectory(directoryPath);
		//			System.IO.File.WriteAllBytes(directoryPath + report.FleetName + " - " + "(" + report.ReportNumber + ") - " + report.Name, report.Content);
		//		}

		//		var task = Task.Run(async () => { await client.SendEmailAsync(msg); });
		//		task.Wait();
		//		return true;
		//	}
		//	catch (Exception ex)
		//	{
		//		Log.Warning("Message sending failed to {0}. Message details {1}. Error details {2}", receiverNumber, msg, ex);
		//		return false;
		//	}
		//}
	}
}
