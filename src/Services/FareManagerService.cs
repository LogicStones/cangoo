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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services
{
    public class FareManagerService
    {
        public static async Task<EstimateFareResponse> GetFareEstimate(
            string pickUpPostalCode, string pickUpLatitude, string pickUpLongitude,
            string midwayPostalCode, string midwayLatitude, string midwayLongitude,
            string dropOffPostalCode, string dropOffLatitude, string dropOffLongitutde,
            string polyLine)
        {
            var result = await PrepareResponseObject(polyLine);

            var ApplicationId = ConfigurationManager.AppSettings["ApplicationID"].ToString();
            using (CangooEntities dbContext = new CangooEntities())
            {
                var areasCategoryFareList = await GetApplicationRideServicesAreaCategoryFareList(ApplicationId);
                var districtZonesList = await GetApplicationCourierServicesDistrictsZoneList(ApplicationId);

                if (areasCategoryFareList.Any())
                {
                    //TBD : Get only specified areas with ids (Optimization)
                    var areasList = await GetApplicationRideServiceAreasList(ApplicationId);

                    RideServicesArea PickupServiceArea = new RideServicesArea();
                    RideServicesArea MidwayServiceArea = new RideServicesArea();
                    RideServicesArea DropOffServiceArea = new RideServicesArea();

                    foreach (var area in areasList)
                    {
                        if (PolygonService.IsLatLonExistsInPolygon(PolygonService.ConvertLatLonObjectsArrayToPolygonString(area.AreaLatLong), pickUpLatitude, pickUpLongitude))
                        {
                            PickupServiceArea = area;
                        }

                        if (!string.IsNullOrEmpty(midwayLatitude))
                        {

                            if (PolygonService.IsLatLonExistsInPolygon(PolygonService.ConvertLatLonObjectsArrayToPolygonString(area.AreaLatLong), midwayLatitude, midwayLongitude))
                            {
                                MidwayServiceArea = area;
                            }
                        }

                        if (PolygonService.IsLatLonExistsInPolygon(PolygonService.ConvertLatLonObjectsArrayToPolygonString(area.AreaLatLong), dropOffLatitude, dropOffLongitutde))
                        {
                            DropOffServiceArea = area;
                        }

                        if (string.IsNullOrEmpty(midwayLatitude))
                        {
                            if (!PickupServiceArea.AreaID.Equals(Guid.Empty) &&
                                !DropOffServiceArea.AreaID.Equals(Guid.Empty))
                            {
                                break;
                            }
                        }
                        else
                        {
                            if (!PickupServiceArea.AreaID.Equals(Guid.Empty) &&
                                !MidwayServiceArea.Area.Equals(Guid.Empty) &&
                                !DropOffServiceArea.AreaID.Equals(Guid.Empty))
                            {
                                break;
                            }
                        }
                    }

                    //If false then it means either request area doesn't in system or algorithm failed to identify
                    if ((string.IsNullOrEmpty(midwayPostalCode) && PickupServiceArea.AreaID != Guid.Empty && DropOffServiceArea.AreaID != Guid.Empty) ||
                        (!string.IsNullOrEmpty(midwayPostalCode) && PickupServiceArea.AreaID != Guid.Empty && MidwayServiceArea.AreaID != Guid.Empty && DropOffServiceArea.AreaID != Guid.Empty))
                    {
                        var inBoundTripStats = GetInBoundTimeAndDistance(pickUpLatitude, pickUpLongitude, dropOffLatitude, dropOffLongitutde);

                        //All categories may have same fare manager for selected area
                        if (areasCategoryFareList.Select(ac => ac.AreaID).Distinct().Count() == 1)
                        {
                            var fareManagerId = areasCategoryFareList.Where(ac => ac.AreaID.Equals(PickupServiceArea.AreaID)).FirstOrDefault().RideServicesFareManagerID;
                            var inBoundtotalFare = await CalcuateFare((Guid)fareManagerId, inBoundTripStats.DistanceInKM, inBoundTripStats.TimeInMinutes);

                            result.Categories.Select(c => { c.Amount = inBoundtotalFare.ToString(); return c; }).ToList();

                            foreach (var item in result.Categories)
                            {
                                item.ETA = await VehiclesService.GetVehicleETA(item.CategoryID);
                            }
                        }
                        else
                        {
                            var standardFareManagerId = areasCategoryFareList.Where(ac => ac.AreaID.Equals(PickupServiceArea.AreaID) && ac.CategoryID == (int)VehicleCategories.Standard).FirstOrDefault().ID;
                            var comfortFareManagerId = areasCategoryFareList.Where(ac => ac.AreaID.Equals(PickupServiceArea.AreaID) && ac.CategoryID == (int)VehicleCategories.Comfort).FirstOrDefault().ID;
                            var premiumFareManagerId = areasCategoryFareList.Where(ac => ac.AreaID.Equals(PickupServiceArea.AreaID) && ac.CategoryID == (int)VehicleCategories.Premium).FirstOrDefault().ID;
                            var grossraumFareManagerId = areasCategoryFareList.Where(ac => ac.AreaID.Equals(PickupServiceArea.AreaID) && ac.CategoryID == (int)VehicleCategories.Grossraum).FirstOrDefault().ID;
                            var greenTaxiFareManagerId = areasCategoryFareList.Where(ac => ac.AreaID.Equals(PickupServiceArea.AreaID) && ac.CategoryID == (int)VehicleCategories.GreenTaxi).FirstOrDefault().ID;
                        }
                    }
                }

                if (districtZonesList.Any())
                {
                    if (string.IsNullOrEmpty(midwayPostalCode))
                    {
                        var zoneId = GetZoneIdByPostCodes(pickUpPostalCode, dropOffPostalCode, districtZonesList);
                        //if null it means zone is not assigned to requested postal codes.
                        if (zoneId != null)
                        {
                            var zone = await GetApplicationCourierServicesZoneById((int)zoneId);
                            result.Courier = new CourierFareEstimate
                            {
                                Amount = zone.Price.ToString(),
                                Zones = zone.ZoneName,
                                ETA = await VehiclesService.GetVehicleETA(((int)VehicleCategories.Standard).ToString())
                            };
                        }
                    }
                    else
                    {
                        var firstZoneId = GetZoneIdByPostCodes(pickUpPostalCode, midwayPostalCode, districtZonesList);
                        var secondZoneId = GetZoneIdByPostCodes(midwayPostalCode, dropOffPostalCode, districtZonesList);

                        if (firstZoneId != null && secondZoneId != null)
                        {
                            var firstZone = await GetApplicationCourierServicesZoneById((int)firstZoneId);
                            var secondZone = await GetApplicationCourierServicesZoneById((int)secondZoneId);
                            result.Courier = new CourierFareEstimate
                            {
                                Amount = (firstZone.Price + secondZone.Price).ToString(),
                                Zones = firstZone.ZoneName + " - " + secondZone.ZoneName,
                                ETA = await VehiclesService.GetVehicleETA(((int)VehicleCategories.Standard).ToString())
                            };
                        }
                    }
                }
            }

            return result;
        }

        private static async Task<EstimateFareResponse> PrepareResponseObject(string polyLine)
        {
            return new EstimateFareResponse
            {
                PolyLine = polyLine,
                Categories = new List<VehicleCategoryFareEstimate>
                {
                    new VehicleCategoryFareEstimate
                    {
                        CategoryID = ((int)VehicleCategories.Standard).ToString()
                    },
                    new VehicleCategoryFareEstimate
                    {
                        CategoryID = ((int)VehicleCategories.Comfort).ToString()
                    },
                    new VehicleCategoryFareEstimate
                    {
                        CategoryID = ((int)VehicleCategories.Premium).ToString()
                    },
                    new VehicleCategoryFareEstimate
                    {
                        CategoryID = ((int)VehicleCategories.Grossraum).ToString()
                    },
                    new VehicleCategoryFareEstimate
                    {
                        CategoryID = ((int)VehicleCategories.GreenTaxi).ToString()
                    }
                },
                Courier = new CourierFareEstimate(),
                Facilities = await FacilitiesManagerService.GetFacilitiesListAsync()
            };
        }

        #region Ride Service
        private static async Task<List<RideServicesAreaCategoryFare>> GetApplicationRideServicesAreaCategoryFareList(string applicationId)
        {
            using (var dbContext = new CangooEntities())
            {
                return await dbContext.RideServicesAreaCategoryFares.Where(f => f.ApplicationID.ToString().Equals(applicationId)).ToListAsync();
            }
        }

        private static async Task<List<RideServicesArea>> GetApplicationRideServiceAreasList(string applicationId)
        {
            using (var dbContext = new CangooEntities())
            {
                return await dbContext.RideServicesAreas.Where(f => f.ApplicationID.ToString().Equals(applicationId)).ToListAsync();
            }
        }

        private static TripStats GetInBoundTimeAndDistance(string pickUpLatitude, string pickUpLongitude, string dropOffLatitude, string dropOffLongitutde)
        {
            var result = DistanceMatrixAPI.GetTimeAndDistance(pickUpLatitude + "," + pickUpLongitude, dropOffLatitude + "," + dropOffLongitutde);
            return new TripStats
            {
                DistanceInKM = Convert.ToDecimal(result.distanceInMeters / 1000.0),
                TimeInMinutes = Convert.ToDecimal(result.durationInSeconds / 60.0)
            };
        }

        private static async Task<RideServicesFareManager> GetFareManagerById(Guid fareManagerId)
        {
            using (var dbContext = new CangooEntities())
            {
                return await dbContext.RideServicesFareManagers.Where(f => f.RideServicesID == fareManagerId).FirstOrDefaultAsync();
            }
        }

        private static decimal FormatFareValue(decimal totalFare)
        {
            string decimalValue = "";

            if (int.Parse(totalFare.ToString("0.00").Split('.')[1]) % 10 == 5 || int.Parse(totalFare.ToString("0.00").Split('.')[1]) % 10 == 0)
            {
                decimalValue = totalFare.ToString("0.00").Split('.')[1];
            }
            else if (int.Parse(totalFare.ToString("0.00").Split('.')[1]) % 10 < 5)
            {
                decimalValue = (int.Parse(totalFare.ToString("0.00").Split('.')[1]) / 10).ToString();
            }
            else
            {
                decimalValue = (int.Parse(totalFare.ToString("0.00").Split('.')[1]) / 10).ToString() + "99";
            }

            totalFare = decimal.Parse(totalFare.ToString().Split('.')[0] + "." + decimalValue);
            return totalFare;
        }

        private static decimal GetTraveledDistanceFromFireBase(string tripID, string captainID)
        {
            //FireBaseController fb = new FireBaseController();
            //var distance = fb.getTripDistance("Trips/" + tripID + "/" + captainID + "/" + "distanceTraveled");
            //if (distance != null)
            //    return Convert.ToDecimal(distance) / 1000;
            //else
            return 0.00M;
        }

        private static string GetPolyLineFromFireBase(string polyLine, string tripID, string captainID)
        {
            //FireBaseController fb = new FireBaseController();
            //var ployLineLocations = fb.getTripPolyLineDetails("Trips/" + tripID + "/" + captainID + "/" + "polyline");
            //if (ployLineLocations != null)
            //{
            //    foreach (var location in ployLineLocations)
            //    {
            //        polyLine += location.Value.latitude + "," + location.Value.longitude + "|";
            //    }
            //    polyLine = polyLine.Length > 0 ? polyLine.Remove(polyLine.Length - 1) : "";
            //}
            return polyLine;
        }

        #endregion

        #region Courier Service
        private static async Task<List<CourierServicesDistrict>> GetApplicationCourierServicesDistrictsZoneList(string applicationId)
        {
            using (var dbContext = new CangooEntities())
            {
                return await dbContext.CourierServicesDistricts.Where(csd => csd.ApplicationID.ToString().Equals(applicationId)).ToListAsync();
            }
        }

        private static int? GetZoneIdByPostCodes(string pickUpPostalCode, string dropOffPostalCode, List<CourierServicesDistrict> districtZonesList)
        {
            var xIndex = districtZonesList.Where(dz => dz.DistrictName.Equals(pickUpPostalCode) && dz.IsDistrictLabel == true && dz.ZoneID == null && dz.Yindex == 0).FirstOrDefault();
            var yIndex = districtZonesList.Where(dz => dz.DistrictName.Equals(dropOffPostalCode) && dz.IsDistrictLabel == true && dz.ZoneID == null && dz.Xindex == 0).FirstOrDefault();

            if (xIndex == null || yIndex == null)
                return null;

            var zone = districtZonesList.Where(dz => (dz.Xindex == xIndex.Xindex) && (dz.Yindex == yIndex.Yindex) && dz.IsDistrictLabel == false).FirstOrDefault();
            return zone?.ZoneID;
        }

        private static async Task<CourierServiceZone> GetApplicationCourierServicesZoneById(int zoneId)
        {
            using (var dbContext = new CangooEntities())
            {
                return await dbContext.CourierServiceZones.Where(csz => csz.ZoneID == zoneId).FirstOrDefaultAsync();
            }
        }

        #endregion

        private static async Task<decimal> CalcuateFare(Guid fareManagerId, decimal distanceInKM, decimal timeInMinutes)
        {
            using (var dbContext = new CangooEntities())
            {
                var fareManager = await GetFareManagerById(fareManagerId);
                decimal inBoundDistanceFare = 0, inBoundTimeFare = 0, inBoundSurchargeAmount = 0, inBoundBaseFare = 0, inBoundTotalFare = 0;

                bool isWeekend = WeekendOrHoliday();
                bool isMorningShift = DateTime.UtcNow.TimeOfDay >= fareManager.MorningShiftStartTime && DateTime.UtcNow.TimeOfDay <= fareManager.MorningShiftEndTime;

                CalculateDistanceAndTimeFare(fareManager.RideServicesID.ToString(),
                    isWeekend ? (int)FareManagerShifts.Weekend : isMorningShift ? (int)FareManagerShifts.Morning : (int)FareManagerShifts.Evening,
                    distanceInKM,
                    (decimal)(isMorningShift ? fareManager.MorningPerKmFare : fareManager.EveningPerKmFare),
                    timeInMinutes,
                    (decimal)(isMorningShift ? fareManager.MorningPerMinFare : fareManager.EveningPerMinFare),
                    out inBoundDistanceFare,
                    out inBoundTimeFare);

                inBoundBaseFare = Convert.ToDecimal(isMorningShift ? fareManager.MorningBaseFare : fareManager.EveningBaseFare);
                inBoundTotalFare = inBoundBaseFare + inBoundDistanceFare + inBoundTimeFare;
                inBoundSurchargeAmount = Convert.ToDecimal(inBoundTotalFare * (isMorningShift ? fareManager.MorningSurcharge : fareManager.EveningSurcharge) / 100);
                decimal totalFare = inBoundTotalFare + inBoundSurchargeAmount;

                return FormatFareValue(totalFare);
            }
        }

        private static bool WeekendOrHoliday()
        {
            // TBD: Check date from public holidays lookup table
            return DateTime.UtcNow.DayOfWeek == DayOfWeek.Saturday || DateTime.UtcNow.DayOfWeek == DayOfWeek.Sunday;
        }

        private static void CalculateDistanceAndTimeFare(string rideServicesID, int shiftId, decimal estimatedDistanceInKM, decimal perKMFare, decimal estimatedTimeInMinutes, decimal perMinFare, out decimal distanceFare, out decimal timeFare)
        {
            using (var dbContext = new CangooEntities())
            {
                #region distance fare calculation

                distanceFare = 0;

                //distance ranges are saved in kilo meters
                var lstFareDistanceRange = dbContext.RideServicesFareRanges
                    .Where(f => f.ShiftID == shiftId && f.RideServicesID.ToString().Equals(rideServicesID)).ToList()
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
                var lstFareTimeRange = dbContext.RideServicesTimeRanges
                    .Where(f => f.ShiftID == shiftId && f.RideServicesID.ToString().Equals(rideServicesID)).ToList()
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




        #region existing calculation

        //public static decimal CalculateEstimatedFare(int seatingCapacity, bool isWalkIn, bool isTripEnd, string routePolyLine, bool isAtDropOffLocation, bool isFareChangeAllowed, Guid? TripId, string applicationID,
        //   string pickUpLatitude, string pickUpLongitude, string dropOffLatitude, string dropOffLongitude, ref FareManager pickUpAreaFareManager, ref FareManager dropOffAreaFareManager, ref Dictionary<dynamic, dynamic> dic)
        ////string distance, TimeSpan? waitingDur
        //{
        //    using (var context = new CangooEntities())
        //    {
        //        PrepareFareResponseObject(dic);

        //        var fareManagers = context.FareManagers.Where(f => f.ApplicationID.ToString().Equals(applicationID)).ToList();

        //        if (!fareManagers.Any())
        //            return 0;

        //        pickUpAreaFareManager = new FareManager();
        //        dropOffAreaFareManager = new FareManager();

        //        foreach (var item in fareManagers)
        //        {
        //            if (Polygon.IsLatLonExistsInPolygon(Polygon.ConvertLatLonObjectsArrayToPolygonString(item.AreaLatLong), pickUpLatitude, pickUpLongitude))
        //                pickUpAreaFareManager = item;

        //            if (Polygon.IsLatLonExistsInPolygon(Polygon.ConvertLatLonObjectsArrayToPolygonString(item.AreaLatLong), dropOffLatitude, dropOffLongitude))
        //                dropOffAreaFareManager = item;

        //            if (!pickUpAreaFareManager.FareManagerID.Equals(Guid.Empty) && !dropOffAreaFareManager.FareManagerID.Equals(Guid.Empty))
        //                break;
        //        }

        //        //TBD: Remove this quick fix, efficient algo should be applied to make sure exact fare managers are selected from database

        //        //if (pickUpAreaFareManager.FareManagerID.Equals(Guid.Empty) && dropOffAreaFareManager.FareManagerID.Equals(Guid.Empty))
        //        //{
        //        //    pickUpAreaFareManager = context.FareManagers.OrderByDescending(fm => fm.UpdatedAt).FirstOrDefault();
        //        //    dropOffAreaFareManager = pickUpAreaFareManager;
        //        //}
        //        //else if (pickUpAreaFareManager.FareManagerID.Equals(Guid.Empty) && !dropOffAreaFareManager.FareManagerID.Equals(Guid.Empty))
        //        //    pickUpAreaFareManager = dropOffAreaFareManager;
        //        //else if (!pickUpAreaFareManager.FareManagerID.Equals(Guid.Empty) && dropOffAreaFareManager.FareManagerID.Equals(Guid.Empty))
        //        //    dropOffAreaFareManager = pickUpAreaFareManager;

        //        dic["pickUpFareManagerID"] = pickUpAreaFareManager.FareManagerID.ToString();
        //        dic["dropOffFareMangerID"] = dropOffAreaFareManager.FareManagerID.ToString();

        //        bool isInBound = Polygon.IsLatLonExistsInPolygon(Polygon.ConvertLatLonObjectsArrayToPolygonString(pickUpAreaFareManager.AreaLatLong), dropOffLatitude, dropOffLongitude);

        //        decimal totalFare = 0;//, distanceFare = 0, timeFare = 0;
        //        decimal formattedTotalFare = 0;

        //        if (isWalkIn)   //Updated calculation scenario not implemented
        //        {
        //            totalFare = 0;
        //        }
        //        else
        //        {
        //            decimal inBoundDistanceInKM = 0, outBoundDistanceInKM = 0,
        //                inBoundDistanceFare = 0, inBoundTimeFare = 0, inBoundSurchargeAmount = 0, inBoundBaseFare = 0, inBoundTotalFare = 0,
        //                outBoundDistanceFare = 0, outBoundTimeFare = 0, outBoundSurchargeAmount = 0, outBoundBaseFare = 0, outBoundTotalFare = 0,
        //                inBoundTimeInMinutes = 0, outBoundTimeInMinutes = 0;
        //            string polyLine = "";
        //            bool isMorningShift = false;


        //            if (isFareChangeAllowed)
        //            {
        //                /*  
        //                 *  1 - In every case need to re-calculate. isFareChangeAllowed will be true only in case of end trip. isTripEnd will be always true here) 
        //                 *  2 - isAtDropOffLocation doesn't matter because trip time and distance is changed
        //                 */
        //                if (isInBound)
        //                {
        //                    //Get distance from firebase (Trips / TripID / CaptainID / distanceTraveled), 
        //                    //trip time (TripEndDateTime - TripStartDateTime) from database, 
        //                    //then apply fare manager ranges

        //                    var trip = context.Trips.Where(t => t.TripID.ToString().Equals(TripId.ToString())).FirstOrDefault();
        //                    trip.TripEndDatetime = getUtcDateTime();    //TBD: Trip instance should be passed from endTrip action. Time is already set over there.
        //                    isMorningShift = DateTime.UtcNow.TimeOfDay >= pickUpAreaFareManager.MorningShiftStartTime && DateTime.UtcNow.TimeOfDay <= pickUpAreaFareManager.MorningShiftEndTime;
        //                    inBoundDistanceInKM = GetTraveledDistanceFromFireBase(trip.TripID.ToString(), trip.CaptainID.ToString());
        //                    inBoundTimeInMinutes = (int)(((DateTime)trip.TripEndDatetime - (DateTime)trip.TripStartDatetime).TotalMinutes);

        //                    CalculateDistanceAndTimeFare(pickUpAreaFareManager.FareManagerID.ToString(),
        //                        isMorningShift,
        //                        inBoundDistanceInKM,
        //                        (decimal)(isMorningShift ? pickUpAreaFareManager.MorningPerKmFare : pickUpAreaFareManager.EveningPerKmFare),
        //                        inBoundTimeInMinutes,
        //                        (decimal)(isMorningShift ? pickUpAreaFareManager.MorningPerMinFare : pickUpAreaFareManager.EveningPerMinFare),
        //                        out inBoundDistanceFare,
        //                        out inBoundTimeFare);

        //                    inBoundBaseFare = Convert.ToDecimal(isMorningShift ? pickUpAreaFareManager.MorningBaseFare : pickUpAreaFareManager.EveningBaseFare);
        //                    inBoundTotalFare = inBoundBaseFare + inBoundDistanceFare + inBoundTimeFare;
        //                    inBoundSurchargeAmount = Convert.ToDecimal(inBoundTotalFare * (isMorningShift ? pickUpAreaFareManager.MorningSurcharge : pickUpAreaFareManager.EveningSurcharge) / 100);
        //                    totalFare = inBoundTotalFare + inBoundSurchargeAmount;
        //                    polyLine = GetPolyLineFromFireBase(polyLine, trip.TripID.ToString(), trip.CaptainID.ToString());
        //                }
        //                else
        //                {
        //                    //Get ployline ordered lat longs from firebase, (Trips / TripID / CaptainID / polyline / id /latitude + longitude + locationTime (diff / 1000 = seconds)) use first lat long to set pickup faremanager.
        //                    //Traverse ploygon and find boundary point. 
        //                    //Get inbound/outbund time using difference of polyline lat long.
        //                    //Get inbound/outbund distance using difference of polyline lat long.

        //                    var trip = context.Trips.Where(t => t.TripID == TripId).FirstOrDefault();

        //                    FireBaseController fb = new FireBaseController();
        //                    var ployLineLocations = fb.getTripPolyLineDetails("Trips/" + trip.TripID.ToString() + "/" + trip.CaptainID.ToString() + "/" + "polyline");
        //                    var inBoundDistanceInMeters = 0.0;
        //                    var inBoundTimeInSeconds = 0.0;
        //                    var outBoundDistanceInMeters = 0.0;
        //                    var outBoundTimeInSeconds = 0.0;

        //                    bool isGoneOutBound = false;
        //                    GeoCoordinate lastPosition = null;
        //                    DateTime lastLocationUpdateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

        //                    foreach (var location in ployLineLocations)
        //                    {
        //                        polyLine += location.Value.latitude + "," + location.Value.longitude + "|";

        //                        if (!isGoneOutBound)
        //                        {
        //                            if (Polygon.IsLatLonExistsInPolygon(Polygon.ConvertLatLonObjectsArrayToPolygonString(pickUpAreaFareManager.AreaLatLong), location.Value.latitude, location.Value.longitude))
        //                            {
        //                                if (lastPosition != null)
        //                                {
        //                                    var currentPosition = new GeoCoordinate(Convert.ToDouble(location.Value.latitude), Convert.ToDouble(location.Value.longitude));
        //                                    inBoundDistanceInMeters += currentPosition.GetDistanceTo(lastPosition);  //distance in meters

        //                                    DateTime currentLocationUpdateTime = DateTimeOffset.FromUnixTimeMilliseconds(location.Value.locationTime).UtcDateTime;
        //                                    inBoundTimeInSeconds += (currentLocationUpdateTime - lastLocationUpdateTime).TotalSeconds;
        //                                }
        //                            }
        //                            else
        //                            {
        //                                isGoneOutBound = true;
        //                                isMorningShift = DateTime.UtcNow.TimeOfDay >= pickUpAreaFareManager.MorningShiftStartTime && DateTime.UtcNow.TimeOfDay <= pickUpAreaFareManager.MorningShiftEndTime;
        //                                inBoundDistanceInKM = Convert.ToDecimal(inBoundDistanceInMeters / 1000.0);
        //                                inBoundTimeInMinutes = Convert.ToDecimal(inBoundTimeInSeconds / 60.0);

        //                                CalculateDistanceAndTimeFare(pickUpAreaFareManager.FareManagerID.ToString(),
        //                                    isMorningShift,
        //                                    inBoundDistanceInKM,
        //                                    (decimal)(isMorningShift ? pickUpAreaFareManager.MorningPerKmFare : pickUpAreaFareManager.EveningPerKmFare),
        //                                    inBoundTimeInMinutes,
        //                                    (decimal)(isMorningShift ? pickUpAreaFareManager.MorningPerMinFare : pickUpAreaFareManager.EveningPerMinFare),
        //                                    out inBoundDistanceFare,
        //                                    out inBoundTimeFare);

        //                                inBoundBaseFare = Convert.ToDecimal(isMorningShift ? pickUpAreaFareManager.MorningBaseFare : pickUpAreaFareManager.EveningBaseFare);
        //                                inBoundTotalFare = inBoundBaseFare + inBoundDistanceFare + inBoundTimeFare;
        //                                inBoundSurchargeAmount = Convert.ToDecimal(inBoundTotalFare * (isMorningShift ? pickUpAreaFareManager.MorningSurcharge : pickUpAreaFareManager.EveningSurcharge) / 100);
        //                            }
        //                        }
        //                        else
        //                        {
        //                            var currentPosition = new GeoCoordinate(Convert.ToDouble(location.Value.latitude), Convert.ToDouble(location.Value.longitude));
        //                            outBoundDistanceInMeters += currentPosition.GetDistanceTo(lastPosition);  //distance in meters

        //                            DateTime currentLocationUpdateTime = DateTimeOffset.FromUnixTimeMilliseconds(location.Value.locationTime).UtcDateTime;
        //                            outBoundTimeInSeconds += (currentLocationUpdateTime - lastLocationUpdateTime).TotalSeconds;

        //                        }
        //                        lastPosition = new GeoCoordinate(Convert.ToDouble(location.Value.latitude), Convert.ToDouble(location.Value.longitude));
        //                        lastLocationUpdateTime = DateTimeOffset.FromUnixTimeMilliseconds(location.Value.locationTime).UtcDateTime;
        //                    }

        //                    polyLine = polyLine.Length > 0 ? polyLine.Remove(polyLine.Length - 1) : "";

        //                    isMorningShift = DateTime.UtcNow.TimeOfDay >= dropOffAreaFareManager.MorningShiftStartTime && DateTime.UtcNow.TimeOfDay <= dropOffAreaFareManager.MorningShiftEndTime;

        //                    outBoundDistanceInKM = Convert.ToDecimal(outBoundDistanceInMeters / 1000.0);
        //                    outBoundTimeInMinutes = Convert.ToDecimal(outBoundTimeInSeconds / 60.0);

        //                    CalculateDistanceAndTimeFare(dropOffAreaFareManager.FareManagerID.ToString(),
        //                        isMorningShift,
        //                        outBoundDistanceInKM,
        //                        (decimal)(isMorningShift ? dropOffAreaFareManager.MorningPerKmFare : dropOffAreaFareManager.EveningPerKmFare),
        //                        outBoundTimeInMinutes,
        //                        (decimal)(isMorningShift ? dropOffAreaFareManager.MorningPerMinFare : dropOffAreaFareManager.EveningPerMinFare),
        //                        out outBoundDistanceFare,
        //                        out outBoundTimeFare);

        //                    outBoundBaseFare = Convert.ToDecimal(isMorningShift ? dropOffAreaFareManager.MorningBaseFare : dropOffAreaFareManager.EveningBaseFare);
        //                    outBoundTotalFare = outBoundBaseFare + outBoundDistanceFare + outBoundTimeFare;
        //                    outBoundSurchargeAmount = Convert.ToDecimal(outBoundTotalFare * (isMorningShift ? dropOffAreaFareManager.MorningSurcharge : dropOffAreaFareManager.EveningSurcharge) / 100);

        //                    totalFare = inBoundTotalFare + inBoundSurchargeAmount + outBoundTotalFare + outBoundSurchargeAmount;
        //                }
        //            }
        //            else
        //            {
        //                if (isInBound)
        //                {
        //                    if (isTripEnd)  //End Trip API call
        //                    {
        //                        if (isAtDropOffLocation) //Will be set false by application if destination was not set OR drop off location was not within radius of 200 meters
        //                        {
        //                            //If normal booking return the same fare which was calculated in estimate API call. In case of later booking recalculate
        //                            var trip = context.Trips.Where(t => t.TripID.ToString().Equals(TripId.ToString())).FirstOrDefault();

        //                            //CR : Don't recalculate laterboking fare in any case.

        //                            //if ((bool)trip.isLaterBooking)
        //                            //{
        //                            //    //In case of later booking recalculate
        //                            //    //Get distance and time from google, calculate fare according to fare manager ranges.
        //                            //    var result = DistanceMatrixAPI.GetTimeAndDistance(pickUpLatitude + "," + pickUpLongitude, dropOffLatitude + "," + dropOffLongitude);
        //                            //    isMorningShift = DateTime.UtcNow.TimeOfDay >= pickUpAreaFareManager.MorningShiftStartTime && DateTime.UtcNow.TimeOfDay <= pickUpAreaFareManager.MorningShiftEndTime;

        //                            //    inBoundDistanceInKM = Convert.ToDecimal(result.distanceInMeters / 1000.0);
        //                            //    inBoundTimeInMinutes = Convert.ToDecimal(result.durationInSeconds / 60.0);

        //                            //    CalculateDistanceAndTimeFare(pickUpAreaFareManager.FareManagerID.ToString(),
        //                            //        isMorningShift,
        //                            //        inBoundDistanceInKM,
        //                            //        (decimal)(isMorningShift ? pickUpAreaFareManager.MorningPerKmFare : pickUpAreaFareManager.EveningPerKmFare),
        //                            //        inBoundTimeInMinutes,
        //                            //        (decimal)(isMorningShift ? pickUpAreaFareManager.MorningPerMinFare : pickUpAreaFareManager.EveningPerMinFare),
        //                            //        out inBoundDistanceFare,
        //                            //        out inBoundTimeFare);

        //                            //    inBoundBaseFare = Convert.ToDecimal(isMorningShift ? pickUpAreaFareManager.MorningBaseFare : pickUpAreaFareManager.EveningBaseFare);
        //                            //    inBoundTotalFare = inBoundBaseFare + inBoundDistanceFare + inBoundTimeFare;
        //                            //    inBoundSurchargeAmount = Convert.ToDecimal(inBoundTotalFare * (isMorningShift ? pickUpAreaFareManager.MorningSurcharge : pickUpAreaFareManager.EveningSurcharge) / 100);
        //                            //    totalFare = inBoundTotalFare + inBoundSurchargeAmount;
        //                            //    polyLine = routePolyLine;
        //                            //}
        //                            //else
        //                            //{

        //                            inBoundDistanceInKM = Convert.ToDecimal(trip.InBoundDistanceInMeters / 1000.0);
        //                            inBoundDistanceFare = Convert.ToDecimal(trip.InBoundDistanceFare);
        //                            inBoundTimeInMinutes = Convert.ToDecimal(trip.InBoundTimeInSeconds / 60.0);
        //                            inBoundTimeFare = Convert.ToDecimal(trip.InBoundTimeFare);
        //                            outBoundDistanceInKM = Convert.ToDecimal(trip.OutBoundDistanceInMeters / 1000.0);
        //                            outBoundDistanceFare = Convert.ToDecimal(trip.OutBoundDistanceFare);
        //                            outBoundTimeInMinutes = Convert.ToDecimal(trip.OutBoundTimeInSeconds / 60.0);
        //                            outBoundTimeFare = Convert.ToDecimal(trip.OutBoundTimeFare);
        //                            inBoundSurchargeAmount = Convert.ToDecimal(trip.InBoundSurchargeAmount);
        //                            outBoundSurchargeAmount = Convert.ToDecimal(trip.OutBoundSurchargeAmount);
        //                            inBoundBaseFare = Convert.ToDecimal(trip.InBoundBaseFare);
        //                            outBoundBaseFare = Convert.ToDecimal(trip.OutBoundBaseFare);

        //                            totalFare = inBoundDistanceFare + inBoundTimeFare + inBoundBaseFare + inBoundSurchargeAmount +
        //                                        outBoundDistanceFare + outBoundTimeFare + outBoundBaseFare + outBoundSurchargeAmount;

        //                            polyLine = GetPolyLineFromFireBase(polyLine, trip.TripID.ToString(), trip.CaptainID.ToString());

        //                            //}
        //                        }
        //                        else
        //                        {
        //                            //Get distance from firebase trip node, 
        //                            //Calculate trip time (TripEndDateTime - TripStartDateTime) from database, 
        //                            //Apply fare manager ranges

        //                            var trip = context.Trips.Where(t => t.TripID.ToString().Equals(TripId.ToString())).FirstOrDefault();
        //                            trip.TripEndDatetime = getUtcDateTime();    //TBD: Trip instance should be passed from endTrip action. Time is already set over there.

        //                            isMorningShift = DateTime.UtcNow.TimeOfDay >= pickUpAreaFareManager.MorningShiftStartTime && DateTime.UtcNow.TimeOfDay <= pickUpAreaFareManager.MorningShiftEndTime;
        //                            inBoundDistanceInKM = GetTraveledDistanceFromFireBase(trip.TripID.ToString(), trip.CaptainID.ToString());
        //                            inBoundTimeInMinutes = (int)(((DateTime)trip.TripEndDatetime - (DateTime)trip.TripStartDatetime).TotalMinutes);

        //                            CalculateDistanceAndTimeFare(pickUpAreaFareManager.FareManagerID.ToString(),
        //                                isMorningShift,
        //                                inBoundDistanceInKM,
        //                                (decimal)(isMorningShift ? pickUpAreaFareManager.MorningPerKmFare : pickUpAreaFareManager.EveningPerKmFare),
        //                                inBoundTimeInMinutes,
        //                                (decimal)(isMorningShift ? pickUpAreaFareManager.MorningPerMinFare : pickUpAreaFareManager.EveningPerMinFare),
        //                                out inBoundDistanceFare,
        //                                out inBoundTimeFare);

        //                            inBoundBaseFare = Convert.ToDecimal(isMorningShift ? pickUpAreaFareManager.MorningBaseFare : pickUpAreaFareManager.EveningBaseFare);
        //                            inBoundTotalFare = inBoundBaseFare + inBoundDistanceFare + inBoundTimeFare;
        //                            inBoundSurchargeAmount = Convert.ToDecimal(inBoundTotalFare * (isMorningShift ? pickUpAreaFareManager.MorningSurcharge : pickUpAreaFareManager.EveningSurcharge) / 100);
        //                            totalFare = inBoundTotalFare + inBoundSurchargeAmount;

        //                            polyLine = GetPolyLineFromFireBase(polyLine, trip.TripID.ToString(), trip.CaptainID.ToString());

        //                        }
        //                    }
        //                    else    //Fare Estimate API call
        //                    {
        //                        //Get distance and time from google, calculate fare according to fare manager ranges.
        //                        var result = DistanceMatrixAPI.GetTimeAndDistance(pickUpLatitude + "," + pickUpLongitude, dropOffLatitude + "," + dropOffLongitude);
        //                        isMorningShift = DateTime.UtcNow.TimeOfDay >= pickUpAreaFareManager.MorningShiftStartTime && DateTime.UtcNow.TimeOfDay <= pickUpAreaFareManager.MorningShiftEndTime;

        //                        inBoundDistanceInKM = Convert.ToDecimal(result.distanceInMeters / 1000.0);
        //                        inBoundTimeInMinutes = Convert.ToDecimal(result.durationInSeconds / 60.0);

        //                        CalculateDistanceAndTimeFare(pickUpAreaFareManager.FareManagerID.ToString(),
        //                            isMorningShift,
        //                            inBoundDistanceInKM,
        //                            (decimal)(isMorningShift ? pickUpAreaFareManager.MorningPerKmFare : pickUpAreaFareManager.EveningPerKmFare),
        //                            inBoundTimeInMinutes,
        //                            (decimal)(isMorningShift ? pickUpAreaFareManager.MorningPerMinFare : pickUpAreaFareManager.EveningPerMinFare),
        //                            out inBoundDistanceFare,
        //                            out inBoundTimeFare);

        //                        inBoundBaseFare = Convert.ToDecimal(isMorningShift ? pickUpAreaFareManager.MorningBaseFare : pickUpAreaFareManager.EveningBaseFare);
        //                        inBoundTotalFare = inBoundBaseFare + inBoundDistanceFare + inBoundTimeFare;
        //                        inBoundSurchargeAmount = Convert.ToDecimal(inBoundTotalFare * (isMorningShift ? pickUpAreaFareManager.MorningSurcharge : pickUpAreaFareManager.EveningSurcharge) / 100);
        //                        totalFare = inBoundTotalFare + inBoundSurchargeAmount;
        //                        polyLine = routePolyLine;
        //                    }
        //                }
        //                else
        //                {
        //                    if (isTripEnd)  //End Trip API call
        //                    {
        //                        if (isAtDropOffLocation) //Will be set false by application if destination was not set OR drop off location was not within radius of 200 meters
        //                        {
        //                            //If normal booking return the same fare which was calculated in estimate API call. In case of later booking recalculate
        //                            var trip = context.Trips.Where(t => t.TripID.ToString().Equals(TripId.ToString())).FirstOrDefault();

        //                            //CR : Don't recalculate laterboking fare in any case.

        //                            //if ((bool)trip.isLaterBooking)
        //                            //{

        //                            //    //In case of later booking recalculate
        //                            //    //Get distance and time from database (saved on estimate fare), calculate fare according to fare manager new ranges (if changed).

        //                            //    #region InBoundCalculation

        //                            //    isMorningShift = DateTime.UtcNow.TimeOfDay >= pickUpAreaFareManager.MorningShiftStartTime && DateTime.UtcNow.TimeOfDay <= pickUpAreaFareManager.MorningShiftEndTime;

        //                            //    inBoundDistanceInKM = Convert.ToDecimal(trip.InBoundDistanceInMeters / 1000.0);
        //                            //    inBoundTimeInMinutes = Convert.ToDecimal(trip.InBoundTimeInSeconds / 60.0);

        //                            //    CalculateDistanceAndTimeFare(pickUpAreaFareManager.FareManagerID.ToString(),
        //                            //        isMorningShift,
        //                            //        inBoundDistanceInKM,
        //                            //        (decimal)(isMorningShift ? pickUpAreaFareManager.MorningPerKmFare : pickUpAreaFareManager.EveningPerKmFare),
        //                            //        inBoundTimeInMinutes,
        //                            //        (decimal)(isMorningShift ? pickUpAreaFareManager.MorningPerMinFare : pickUpAreaFareManager.EveningPerMinFare),
        //                            //        out inBoundDistanceFare,
        //                            //        out inBoundTimeFare);

        //                            //    inBoundBaseFare = Convert.ToDecimal(isMorningShift ? pickUpAreaFareManager.MorningBaseFare : pickUpAreaFareManager.EveningBaseFare);
        //                            //    inBoundTotalFare = inBoundBaseFare + inBoundDistanceFare + inBoundTimeFare;
        //                            //    inBoundSurchargeAmount = Convert.ToDecimal(inBoundTotalFare * (isMorningShift ? pickUpAreaFareManager.MorningSurcharge : pickUpAreaFareManager.EveningSurcharge) / 100);

        //                            //    #endregion

        //                            //    #region OutBoundCalculation

        //                            //    isMorningShift = DateTime.UtcNow.TimeOfDay >= dropOffAreaFareManager.MorningShiftStartTime && DateTime.UtcNow.TimeOfDay <= dropOffAreaFareManager.MorningShiftEndTime;

        //                            //    outBoundDistanceInKM = Convert.ToDecimal(trip.OutBoundDistanceInMeters / 1000.0);
        //                            //    outBoundTimeInMinutes = Convert.ToDecimal(trip.OutBoundTimeInSeconds / 60.0);

        //                            //    CalculateDistanceAndTimeFare(dropOffAreaFareManager.FareManagerID.ToString(),
        //                            //        isMorningShift,
        //                            //        outBoundDistanceInKM,
        //                            //        (decimal)(isMorningShift ? dropOffAreaFareManager.MorningPerKmFare : dropOffAreaFareManager.EveningPerKmFare),
        //                            //        outBoundTimeInMinutes,
        //                            //        (decimal)(isMorningShift ? dropOffAreaFareManager.MorningPerMinFare : dropOffAreaFareManager.EveningPerMinFare),
        //                            //        out outBoundDistanceFare,
        //                            //        out outBoundTimeFare);

        //                            //    outBoundBaseFare = Convert.ToDecimal(isMorningShift ? dropOffAreaFareManager.MorningBaseFare : dropOffAreaFareManager.EveningBaseFare);
        //                            //    outBoundTotalFare = outBoundBaseFare + outBoundDistanceFare + outBoundTimeFare;
        //                            //    outBoundSurchargeAmount = Convert.ToDecimal(inBoundTotalFare * (isMorningShift ? dropOffAreaFareManager.MorningSurcharge : dropOffAreaFareManager.EveningSurcharge) / 100);

        //                            //    totalFare = inBoundTotalFare + inBoundSurchargeAmount + outBoundTotalFare + outBoundSurchargeAmount;

        //                            //    #endregion

        //                            //    polyLine = GetPolyLineFromFireBase(polyLine, trip.TripID.ToString(), trip.CaptainID.ToString());
        //                            //}
        //                            //else
        //                            //{
        //                            inBoundDistanceInKM = Convert.ToDecimal(trip.InBoundDistanceInMeters / 1000.0);
        //                            inBoundDistanceFare = Convert.ToDecimal(trip.InBoundDistanceFare);
        //                            inBoundTimeInMinutes = Convert.ToDecimal(trip.InBoundTimeInSeconds / 60.0);
        //                            inBoundTimeFare = Convert.ToDecimal(trip.InBoundTimeFare);
        //                            outBoundDistanceInKM = Convert.ToDecimal(trip.OutBoundDistanceInMeters / 1000.0);
        //                            outBoundDistanceFare = Convert.ToDecimal(trip.OutBoundDistanceFare);
        //                            outBoundTimeInMinutes = Convert.ToDecimal(trip.OutBoundTimeInSeconds / 60.0);
        //                            outBoundTimeFare = Convert.ToDecimal(trip.OutBoundTimeFare);
        //                            inBoundSurchargeAmount = Convert.ToDecimal(trip.InBoundSurchargeAmount);
        //                            outBoundSurchargeAmount = Convert.ToDecimal(trip.OutBoundSurchargeAmount);
        //                            inBoundBaseFare = Convert.ToDecimal(trip.InBoundBaseFare);
        //                            outBoundBaseFare = Convert.ToDecimal(trip.OutBoundBaseFare);

        //                            totalFare = inBoundDistanceFare + inBoundTimeFare + inBoundBaseFare + inBoundSurchargeAmount +
        //                                        outBoundDistanceFare + outBoundTimeFare + outBoundBaseFare + outBoundSurchargeAmount;

        //                            polyLine = GetPolyLineFromFireBase(polyLine, trip.TripID.ToString(), trip.CaptainID.ToString());
        //                            //}
        //                        }
        //                        else
        //                        {
        //                            //Get ployline ordered lat longs from firebase, (Trips / TripID / CaptainID / polyline / id /latitude + longitude + locationTime (diff / 1000 = seconds)) use first lat long to set pickup faremanager.
        //                            //Traverse ploygon and find boundary point. 
        //                            //Get inbound/outbund time using difference of polyline lat long.
        //                            //Get inbound/outbund distance using difference of polyline lat long.

        //                            var trip = context.Trips.Where(t => t.TripID == TripId).FirstOrDefault();

        //                            FireBaseController fb = new FireBaseController();
        //                            var ployLineLocations = fb.getTripPolyLineDetails("Trips/" + trip.TripID.ToString() + "/" + trip.CaptainID.ToString() + "/" + "polyline");

        //                            var inBoundDistanceInMeters = 0.0;
        //                            var inBoundTimeInSeconds = 0.0;
        //                            var outBoundDistanceInMeters = 0.0;
        //                            var outBoundTimeInSeconds = 0.0;

        //                            bool isGoneOutBound = false;
        //                            GeoCoordinate lastPosition = null;
        //                            DateTime lastLocationUpdateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

        //                            foreach (var location in ployLineLocations)
        //                            {
        //                                polyLine += location.Value.latitude + "," + location.Value.longitude + "|";

        //                                if (!isGoneOutBound)
        //                                {
        //                                    if (Polygon.IsLatLonExistsInPolygon(Polygon.ConvertLatLonObjectsArrayToPolygonString(pickUpAreaFareManager.AreaLatLong), location.Value.latitude, location.Value.longitude))
        //                                    {
        //                                        if (lastPosition != null)
        //                                        {
        //                                            var currentPosition = new GeoCoordinate(Convert.ToDouble(location.Value.latitude), Convert.ToDouble(location.Value.longitude));
        //                                            inBoundDistanceInMeters += currentPosition.GetDistanceTo(lastPosition);  //distance in meters

        //                                            DateTime currentLocationUpdateTime = DateTimeOffset.FromUnixTimeMilliseconds(location.Value.locationTime).UtcDateTime;
        //                                            inBoundTimeInSeconds += (currentLocationUpdateTime - lastLocationUpdateTime).TotalSeconds;
        //                                        }
        //                                    }
        //                                    else
        //                                    {
        //                                        isGoneOutBound = true;
        //                                        isMorningShift = DateTime.UtcNow.TimeOfDay >= pickUpAreaFareManager.MorningShiftStartTime && DateTime.UtcNow.TimeOfDay <= pickUpAreaFareManager.MorningShiftEndTime;
        //                                        inBoundDistanceInKM = Convert.ToDecimal(inBoundDistanceInMeters / 1000.0);
        //                                        inBoundTimeInMinutes = Convert.ToDecimal(inBoundTimeInSeconds / 60.0);

        //                                        CalculateDistanceAndTimeFare(pickUpAreaFareManager.FareManagerID.ToString(),
        //                                            isMorningShift,
        //                                            inBoundDistanceInKM,
        //                                            (decimal)(isMorningShift ? pickUpAreaFareManager.MorningPerKmFare : pickUpAreaFareManager.EveningPerKmFare),
        //                                            inBoundTimeInMinutes,
        //                                            (decimal)(isMorningShift ? pickUpAreaFareManager.MorningPerMinFare : pickUpAreaFareManager.EveningPerMinFare),
        //                                            out inBoundDistanceFare,
        //                                            out inBoundTimeFare);

        //                                        inBoundBaseFare = Convert.ToDecimal(isMorningShift ? pickUpAreaFareManager.MorningBaseFare : pickUpAreaFareManager.EveningBaseFare);
        //                                        inBoundTotalFare = inBoundBaseFare + inBoundDistanceFare + inBoundTimeFare;
        //                                        inBoundSurchargeAmount = Convert.ToDecimal(inBoundTotalFare * (isMorningShift ? pickUpAreaFareManager.MorningSurcharge : pickUpAreaFareManager.EveningSurcharge) / 100);
        //                                    }
        //                                }
        //                                else
        //                                {
        //                                    var currentPosition = new GeoCoordinate(Convert.ToDouble(location.Value.latitude), Convert.ToDouble(location.Value.longitude));
        //                                    outBoundDistanceInMeters += currentPosition.GetDistanceTo(lastPosition);  //distance in meters

        //                                    DateTime currentLocationUpdateTime = DateTimeOffset.FromUnixTimeMilliseconds(location.Value.locationTime).UtcDateTime;
        //                                    outBoundTimeInSeconds += (currentLocationUpdateTime - lastLocationUpdateTime).TotalSeconds;

        //                                }
        //                                lastPosition = new GeoCoordinate(Convert.ToDouble(location.Value.latitude), Convert.ToDouble(location.Value.longitude));
        //                                lastLocationUpdateTime = DateTimeOffset.FromUnixTimeMilliseconds(location.Value.locationTime).UtcDateTime;
        //                            }

        //                            polyLine = polyLine.Length > 0 ? polyLine.Remove(polyLine.Length - 1) : "";

        //                            isMorningShift = DateTime.UtcNow.TimeOfDay >= dropOffAreaFareManager.MorningShiftStartTime && DateTime.UtcNow.TimeOfDay <= dropOffAreaFareManager.MorningShiftEndTime;

        //                            outBoundDistanceInKM = Convert.ToDecimal(outBoundDistanceInMeters / 1000.0);
        //                            outBoundTimeInMinutes = Convert.ToDecimal(outBoundTimeInSeconds / 60.0);

        //                            CalculateDistanceAndTimeFare(dropOffAreaFareManager.FareManagerID.ToString(),
        //                                isMorningShift,
        //                                outBoundDistanceInKM,
        //                                (decimal)(isMorningShift ? dropOffAreaFareManager.MorningPerKmFare : dropOffAreaFareManager.EveningPerKmFare),
        //                                outBoundTimeInMinutes,
        //                                (decimal)(isMorningShift ? dropOffAreaFareManager.MorningPerMinFare : dropOffAreaFareManager.EveningPerMinFare),
        //                                out outBoundDistanceFare,
        //                                out outBoundTimeFare);

        //                            outBoundBaseFare = Convert.ToDecimal(isMorningShift ? dropOffAreaFareManager.MorningBaseFare : dropOffAreaFareManager.EveningBaseFare);
        //                            outBoundTotalFare = outBoundBaseFare + outBoundDistanceFare + outBoundTimeFare;
        //                            outBoundSurchargeAmount = Convert.ToDecimal(outBoundTotalFare * (isMorningShift ? dropOffAreaFareManager.MorningSurcharge : dropOffAreaFareManager.EveningSurcharge) / 100);

        //                            totalFare = inBoundTotalFare + inBoundSurchargeAmount + outBoundTotalFare + outBoundSurchargeAmount;
        //                        }
        //                    }
        //                    else //Fare Estimate API call
        //                    {
        //                        //Get Polyline from App, traverse and get boundary point. 
        //                        //Get inbound / outbound time and distance from distance api. 
        //                        //Apply fare ranges according to relevant fare managers.

        //                        GeoCoordinate outBoundFirstPosition = null;
        //                        //Need to hit distance api for time estimate, so no need to calcualte manual distance in this case - will be received from api

        //                        foreach (var point in routePolyLine.Split('|'))
        //                        {
        //                            if (!Polygon.IsLatLonExistsInPolygon(Polygon.ConvertLatLonObjectsArrayToPolygonString(pickUpAreaFareManager.AreaLatLong), point.Split(',')[0], point.Split(',')[1]))
        //                            {
        //                                var result123 = DistanceMatrixAPI.GetTimeAndDistance(pickUpLatitude + "," + pickUpLongitude, point.Split(',')[0] + "," + point.Split(',')[1]);
        //                                isMorningShift = DateTime.UtcNow.TimeOfDay >= pickUpAreaFareManager.MorningShiftStartTime && DateTime.UtcNow.TimeOfDay <= pickUpAreaFareManager.MorningShiftEndTime;

        //                                inBoundDistanceInKM = Convert.ToDecimal(result123.distanceInMeters / 1000.0);
        //                                inBoundTimeInMinutes = Convert.ToDecimal(result123.durationInSeconds / 60.0);

        //                                CalculateDistanceAndTimeFare(pickUpAreaFareManager.FareManagerID.ToString(),
        //                                    isMorningShift,
        //                                    inBoundDistanceInKM,
        //                                    (decimal)(isMorningShift ? pickUpAreaFareManager.MorningPerKmFare : pickUpAreaFareManager.EveningPerKmFare),
        //                                    inBoundTimeInMinutes,
        //                                    (decimal)(isMorningShift ? pickUpAreaFareManager.MorningPerMinFare : pickUpAreaFareManager.EveningPerMinFare),
        //                                    out inBoundDistanceFare,
        //                                    out inBoundTimeFare);

        //                                inBoundBaseFare = Convert.ToDecimal(isMorningShift ? pickUpAreaFareManager.MorningBaseFare : pickUpAreaFareManager.EveningBaseFare);
        //                                inBoundTotalFare = inBoundBaseFare + inBoundDistanceFare + inBoundTimeFare;
        //                                inBoundSurchargeAmount = Convert.ToDecimal(inBoundTotalFare * (isMorningShift ? pickUpAreaFareManager.MorningSurcharge : pickUpAreaFareManager.EveningSurcharge) / 100);

        //                                outBoundFirstPosition = new GeoCoordinate(Convert.ToDouble(point.Split(',')[0]), Convert.ToDouble(point.Split(',')[1]));

        //                                break;
        //                            }
        //                        }

        //                        isMorningShift = DateTime.UtcNow.TimeOfDay >= dropOffAreaFareManager.MorningShiftStartTime && DateTime.UtcNow.TimeOfDay <= dropOffAreaFareManager.MorningShiftEndTime;

        //                        var result = DistanceMatrixAPI.GetTimeAndDistance(outBoundFirstPosition.Latitude.ToString() + "," + outBoundFirstPosition.Longitude.ToString(), dropOffLatitude + "," + dropOffLongitude);
        //                        outBoundDistanceInKM = Convert.ToDecimal(result.distanceInMeters / 1000.0);
        //                        outBoundTimeInMinutes = Convert.ToDecimal(result.durationInSeconds / 60.0);

        //                        CalculateDistanceAndTimeFare(dropOffAreaFareManager.FareManagerID.ToString(),
        //                            isMorningShift,
        //                            outBoundDistanceInKM,
        //                            (decimal)(isMorningShift ? dropOffAreaFareManager.MorningPerKmFare : dropOffAreaFareManager.EveningPerKmFare),
        //                            outBoundTimeInMinutes,
        //                            (decimal)(isMorningShift ? dropOffAreaFareManager.MorningPerMinFare : dropOffAreaFareManager.EveningPerMinFare),
        //                            out outBoundDistanceFare,
        //                            out outBoundTimeFare);

        //                        outBoundBaseFare = Convert.ToDecimal(isMorningShift ? dropOffAreaFareManager.MorningBaseFare : dropOffAreaFareManager.EveningBaseFare);
        //                        outBoundTotalFare = outBoundBaseFare + outBoundDistanceFare + outBoundTimeFare;
        //                        outBoundSurchargeAmount = Convert.ToDecimal(outBoundTotalFare * (isMorningShift ? dropOffAreaFareManager.MorningSurcharge : dropOffAreaFareManager.EveningSurcharge) / 100);

        //                        totalFare = inBoundTotalFare + inBoundSurchargeAmount + outBoundTotalFare + outBoundSurchargeAmount;
        //                        polyLine = routePolyLine;
        //                    }
        //                }
        //            }

        //            //Trip minimum amount should be 6.6
        //            if (totalFare <= 6.6M)
        //            {
        //                totalFare = 6.60M;
        //                formattedTotalFare = 6.60M;
        //            }
        //            else
        //            {
        //                //totalFare = FormatFareValue(totalFare);
        //                formattedTotalFare = FormatFareValue(totalFare);

        //                inBoundBaseFare += (formattedTotalFare - totalFare); //Adjust fractional parts, for trips history
        //            }

        //            dic["inBoundDistanceInKM"] = string.Format("{0:0.00}", inBoundDistanceInKM);
        //            dic["inBoundTimeInMinutes"] = string.Format("{0:0.00}", inBoundTimeInMinutes);
        //            dic["outBoundDistanceInKM"] = string.Format("{0:0.00}", outBoundDistanceInKM);
        //            dic["outBoundTimeInMinutes"] = string.Format("{0:0.00}", outBoundTimeInMinutes);

        //            //vehicle with 6,8 seating capacity should be charged 2 euro extra
        //            dic["inBoundBaseFare"] = string.Format("{0:0.00}", totalFare > 6.6M ? seatingCapacity > 4 ? inBoundBaseFare + 2 : inBoundBaseFare : 6.6M);
        //            dic["inBoundDistanceFare"] = string.Format("{0:0.00}", totalFare > 6.6M ? inBoundDistanceFare : 0);
        //            dic["inBoundTimeFare"] = string.Format("{0:0.00}", totalFare > 6.6M ? inBoundTimeFare : 0);
        //            dic["inBoundSurchargeAmount"] = string.Format("{0:0.00}", totalFare > 6.6M ? inBoundSurchargeAmount : 0);
        //            dic["outBoundDistanceFare"] = string.Format("{0:0.00}", totalFare > 6.6M ? outBoundDistanceFare : 0);
        //            dic["outBoundTimeFare"] = string.Format("{0:0.00}", totalFare > 6.6M ? outBoundTimeFare : 0);
        //            dic["outBoundSurchargeAmount"] = string.Format("{0:0.00}", totalFare > 6.6M ? outBoundSurchargeAmount : 0);
        //            dic["outBoundBaseFare"] = string.Format("{0:0.00}", totalFare > 6.6M ? outBoundBaseFare : 0);
        //            dic["polyLine"] = polyLine;
        //        }

        //        return formattedTotalFare;
        //    }
        //}


        #endregion

    }
}
