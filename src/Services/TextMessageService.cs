using Integrations;
using System;



namespace Services
{
    public class TextMessageService
    {
		public static bool SendSms(string msg, string receiverNumber)
		{
			return Twilio.SendSms(msg, receiverNumber);
		}
	}
}
