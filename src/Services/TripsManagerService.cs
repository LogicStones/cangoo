using Constants;
using DatabaseModel;
using DTOs.API;
using Integrations;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services
{
    public class TripsManagerService
    {
        public static async Task<List<TripOverView>> GetPassengerCompletedTrips(string passengerId, int offSet, int limit)
        {
            using (CangooEntities dbContext = new CangooEntities())
            {
                var query = dbContext.Database.SqlQuery<TripOverView>("exec spGetPassengerCompletedTrips @PassengerID, @OffSet, @Limit",
                                                                new SqlParameter("@PassengerID", passengerId),
                                                                new SqlParameter("@OffSet", offSet),
                                                                new SqlParameter("@Limit", limit));
                return await query.ToListAsync();
            }
        }
        
        public static async Task<List<TripOverView>> GetPassengerCancelledTrips(string passengerId, int offSet, int limit)
        {
            using (CangooEntities dbContext = new CangooEntities())
            {
                var query = dbContext.Database.SqlQuery<TripOverView>("exec spGetPassengerCancelledTrips @PassengerID, @OffSet, @Limit",
                                                                new SqlParameter("@PassengerID", passengerId),
                                                                new SqlParameter("@OffSet", offSet),
                                                                new SqlParameter("@Limit", limit));
                return await query.ToListAsync();
            }
        }
        
        public static async Task<List<TripOverView>> GetPassengerScheduledTrips(string passengerId, int offSet, int limit)
        {
            using (CangooEntities dbContext = new CangooEntities())
            {
                var query = dbContext.Database.SqlQuery<TripOverView>("exec spGetPassengerScheduledTrips @PassengerID, @OffSet, @Limit",
                                                                new SqlParameter("@PassengerID", passengerId),
                                                                new SqlParameter("@OffSet", offSet),
                                                                new SqlParameter("@Limit", limit));
                return await query.ToListAsync();
            }
        }

        public static async Task<TripDetails> GetTripDetails(string tripId)
        {
            using (CangooEntities dbContext = new CangooEntities())
            {
                var query = dbContext.Database.SqlQuery<TripDetails>("exec spGetTripDetails @TripId",
                                                                new SqlParameter("@TripId", tripId));
                var details = await query.FirstOrDefaultAsync();
                details.FacilitiesList = await FacilitiesManagerService.GetFacilitiesListAsync();
                return details;
            }
        }

        public static async Task<List<GetRecentLocationDetails>> GetRecentLocation(string passengerId)
        {
            using (CangooEntities dbContext = new CangooEntities())
            {
                var query = dbContext.Database.SqlQuery<GetRecentLocationDetails>("SELECT TOP(5) DropOffLocationLatitude,DropOffLocationLongitude,DropOffLocation FROM Trips WHERE UserID=@passengerId AND TripStatusID=1 ORDER BY ArrivalDateTime DESC",
                                                                                                                    new SqlParameter("@passengerId", passengerId));
                return await query.ToListAsync();
            }
        }

        public static async Task<decimal> CalculateEstimatedFare(string PickUpArea, string PickUpLatitude, string PickUpLongitude, string DropOffArea, string DropOffLatitude, string DropOffLongitutde)
        {
            var ApplicationId = ConfigurationManager.AppSettings["ApplicationID"].ToString();
            using (CangooEntities dbContext = new CangooEntities())
            {
                var areas = dbContext.RideServicesAreas.Where(f => f.ApplicationID.ToString().Equals(ApplicationId)).ToList();
                if (!areas.Any())
                {
                    return 0;
                }
                else
                {
                    RideServicesArea PickupServiceArea = new RideServicesArea();
                    RideServicesArea DropOffServiceArea = new RideServicesArea();
                    foreach (var data in areas)
                    {
                        if (PolygonService.IsLatLonExistsInPolygon(PolygonService.ConvertLatLonObjectsArrayToPolygonString(data.AreaLatLong), PickUpLatitude, PickUpLongitude))
                        {
                            PickupServiceArea = data;
                        }
                        if (PolygonService.IsLatLonExistsInPolygon(PolygonService.ConvertLatLonObjectsArrayToPolygonString(data.AreaLatLong), DropOffLatitude, DropOffLongitutde))
                        {
                            DropOffServiceArea = data;
                        }
                        if (!PickupServiceArea.AreaID.Equals(Guid.Empty) && !DropOffServiceArea.AreaID.Equals(Guid.Empty))
                        {
                            break;
                        }

                    }
                    decimal inBoundDistanceInKM = 0, 
                        inBoundDistanceFare = 0, inBoundTimeFare = 0, inBoundSurchargeAmount = 0, inBoundBaseFare = 0, inBoundTotalFare = 0,
                        inBoundTimeInMinutes = 0;
                    bool isMorningShift = false;
                    var fareManager = from a in dbContext.RideServicesAreas
                                      join b in dbContext.RideServicesAreaFares on a.AreaID equals b.AreaID
                                      join c in dbContext.RideServicesFareManagers on b.AreaID equals c.RideServicesID
                                      select new
                                      {
                                          RideServicesID = c.RideServicesID,
                                          EveningShiftStartTime = c.EveningShiftStartTime,
                                          EveningShiftEndTime = c.EveningShiftEndTime,
                                          EveningSurcharge = c.EveningSurcharge,
                                          EveningBaseFare = c.EveningBaseFare,
                                          EveningPerKmFare = c.EveningPerKmFare,
                                          EveningPerMinFare = c.EveningPerMinFare,
                                          EveningBookingFare = c.EveningBookingFare,
                                          EveningWaitingFare = c.EveningWaitingFare,
                                          MorningShiftStartTime = c.MorningShiftStartTime,
                                          MorningShiftEndTime = c.MorningShiftEndTime,
                                          MorningSurcharge = c.MorningSurcharge,
                                          MorningBaseFare = c.MorningBaseFare,
                                          MorningPerKmFare = c.MorningPerKmFare,
                                          MorningPerMinFare = c.MorningPerMinFare,
                                          MorningBookingFare = c.MorningBookingFare,
                                          MorningWaitingFare = c.MorningWaitingFare,
                                          WeekendShiftStartTime = c.WeekendShiftStartTime,
                                          WeekendShiftEndTime = c.WeekendShiftEndTime,
                                          WeekendSurcharge = c.WeekendSurcharge,
                                          WeekendBaseFare = c.WeekendBaseFare,
                                          WeekendPerKmFare = c.WeekendPerKmFare,
                                          WeekendPerMinFare = c.WeekendPerMinFare,
                                          WeekendBookingFare = c.WeekendBookingFare,
                                          WeekendWaitingFare = c.WeekendWaitingFare,
                                          MorningWishCarExtraPrice = c.MorningWishCarExtraPrice,
                                          EveningWishCarExtraPrice = c.EveningWishCarExtraPrice,
                                          WeekendWishCarExtraPrice = c.WeekendWishCarExtraPrice,
                                          WeekendIsFixed = c.WeekendIsFixed,
                                          MorningIsFixed = c.MorningIsFixed,
                                          EveningIsFixed = c.EveningIsFixed
                                      };

                    var result = DistanceMatrixAPI.GetTimeAndDistance(PickUpLatitude + "," + PickUpLongitude, DropOffLatitude + "," + DropOffLongitutde);
                    inBoundDistanceInKM = Convert.ToDecimal(result.distanceInMeters / 1000.0);
                    inBoundTimeInMinutes = Convert.ToDecimal(result.durationInSeconds / 60.0);
                    foreach (var data in fareManager)
                    {
                        isMorningShift = DateTime.UtcNow.TimeOfDay >= data.MorningShiftStartTime && DateTime.UtcNow.TimeOfDay <= data.MorningShiftEndTime;
                        CalculateDistanceAndTimeFare(data.RideServicesID.ToString(),
                        isMorningShift,
                        inBoundDistanceInKM,
                        (decimal)(isMorningShift ? data.MorningPerKmFare : data.EveningPerKmFare),
                        inBoundTimeInMinutes,
                        (decimal)(isMorningShift ? data.MorningPerMinFare : data.EveningPerMinFare),
                        out inBoundDistanceFare,
                        out inBoundTimeFare);
                        inBoundBaseFare = Convert.ToDecimal(isMorningShift ? data.MorningBaseFare : data.EveningBaseFare);
                        inBoundTotalFare = inBoundBaseFare + inBoundDistanceFare + inBoundTimeFare;
                        inBoundSurchargeAmount = Convert.ToDecimal(inBoundTotalFare * (isMorningShift ? data.MorningSurcharge : data.EveningSurcharge) / 100);
                    }

                    var totalFare = inBoundTotalFare + inBoundSurchargeAmount;
                }
                return 0;
            }
        }

        private static void CalculateDistanceAndTimeFare(string RideServicesID, bool isMorningShift, decimal estimatedDistanceInKM, decimal perKMFare, decimal estimatedTimeInMinutes, decimal perMinFare, out decimal distanceFare, out decimal timeFare)
        {
            using (var dbContext = new CangooEntities())
            {
                #region distance fare calculation

                distanceFare = 0;

                //distance ranges are saved in kilo meters
                var lstFareDistanceRange = dbContext.RideServicesFareRanges.Where(f => f.ShiftID == 1 &&
                                                            f.RideServicesID.ToString().Equals(RideServicesID)).ToList()
                                                            .OrderBy(f => f.Range);

                if (lstFareDistanceRange.Any())
                {
                    foreach (var ran in lstFareDistanceRange)
                    {
                        var arrRange = ran.Range.Split(';');

                        if (Convert.ToDecimal(arrRange[1]) <= estimatedDistanceInKM)
                        {
                            distanceFare += Convert.ToDecimal(ran.Charges) * (Convert.ToDecimal(arrRange[1]) - Convert.ToDecimal(arrRange[0]));
                        }
                        else
                        {
                            distanceFare += Convert.ToDecimal(ran.Charges) * (estimatedDistanceInKM - Convert.ToDecimal(arrRange[0]));
                            break;
                        }

                    }
                }
                else
                {
                    distanceFare = estimatedDistanceInKM * perKMFare;
                }

                #endregion

                #region time fare calculation


                timeFare = 0;
                //distance ranges are saved in minutes
                var lstFareTimeRange = dbContext.RideServicesTimeRanges.Where(f => f.ShiftID == 1 &&
                                                            f.RideServicesID.ToString().Equals(RideServicesID)).ToList()
                                                            .OrderBy(f => f.Range);

                if (lstFareTimeRange.Any())
                {
                    foreach (var ran in lstFareTimeRange)
                    {
                        var arrRange = ran.Range.Split(';');

                        if (Convert.ToDecimal(arrRange[1]) <= estimatedTimeInMinutes)
                        {
                            timeFare += Convert.ToDecimal(ran.Charges) * (Convert.ToDecimal(arrRange[1]) - Convert.ToDecimal(arrRange[0]));
                        }
                        else
                        {
                            timeFare += Convert.ToDecimal(ran.Charges) * (estimatedTimeInMinutes - Convert.ToDecimal(arrRange[0]));
                            break;
                        }
                    }
                }
                else
                {
                    timeFare = estimatedTimeInMinutes * perMinFare;
                }

                #endregion
            }
        }
    }
}