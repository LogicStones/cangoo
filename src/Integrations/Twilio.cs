using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using Serilog;

namespace Integrations
{
    public class Twilio
    {
        public static bool SendSms(string msg, string receiverNumber)
        {
            try
            {
                TwilioClient.Init(
                    ConfigurationManager.AppSettings["TwilioAccountSid"],
                    ConfigurationManager.AppSettings["TwilioAuthToken"]
                    );

                var message = MessageResource.Create(
                    from: new PhoneNumber(ConfigurationManager.AppSettings["TwilioFromNumber"]),
                    body: msg,
                    to: new PhoneNumber(receiverNumber)
                );
                //return message.Status;
                return true;
            }
            catch (Exception ex)
            {
                Log.Warning("Message sending failed to {0}. Message details {1}. Error details {2}", receiverNumber, msg, ex);
                return false;
                //return MessageResource.StatusEnum.Failed;
            }
        }
    }
}