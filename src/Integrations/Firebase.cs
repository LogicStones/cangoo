using FireSharp.Config;
using FireSharp.Interfaces;
using FireSharp.Response;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integrations
{
    public class FirebaseIntegration
    {
        private static IFirebaseClient client;
        private static IFirebaseConfig config = new FirebaseConfig
        {
            AuthSecret = ConfigurationManager.AppSettings["FirebaseAuthSecret"].ToString(),
            BasePath = ConfigurationManager.AppSettings["FirebaseBasePath"].ToString()
        };

        public static async Task RideDataWriteOnFireBase(string statusRide, bool isUser, string path, dynamic data, string driverID, string userID, bool isWeb)
        {
            SetClient();

            SetResponse rs = client.Set(path + "/TripStatus", statusRide);
            if (isUser)
            {
                //if (Enumration.returnRideFirebaseStatus(RideFirebaseStatus.RequestSent).Equals(statusRide))
                //{
                //    PassengerRideRequest pr = data;
                //    client.Set(path + "/Info", new Dictionary<string, dynamic> {
                //        {"isLaterBooking", pr.isLaterBooking },
                //        {"requestTimeOut", 300 },
                //        {"bookingDateTime", Common.getUtcDateTime() },
                //        {"bookingMode", pr.bookingMode },
                //    });
                //}

                rs = await client.SetTaskAsync(path + "/" + userID, data);
            }
            else
            {
                rs = await client.SetTaskAsync(path + "/" + driverID, data);
            }
        }

        public static async Task Write(string path, dynamic data)
        {
            SetClient();
            await client.SetTaskAsync("", data);
        }

        public static async Task<FirebaseResponse> Read(string path)
        {
            SetClient();
            FirebaseResponse response = await client.GetTaskAsync(path);
            return response;
        }

        public static async Task Update(string path, dynamic data)
        {
            SetClient();
            await client.UpdateTaskAsync(path, data);
        }

        public static async Task Delete(string path)
        {
            SetClient();
            await client.DeleteTaskAsync(path);
        }

        private static void SetClient()
        {
            client = new FireSharp.FirebaseClient(config);
        }
    }
}
