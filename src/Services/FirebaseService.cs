using Constants;
using DTOs.API;
using Integrations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using Services.Automapper;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Device.Location;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Services
{
    public class FirebaseService
    {
        #region Single Login

        public static async Task<bool> isDriverInTrip(string driverId)
        {
            //string normalTripCheck = "OnlineDriver/" + DriverID + "/OngoingRide";
            //string walkinTripCheck = "OnlineDriver/" + DriverID + "/WalkInCustomer";

            var resp = await FirebaseIntegration.Read("OnlineDriver/" + driverId + "/OngoingRide");

            //bool isInNormalTrip = true;
            //bool isInWalkInTrip = true;

            if (string.IsNullOrEmpty(resp.Body) || resp.Body.Equals("null") || resp.Body.Equals("\"\""))
                return false;//isInNormalTrip = false;

            //resp = client.Get(walkinTripCheck);

            //if (string.IsNullOrEmpty(resp.Body) || resp.Body.Equals("null") || resp.Body.Equals("\"\""))
            //    isInWalkInTrip = false;

            //if (isInNormalTrip || isInWalkInTrip)
            //    return true;

            return true;
        }

        #endregion

        #region Online / Offline Driver

        public static async Task<Dictionary<string, FirebaseDriver>> GetOnlineDrivers()
        {
            var fbOnlineDrivers = await FirebaseIntegration.Read("OnlineDriver");

            //Key: DriverID, Value: Online Driver Properties

            Dictionary<string, FirebaseDriver> onlineDrivers = new Dictionary<string, FirebaseDriver>();

            if (!string.IsNullOrEmpty(fbOnlineDrivers.Body) && !fbOnlineDrivers.Body.Equals("null"))
            {
                Dictionary<string, dynamic> dirtyData = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(fbOnlineDrivers.Body);
                foreach (var item in dirtyData)
                {
                    try
                    {
                        var driverId = Guid.Parse(item.Key.Trim());
                        try
                        {
                            FirebaseDriver driverData = JsonConvert.DeserializeObject<JObject>(item.Value.ToString()).ToObject<FirebaseDriver>();// (FirebaseDriver)((JObject)item.Value);//JsonConvert.DeserializeObject<FirebaseDriver>(item.Value);

                            if (string.IsNullOrEmpty(driverData.driverID) || driverData.location == null) //Driver inconsistent data
                                await ForceFullyOfflineDriver(driverId.ToString(), driverData.vehicleID);
                            else
                                onlineDrivers.Add(item.Key, driverData);
                        }
                        catch (Exception ex)    //OnlineDriver child node have dirty data
                        {
                            await ForceFullyOfflineDriver(driverId.ToString(), "");
                            Log.Error("Failed to parse OnlineDriver to FirebaseDriver !!", ex);
                        }
                    }
                    catch (Exception ex)    //OnlineDriver node have dirty data
                    {
                        await FirebaseIntegration.Delete("OnlineDriver/" + item.Key);
                        Log.Error("Dirty data in OnlineDriver !!", ex);
                    }
                }
            }
            return onlineDrivers;
        }

        public static async Task ForceFullyOfflineDriver(string driverId, string vehicleId)
        {
            await OfflineDriver(driverId);
            DriverService.OnlineOfflineDriver(driverId, string.IsNullOrEmpty(vehicleId) ? "" : vehicleId, false, Guid.Parse(ConfigurationManager.AppSettings["ApplicationID"].ToString()));
        }

        public static async Task<FirebaseDriver> GetOnlineDriverById(string driverId)
        {
            //get online driver from firebase
            //client = new FireSharp.FirebaseClient(config);
            //FirebaseResponse resp = client.Get();

            var fbOnlineDriver = await FirebaseIntegration.Read("OnlineDriver/" + driverId);

            //Key: DriverID, Value: Online Driver Properties

            if (!string.IsNullOrEmpty(fbOnlineDriver.Body) && !fbOnlineDriver.Body.Equals("null"))
            {
                return JsonConvert.DeserializeObject<FirebaseDriver>(fbOnlineDriver.Body);
            }

            return null;
        }

        public static async Task OnlineDriver(string driverId, string vehicleId, int makeId, string companyId, string seatingCapacity, string make, string model, string driverName,
                                              Nullable<bool> isPriorityHoursActive, string priorityHourEndTime, string earningPoints, string vehicleFeatures, string driverFeatures,
                                              string deviceToken, string userName, string phoneNumber, int modelId, string plateNumber, string category,
                                              string categoryId, string registrationYear, string color)
        {
            var fd = new FirebaseDriver
            {
                companyID = companyId,
                driverID = driverId,
                userName = userName,
                driverName = driverName,
                phoneNumber = phoneNumber,
                deviceToken = deviceToken,
                driverFacilities = driverFeatures,

                //lat = 0.0,
                //lon = 0.0,
                isBusy = false.ToString(),
                isPriorityHoursActive = isPriorityHoursActive != true ? false : true,
                priorityHourEndTime = priorityHourEndTime,
                onlineSince = DateTime.UtcNow.ToString(Formats.DateFormat),
                earningPoints = string.IsNullOrEmpty(earningPoints) ? "0.0" : earningPoints,
                priorityHourRemainingTime = isPriorityHoursActive == true ? ((int)(DateTime.Parse(priorityHourEndTime).Subtract(DateTime.Parse(DateTime.UtcNow.ToString(Formats.DateFormat))).TotalMinutes)).ToString() : "0",
                lastUpdated = "",
                OngoingRide = "",

                vehicleID = vehicleId,
                makeID = makeId,
                make = make,
                modelID = modelId,
                model = model,
                categoryID = categoryId,
                category = category,
                color = color,
                plateNumber = plateNumber,
                seatingCapacity = seatingCapacity,
                registrationYear = registrationYear,
                vehicleFacilities = vehicleFeatures
            };

            await FirebaseIntegration.Write("OnlineDriver/" + driverId, fd);
        }

        public static async Task OfflineDriver(string driverId)
        {
            await FirebaseIntegration.Delete("OnlineDriver/" + driverId);
        }

        #endregion

        #region Send Request to Online Drivers

        public static async Task<bool> SendRideRequestToOnlineDrivers(string tripId, string passengerId, string vehicleCategoryId, int reqSeatingCapacity, DriverBookingRequestNotification bookingRN)
        {
            try
            {

                //Passenger data
                if (bookingRN.isReRouteRequest)
                {
                    if (!bookingRN.isLaterBooking)
                    {
                        bookingRN.reRouteRequestTime = DateTime.UtcNow.ToString(Formats.DateFormat);

                        await WriteTripPassengerDetails(AutoMapperConfig._mapper.Map<DriverBookingRequestNotification, FirebasePassenger>(bookingRN), passengerId);
                        await SetTripStatus(tripId, Enum.GetName(typeof(TripStatuses), TripStatuses.ReRouting));

                        // free captain > update user > send request 
                        await SendReRouteNotfication(bookingRN.BookingModeId, bookingRN.reRouteRequestTime, bookingRN.requestTimeOut.ToString(), bookingRN.previousCaptainId.ToString(), tripId, bookingRN.deviceToken); //passengerId, 
                    }
                    else
                    {
                        await WriteTripPassengerDetails(AutoMapperConfig._mapper.Map<DriverBookingRequestNotification, FirebasePassenger>(bookingRN), passengerId);
                        await SetTripStatus(tripId, Enum.GetName(typeof(TripStatuses), TripStatuses.RequestSent));

                        //UPDATE: Captain can canel inprocess later booking, so user / captain should be set free in every case.
                        // free captain > update user > send request 
                        await SendInProcessLaterBookingReRouteNotfication(bookingRN.BookingModeId, bookingRN.previousCaptainId.ToString(), tripId, passengerId, bookingRN.deviceToken);
                    }
                }
                else
                {

                    await WriteTripPassengerDetails(AutoMapperConfig._mapper.Map<DriverBookingRequestNotification, FirebasePassenger>(bookingRN), passengerId);
                    await SetTripStatus(tripId, Enum.GetName(typeof(TripStatuses), TripStatuses.RequestSent));

                    await UpdateDiscountTypeAndAmount(tripId, bookingRN.discountAmount, bookingRN.discountType);

                }

                #region MakePreferredAndNormalDriversList

                //Key: DriverID, Value: Online Driver Properties

                List<FirebaseDriver> lstNormalCaptains = new List<FirebaseDriver>();
                List<FirebaseDriver> lstPreferredCaptains = new List<FirebaseDriver>();

                string captainIDs = "";
                string preferredCaptainIDs = "";

                var onlineDrivers = await GetOnlineDrivers();

                foreach (var od in onlineDrivers)
                {
                    FirebaseDriver driver = od.Value;// JsonConvert.DeserializeObject<FirebaseDriver>(JsonConvert.SerializeObject(od.Value));
                    if (string.IsNullOrEmpty(driver.driverID) || driver.location == null) //Dirty data
                    {
                        continue;
                    }

                    if (!driver.categoryID.Contains(vehicleCategoryId))
                    {
                        continue;
                    }

                    //If user have requested some facilities then request should be sent to only eligible captains
                    if (!string.IsNullOrEmpty(bookingRN.requiredFacilities))
                    {
                        var reqFac = bookingRN.requiredFacilities.ToLower().Split(',');
                        var vehFac = driver.vehicleFacilities.ToLower().Split(',');
                        var capFac = driver.driverFacilities.ToLower().Split(',');

                        var vehCheck = reqFac.Intersect(vehFac);
                        var capCheck = reqFac.Intersect(capFac);

                        //If both vehicle and captain don't have required facilities

                        if (!(reqFac.SequenceEqual(vehCheck) && reqFac.SequenceEqual(capCheck)))
                            continue;
                    }

                    if (driver.isPriorityHoursActive)
                    {
                        preferredCaptainIDs = string.IsNullOrEmpty(preferredCaptainIDs) ? driver.driverID : preferredCaptainIDs + "," + driver.driverID;

                        //If its later booking then send request to captains even if he is already busy
                        if (bookingRN.isLaterBooking)
                        {
                            lstPreferredCaptains.Add(driver);
                        }
                        //In case of normal booking send request to free captains only
                        else
                        {
                            if (driver.isBusy.ToLower().Equals("false"))
                            {
                                lstPreferredCaptains.Add(driver);
                            }
                        }
                    }
                    else
                    {
                        captainIDs = string.IsNullOrEmpty(captainIDs) ? driver.driverID : captainIDs + "," + driver.driverID;

                        //If it's later booking then send request to captains even if he is already busy
                        if (bookingRN.isLaterBooking)
                        {
                            lstNormalCaptains.Add(driver);
                        }
                        //In case of normal booking send request to free captains only
                        else
                        {
                            if (driver.isBusy.ToLower().Equals("false"))
                            {
                                lstNormalCaptains.Add(driver);
                            }
                        }
                    }
                }
                #endregion

                //CaptainID won't be null in case of ReRouted trip request and don't send request to that captain again who have cancelled the current trip. 
                //TBD: All the captains who ever accepted the trip can be excluded using ReroutedRidesLog table

                if (!string.IsNullOrEmpty(bookingRN.previousCaptainId))
                {
                    lstNormalCaptains.RemoveAll(x => x.driverID.Equals(bookingRN.previousCaptainId));
                    lstPreferredCaptains.RemoveAll(x => x.driverID.Equals(bookingRN.previousCaptainId));
                }

                //No driver available
                if (lstNormalCaptains.Count == 0 && lstPreferredCaptains.Count == 0)
                    return false;


                /* REFACTOR
                 * This function call seems to be unnecessary*/

                //ALERT !!
                //Double check the logic, following nodes are not added anywhere else before, in case of first time ride request.
                //Probably same function is called when scheduling later booking (either in user of driver controller)

                //NEW IMPLEMENTATION : Moved to IsReRouteRequest else part
                //await UpdateDiscountTypeAndAmount(tripId, bookingRN.DiscountAmount ?? "0.00", bookingRN.DiscountType ?? "normal");
                await SetTripDispatchedStatus(tripId, bookingRN.isDispatchedRide);

                string applicationId = ConfigurationManager.AppSettings["ApplicationID"].ToString();
                var applicationSettings = await ApplicationSettingService.GetApplicationSettings(applicationId);

                var distanceInterval = (int)applicationSettings.RequestRadiusInterval;
                var requestSearchRange = bookingRN.isLaterBooking ? (int)(applicationSettings.LaterBookingRequestSearchRange * 1000) : (int)(applicationSettings.RequestSearchRange * 1000);
                var captainMinRating = applicationSettings.CaptainMinRating;

                int minDistanceRange = 0;
                var maxDistanceRange = distanceInterval;
                var pickPosition = new GeoCoordinate(Convert.ToDouble(bookingRN.lat), Convert.ToDouble(bookingRN.lan));

                var lstPreferredCaptainsDetail = new List<DatabaseOlineDriversDTO>();
                if (!string.IsNullOrEmpty(preferredCaptainIDs))
                    lstPreferredCaptainsDetail = await DriverService.GetOnlineDriversByIds(preferredCaptainIDs);

                var lstNormalCaptainsDetail = new List<DatabaseOlineDriversDTO>();
                if (!string.IsNullOrEmpty(captainIDs))
                    lstNormalCaptainsDetail = await DriverService.GetOnlineDriversByIds(captainIDs);

                do
                {
                    Dictionary<string, string> lstFilteredPreferredCaptains = new Dictionary<string, string>();
                    Dictionary<string, string> lstFilteredNormalCaptains = new Dictionary<string, string>();

                    var lstRequestLog = new List<TripRequestLogDTO>();

                    FilterDriverForRideRequest(lstPreferredCaptains, lstPreferredCaptainsDetail, lstFilteredPreferredCaptains, lstRequestLog, tripId, reqSeatingCapacity, bookingRN, requestSearchRange, captainMinRating, minDistanceRange, maxDistanceRange, pickPosition);

                    //if request is already accpeted then no need to save current drivers list in log, so break loop 
                    if (lstFilteredPreferredCaptains.Any())
                    {
                        if (await IsRequestAccepted(tripId))
                        {
                            break;
                        }
                        else
                        {
                            await SendRequestToFilteredDrivers(bookingRN, applicationSettings, lstFilteredPreferredCaptains, lstRequestLog);
                        }
                    }

                    //TBD: Loop Optimization - Remove currently selected drivers from lstPreferredOnlineDriver
                    //ExcludeLoggedDriversForNextIteration(lstPreferredCaptains, lstPreferredCaptainsDetail, lstFilteredPreferredCaptains);

                    lstRequestLog = new List<TripRequestLogDTO>();

                    FilterDriverForRideRequest(lstNormalCaptains, lstNormalCaptainsDetail, lstFilteredNormalCaptains, lstRequestLog, tripId, reqSeatingCapacity, bookingRN, requestSearchRange, captainMinRating, minDistanceRange, maxDistanceRange, pickPosition);

                    //TBD: Loop Optimization - Remove currently selected drivers from lstOnlineDriver
                    //ExcludeLoggedDriversForNextIteration(lstNormalCaptains, lstNormalCaptainsDetail, lstFilteredNormalCaptains);

                    //if request is already accpeted then no need to save current drivers list in log, so break loop 
                    if (lstFilteredNormalCaptains.Any())
                    {
                        if (await IsRequestAccepted(tripId))
                        {
                            break;
                        }
                        else
                        {
                            await SendRequestToFilteredDrivers(bookingRN, applicationSettings, lstFilteredNormalCaptains, lstRequestLog);
                        }
                    }

                    minDistanceRange = maxDistanceRange;
                    maxDistanceRange = maxDistanceRange + distanceInterval > requestSearchRange ? requestSearchRange : maxDistanceRange + distanceInterval;

                } while (!await IsRequestAccepted(tripId) && minDistanceRange < requestSearchRange);

                return true; //Ride is accepted

            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private static async Task SendRequestToFilteredDrivers(DriverBookingRequestNotification bookingRN, DatabaseModel.ApplicationSetting applicationSettings, Dictionary<string, string> lstFilteredCaptains, List<TripRequestLogDTO> lstRequestLog)
        {
            await PushyService.BroadCastNotification(lstFilteredCaptains, bookingRN);
            await TripsManagerService.LogBookRequestRecipientDrivers(lstRequestLog);
            Thread.Sleep(Convert.ToInt32(applicationSettings.RequestWaitingTime * 1000));
        }

        private static void ExcludeLoggedDriversForNextIteration(List<FirebaseDriver> lstCaptains, List<DatabaseOlineDriversDTO> lstCaptainsDetail, Dictionary<string, string> lstFilteredCaptains)
        {
            var lstUsedPreferredCaptains = lstCaptainsDetail.Where(pcd => lstFilteredCaptains.Any(fpc => fpc.Key.Equals(pcd.DeviceToken)));
            lstCaptains.RemoveAll(pc => lstUsedPreferredCaptains.Any(upc => upc.CaptainID.Equals(pc.driverID)));
        }

        private static void FilterDriverForRideRequest(List<FirebaseDriver> lstCaptains, List<DatabaseOlineDriversDTO> lstCaptainsDetail, Dictionary<string, string> lstFilteredCaptains, List<TripRequestLogDTO> lstRequestLog, string tripId, int reqSeatingCapacity, DriverBookingRequestNotification bookingRN, int requestSearchRange, double? captainMinRating, int? minDistanceRange, int? maxDistanceRange, GeoCoordinate pickPosition)
        {
            foreach (var dr in lstCaptains)
            {
                var capDetail = lstCaptainsDetail.Where(c => c.CaptainID.ToString().Equals(dr.driverID)).FirstOrDefault();
                if (capDetail != null)
                {
                    if (dr.location == null)
                        continue;

                    var driverPosition = new GeoCoordinate(dr.location.l[0], dr.location.l[1]);
                    var distance = driverPosition.GetDistanceTo(pickPosition);  //distance in meters

                    if (distance <= requestSearchRange)
                    {
                        if (capDetail.Rating >= captainMinRating && distance >= minDistanceRange && distance <= maxDistanceRange && Convert.ToInt32(dr.seatingCapacity) >= reqSeatingCapacity)
                        {
                            lstFilteredCaptains.Add(capDetail.DeviceToken, bookingRN.isLaterBooking ? capDetail.LaterBookingNotificationTone : capDetail.NormalBookingNotificationTone);

                            lstRequestLog.Add(new TripRequestLogDTO
                            {
                                CaptainID = Guid.Parse(dr.driverID),
                                RequestLogID = Guid.NewGuid(),
                                CaptainLocationLatitude = dr.location.l[0].ToString(),
                                CaptainLocationLongitude = dr.location.l[1].ToString(),
                                DistanceToPickUpLocation = distance,
                                isReRouteRequest = bookingRN.isReRouteRequest,
                                TimeStamp = DateTime.UtcNow,
                                TripID = Guid.Parse(tripId)
                            });
                        }
                    }
                }
            }
        }

        private static async Task<bool> IsRequestAccepted(string tripId)
        {
            dynamic resp = await GetTripDetails(tripId);

            if (!string.IsNullOrEmpty(resp.Body) && !resp.Body.Equals("null"))
            {
                Dictionary<dynamic, dynamic> dicStatus = JsonConvert.DeserializeObject<Dictionary<dynamic, dynamic>>(resp.Body);

                var status = dicStatus.FirstOrDefault(s => s.Key == "TripStatus").Value;

                return
                    status == null ? true :
                    (status.Equals(Enum.GetName(typeof(TripStatuses), TripStatuses.RequestSent)) || // Enumration.returnRideFirebaseStatus(RideFirebaseStatus.RequestSent)) 
                    status.Equals(Enum.GetName(typeof(TripStatuses), TripStatuses.ReRouting)) //Enumration.returnRideFirebaseStatus(RideFirebaseStatus.ReRouting))
                    ) ? false : true;
            }

            //trip not found, break request sending loop
            return true;
        }

        private static async Task<dynamic> GetTripDetails(string tripID)
        {
            return await FirebaseIntegration.Read("Trips/" + tripID);
        }

        public static async Task<FirebasePassenger> GetTripPassengerDetails(string tripId, string passengerId)
        {
            dynamic trip = await FirebaseIntegration.Read("Trips/" + tripId + "/" + passengerId);

            if (!string.IsNullOrEmpty(trip.Body) && !trip.Body.Equals("null"))
            {
                var passenger = JsonConvert.DeserializeObject<FirebasePassenger>(trip.Body);

                return new FirebasePassenger
                {
                    Brand = passenger.Brand,
                    CardId = passenger.CardId,
                    CustomerId = passenger.CustomerId,
                    Last4Digits = passenger.Last4Digits,
                    PaymentModeId = passenger.PaymentModeId,
                    WalletBalance = passenger.WalletBalance
                };

                //UpdateDiscountTypeAndAmount(false, tripId, temp["discount"]["amount"].ToString(), temp["discount"]["type"].ToString(), temp["isDispatchedRide"]);
            }
            return new FirebasePassenger();
        }

        public static async Task<DiscountTypeDTO> GetTripDiscountDetails(string tripId)
        {
            dynamic resp = await GetTripDetails(tripId);
            if (!string.IsNullOrEmpty(resp.Body) && !resp.Body.Equals("null"))
            {
                Dictionary<string, dynamic> temp = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(resp.Body);
                return new DiscountTypeDTO
                {
                    DiscountAmount = temp["discount"]["amount"].ToString(),
                    DiscountType = temp["discount"]["type"].ToString()
                };
                //UpdateDiscountTypeAndAmount(false, tripId, temp["discount"]["amount"].ToString(), temp["discount"]["type"].ToString(), temp["isDispatchedRide"]);
            }
            return new DiscountTypeDTO();
        }

        public static async Task UpdateDiscountTypeAndAmount(string tripId, string discountAmount, string discountType)
        {
            Dictionary<string, string> dic = new Dictionary<string, string>
                {
                    //{ "/isDispatchedRide", isDispatchedRide },
                    { "amount", discountAmount },
                    { "type", discountType }
                };

            await FirebaseIntegration.Write("Trips/" + tripId + "/discount/", dic);
        }

        public static async Task<string> GetTripDispatchedStatus(string tripId)
        {
            dynamic resp = await GetTripDetails(tripId);
            if (!string.IsNullOrEmpty(resp.Body) && !resp.Body.Equals("null"))
            {
                Dictionary<string, dynamic> temp = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(resp.Body);
                return temp["isDispatchedRide"].ToString();
                //UpdateDiscountTypeAndAmount(false, tripId, temp["discount"]["amount"].ToString(), temp["discount"]["type"].ToString(), temp["isDispatchedRide"]);
            }
            return false.ToString();
        }

        public static async Task SetTripDispatchedStatus(string tripId, string isDispatchedRide)
        {
            await FirebaseIntegration.Write("Trips/" + tripId + "/isDispatchedRide", isDispatchedRide);
        }

        private static async Task SendInProcessLaterBookingReRouteNotfication(string bookingModeId, string captainId, string tripId, string passengerId, string deviceToken)
        {
            //step 1 : Free current captain

            await SetDriverFree(captainId, tripId);
            await RemoveDriverFromTrip(captainId, tripId);
            await DeletePassengerTrip(passengerId);

            //step 2 : update passenger about trip status
            if (int.Parse(bookingModeId) == (int)BookingModes.UserApplication)
            {
                Dictionary<string, string> dic = new Dictionary<string, string>
                    {
                        { "tripID", tripId }
                    };
                await PushyService.UniCast(deviceToken, dic, NotificationKeys.pas_InProcessLaterBookingReRouted);
            }
        }

        private static async Task SendReRouteNotfication(string bookingModeId, string reRouteRequestTime, string requestTimeOut, string captainId, string tripId, string deviceToken)
        {
            //step 1 : Free current captain
            await SetDriverFree(captainId, tripId);
            await RemoveDriverFromTrip(captainId, tripId);

            //step 2 : update passenger about trip status
            if (int.Parse(bookingModeId) == (int)BookingModes.UserApplication)
            {
                Dictionary<string, string> dic = new Dictionary<string, string>
                    {
                        { "tripID", tripId },
                        { "requestTimeOut", requestTimeOut },
                        { "reRouteRequestTime", reRouteRequestTime }
                    };
                await PushyService.UniCast(deviceToken, dic, NotificationKeys.pas_rideReRouted);
            }
        }

        public static async Task SetPassengerTrip(string passengerId, string tripId)
        {
            Dictionary<string, string> data = new Dictionary<string, string>
                            {
                                { "tripID", tripId}
                            };
            await FirebaseIntegration.Write("CustomerTrips/" + passengerId, data);
        }

        public static async Task DeletePassengerTrip(string passengerId)
        {
            await FirebaseIntegration.Delete("CustomerTrips/" + passengerId + "/tripID");
        }

        public static async Task FreePassengerFromCurrentTrip(string passengerId, string tripId)
        {
            var oldResp = await FirebaseIntegration.Read("CustomerTrips/" + passengerId);
            if (!string.IsNullOrEmpty(oldResp.Body) && !oldResp.Body.Equals("null"))
            {
                //make sure if user is in in same trip (may be in another trip, or in walkin trip payment processing)
                //TBD: Create new class for firebase user
                FirebaseDriver customerTrips = JsonConvert.DeserializeObject<FirebaseDriver>(oldResp.Body);

                if (customerTrips.tripId.Equals(tripId))
                    await DeletePassengerTrip(passengerId);
            }
        }

        private static async Task RemoveDriverFromTrip(string captainId, string tripId)
        {
            await FirebaseIntegration.Delete("Trips/" + tripId + "/" + captainId);
        }

        public static async Task SetDriverFree(string driverId, string tripId)
        {
            FirebaseDriver onlineDriver = await GetOnlineDriverById(driverId);
            if (onlineDriver != null)
            {
                string path = "OnlineDriver/" + driverId;

                //Dictionary<string, string> dic = new Dictionary<string, string>();
                if (string.IsNullOrEmpty(onlineDriver.OngoingRide))
                {
                    //dic.Add("OngoingRide", "");
                    //dic.Add("isBusy", "false");
                    await FirebaseIntegration.Update(path, new DriverStatus());
                }
                else if (onlineDriver.OngoingRide.Equals(tripId))
                {
                    //dic.Add("OngoingRide", "");
                    //dic.Add("isBusy", "false");
                    await FirebaseIntegration.Update(path, new DriverStatus());
                }
            }
        }

        public static async Task SetDriverBusy(string driverId, string tripId)
        {
            string path = "OnlineDriver/" + driverId;
            //Dictionary<string, string> dic = new Dictionary<string, string>();
            //dic.Add("OngoingRide", tripId);
            //dic.Add("isBusy", "true");
            await FirebaseIntegration.Update(path, new DriverStatus { OngoingRide = tripId, isBusy = "true" });
        }

        public static async Task SetPassengerLaterBookingCancelReasons()
        {
            await FirebaseIntegration.Write("CancelReasonsLaterBooking/Passenger", await CancelReasonsService.GetPassengerCancelReasons(false, true, false));
        }

        public static async Task SetDriverLaterBookingCancelReasons()
        {
            await FirebaseIntegration.Write("CancelReasonsLaterBooking/Captain", await CancelReasonsService.GetDriverCancelReasons(false, true, true));
        }

        #endregion

        #region Request Dispatched / ReRouted

        public static async Task SendNotificationsAfterDispatchingRide(string deviceToken, string driverId, string tripId)
        {
            //step 1 : Free current captain
            await SetDriverFree(driverId, tripId);

            //step 2 : Update driver about trip status
            Dictionary<string, string> dic = new Dictionary<string, string>
                    {
                        { "tripID", tripId }
                    };

            await PushyService.UniCast(deviceToken, dic, NotificationKeys.cap_rideDispatched);

            //step 3 : update trip node
            await RemoveTripDriverDetails(tripId, driverId);
        }

        public static async Task SetEstimateDistanceToPickUpLocation(string driverId, string estDistToPickUpLoc)
        {
            await FirebaseIntegration.Write("OnlineDriver/" + driverId + "/EstDistToPickUpLoc/", estDistToPickUpLoc);
        }

        public static async Task UpdateDriverEarnedPoints(string driverId, string earnedPoints)
        {
            await FirebaseIntegration.Update("OnlineDriver/" + driverId + "/", new DriverEarnedPoints
            {
                earningPoints = earnedPoints
            });
        }

        public static async Task<double> GetTripEstimatedDistanceOnArrival(string driverId)
        {
            var response = await FirebaseIntegration.Read("OnlineDriver/" + driverId + "/EstDistToPickUpLoc/");

            double distance = 0.0;

            if (!string.IsNullOrEmpty(response.Body) && !response.Body.Equals("null"))
            {
                distance = JsonConvert.DeserializeObject<double>(response.Body);
                //get upcoming laterbookings from database and write on firebase database
            }
            return distance;
        }

        #endregion

        public static async Task FareAlreadyPaidFreeUserAndDriver(string tripID, string userID, string driverID)
        {
            //delete trip node
            await DeleteTrip(tripID);

            //free driver
            await SetDriverFree(driverID, tripID);

            //free user
            await FreePassengerFromCurrentTrip(userID, tripID);
        }

        public static async Task WriteTripPassengerDetails(FirebasePassenger data, string passengerId)
        {
            await FirebaseIntegration.Write("Trips/" + data.TripId + "/" + passengerId, data);
        }

        public static async Task UpdateTripPassengerDetailsOnAccepted(PassengerRequestAcceptedNotification data, string passengerId)
        {
            await FirebaseIntegration.Write("Trips/" + data.TripId + "/" + passengerId, data);
        }

        public static async Task UpdateTripPassengerDetailsOnEnd(PaymentPendingPassenger data, string tripId, string passengerId)
        {
            await FirebaseIntegration.Write("Trips/" + tripId + "/" + passengerId, data);
        }

        public static async Task WriteTripDriverDetails(AcceptRideDriverModel data, string driverId)
        {
            await FirebaseIntegration.Write("Trips/" + data.tripID + "/" + driverId, data);
        }

        public static async Task UpdateTripDriverDetailsOnArrival(ArrivedDriverRideModel data, string tripID, string driverId, string earnedPoints)
        {
            await FirebaseIntegration.Write("Trips/" + tripID + "/" + driverId, data);
            await UpdateDriverEarnedPoints(driverId, earnedPoints);
        }

        public static async Task UpdateTripDriverDetailsOnStart(startDriverRideModel data, string tripID, string driverId)
        {
            await FirebaseIntegration.Write("Trips/" + tripID + "/" + driverId, data);
        }
        
        public static async Task UpdateTripDriverDetailsOnEnd(EndDriverRideModel data, string tripID, string driverId)
        {
            await FirebaseIntegration.Write("Trips/" + tripID + "/" + driverId, data);
        }

        public static async Task RemoveTripDriverDetails(string tripId, string driverId)
        {
            await FirebaseIntegration.Delete("Trips/" + tripId + "/" + driverId);
        }

        public static async Task SetTripStatus(string tripId, string status)
        {
            await FirebaseIntegration.Write("Trips/" + tripId + "/TripStatus", status);
        }

        public static async Task SetTipAmount(string tripId, string tipAmount)
        {
            await FirebaseIntegration.Write("Trips/" + tripId + "/TipAmount", tipAmount);
        }

        public static async Task<string> GetTipAmount(string tripId)
        {
            var response = await FirebaseIntegration.Read("Trips/" + tripId + "/TipAmount");

            if (!string.IsNullOrEmpty(response.Body) && !response.Body.Equals("null"))
            {
                return JsonConvert.DeserializeObject<string>(response.Body);
            }
            return "0";
        }

        public static async Task DeleteTrip(string tripId)
        {
            await FirebaseIntegration.Delete("Trips/" + tripId);
        }

        public static async Task AddPendingLaterBookings(string userID, string tripID, string pickupDateTime, string numberOfPerson)
        {
            await FirebaseIntegration.Write("PendingLaterBookings/" + tripID, new PendingLaterBooking
            {
                userID = userID,
                pickUpDateTime = pickupDateTime,
                numberOfPerson = numberOfPerson
            });
        }

        public static async Task DeletePendingLaterBooking(string tripId)
        {
            await FirebaseIntegration.Delete("PendingLaterBookings/" + tripId);
        }

        public static async Task<Dictionary<string, PendingLaterBooking>> GetPendingLaterBookings()
        {
            var pendingLaterBookings = new Dictionary<string, PendingLaterBooking>();

            var response = await FirebaseIntegration.Read("PendingLaterBookings");

            if (!string.IsNullOrEmpty(response.Body) && !response.Body.Equals("null"))
            {
                pendingLaterBookings = JsonConvert.DeserializeObject<Dictionary<string, PendingLaterBooking>>(response.Body);
            }

            return pendingLaterBookings;
        }

        public static async Task AddUpcomingLaterBooking(string driverId, UpcomingLaterBooking data)
        {
            await FirebaseIntegration.Write("UpcomingLaterBooking/" + driverId, data);
        }

        public static async Task DeleteUpcomingLaterBooking(string driverId)
        {
            await FirebaseIntegration.Delete("UpcomingLaterBooking/" + driverId);
        }

        public static async Task UpdateUpcomingBooking30MinuteFlag(string driverId)
        {
            await FirebaseIntegration.Update("UpcomingLaterBooking/" + driverId + "/isSend30MinutSendFCM", true);
        }
        
        public static async Task UpdateUpcomingBooking20MinuteFlag(string driverId)
        {
            await FirebaseIntegration.Update("UpcomingLaterBooking/" + driverId + "/isSend20MinutSendFCM", true);
        }

        public static async Task<Dictionary<string, UpcomingLaterBooking>> GetUpcomingLaterBookings()
        {
            var bookings = new Dictionary<string, UpcomingLaterBooking>();

            var response = await FirebaseIntegration.Read("UpcomingLaterBooking");

            if (!string.IsNullOrEmpty(response.Body) && !response.Body.Equals("null"))
            {
                bookings = JsonConvert.DeserializeObject<Dictionary<string, UpcomingLaterBooking>>(response.Body);
            }

            return bookings;
        }

        public static async Task SetTripCancelReasonsForPassenger(string tripId, string passengerId, Dictionary<string, dynamic> data)
        {
            await FirebaseIntegration.Write("Trips/" + tripId + "/" + passengerId + "/", data);
        }

        public static async Task SetCurrentTime()
        {
            await FirebaseIntegration.Write("CurenntDateTime/", DateTime.UtcNow.ToString(Formats.DateFormat));
        }

        public static async Task SetGlobalSettings()
        {
            var settings = await ApplicationSettingService.GetApplicationSettings(ConfigurationManager.AppSettings["ApplicationID"].ToString());
            await FirebaseIntegration.Write("GolbalSettings/PriorityHourEnableThreshold", settings.AwardPointsThreshold != null ? settings.AwardPointsThreshold.ToString() : "100");
        }
        
        public static async Task SetPriorityHourStatus(bool isActive, string priorityHourRemainingTime, string captainID, string priorityHourEndTime, string earnedPoints)
        {
            Dictionary<string, dynamic> dic = new Dictionary<string, dynamic>
                {
                    { "isPriorityHoursActive", isActive },
                    { "priorityHourEndTime", priorityHourEndTime },
                    { "earningPoints", earnedPoints },
                    { "priorityHourRemainingTime", priorityHourRemainingTime }
                };
            await FirebaseIntegration.Update("OnlineDriver/" + captainID + "/", dic);
        }

        public static async Task UpdatePriorityHourTime(string driverId, string priorityHourRemainingTime)
        {
            Dictionary<string, dynamic> dic = new Dictionary<string, dynamic>
                {
                    { "priorityHourRemainingTime", priorityHourRemainingTime }
                };
            await FirebaseIntegration.Update("OnlineDriver/" + driverId + "/", dic);
        }

        public static async Task UpdateUtilities(string path, string data)
        {
            await FirebaseIntegration.Write(path, data);
        }

        //#region Ride started

        //public bool updateDriverStatus(string driverID, string status, string tripId)
        //{
        //    client = new FireSharp.FirebaseClient(config);
        //    string path = "OnlineDriver/" + driverID;
        //    Dictionary<string, string> dic = new Dictionary<string, string>();

        //    if (status.Equals("false"))
        //    {
        //        //make sure driver is in the same trip we are trying to free
        //        var oldResp = client.Get("OnlineDriver/" + driverID);
        //        if (!string.IsNullOrEmpty(oldResp.Body) && !oldResp.Body.Equals("null"))
        //        {
        //            var onlineDriver = JsonConvert.DeserializeObject<FirbaseDriver>(oldResp.Body);

        //            if (onlineDriver.OngoingRide == null)
        //            {
        //                dic.Add("OngoingRide", "");
        //                dic.Add("isBusy", "false");
        //                client.Update(path, dic);
        //            }
        //            else if (onlineDriver.OngoingRide.Equals(tripId))
        //            {
        //                dic.Add("OngoingRide", "");
        //                dic.Add("isBusy", "false");
        //                client.Update(path, dic);
        //            }
        //        }
        //    }
        //    else
        //    {
        //        dic.Add("OngoingRide", tripId);
        //        dic.Add("isBusy", "true");
        //        client.Update(path, dic);
        //    }
        //    return true;
        //}

        //public async Task updateGo4Module(string captainName, string userName, string deviceToken)
        //{
        //    Go4ModuleModel fcm = new Go4ModuleModel()
        //    {
        //        captainName = captainName,
        //        passengerName = userName
        //    };
        //    await sentSingleFCM(deviceToken, fcm, "g4m_rideStarted");
        //}

        //private async void rideDataWriteOnFireBase(string statusRide, bool isUser, string path, dynamic data, string driverID, string userID, bool isWeb)
        //{
        //    client = new FireSharp.FirebaseClient(config);

        //    SetResponse rs = client.Set(path + "/TripStatus", statusRide);
        //    if (isUser)
        //    {
        //        //if (Enumration.returnRideFirebaseStatus(RideFirebaseStatus.RequestSent).Equals(statusRide))
        //        //{
        //        //    PassengerRideRequest pr = data;
        //        //    client.Set(path + "/Info", new Dictionary<string, dynamic> {
        //        //        {"isLaterBooking", pr.isLaterBooking },
        //        //        {"requestTimeOut", 300 },
        //        //        {"bookingDateTime", Common.getUtcDateTime() },
        //        //        {"bookingMode", pr.bookingMode },
        //        //    });
        //        //}

        //        rs = await client.SetTaskAsync(path + "/" + userID, data);
        //    }
        //    else
        //    {
        //        rs = await client.SetTaskAsync(path + "/" + driverID, data);
        //    }
        //}

        //public async Task<bool> updateChangeFareRequestStatus(string tripID, string captainID, string captainDeviceToken, string response, dynamic notificationData)
        //{
        //    client = new FireSharp.FirebaseClient(config);
        //    var path = "Trips/" + tripID + "/" + captainID + "/";
        //    Dictionary<string, string> dic = new Dictionary<string, string>
        //    {
        //        { "isFareChangePermissionGranted", response.ToLower()}
        //    };

        //    client.Update(path, dic);
        //    await sentSingleFCM(captainDeviceToken, notificationData, "cap_FareChangeResponse");

        //    return true;
        //}

        //#endregion

        //#region Ride End

        //public string getTripDistance(string tripPath)
        //{
        //    client = new FireSharp.FirebaseClient(config);
        //    var fbResponse = client.Get(tripPath);
        //    if (!string.IsNullOrEmpty(fbResponse.Body) && !fbResponse.Body.Equals("null"))
        //        return fbResponse.Body.Replace("\"", "");

        //    return "0";
        //}

        public static async Task<Dictionary<string, LocationUpdate>> GetTripPolyLineDetails(string tripId, string driverId)
        {
            var reponse = await FirebaseIntegration.Read("Trips/" + tripId + "/" + driverId + "/" + "polyline");
         
            if (!string.IsNullOrEmpty(reponse.Body) && !reponse.Body.Equals("null"))
            {
                //Key: RandomID, Value: Location Properties
                return JsonConvert.DeserializeObject<Dictionary<string, LocationUpdate>>(reponse.Body);
            }

            return new Dictionary<string, LocationUpdate>();
        }

        //#endregion

        //#region PayPal / CreditCard Payment

        //public async Task<bool> sendFCMForPaypalPaymentToPassenger(string fleetID, string isOverride, string vehicleID, string estimatedFare, string walletUsedAmount,
        // string walletTotalAmount, string voucherUsedAmount, string promoDiscountAmount,
        //    string totalFare, string duration, string distance, string paymentMode, string tripID, string driverID)
        //{
        //    using (CanTaxiResellerEntities context = new CanTaxiResellerEntities())
        //    {
        //        var resseller = context.spGetResellerPaypalAccountDetail(tripID, driverID).FirstOrDefault();
        //        if (resseller == null)
        //        {
        //            return false;
        //        }

        //        MobilePayment pa = PopulateMobilePaymentObject(fleetID, isOverride, vehicleID, estimatedFare, walletUsedAmount, walletTotalAmount, voucherUsedAmount, promoDiscountAmount, totalFare, duration, distance, paymentMode, tripID, driverID, resseller.UserID);

        //        string path = "Trips/" + tripID + "/";

        //        await FirebaseIntegration.RideDataWriteOnFireBase(App_Start.Enumration.returnRideFirebaseStatus(App_Start.RideFirebaseStatus.PaymentRequested), true, path, pa, "", resseller.UserID, false);
        //        await FirebaseIntegration.RideDataWriteOnFireBase(App_Start.Enumration.returnRideFirebaseStatus(App_Start.RideFirebaseStatus.PaymentRequested), false, path, pa, driverID, "", false);

        //        await sentSingleFCM(resseller.DeviceToken, pa, "pas_paypalPayment");

        //        return true;
        //    }
        //}

        //public static async Task<bool> sendFCMForCreditCardPaymentToPassenger(string fleetID, string isOverride, string vehicleID, string estimatedFare, string walletUsedAmount,
        //    string walletTotalAmount, string voucherUsedAmount, string promoDiscountAmount, string totalFare, string duration, string distance, string paymentMode, string tripID, string driverID)
        //{
        //        var result = TripsManagerService.GetTripPassengerDetailsByTripID(tripID);

        //        if (result == null)
        //        {
        //            return false;
        //        }

        //        MobilePayment pa = PopulateMobilePaymentObject(fleetID, isOverride, vehicleID, estimatedFare, walletUsedAmount, walletTotalAmount, voucherUsedAmount, promoDiscountAmount, totalFare, duration, distance, paymentMode, tripID, driverID, result.UserID);

        //        string path = "Trips/" + tripID + "/";

        //        await FirebaseIntegration.ridePassengerDataWriteOnFireBase(Enum.GetName(typeof(TripStatuses), (int)TripStatuses.PaymentRequested), path, pa, "", result.UserID, false);
        //        await FirebaseIntegration.rideDriverDataWriteOnFireBase(Enum.GetName(typeof(TripStatuses), (int)TripStatuses.PaymentRequested), path, pa, driverID, "", false);

        //        await PushyService.UniCast(result.DeviceToken, pa, "pas_creditCardPayment");
        //        return true;
        //}

        //private async void ridePassengerDataWriteOnFireBase(string statusRide, string path, dynamic data, string driverID, string userID, bool isWeb)
        //{
        //    client = new FireSharp.FirebaseClient(config);

        //    SetResponse rs = client.Set(path + "/TripStatus", statusRide);

        //    //if (Enumration.returnRideFirebaseStatus(RideFirebaseStatus.RequestSent).Equals(statusRide))
        //    //{
        //    //    PassengerRideRequest pr = data;
        //    //    client.Set(path + "/Info", new Dictionary<string, dynamic> {
        //    //        {"isLaterBooking", pr.isLaterBooking },
        //    //        {"requestTimeOut", 300 },
        //    //        {"bookingDateTime", Common.getUtcDateTime() },
        //    //        {"bookingMode", pr.bookingMode },
        //    //    });
        //    //}

        //    rs = await client.SetTaskAsync(path + "/" + userID, data);
        //}

        //private async void rideDriverDataWriteOnFireBase(string statusRide, string path, dynamic data, string driverID, string userID, bool isWeb)
        //{
        //    client = new FireSharp.FirebaseClient(config);

        //    SetResponse rs = client.Set(path + "/TripStatus", statusRide);

        //    rs = await client.SetTaskAsync(path + "/" + driverID, data);
        //}

        //private static MobilePayment PopulateMobilePaymentObject(string fleetID, string isOverride, string vehicleID, string estimatedFare, string walletUsedAmount, string walletTotalAmount, string voucherUsedAmount, string promoDiscountAmount, string totalFare, string duration, string distance, string paymentMode, string tripID, string driverID, string userID)
        //{
        //    return new MobilePayment()
        //    {
        //        estmateFare = estimatedFare,
        //        duration = duration,
        //        distance = distance,
        //        paymentMode = paymentMode,
        //        isPaymentRequested = true,
        //        tripID = tripID,
        //        isOverride = isOverride,
        //        vehicleID = vehicleID,
        //        promoDiscountAmount = promoDiscountAmount,
        //        voucherUsedAmount = voucherUsedAmount,
        //        walletAmountUsed = walletUsedAmount,
        //        totalFare = totalFare,
        //        fleetID = fleetID,
        //        walletTotalAmount = walletTotalAmount,
        //        paymentRequestTime = Common.getUtcDateTime().ToString(Common.dateFormat),
        //        driverID = driverID,
        //        userID = userID
        //    };
        //}

        //#endregion

        //#region Set Driver Busy / Free

        //public void freeUserFromTrip(string tripID, string userID)
        //{
        //    client = new FireSharp.FirebaseClient(config);

        //    var oldResp = client.Get("CustomerTrips/" + userID);
        //    if (!string.IsNullOrEmpty(oldResp.Body) && !oldResp.Body.Equals("null"))
        //    {
        //        //make sure if user is in in same trip (may be in another trip, or in walkin trip payment processing)
        //        //TBD: Create new class for firebase user
        //        var customerTrips = JsonConvert.DeserializeObject<FirbaseDriver>(oldResp.Body);
        //        if (customerTrips.tripID.Equals(tripID))
        //            client.Delete("CustomerTrips/" + userID + "/tripID");
        //    }
        //}

        //public void freeUserFromWalkInTrip(string userId, string tripId)
        //{
        //    client = new FireSharp.FirebaseClient(config);

        //    //Make sure user is in same walkin trip

        //    var oldResp = client.Get("CustomerTrips/" + userId + "/WalkInPaymentDetails");
        //    if (!string.IsNullOrEmpty(oldResp.Body) && !oldResp.Body.Equals("null"))
        //    {
        //        var customerWalkInTrip = JsonConvert.DeserializeObject<WalkInTrip>(oldResp.Body);

        //        if (customerWalkInTrip.newTripID.Equals(tripId))
        //        {
        //            client.Delete("CustomerTrips/" + userId + "/isWalkInPaymentRequested");
        //            client.Delete("CustomerTrips/" + userId + "/WalkInPaymentDetails");
        //        }
        //    }
        //}

        //public void freeDriverFromWalkInTrip(string driverId, string tripId)
        //{
        //    client = new FireSharp.FirebaseClient(config);

        //    //make sure driver is in the same trip we are trying to free

        //    var oldResp = client.Get("OnlineDriver/" + driverId + "/WalkInCustomer/PaymentDetails");
        //    if (!string.IsNullOrEmpty(oldResp.Body) && !oldResp.Body.Equals("null"))
        //    {
        //        var driverWalkInTrip = JsonConvert.DeserializeObject<walkInPassengerPaypalPaymentFCM>(oldResp.Body);

        //        if (driverWalkInTrip.newTripID.Equals(tripId))
        //        {
        //            client.Delete("OnlineDriver/" + driverId + "/WalkInCustomer/PaymentDetails");
        //        }
        //    }
        //}

        //public void setWalkInPaymentData(string driverID, string userID, walkInPassengerPaypalPaymentFCM pfcm)
        //{
        //    client = new FireSharp.FirebaseClient(config);
        //    client.Set("OnlineDriver/" + driverID + "/WalkInCustomer/PaymentDetails/", pfcm);
        //    client.Set("CustomerTrips/" + userID + "/isWalkInPaymentRequested", "true");
        //    client.Set("CustomerTrips/" + userID + "/WalkInPaymentDetails/", pfcm);
        //}

        //public void removeWalkInPaymentData(string driverID, string userID, string tripID)
        //{
        //    freeUserFromWalkInTrip(userID, tripID);
        //    freeDriverFromWalkInTrip(driverID, tripID);
        //}

        //#endregion

        //#region Delete Trip

        //public async Task<bool> delTripNode(string tripID)
        //{
        //    client = new FireSharp.FirebaseClient(config);
        //    string path = "Trips/" + tripID;
        //    await client.DeleteTaskAsync(path);
        //    return true;
        //}

        //#endregion

        //#region Update Trip Status

        //public void changeTripStatus(string tripPath, string statusRide)
        //{
        //    client = new FireSharp.FirebaseClient(config);
        //    client.Set(tripPath + "/TripStatus", statusRide);
        //}

        //#endregion

        //#region	Later Booking
        //public void addRemovePendingLaterBookings(bool isAdd, string userID, string tripID, string pickupDateTime, int numberOfPerson)
        //{
        //    client = new FireSharp.FirebaseClient(config);
        //    if (isAdd)
        //    {
        //        Dictionary<string, dynamic> dic = new Dictionary<string, dynamic>() {
        //                            {"userID",userID},
        //                            {"pickupDateTime", pickupDateTime },
        //                            {"numberOfPerson", numberOfPerson }
        //                        };

        //        SetResponse rs = client.Set("PendingLaterBookings/" + tripID, dic);
        //    }
        //    else
        //    {
        //        client.Delete("PendingLaterBookings/" + tripID);
        //        client.Delete("Trips/" + tripID);
        //    }
        //}

        //public bool addDeleteNode(bool isDel, dynamic model, string path)
        //{
        //    client = new FireSharp.FirebaseClient(config);
        //    if (isDel)
        //    {
        //        client.Delete(path);
        //    }
        //    else
        //    {
        //        client.Set(path, model);
        //    }
        //    return true;
        //}

        //#endregion

        //#region Priority Hour
    }
}
