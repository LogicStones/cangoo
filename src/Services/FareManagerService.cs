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
        //public static async Task<string> IsSpecialPromotionApplicable(string pickUplatitude, string pickUplongitude, string dropOfflatitude, string dropOfflongitude, string applicationID,
        //    ref Dictionary<dynamic, dynamic> dic, bool isLaterBooking = false, DateTime? dt = null)
        public static async Task<SpecialPromotionDTO> IsSpecialPromotionApplicable(string pickUplatitude, string pickUplongitude,
            string dropOfflatitude, string dropOfflongitude,
            string applicationID, bool isLaterBooking = false, DateTime? dt = null)
        {
            using (var dbContext = new CangooEntities())
            {
                dt = isLaterBooking ? dt : DateTime.UtcNow;
                //var specialPromo = await context.PromoManagers.Where(p => p.ApplicationID.ToString() == applicationID && p.isSpecialPromo == true && p.StartDate <= dt && p.ExpiryDate >= dt).ToListAsync();
                var specialPromo = dbContext.PromoManagers.Where(p => p.ApplicationID.ToString() == applicationID && p.isSpecialPromo == true && p.StartDate <= dt && p.ExpiryDate >= dt).ToList();

                var result = new SpecialPromotionDTO();
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
                            result.PromotionId = promotion.PromoID.ToString();
                            break;
                        }
                    }
                }
                return result;
            }
        }

        public static async Task<EstimateFareResponse> GetFareEstimate(
            string pickUpPostalCode, string pickUpLatitude, string pickUpLongitude,
            string midwayPostalCode, string midwayLatitude, string midwayLongitude,
            string dropOffPostalCode, string dropOffLatitude, string dropOffLongitutde,
            string polyLine, string inBoundTimeInSeconds, string inBoundDistanceInMeters, string outBoundTimeInSeconds, string outBoundDistanceInMeters)
        {
            var result = await PrepareResponseObject(polyLine, inBoundTimeInSeconds, inBoundDistanceInMeters, outBoundTimeInSeconds, outBoundDistanceInMeters);

            var ApplicationId = ConfigurationManager.AppSettings["ApplicationID"].ToString();
            using (CangooEntities dbContext = new CangooEntities())
            {
                var areasCategoryFareList = await GetApplicationRideServicesAreaCategoryFareList(ApplicationId);
                var districtZonesList = await GetApplicationCourierServicesDistrictsZoneList(ApplicationId);

                if (areasCategoryFareList.Any())
                {
                    //TBD : Get only specified areas with ids (Optimization)
                    var areasList = await GetApplicationRideServiceAreasList(ApplicationId);

                    RideServicesArea pickUpServiceArea = new RideServicesArea();
                    RideServicesArea midWayServiceArea = new RideServicesArea();
                    RideServicesArea dropOffServiceArea = new RideServicesArea();

                    foreach (var area in areasList)
                    {
                        if (PolygonService.IsLatLonExistsInPolygon(PolygonService.ConvertLatLonObjectsArrayToPolygonString(area.AreaLatLong), pickUpLatitude, pickUpLongitude))
                        {
                            pickUpServiceArea = area;
                        }

                        if (!string.IsNullOrEmpty(midwayLatitude))
                        {

                            if (PolygonService.IsLatLonExistsInPolygon(PolygonService.ConvertLatLonObjectsArrayToPolygonString(area.AreaLatLong), midwayLatitude, midwayLongitude))
                            {
                                midWayServiceArea = area;
                            }
                        }

                        if (PolygonService.IsLatLonExistsInPolygon(PolygonService.ConvertLatLonObjectsArrayToPolygonString(area.AreaLatLong), dropOffLatitude, dropOffLongitutde))
                        {
                            dropOffServiceArea = area;
                        }

                        if (string.IsNullOrEmpty(midwayLatitude))
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

                    //If all cases fails then it means either request area doesn't exist in system or algorithm failed to identify

                    if (string.IsNullOrEmpty(midwayPostalCode) && pickUpServiceArea.AreaID != Guid.Empty && dropOffServiceArea.AreaID != Guid.Empty)
                    {
                        //No midway, so there will be no outbound fare calculation. outBoundFareManagerId will be null

                        if (areasCategoryFareList.Where(ac => ac.AreaID == pickUpServiceArea.AreaID).Select(ac => ac.AreaID).Distinct().Count() == 1) //All categories may have same fare manager for selected area
                        {
                            var inBoundFareManagerId = (Guid)areasCategoryFareList.Where(ac => ac.AreaID == pickUpServiceArea.AreaID).FirstOrDefault().RSFMID;
                            await SetFareBreakDownAndETA(result, inBoundFareManagerId, false, null);
                        }
                        else
                        {
                            await SetCategoryWiseFareDetails(result, areasCategoryFareList, pickUpServiceArea, false, null);
                        }
                    }
                    else if (!string.IsNullOrEmpty(midwayPostalCode) && pickUpServiceArea.AreaID != Guid.Empty && midWayServiceArea.AreaID != Guid.Empty && dropOffServiceArea.AreaID != Guid.Empty)
                    {
                        if (areasCategoryFareList.Where(ac => ac.AreaID == pickUpServiceArea.AreaID).Select(ac => ac.AreaID).Distinct().Count() == 1 &&
                            areasCategoryFareList.Where(ac => ac.AreaID == midWayServiceArea.AreaID).Select(ac => ac.AreaID).Distinct().Count() == 1)
                        {
                            var inBoundFareManagerId = (Guid)areasCategoryFareList.Where(ac => ac.AreaID == pickUpServiceArea.AreaID).FirstOrDefault().RSFMID;
                            var outBoundFareManagerId = (Guid)areasCategoryFareList.Where(ac => ac.AreaID == midWayServiceArea.AreaID).FirstOrDefault().RSFMID;
                            await SetFareBreakDownAndETA(result, inBoundFareManagerId, true, outBoundFareManagerId);
                        }
                        else
                        {
                            await SetCategoryWiseFareDetails(result, areasCategoryFareList, pickUpServiceArea, true, midWayServiceArea.AreaID);
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

        private static async Task<EstimateFareResponse> PrepareResponseObject(string polyLine,
            string inBoundTimeInSeconds, string inBoundDistanceInMeters,
            string outBoundTimeInSeconds, string outBoundDistanceInMeters)
        {
            return new EstimateFareResponse
            {
                PolyLine = polyLine,
                InBoundDistanceInKM = (Convert.ToDouble(inBoundDistanceInMeters) / 1000.0).ToString(),
                InBoundTimeInMinutes = (Convert.ToDouble(inBoundTimeInSeconds) / 60.0).ToString(),
                OutBoundDistanceInKM = (Convert.ToDouble(outBoundDistanceInMeters) / 1000.0).ToString(),
                OutBoundTimeInMinutes = (Convert.ToDouble(outBoundTimeInSeconds) / 60.0).ToString(),
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
                Facilities = await FacilitiesService.GetFacilitiesListAsync()
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

        private static async Task SetFareBreakDownAndETA(EstimateFareResponse result, Guid inBoundFareManagerId, bool isOutBound, Guid? outBoundFareManagerId)
        {
            FareDetailsDTO fareDetails = new FareDetailsDTO();

            if (isOutBound)
                fareDetails = await CalcuateFareBreakdown(inBoundFareManagerId, result.InBoundDistanceInKM, result.InBoundTimeInMinutes, isOutBound, outBoundFareManagerId, result.OutBoundDistanceInKM, result.OutBoundTimeInMinutes);
            else
                fareDetails = await CalcuateFareBreakdown(inBoundFareManagerId, result.InBoundDistanceInKM, result.InBoundTimeInMinutes, isOutBound, null, "", "");

            result.Categories
                .Select(c =>
                {
                    c.Amount = fareDetails.TotalFare.ToString();
                    c.FormattingAdjustment = fareDetails.FormattingAdjustment.ToString();

                    c.InBoundRSFMID = inBoundFareManagerId.ToString();
                    c.InBoundBaseFare = fareDetails.InBoundBaseFare.ToString("0.00");
                    c.InBoundBookingFare = fareDetails.InBoundBookingFare.ToString("0.00");
                    c.InBoundWaitingFare = fareDetails.InBoundWaitingFare.ToString("0.00");
                    c.InBoundSurchargeAmount = fareDetails.InBoundSurchargeAmount.ToString("0.00");
                    c.InBoundDistanceFare = fareDetails.InBound.DistanceFare.ToString("0.00");
                    c.InBoundTimeFare = fareDetails.InBound.TimeFare.ToString("0.00");

                    c.OutBoundRSFMID = outBoundFareManagerId.ToString();
                    c.OutBoundDistanceFare = fareDetails.OutBound.DistanceFare.ToString("0.00");
                    c.OutBoundTimeFare = fareDetails.OutBound.TimeFare.ToString("0.00");

                    return c;
                }).ToList();

            await SetVehiclesETA(result);
        }

        private static async Task SetVehiclesETA(EstimateFareResponse result)
        {
            foreach (var item in result.Categories)
            {
                item.ETA = await VehiclesService.GetVehicleETA(item.CategoryID);
            }
        }

        private static async Task SetCategoryWiseFareDetails(EstimateFareResponse result, List<RideServicesAreaCategoryFare> areasCategoryFareList, RideServicesArea pickUpServiceArea, bool isOutBound, Guid? midWayAreaId)
        {
            await CalculateCategoryFare(result, areasCategoryFareList, pickUpServiceArea.AreaID, (int)VehicleCategories.Standard, isOutBound, midWayAreaId);
            await CalculateCategoryFare(result, areasCategoryFareList, pickUpServiceArea.AreaID, (int)VehicleCategories.Comfort, isOutBound, midWayAreaId);
            await CalculateCategoryFare(result, areasCategoryFareList, pickUpServiceArea.AreaID, (int)VehicleCategories.Premium, isOutBound, midWayAreaId);
            await CalculateCategoryFare(result, areasCategoryFareList, pickUpServiceArea.AreaID, (int)VehicleCategories.Grossraum, isOutBound, midWayAreaId);
            await CalculateCategoryFare(result, areasCategoryFareList, pickUpServiceArea.AreaID, (int)VehicleCategories.GreenTaxi, isOutBound, midWayAreaId);
        }

        private static async Task CalculateCategoryFare(EstimateFareResponse result, List<RideServicesAreaCategoryFare> areasCategoryFareList, Guid pickUpAreaId, int categoryId, bool isOutBound, Guid? midWayAreaId)
        {
            var inBoundFareManagerId = (Guid)areasCategoryFareList.Where(ac => ac.AreaID == pickUpAreaId && ac.CategoryID == categoryId).FirstOrDefault().RSFMID;
            var outBoundFareManagerId = areasCategoryFareList.Where(ac => ac.AreaID == midWayAreaId && ac.CategoryID == categoryId).FirstOrDefault();

            var fareDetails = await CalcuateFareBreakdown(inBoundFareManagerId, result.InBoundDistanceInKM, result.InBoundTimeInMinutes, isOutBound, outBoundFareManagerId?.RSFMID, result.OutBoundDistanceInKM, result.OutBoundTimeInMinutes);
            result.Categories
                .Where(c => c.CategoryID.Equals(categoryId.ToString()))
                .Select(c =>
                {
                    c.Amount = fareDetails.TotalFare.ToString();
                    c.FormattingAdjustment = fareDetails.FormattingAdjustment.ToString();

                    c.InBoundRSFMID = inBoundFareManagerId.ToString();
                    c.InBoundBaseFare = fareDetails.InBoundBaseFare.ToString("0.00");
                    c.InBoundBookingFare = fareDetails.InBoundBookingFare.ToString("0.00");
                    c.InBoundWaitingFare = fareDetails.InBoundWaitingFare.ToString("0.00");
                    c.InBoundSurchargeAmount = fareDetails.InBoundSurchargeAmount.ToString("0.00");
                    c.InBoundDistanceFare = fareDetails.InBound.DistanceFare.ToString("0.00");
                    c.InBoundTimeFare = fareDetails.InBound.TimeFare.ToString("0.00");

                    c.OutBoundRSFMID = outBoundFareManagerId.ToString();
                    c.OutBoundDistanceFare = fareDetails.OutBound.DistanceFare.ToString("0.00");
                    c.OutBoundTimeFare = fareDetails.OutBound.TimeFare.ToString("0.00");

                    return c;
                }).ToList();


            //var comfortFareManagerId = (Guid)areasCategoryFareList.Where(ac => ac.AreaID.Equals(PickupServiceArea.AreaID) && ac.CategoryID == (int)VehicleCategories.Comfort).FirstOrDefault().RideServicesFareManagerID;
            //var comfortTotalFare = await CalcuateFare(comfortFareManagerId, null, result.InBoundDistanceInKM, result.InBoundTimeInMinutes, result.OutBoundDistanceInKM, result.OutBoundTimeInMinutes, false);
            //result.Categories.Where(c => c.CategoryID == ((int)VehicleCategories.Comfort).ToString()).Select(c => { c.Amount = comfortTotalFare.ToString(); return c; }).ToList();

            //var premiumFareManagerId = (Guid)areasCategoryFareList.Where(ac => ac.AreaID.Equals(PickupServiceArea.AreaID) && ac.CategoryID == (int)VehicleCategories.Premium).FirstOrDefault().RideServicesFareManagerID;
            //var premiumTotalFare = await CalcuateFare(premiumFareManagerId, null, result.InBoundDistanceInKM, result.InBoundTimeInMinutes, result.OutBoundDistanceInKM, result.OutBoundTimeInMinutes, false);
            //result.Categories.Where(c => c.CategoryID == ((int)VehicleCategories.Premium).ToString()).Select(c => { c.Amount = premiumTotalFare.ToString(); return c; }).ToList();

            //var grossraumFareManagerId = (Guid)areasCategoryFareList.Where(ac => ac.AreaID.Equals(PickupServiceArea.AreaID) && ac.CategoryID == (int)VehicleCategories.Grossraum).FirstOrDefault().RideServicesFareManagerID;
            //var grossraumTotalFare = await CalcuateFare(grossraumFareManagerId, null, result.InBoundDistanceInKM, result.InBoundTimeInMinutes, result.OutBoundDistanceInKM, result.OutBoundTimeInMinutes, false);
            //result.Categories.Where(c => c.CategoryID == ((int)VehicleCategories.Grossraum).ToString()).Select(c => { c.Amount = grossraumTotalFare.ToString(); return c; }).ToList();

            //var greenTaxiFareManagerId = (Guid)areasCategoryFareList.Where(ac => ac.AreaID.Equals(PickupServiceArea.AreaID) && ac.CategoryID == (int)VehicleCategories.GreenTaxi).FirstOrDefault().RideServicesFareManagerID;
            //var greenTaxiotalFare = await CalcuateFare(greenTaxiFareManagerId, null, result.InBoundDistanceInKM, result.InBoundTimeInMinutes, result.OutBoundDistanceInKM, result.OutBoundTimeInMinutes, false);
            //result.Categories.Where(c => c.CategoryID == ((int)VehicleCategories.GreenTaxi).ToString()).Select(c => { c.Amount = greenTaxiotalFare.ToString(); return c; }).ToList();

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

        private static async Task<FareDetailsDTO> CalcuateFareBreakdown(Guid inBoundFareManagerId, string inBounddistanceInKM, string inBoundTimeInMinutes, bool isOutBound, Guid? outBoundFareManagerId, string outBounddistanceInKM, string outBoundTimeInMinutes)
        {
            var result = new FareDetailsDTO();
            //decimal baseFare = 0, waitingFare = 0, bookingFare = 0, surchargeAmount = 0;

            var fareManager = await GetFareManagerById(inBoundFareManagerId);
            // TBD: Check date from public holidays lookup table
            bool isWeekend = WeekendOrHoliday();
            bool isMorningShift = CheckShift((TimeSpan)fareManager.MorningShiftStartTime, (TimeSpan)fareManager.MorningShiftEndTime);

            var distanceAndTimeFare = await CalculateDistanceAndTimeFare(fareManager.RSFMID.ToString(),
                 isWeekend ? (int)FareManagerShifts.Weekend : isMorningShift ? (int)FareManagerShifts.Morning : (int)FareManagerShifts.Evening,
                 inBounddistanceInKM,
                 (decimal)(isWeekend ? fareManager.WeekendPerKmFare : isMorningShift ? fareManager.MorningPerKmFare : fareManager.EveningPerKmFare),
                 inBoundTimeInMinutes,
                 (decimal)(isWeekend ? fareManager.WeekendPerMinFare : isMorningShift ? fareManager.MorningPerMinFare : fareManager.EveningPerMinFare));

            result.InBoundBaseFare = isWeekend ? (decimal)fareManager.WeekendBaseFare : isMorningShift ? (decimal)fareManager.MorningBaseFare : (decimal)fareManager.EveningBaseFare;
            result.InBoundWaitingFare = isWeekend ? (decimal)fareManager.WeekendWaitingFare : isMorningShift ? (decimal)fareManager.MorningWaitingFare : (decimal)fareManager.EveningWaitingFare;
            result.InBoundBookingFare = isWeekend ? (decimal)fareManager.WeekendBookingFare : isMorningShift ? (decimal)fareManager.MorningBookingFare : (decimal)fareManager.EveningBookingFare;
            result.InBound.DistanceFare = distanceAndTimeFare.DistanceFare;
            result.InBound.TimeFare = distanceAndTimeFare.TimeFare;

            if (isOutBound)
            {
                if (inBoundFareManagerId != outBoundFareManagerId)
                {
                    fareManager = await GetFareManagerById((Guid)outBoundFareManagerId);
                    isMorningShift = CheckShift((TimeSpan)fareManager.MorningShiftStartTime, (TimeSpan)fareManager.MorningShiftEndTime);
                }

                distanceAndTimeFare = await CalculateDistanceAndTimeFare(fareManager.RSFMID.ToString(),
                    isWeekend ? (int)FareManagerShifts.Weekend : isMorningShift ? (int)FareManagerShifts.Morning : (int)FareManagerShifts.Evening,
                    outBounddistanceInKM,
                    (decimal)(isWeekend ? fareManager.WeekendPerKmFare : isMorningShift ? fareManager.MorningPerKmFare : fareManager.EveningPerKmFare),
                    outBoundTimeInMinutes,
                    (decimal)(isWeekend ? fareManager.WeekendPerMinFare : isMorningShift ? fareManager.MorningPerMinFare : fareManager.EveningPerMinFare));

                result.OutBound.DistanceFare = distanceAndTimeFare.DistanceFare;
                result.OutBound.TimeFare = distanceAndTimeFare.TimeFare;
            }

            result.TotalFare = result.InBoundBaseFare + result.InBoundBookingFare + result.InBoundWaitingFare +
                result.InBound.DistanceFare + result.InBound.TimeFare + result.OutBound.DistanceFare + result.OutBound.TimeFare;

            result.SurchargeAmount = Convert.ToDecimal(result.TotalFare * (isWeekend ? (decimal)fareManager.WeekendSurcharge : isMorningShift ? (decimal)fareManager.MorningSurcharge : (decimal)fareManager.EveningSurcharge) / 100);
            result.TotalFare += result.SurchargeAmount;

            //Trip minimum amount should be 6.6
            if (result.TotalFare <= 6.6M)
            {
                result.TotalFare = 6.60M;
            }

            var formattedFare = FormatFareValue(result.TotalFare);
            result.FormattingAdjustment = formattedFare - result.TotalFare;
            result.TotalFare = formattedFare;
            return result;
        }

        private static bool CheckShift(TimeSpan morningShiftStartTime, TimeSpan morningShiftEndTime)
        {
            return DateTime.UtcNow.TimeOfDay >= morningShiftStartTime && DateTime.UtcNow.TimeOfDay <= morningShiftEndTime;
        }

        private static bool WeekendOrHoliday()
        {
            return DateTime.UtcNow.DayOfWeek == DayOfWeek.Saturday || DateTime.UtcNow.DayOfWeek == DayOfWeek.Sunday;
        }

        private static async Task<DistanceAndTimeFareDTO> CalculateDistanceAndTimeFare(string rideServicesID, int shiftId, string distanceInKM, decimal perKMFare, string timeInMinutes, decimal perMinFare)
        {
            using (var dbContext = new CangooEntities())
            {
                #region distance fare calculation

                var result = new DistanceAndTimeFareDTO();

                //distance ranges are saved in kilo meters
                var lstFareDistanceRange = await dbContext.RideServicesFareRanges
                    .Where(f => f.ShiftID == shiftId && f.RSFMID.ToString().Equals(rideServicesID))
                    .OrderBy(f => f.Range)
                    .ToListAsync();

                if (lstFareDistanceRange.Any())
                {
                    foreach (var ran in lstFareDistanceRange)
                    {
                        var arrRange = ran.Range.Split(';');

                        if (Convert.ToDouble(arrRange[1]) <= Convert.ToDouble(distanceInKM))
                        {
                            result.DistanceFare += Convert.ToDecimal(ran.Charges) * (Convert.ToDecimal(arrRange[1]) - Convert.ToDecimal(arrRange[0]));
                        }
                        else
                        {
                            result.DistanceFare += Convert.ToDecimal(ran.Charges) * (Convert.ToDecimal(distanceInKM) - Convert.ToDecimal(arrRange[0]));
                            break;
                        }

                    }
                }
                else
                {
                    result.DistanceFare = Convert.ToDecimal(distanceInKM) * perKMFare;
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

                        if (Convert.ToDecimal(arrRange[1]) <= Convert.ToDecimal(timeInMinutes))
                        {
                            result.TimeFare += Convert.ToDecimal(ran.Charges) * (Convert.ToDecimal(arrRange[1]) - Convert.ToDecimal(arrRange[0]));
                        }
                        else
                        {
                            result.TimeFare += Convert.ToDecimal(ran.Charges) * (Convert.ToDecimal(timeInMinutes) - Convert.ToDecimal(arrRange[0]));
                            break;
                        }
                    }
                }
                else
                {
                    result.TimeFare = Convert.ToDecimal(timeInMinutes) * perMinFare;
                }

                #endregion

                return result;
            }
        }
    }
}
