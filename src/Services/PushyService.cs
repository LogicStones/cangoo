using Constants;
using DTOs.API;
using Integrations;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services
{
    public class PushyService
    {
        public static async Task UniCast(string token, dynamic payload, string messageKey)
        {
            var notification = GetiOSNotificationObject(messageKey);
            string json = JsonConvert.SerializeObject(payload);

            if (messageKey.Equals(NotificationKeys.cap_rideCancel))
            {
                if ((payload as dynamic)["isLaterBooking"])
                    notification["loc_key"] = NotificationKeys.cap_laterRideCancel;
                else
                    notification["loc_key"] = NotificationKeys.cap_rideCancel;
            }

            var data = new Dictionary<string, string>
                {
                    { "messageKey", messageKey },
                    { "data",json }
                };

            // Prepare the push HTTP request
            PushyPushRequest push = new PushyPushRequest(data, token, notification);
            // Send the push notification
            await PushyAPI.SendPush(push);
        }

        public static async Task BroadCastNotification(Dictionary<string, string> lstTokenWithTone, DriverBookingRequestNotification response)
        {
            foreach (var item in lstTokenWithTone)
            {
                string messageKey = NotificationKeys.cap_rideRequest;
                var notification = GetiOSNotificationObject(messageKey, item.Value);

                string json = JsonConvert.SerializeObject(response);

                var data = new Dictionary<string, string>
                    {
                        { "messageKey", messageKey },
                        {"data",json }
                    };

                // Prepare the push HTTP request
                //PushyPushRequest push = new PushyPushRequest(data, lstToken.ToArray(), notification);
                PushyPushRequest push = new PushyPushRequest(data, item.Key.ToString(), notification);

                // Send the push notification
                await PushyAPI.SendPush(push);
            }
        }

        private static Dictionary<string, object> GetiOSNotificationObject(string fcmKey, string notificationTone = "notification_tone.mp3")
        {
            return new Dictionary<string, object>
                {
                    { "badge", 0 },
                    { "sound", notificationTone },
                    { "content_available", true }, // iOS app's notification handler will be invoked even if the app is running in the background
                    { "mutable_content", true },
                    { "body", "" },
                    { "loc_key", fcmKey }
                };
        }
    }
}