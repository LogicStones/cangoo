using DatabaseModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services
{
    public class DriverService
    {
        public static void OnlineOfflineDriver(string driverID, string vehicleID, bool isOnline, Guid ApplicationID)
        {
            using (CangooEntities context = new CangooEntities())
            {
                if (isOnline)
                {
                    LogCaptainOnlineVehicle lc = new LogCaptainOnlineVehicle
                    {
                        CaptainID = driverID,
                        VehicleID = vehicleID,
                        OnlineDateTime = DateTime.UtcNow,
                        ApplicationID = ApplicationID
                    };
                    context.LogCaptainOnlineVehicles.Add(lc);

                    var lv = context.Vehicles.Where(v => v.VehicleID.ToString().Equals(vehicleID)).FirstOrDefault();
                    lv.OccupiedBy = lv.OccupiedBy == null ? 1 : lv.OccupiedBy + 1;
                    lv.isOccupied = true;
                    context.SaveChanges();
                }
                else
                {
                    var lc = context.LogCaptainOnlineVehicles.Where(l => l.CaptainID == driverID && l.OfflineDateTime == null).OrderByDescending(l => l.OnlineDateTime).FirstOrDefault();

                    if (lc != null)
                    {
                        lc.OfflineDateTime = DateTime.UtcNow;

                        var vh = context.Vehicles.Where(v => v.VehicleID.ToString().ToLower().Equals(lc.VehicleID.ToLower())).FirstOrDefault();

                        if (vh != null)
                        {
                            vh.LastDrove = DateTime.UtcNow;
                            if (vh.OccupiedBy > 0)
                            {
                                vh.OccupiedBy -= 1;
                                vh.isOccupied = vh.OccupiedBy > 0;
                            }
                        }
                        context.SaveChanges();
                    }
                }
            }
        }

    }
}
