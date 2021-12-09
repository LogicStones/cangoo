using Constants;
using DatabaseModel;
using DTOs.API;
using Integrations;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Common;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Device.Location;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Services
{
    public class VehiclesService
    {
        public static string GetETAByCategoryId(Dictionary<string, FirebaseDriver> onlineDrivers, string categoryId, string pickUpLatitude, string pickUpLongitude)
        {
            double minDistance = 0;
            var pickUpPosition = new GeoCoordinate(double.Parse(pickUpLatitude), double.Parse(pickUpLongitude));

            foreach (var driver in onlineDrivers)
            {
                if (!bool.Parse(driver.Value.isBusy) && driver.Value.categoryID.Contains(categoryId))
                {
                    var driverPosition = new GeoCoordinate(driver.Value.location.l[0], driver.Value.location.l[1]);
                    var distance = driverPosition.GetDistanceTo(pickUpPosition);

                    if (minDistance == 0)
                    {
                        minDistance = distance;
                    }
                    else
                    {
                        if (distance < minDistance)
                            minDistance = distance;
                    }
                }
            }

            //every 400 meters = 1 min
            //time = distance / 400;
            //time = ceil(time);

            return Math.Ceiling(minDistance / 400).ToString() + "-" + (Math.Ceiling(minDistance / 400) + 2).ToString();
        }
    }
}