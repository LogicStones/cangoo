using Newtonsoft.Json;
using RestSharp;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integrations
{
    public class DistanceMatrixAPI
    {
        public static ResponseData GetTimeAndDistance(string originLatLon, string destLatLon)
        {
            var client = new RestClient("https://maps.googleapis.com/maps/api/distancematrix/json?units=imperial&mode=Driving&origins=" + originLatLon + "&destinations=" + destLatLon + "&key=" + ConfigurationManager.AppSettings["DirectionMatrixAPIKey"].ToString())
            {
                Timeout = -1
            };

                var request = new RestRequest(Method.GET);
                IRestResponse response = client.Execute(request);

                var result = JsonConvert.DeserializeObject<Root>(response.Content);

            return new ResponseData
            {
                distanceString = result.rows[0].elements[0].distance.text,
                distanceInMeters = result.rows[0].elements[0].distance.value,
                durationString = result.rows[0].elements[0].duration.text,
                durationInSeconds = result.rows[0].elements[0].duration.value
            };

        }

        public class ResponseData
        {
            public string distanceString { get; set; }
            public int distanceInMeters { get; set; }
            public string durationString { get; set; }
            public int durationInSeconds { get; set; }
        }
        public class Distance
        {
            public string text { get; set; }
            public int value { get; set; }
        }

        public class Duration
        {
            public string text { get; set; }
            public int value { get; set; }
        }

        public class Element
        {
            public Distance distance { get; set; }
            public Duration duration { get; set; }
            public string status { get; set; }
        }

        public class Row
        {
            public List<Element> elements { get; set; }
        }

        public class Root
        {
            public List<string> destination_addresses { get; set; }
            public List<string> origin_addresses { get; set; }
            public List<Row> rows { get; set; }
            public string status { get; set; }
        }
    }
}
