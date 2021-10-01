using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Integrations
{
    public class PushyAPI
    {
        public static async Task SendPush(PushyPushRequest push)
        {
            try
            {
                string SECRET_API_KEY = string.Empty;

                if (((Dictionary<string, string>)push.data)["messageKey"].ToLower().Contains("cap_"))
                    SECRET_API_KEY = ConfigurationManager.AppSettings["PushyCaptainAPISecret"].ToString();
                else if (((Dictionary<string, string>)push.data)["messageKey"].ToLower().Contains("pas_"))
                    SECRET_API_KEY = ConfigurationManager.AppSettings["PushyPassengerAPISecret"].ToString();
                else //if(((Dictionary<string, string>)push.data)["messageKey"].ToLower().Contains("cap_"))
                    SECRET_API_KEY = ConfigurationManager.AppSettings["PushyGo4ModuleAPISecret"].ToString();

                // Create an HTTP request to the Pushy API
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://api.pushy.me/push?api_key=" + SECRET_API_KEY);

                // Send a JSON content-type header
                request.ContentType = "application/json";

                // Set request method to POST
                request.Method = "POST";

                // Convert request post body to JSON (using JSON.NET package from Nuget)
                string postData = JsonConvert.SerializeObject(push);

                // Convert post data to a byte array
                byte[] byteArray = Encoding.UTF8.GetBytes(postData);

                // Set the ContentLength property of the WebRequest
                request.ContentLength = byteArray.Length;

                // Get the request stream
                Stream dataStream = request.GetRequestStream();

                // Write the data to the request stream
                dataStream.Write(byteArray, 0, byteArray.Length);

                // Close the stream
                dataStream.Close();

                // Proceed with caution
                WebResponse response;

                // Execute the request
                response = await request.GetResponseAsync();

                /*IN LAST VERSION FOLLOWING BLOCK OF CODE WAS COMMENTED AND PLACED AFTER CATCH BLOCK*/

                // Open the stream using a StreamReader for easy access
                StreamReader reader = new StreamReader(response.GetResponseStream());

                // Read the response JSON for debugging
                string responseData = reader.ReadToEnd();

                // Clean up the streams
                reader.Close();
                response.Close();
                dataStream.Close();

                Log.Information("Successfully sent notification. {0} ", responseData);
            }
            catch (WebException exc)
            {
                // Get returned JSON error as string
                string errorJSON = new StreamReader(exc.Response.GetResponseStream()).ReadToEnd();

                // Parse into object
                PushyAPIError error = JsonConvert.DeserializeObject<PushyAPIError>(errorJSON);
                // Throw error
                Log.Error("Failed to send push notification. {0} ", error.error);
                //throw new Exception();
            }
        }
    }

    public class PushyPushRequest
    {
        public object to;
        public object data;
        public object notification;
        public object time_to_live;

        public PushyPushRequest(object data, object to, object notification)
        {
            this.to = to;
            this.data = data;
            this.notification = notification;
            this.time_to_live = 20;     //notification will expire after 20 seconds
        }
    }

    class PushyAPIError
    {
        public string error;

        public PushyAPIError(string error)
        {
            this.error = error;
        }
    }
}
