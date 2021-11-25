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
        public static async Task<DiscountTypeDTO> IsSpecialPromotionApplicable(string pickUplatitude, string pickUplongitude,
            string dropOfflatitude, string dropOfflongitude,
            string applicationID, bool isLaterBooking = false, DateTime? dt = null)
        {
            using (var dbContext = new CangooEntities())
            {
                dt = isLaterBooking ? dt : DateTime.UtcNow;
                //var specialPromo = await context.PromoManagers.Where(p => p.ApplicationID.ToString() == applicationID && p.isSpecialPromo == true && p.StartDate <= dt && p.ExpiryDate >= dt).ToListAsync();
                var specialPromo = dbContext.PromoManagers.Where(p => p.ApplicationID.ToString() == applicationID && p.isSpecialPromo == true && p.StartDate <= dt && p.ExpiryDate >= dt).ToList();

                var result = new DiscountTypeDTO();
                if (specialPromo.Any())
                {
                    foreach (var promotion in specialPromo)
                    {
                        if (PolygonService.IsLatLonExistsInPolygon(PolygonService.ConvertLatLonObjectsArrayToPolygonString(promotion.PickupLocation), pickUplatitude, pickUplongitude) &&
                            PolygonService.IsLatLonExistsInPolygon(PolygonService.ConvertLatLonObjectsArrayToPolygonString(promotion.DropOffLocation), dropOfflatitude, dropOfflongitude))
                        {
                            //dic["discountType"] = "special";
                            //dic["discountAmount"] = string.Format("{0:0.00}", (decimal)promotion.Amount);
                            //promotionID = promotion.PromoID.ToString();
                            result.DiscountType = "special";
                            result.DiscountAmount = string.Format("{0:0.00}", (decimal)promotion.Amount);
                            result.PromoCodeId = promotion.PromoID.ToString();
                            break;
                        }
                    }
                }
                return result;
            }
        }

        public static string ApplyPromoCode(string applicationID, string userID, ref DriverEndTripResponse result)
        {
            using (var dbContext = new CangooEntities())
            {
                DateTime dt = DateTime.UtcNow;
                var availablePromoCodes = dbContext.PromoManagers.Where(p => p.ApplicationID.ToString() == applicationID && p.isSpecialPromo == false && p.StartDate <= dt && p.ExpiryDate >= dt).OrderBy(p => p.StartDate).ToList();

                string promoCodeID = "";

                if (availablePromoCodes.Any())
                {
                    var appliedPromoCode = dbContext.UserPromos.Where(up => up.UserID == userID && up.isActive == true).FirstOrDefault();
                    if (appliedPromoCode != null)
                    {
                        //Business Rule: Only one promo can be applied 
                        if (availablePromoCodes.Exists(p => p.PromoID == appliedPromoCode.PromoID))
                        {
                            var promo = availablePromoCodes.Find(p => p.PromoID == appliedPromoCode.PromoID);

                            if (appliedPromoCode.NoOfUsage < promo.Repetition)
                            {

                                if (promo.isFixed == true)
                                {
                                    result.discountType = "fixed";
                                    result.discountAmount = string.Format("{0:0.00}", (decimal)promo.Amount);
                                }
                                else
                                {
                                    result.discountType = "percentage";
                                    result.discountAmount = string.Format("{0:0.00}", (decimal)promo.Amount);
                                }
                                promoCodeID = promo.PromoID.ToString();
                            }
                        }
                    }
                }
                return promoCodeID;
            }
        }

        public static async Task<EstimateFareResponse> GetFareEstimate(
            string pickUpPostalCode, string pickUpLatitude, string pickUpLongitude,
            string midwayStop1PostalCode, string midwayStop1Latitude, string midwayStop1Longitude,
            string dropOffPostalCode, string dropOffLatitude, string dropOffLongitutde,
            string polyLine, string inBoundTimeInSeconds, string inBoundDistanceInMeters, 
            string outBoundTimeInSeconds, string outBoundDistanceInMeters)
        {
            var result = await PrepareResponseObject(polyLine, inBoundTimeInSeconds, inBoundDistanceInMeters, outBoundTimeInSeconds, outBoundDistanceInMeters);

            var applicationId = ConfigurationManager.AppSettings["ApplicationID"].ToString();

            using (CangooEntities dbContext = new CangooEntities())
            {
                var areasCategoryFareList = await GetApplicationRideServicesAreaCategoryFareList(applicationId);

                var onlineDrivers =  await FirebaseService.GetOnlineDrivers();
                if (areasCategoryFareList.Any())
                {
                    #region Step 1 : Identify Pickup / Midway / Dropoff areas

                    //TBD : Get only specified areas with ids (Optimization)
                    var areasList = await GetApplicationRideServiceAreasList(applicationId);

                    RideServicesArea pickUpServiceArea = new RideServicesArea();
                    RideServicesArea midWayServiceArea = new RideServicesArea();
                    RideServicesArea dropOffServiceArea = new RideServicesArea();

                    foreach (var area in areasList)
                    {
                        if (PolygonService.IsLatLonExistsInPolygon(PolygonService.ConvertLatLonObjectsArrayToPolygonString(area.AreaLatLong), pickUpLatitude, pickUpLongitude))
                        {
                            pickUpServiceArea = area;
                        }

                        if (!string.IsNullOrEmpty(midwayStop1Latitude))
                        {

                            if (PolygonService.IsLatLonExistsInPolygon(PolygonService.ConvertLatLonObjectsArrayToPolygonString(area.AreaLatLong), midwayStop1Latitude, midwayStop1Longitude))
                            {
                                midWayServiceArea = area;
                            }
                        }

                        if (PolygonService.IsLatLonExistsInPolygon(PolygonService.ConvertLatLonObjectsArrayToPolygonString(area.AreaLatLong), dropOffLatitude, dropOffLongitutde))
                        {
                            dropOffServiceArea = area;
                        }

                        if (string.IsNullOrEmpty(midwayStop1Latitude))
                        {
                            if (!pickUpServiceArea.AreaID.Equals(Guid.Empty) &&
                                !dropOffServiceArea.AreaID.Equals(Guid.Empty))
                            {
                                break;
                            }
                        }
                        else
                        {
                            if (!pickUpServiceArea.AreaID.Equals(Guid.Empty) &&
                                !midWayServiceArea.AreaID.Equals(Guid.Empty) &&
                                !dropOffServiceArea.AreaID.Equals(Guid.Empty))
                            {
                                break;
                            }
                        }
                    }

                    #endregion

                    //TBD : Special Promotion
                    //TBD : If a category is not added in an area

                    //If all cases fails then it means either request area doesn't exist in system or algorithm failed to identify

                    #region Step 2 : Calculate Vehicle Category fare in identified areas

                    if (string.IsNullOrEmpty(midwayStop1PostalCode) && pickUpServiceArea.AreaID != Guid.Empty && dropOffServiceArea.AreaID != Guid.Empty)
                    {
                        //No midway, so there will be no outbound fare calculation. outBoundFareManagerId will be null

                        if (areasCategoryFareList.Where(ac => ac.AreaID == pickUpServiceArea.AreaID).Select(ac => ac.AreaID).Distinct().Count() == 1) //All categories may have same fare manager for selected area
                        {
                            var inBoundFareManagerId = (Guid)areasCategoryFareList.Where(ac => ac.AreaID == pickUpServiceArea.AreaID).FirstOrDefault().RSFMID;
                            await SetFareBreakDownAndETA(onlineDrivers, result, inBoundFareManagerId, false, null, false, "", pickUpLatitude, pickUpLongitude);
                        }
                        else
                        {
                            await SetCategoryWiseFareDetails(onlineDrivers, result, areasCategoryFareList, pickUpServiceArea.AreaID, false, null, pickUpLatitude, pickUpLongitude);
                        }
                    }
                    else if (!string.IsNullOrEmpty(midwayStop1PostalCode) && pickUpServiceArea.AreaID != Guid.Empty && midWayServiceArea.AreaID != Guid.Empty && dropOffServiceArea.AreaID != Guid.Empty)
                    {
                        if (areasCategoryFareList.Where(ac => ac.AreaID == pickUpServiceArea.AreaID).Select(ac => ac.AreaID).Distinct().Count() == 1 &&
                            areasCategoryFareList.Where(ac => ac.AreaID == midWayServiceArea.AreaID).Select(ac => ac.AreaID).Distinct().Count() == 1)
                        {
                            var inBoundFareManagerId = (Guid)areasCategoryFareList.Where(ac => ac.AreaID == pickUpServiceArea.AreaID).FirstOrDefault().RSFMID;
                            var outBoundFareManagerId = (Guid)areasCategoryFareList.Where(ac => ac.AreaID == midWayServiceArea.AreaID).FirstOrDefault().RSFMID;
                            await SetFareBreakDownAndETA(onlineDrivers, result, inBoundFareManagerId, true, outBoundFareManagerId, false, "", pickUpLatitude, pickUpLongitude);
                        }
                        else
                        {
                            await SetCategoryWiseFareDetails(onlineDrivers, result, areasCategoryFareList, pickUpServiceArea.AreaID, true, midWayServiceArea.AreaID, pickUpLatitude, pickUpLongitude);
                        }
                    }

                    #endregion
                }

                var districtZonesList = await GetApplicationCourierServicesDistrictsZoneList(applicationId);

                if (districtZonesList.Any())
                {
                    if (string.IsNullOrEmpty(midwayStop1PostalCode))
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
                                ETA = VehiclesService.GetETAByCategoryId(onlineDrivers, ((int)VehicleCategories.Standard).ToString(), pickUpLatitude, pickUpLongitude)
                            };
                        }
                    }
                    else
                    {
                        var firstZoneId = GetZoneIdByPostCodes(pickUpPostalCode, midwayStop1PostalCode, districtZonesList);
                        var secondZoneId = GetZoneIdByPostCodes(midwayStop1PostalCode, dropOffPostalCode, districtZonesList);

                        if (firstZoneId != null && secondZoneId != null)
                        {
                            var firstZone = await GetApplicationCourierServicesZoneById((int)firstZoneId);
                            var secondZone = await GetApplicationCourierServicesZoneById((int)secondZoneId);
                            result.Courier = new CourierFareEstimate
                            {
                                Amount = (firstZone.Price + secondZone.Price).ToString(),
                                Zones = firstZone.ZoneName + " - " + secondZone.ZoneName,
                                ETA = VehiclesService.GetETAByCategoryId(onlineDrivers, ((int)VehicleCategories.Standard).ToString(), pickUpLatitude, pickUpLongitude)
                            };
                        }
                    }
                }
            }

            return result;
        }

        private static async Task<EstimateFareResponse> PrepareResponseObject(string polyLine,
            string inBoundTimeInSeconds, string inBoundDistanceInMeters,
            string outBoundTimeInSeconds, string outBoundDistanceInMeters)
        {
            return new EstimateFareResponse
            {
                PolyLine = polyLine,
                InBoundDistanceInMeters = inBoundDistanceInMeters,
                InBoundTimeInSeconds = inBoundTimeInSeconds,
                OutBoundDistanceInMeters = outBoundDistanceInMeters,
                OutBoundTimeInSeconds = outBoundTimeInSeconds,
                Categories = new List<VehicleCategoryFareEstimate>
                {
                    new VehicleCategoryFareEstimate
                    {
                        CategoryId = ((int)VehicleCategories.Standard).ToString()
                    },
                    new VehicleCategoryFareEstimate
                    {
                        CategoryId = ((int)VehicleCategories.Comfort).ToString()
                    },
                    new VehicleCategoryFareEstimate
                    {
                        CategoryId = ((int)VehicleCategories.Premium).ToString()
                    },
                    new VehicleCategoryFareEstimate
                    {
                        CategoryId = ((int)VehicleCategories.Grossraum).ToString()
                    },
                    new VehicleCategoryFareEstimate
                    {
                        CategoryId = ((int)VehicleCategories.GreenTaxi).ToString()
                    }
                },
                Courier = new CourierFareEstimate(),
                Facilities = await FacilitiesService.GetPassengerFacilitiesList()
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

        private static async Task<RideServicesFareManager> GetFareManagerById(Guid fareManagerId)
        {
            using (var dbContext = new CangooEntities())
            {
                return await dbContext.RideServicesFareManagers.Where(f => f.RSFMID == fareManagerId).FirstOrDefaultAsync();
            }
        }

        private static bool CheckShift(TimeSpan morningShiftStartTime, TimeSpan morningShiftEndTime)
        {
            return DateTime.UtcNow.TimeOfDay >= morningShiftStartTime && DateTime.UtcNow.TimeOfDay <= morningShiftEndTime;
        }

        private static async Task<bool> WeekendOrHoliday()
        {
            if (DateTime.UtcNow.DayOfWeek == DayOfWeek.Saturday || DateTime.UtcNow.DayOfWeek == DayOfWeek.Sunday)
                return true;

            using (var dbContext = new CangooEntities())
            {
                var today = DateTime.Now.Date;
                return await dbContext.PublicHolidays.Where(ph => ph.IsActive == true && ph.Date == today).AnyAsync();
            }
        }

        private static async Task SetFareBreakDownAndETA(Dictionary<string, FirebaseDriver> onlineDrivers, EstimateFareResponse result, Guid inBoundFareManagerId, bool isOutBound, Guid? outBoundFareManagerId, bool IsSingleCategory, string categoryId, string pickUpLatitude, string pickUpLongitude)
        {
            FareBreakDownDTO fareDetails = new FareBreakDownDTO();

            fareDetails = await CalcuateFareBreakdown(inBoundFareManagerId, result.InBoundDistanceInMeters, result.InBoundTimeInSeconds, isOutBound, isOutBound ? outBoundFareManagerId : null, isOutBound ? result.OutBoundDistanceInMeters : "0", isOutBound ? result.OutBoundTimeInSeconds : "0");

            if (!IsSingleCategory)
            {
                //Same fare will be set for every category
                result.Categories
                    .Select(c =>
                    {
                        return SetVehicleCategoryFare(inBoundFareManagerId, outBoundFareManagerId, c, fareDetails);
                    }).ToList();

                //Each catgeory will have different ETA
                foreach (var item in result.Categories)
                {
                    item.ETA = VehiclesService.GetETAByCategoryId(onlineDrivers, item.CategoryId, pickUpLatitude, pickUpLongitude);
                }
            }
            else
            {
                result.Categories
                  .Where(c => c.CategoryId.Equals(categoryId.ToString()))
                  .Select(async c =>
                  {
                      c.ETA = VehiclesService.GetETAByCategoryId(onlineDrivers, categoryId.ToString(), pickUpLatitude, pickUpLongitude);
                      return SetVehicleCategoryFare(inBoundFareManagerId, outBoundFareManagerId, c, fareDetails);
                  }).ToList();
            }
        }

        private static async Task CalculateFareWithCategoryInOutBoundFareManagers(Dictionary<string, FirebaseDriver> onlineDrivers, EstimateFareResponse result, List<RideServicesAreaCategoryFare> areasCategoryFareList, Guid pickUpAreaId, int categoryId, bool isOutBound, Guid? midWayAreaId, string pickUpLatitude, string pickUpLongitude)
        {
            var inBoundFareManagerId = (Guid)areasCategoryFareList.Where(ac => ac.AreaID == pickUpAreaId && ac.CategoryID == categoryId).FirstOrDefault().RSFMID;
            var outBoundFareManagerId = areasCategoryFareList.Where(ac => ac.AreaID == midWayAreaId && ac.CategoryID == categoryId).FirstOrDefault();

            await SetFareBreakDownAndETA(onlineDrivers, result, inBoundFareManagerId, isOutBound, outBoundFareManagerId?.RSFMID, true, categoryId.ToString(), pickUpLatitude, pickUpLongitude);
        }

        private static VehicleCategoryFareEstimate SetVehicleCategoryFare(Guid inBoundFareManagerId, Guid? outBoundFareManagerId, VehicleCategoryFareEstimate category, FareBreakDownDTO fareDetails)
        {
            category.TotalFare = fareDetails.TotalFare;
            category.FormattingAdjustment = fareDetails.FormattingAdjustment;

            category.InBoundRSFMId = inBoundFareManagerId.ToString();
            category.BaseFare = fareDetails.BaseFare;
            category.BookingFare = fareDetails.BookingFare;
            category.WaitingFare = fareDetails.WaitingFare;
            category.SurchargeAmount = fareDetails.SurchargeAmount;
            category.InBoundDistanceFare = fareDetails.InBoundDistanceFare;
            category.InBoundTimeFare = fareDetails.InBoundTimeFare;

            category.OutBoundRSFMId = outBoundFareManagerId.ToString();
            category.OutBoundDistanceFare = fareDetails.OutBoundDistanceFare;
            category.OutBoundTimeFare = fareDetails.OutBoundTimeFare;

            return category;
        }

        private static async Task<FareBreakDownDTO> CalcuateFareBreakdown(Guid inBoundFareManagerId, string inBoundDistanceInMeters, string inBoundTimeInSeconds, bool isOutBound, Guid? outBoundFareManagerId, string outBoundDistanceInMeters, string outBoundTimeInSeconds)
        {
            var result = new FareBreakDownDTO();
            //decimal baseFare = 0, waitingFare = 0, bookingFare = 0, surchargeAmount = 0;

            var fareManager = await GetFareManagerById(inBoundFareManagerId);
            // TBD: Check date from public holidays lookup table
            bool isWeekend = await WeekendOrHoliday();
            bool isMorningShift = CheckShift((TimeSpan)fareManager.MorningShiftStartTime, (TimeSpan)fareManager.MorningShiftEndTime);

            var distanceAndTimeFare = await CalculateDistanceAndTimeFare(fareManager.RSFMID.ToString(),
                 isWeekend ? (int)FareManagerShifts.Weekend : isMorningShift ? (int)FareManagerShifts.Morning : (int)FareManagerShifts.Evening,
                 inBoundDistanceInMeters,
                 (decimal)(isWeekend ? fareManager.WeekendPerKmFare : isMorningShift ? fareManager.MorningPerKmFare : fareManager.EveningPerKmFare),
                 inBoundTimeInSeconds,
                 (decimal)(isWeekend ? fareManager.WeekendPerMinFare : isMorningShift ? fareManager.MorningPerMinFare : fareManager.EveningPerMinFare));

            result.BaseFare = (isWeekend ? (decimal)fareManager.WeekendBaseFare : isMorningShift ? (decimal)fareManager.MorningBaseFare : (decimal)fareManager.EveningBaseFare).ToString("0.00");
            result.WaitingFare = (isWeekend ? (decimal)fareManager.WeekendWaitingFare : isMorningShift ? (decimal)fareManager.MorningWaitingFare : (decimal)fareManager.EveningWaitingFare).ToString("0.00");
            result.BookingFare = (isWeekend ? (decimal)fareManager.WeekendBookingFare : isMorningShift ? (decimal)fareManager.MorningBookingFare : (decimal)fareManager.EveningBookingFare).ToString("0.00");
            result.InBoundDistanceFare = distanceAndTimeFare.DistanceFare.ToString("0.00");
            result.InBoundTimeFare = distanceAndTimeFare.TimeFare.ToString("0.00");

            if (isOutBound)
            {
                if (inBoundFareManagerId != outBoundFareManagerId)
                {
                    fareManager = await GetFareManagerById((Guid)outBoundFareManagerId);
                    isMorningShift = CheckShift((TimeSpan)fareManager.MorningShiftStartTime, (TimeSpan)fareManager.MorningShiftEndTime);
                }

                distanceAndTimeFare = await CalculateDistanceAndTimeFare(fareManager.RSFMID.ToString(),
                    isWeekend ? (int)FareManagerShifts.Weekend : isMorningShift ? (int)FareManagerShifts.Morning : (int)FareManagerShifts.Evening,
                    outBoundDistanceInMeters,
                    (decimal)(isWeekend ? fareManager.WeekendPerKmFare : isMorningShift ? fareManager.MorningPerKmFare : fareManager.EveningPerKmFare),
                    outBoundTimeInSeconds,
                    (decimal)(isWeekend ? fareManager.WeekendPerMinFare : isMorningShift ? fareManager.MorningPerMinFare : fareManager.EveningPerMinFare));

                result.OutBoundDistanceFare = distanceAndTimeFare.DistanceFare.ToString("0.00"); ;
                result.OutBoundTimeFare = distanceAndTimeFare.TimeFare.ToString("0.00"); ;
            }

            result.TotalFare = (decimal.Parse(result.BaseFare) + decimal.Parse(result.BookingFare) + decimal.Parse(result.WaitingFare) +
                decimal.Parse(result.InBoundDistanceFare) + decimal.Parse(result.InBoundTimeFare) + decimal.Parse(result.OutBoundDistanceFare) + decimal.Parse(result.OutBoundTimeFare)).ToString("0.00");

            result.SurchargeAmount = Convert.ToDecimal(decimal.Parse(result.TotalFare) * (isWeekend ? (decimal)fareManager.WeekendSurcharge : isMorningShift ? (decimal)fareManager.MorningSurcharge : (decimal)fareManager.EveningSurcharge) / 100).ToString("0.00");
            result.TotalFare = (decimal.Parse(result.TotalFare) + decimal.Parse(result.SurchargeAmount)).ToString();

            //Trip minimum amount should be 6.6
            if (decimal.Parse(result.TotalFare) <= 6.6M)
            {
                result.TotalFare = 6.60M.ToString("0.00");
            }

            var formattedFare = FormatFareValue(decimal.Parse(result.TotalFare));
            result.FormattingAdjustment = (formattedFare - decimal.Parse(result.TotalFare)).ToString("0.00");
            result.TotalFare = formattedFare.ToString("0.00");
            return result;
        }

        private static async Task<DistanceAndTimeFareDTO> CalculateDistanceAndTimeFare(string rideServicesID, int shiftId, string distanceInMeters, decimal perKMFare, string timeInSeconds, decimal perMinFare)
        {
            using (var dbContext = new CangooEntities())
            {
                #region distance fare calculation

                var distanceInKM = Convert.ToDecimal(distanceInMeters) / 1000.0M;
                var timeInMinutes = Convert.ToDecimal(timeInSeconds) / 60.0M;

                var result = new DistanceAndTimeFareDTO();

                //distance ranges are saved in kilo meters
                var lstFareDistanceRange = await dbContext.RideServicesDistanceRanges
                    .Where(f => f.ShiftID == shiftId && f.RSFMID.ToString().Equals(rideServicesID))
                    .OrderBy(f => f.Range)
                    .ToListAsync();

                if (lstFareDistanceRange.Any())
                {
                    foreach (var ran in lstFareDistanceRange)
                    {
                        var arrRange = ran.Range.Split(';');

                        if (Convert.ToDecimal(arrRange[1]) <= distanceInKM)
                        {
                            result.DistanceFare += Convert.ToDecimal(ran.Charges) * (Convert.ToDecimal(arrRange[1]) - Convert.ToDecimal(arrRange[0]));
                        }
                        else
                        {
                            result.DistanceFare += Convert.ToDecimal(ran.Charges) * (distanceInKM - Convert.ToDecimal(arrRange[0]));
                            break;
                        }

                    }
                }
                else
                {
                    result.DistanceFare = distanceInKM * perKMFare;
                }

                #endregion

                #region time fare calculation


                //distance ranges are saved in minutes
                var lstFareTimeRange = await dbContext.RideServicesTimeRanges
                    .Where(f => f.ShiftID == shiftId && f.RSFMID.ToString().Equals(rideServicesID))
                    .OrderBy(f => f.Range)
                    .ToListAsync();

                if (lstFareTimeRange.Any())
                {
                    foreach (var ran in lstFareTimeRange)
                    {
                        var arrRange = ran.Range.Split(';');

                        if (Convert.ToDecimal(arrRange[1]) <= timeInMinutes)
                        {
                            result.TimeFare += Convert.ToDecimal(ran.Charges) * (Convert.ToDecimal(arrRange[1]) - Convert.ToDecimal(arrRange[0]));
                        }
                        else
                        {
                            result.TimeFare += Convert.ToDecimal(ran.Charges) * (timeInMinutes - Convert.ToDecimal(arrRange[0]));
                            break;
                        }
                    }
                }
                else
                {
                    result.TimeFare = timeInMinutes * perMinFare;
                }

                #endregion

                return result;
            }
        }

        private static async Task SetCategoryWiseFareDetails(Dictionary<string, FirebaseDriver> onlineDrivers, EstimateFareResponse result, List<RideServicesAreaCategoryFare> areasCategoryFareList, Guid pickUpAreaId, bool isOutBound, Guid? midwayAreaId, string pickUpLatitude, string pickUpLongitude)
        {
            await CalculateFareWithCategoryInOutBoundFareManagers(onlineDrivers, result, areasCategoryFareList, pickUpAreaId, (int)VehicleCategories.Standard, isOutBound, midwayAreaId, pickUpLatitude, pickUpLongitude);
            await CalculateFareWithCategoryInOutBoundFareManagers(onlineDrivers, result, areasCategoryFareList, pickUpAreaId, (int)VehicleCategories.Comfort, isOutBound, midwayAreaId, pickUpLatitude, pickUpLongitude);
            await CalculateFareWithCategoryInOutBoundFareManagers(onlineDrivers, result, areasCategoryFareList, pickUpAreaId, (int)VehicleCategories.Premium, isOutBound, midwayAreaId, pickUpLatitude, pickUpLongitude);
            await CalculateFareWithCategoryInOutBoundFareManagers(onlineDrivers, result, areasCategoryFareList, pickUpAreaId, (int)VehicleCategories.Grossraum, isOutBound, midwayAreaId, pickUpLatitude, pickUpLongitude);
            await CalculateFareWithCategoryInOutBoundFareManagers(onlineDrivers, result, areasCategoryFareList, pickUpAreaId, (int)VehicleCategories.GreenTaxi, isOutBound, midwayAreaId, pickUpLatitude, pickUpLongitude);
        }

        public static decimal FormatFareValue(decimal totalFare)
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
}
}
