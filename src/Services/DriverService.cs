using Constants;
using DatabaseModel;
using DTOs.API;
using DTOs.Shared;
using Services.Automapper;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
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

        public static async Task<string> GetDriverDeviceToken(string driverId)
        {
            using (var dbContext = new CangooEntities())
            {
                return dbContext.Captains.Where(c => c.CaptainID.ToString().Equals(driverId)).FirstOrDefault().DeviceToken;
            }
        }

        public static async Task<spGetUpdateTripDataOnAcceptRide_Result> GetUpdatedTripDataOnAcceptRide(
            string tripId, string driverId, string vehicleId, string fleetId, int isLaterBooking)
        {
            using (var dbContext = new CangooEntities())
            {
                return dbContext.spGetUpdateTripDataOnAcceptRide(tripId, driverId, (int)TripStatuses.OnTheWay, vehicleId,  
                    isLaterBooking, fleetId, DateTime.UtcNow).FirstOrDefault();
            }
        }

        public static async Task<Captain> GetDriverById(string driverId)
        {
            using (var context = new CangooEntities())
            {
                return context.Captains.Where(c => c.CaptainID.ToString().Equals(driverId)).FirstOrDefault();
            }
        }

        public static async Task<Captain> GetDriverByInviteCode(string InviteCode)
        {
            using (CangooEntities dbcontext=new CangooEntities())
            {
                return dbcontext.Captains.Where(x => x.ShareCode.ToLower().Equals(InviteCode.ToLower())).FirstOrDefault();
            }
        }

        public static async Task<List<DatabaseOlineDriversDTO>> GetOnlineDriversByIds(string driverIds)
        {
            using (var context = new CangooEntities())
            {
                var onlineDrivers = context.spGetOnlineDriver(driverIds).ToList();
                return AutoMapperConfig._mapper.Map<List<spGetOnlineDriver_Result>, List<DatabaseOlineDriversDTO>>(onlineDrivers);
            }
        }

        public static async Task<UpcomingLaterBooking> GetUpcomingLaterBooking(string driverId)
        {
            using (var dbContext = new CangooEntities())
            {
                var upcoming = dbContext.spGetUpcomingLaterBookingByDriverID(driverId, DateTime.UtcNow.ToString(), (int)TripStatuses.LaterBookingAccepted).FirstOrDefault(); //utc date time
                if (upcoming != null)
                {
                    return new UpcomingLaterBooking
                    {
                        tripID = upcoming.TripID.ToString(),
                        pickUpDateTime = Convert.ToDateTime(upcoming.PickUpBookingDateTime).ToString(Formats.DateTimeFormat),
                        seatingCapacity = Convert.ToInt32(upcoming.Noofperson),
                        pickUplatitude = upcoming.PickupLocationLatitude,
                        pickUplongitude = upcoming.PickupLocationLongitude,
                        pickUpLocation = upcoming.PickUpLocation,
                        dropOfflatitude = upcoming.DropOffLocationLatitude,
                        dropOfflongitude = upcoming.DropOffLocationLongitude,
                        dropOffLocation = upcoming.DropOffLocation,
                        passengerName = upcoming.Name,
                        isSend30MinutSendFCM = (Convert.ToDateTime(upcoming.PickUpBookingDateTime) - DateTime.UtcNow).TotalMinutes <= 30 ? true : false,
                        isSend20MinutSendFCM = (Convert.ToDateTime(upcoming.PickUpBookingDateTime) - DateTime.UtcNow).TotalMinutes <= 20 ? true : false,
                        isWeb = upcoming.isWeb
                    };
                }
                return null;
            }
        }

        public static async Task<spGetDriverVehicleDetail_Result> GetDriverVehicleDetail(string driverId, string vehicleId, string passengerId, bool isWeb)
        {
            using (var dbContext = new CangooEntities())
            {
                return dbContext.spGetDriverVehicleDetail(driverId, vehicleId, passengerId, isWeb).FirstOrDefault();
                
            }
        }

        public static async Task<double> UpdatePriorityHourLog(Guid driverId)
        {
            using (var dbContext = new CangooEntities())
            {
                var captain = dbContext.Captains.Where(c => c.CaptainID == driverId).FirstOrDefault();
                captain.IsPriorityHoursActive = false;

                dbContext.PriorityHourLogs.Add(new PriorityHourLog
                {
                    PriorityHourLogID = Guid.NewGuid(),
                    CaptainID = driverId,
                    PriorityHourEndTime = (DateTime)captain.LastPriorityHourEndTime,
                    PriorityHourStartTime = (DateTime)captain.LastPriorityHourStartTime
                });

                await dbContext.SaveChangesAsync();
                return (double)captain.EarningPoints;
            }
        }
    }
}
