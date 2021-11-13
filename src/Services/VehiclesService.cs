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
            string ETA = string.Empty;
            var pickUpPosition = new GeoCoordinate(double.Parse(pickUpLatitude), double.Parse(pickUpLongitude));
            
            foreach (var driver in onlineDrivers)
            {
                if (!bool.Parse(driver.Value.isBusy) && driver.Value.categoryID.Contains(categoryId))
                {
                    var driverPosition = new GeoCoordinate(driver.Value.location.l[0], driver.Value.location.l[1]);
                    var distance = driverPosition.GetDistanceTo(pickUpPosition);

                    if (string.IsNullOrEmpty(ETA))
                    {
                        ETA = distance.ToString();
                    }
                    else
                    {
                        if (distance < double.Parse(ETA))
                            ETA = distance.ToString();
                    }
                }
            }
            return ETA;
        }
    }
}