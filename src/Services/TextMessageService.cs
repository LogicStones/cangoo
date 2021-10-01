using Integrations;
using System;

using System.Threading.Tasks;


namespace Services
{
	public class TextMessageService
	{
		public static async Task<string> SendAuthenticationOTP(string receiverNumber)
		{
			string verificationCode = AuthenticationService.GenerateOTP();
			await Twilio.SendSms(string.Format("Deine cangoo-TAN lautet\n{0}", verificationCode), receiverNumber);
			return verificationCode;
		}

		public static async Task<string> SendWelcomeSMS(string receiverNumber)
		{
			string verificationCode = AuthenticationService.GenerateOTP();
			await Twilio.SendSms(string.Format("Herzlich willkommen bei cangoo!", verificationCode), receiverNumber);
			return verificationCode;
		}

		public static async Task<string> SendForgotPasswordSMS(string newPassword, string receiverNumber)
		{
			string verificationCode = AuthenticationService.GenerateOTP();
			await Twilio.SendSms(string.Format("Das Passwort für dein cangoo - Konto wurde nun zurückgesetzt.\nDein neues Passwort lautet {0}", newPassword), receiverNumber);
			return verificationCode;
		}

		public static async Task<string> SendAChangePhoneNumberOTP(string receiverNumber)
		{
			string verificationCode = AuthenticationService.GenerateOTP();
			await Twilio.SendSms(string.Format("Your OTP to change phone number is:\n{0}", verificationCode), receiverNumber);
			return verificationCode;
		}
	}
}