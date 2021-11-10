using Constants;
using DTOs.API;
using Integrations;
using Newtonsoft.Json;
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
        //#region Single Login

        //public bool isUserInTrip(string UserID)
        //{
        //    string path = "CustomerTrips/" + UserID;
        //    client = new FireSharp.FirebaseClient(config);
        //    FirebaseResponse resp = client.Get(path);

        //    if (string.IsNullOrEmpty(resp.Body) || resp.Body.Equals("null") || resp.Body.Equals("\"\""))
        //        return false;
        //    else
        //        return true;
        //}

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

        //#endregion

        #region Online / Offline Driver

        public static async Task<Dictionary<string, dynamic>> GetOnlineDrivers()
        {
            //get online driver from firebase
            //client = new FireSharp.FirebaseClient(config);
            //FirebaseResponse resp = client.Get();

            var fbOnlineDrivers = await FirebaseIntegration.Read("OnlineDriver");

            //Key: DriverID, Value: Online Driver Properties

            Dictionary<string, dynamic> rawOnlineDrivers = new Dictionary<string, dynamic>();

            if (!string.IsNullOrEmpty(fbOnlineDrivers.Body) && !fbOnlineDrivers.Body.Equals("null"))
            {
                rawOnlineDrivers = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(fbOnlineDrivers.Body);

                //Make sure driver node have actual data

                var temp = Guid.Empty;

                rawOnlineDrivers = rawOnlineDrivers.Where(kv => Guid.TryParse(kv.Key, out temp) == true).ToDictionary(kv => kv.Key, kv => kv.Value);
            }

            return rawOnlineDrivers;
        }

        public static async Task<FirebaseDriver> GetOnlineDriver(string driverId)
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
                                              int categoryId, string registrationYear, string color)
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
                priorityHourRemainingTime = isPriorityHoursActive == true ? ((int)(DateTime.Parse(priorityHourEndTime).
                Subtract(DateTime.Parse(DateTime.UtcNow.ToString(Formats.DateFormat))).TotalMinutes)).ToString() : "0",
                lastUpdated = "",
                ongoingRide = "",

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

        public static async Task<bool> SendRideRequestToOnlineDrivers(string tripId, string passengerId, int reqSeatingCapacity, DriverBookingRequestNotification bookingRN, dynamic hotelSetting)
        {
            //Passenger data
            if (bool.Parse(bookingRN.IsReRouteRequest))
            {
                if (!bool.Parse(bookingRN.IsLaterBooking))
                {
                    bookingRN.ReRouteRequestTime = DateTime.UtcNow.ToString(Formats.DateFormat);
                    await WriteTripPassengerDetails(bookingRN, passengerId);
                    await SetTripStatus(tripId, Enum.GetName(typeof(TripStatuses), TripStatuses.ReRouting));

                    // free captain > update user > send request 
                    await SendReRouteNotfication(bookingRN.BookingModeId, bookingRN.ReRouteRequestTime, bookingRN.RequestTimeOut.ToString(), bookingRN.PreviousCaptainId.ToString(), tripId, bookingRN.DeviceToken); //passengerId, 
                }
                else
                {
                    await WriteTripPassengerDetails(bookingRN, passengerId);
                    await SetTripStatus(tripId, Enum.GetName(typeof(TripStatuses), TripStatuses.RequestSent));

                    //UPDATE: Captain can canel inprocess later booking, so user / captain should be set free in every case.
                    // free captain > update user > send request 
                    await SendInProcessLaterBookingReRouteNotfication(bookingRN.BookingModeId, bookingRN.PreviousCaptainId.ToString(), tripId, passengerId, bookingRN.DeviceToken);
                }
            }
            else
            {
                await WriteTripPassengerDetails(bookingRN, passengerId);
                await SetTripStatus(tripId, Enum.GetName(typeof(TripStatuses), TripStatuses.RequestSent));

                //NEW IMPLEMENTATION
                await UpdateDiscountTypeAndAmount(tripId, bookingRN.DiscountAmount, bookingRN.DiscountType);
            }

            #region MakePreferredAndNormalDriversList

            //Key: DriverID, Value: Online Driver Properties

            List<FirebaseDriver> lstNormalCaptains = new List<FirebaseDriver>();
            List<FirebaseDriver> lstPreferredCaptains = new List<FirebaseDriver>();

            string captainIDs = "";
            string preferredCaptainIDs = "";

            var rawOnlineDrivers = await GetOnlineDrivers();

            foreach (var od in rawOnlineDrivers)
            {
                FirebaseDriver driver = JsonConvert.DeserializeObject<FirebaseDriver>(JsonConvert.SerializeObject(od.Value));
                if (string.IsNullOrEmpty(driver.driverID) || driver.location == null) //Dirty data
                {
                    continue;
                }
                else
                {
                    //If user have requested some facilities then request should be sent to only eligible captains
                    if (!string.IsNullOrEmpty(bookingRN.RequiredFacilities))
                    {
                        var reqFac = bookingRN.RequiredFacilities.ToLower().Split(',');
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
                        if (bool.Parse(bookingRN.IsLaterBooking))
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
                        if (bool.Parse(bookingRN.IsLaterBooking))
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
            }
            #endregion

            //CaptainID won't be null in case of ReRouted trip request and don't send request to that captain again who have cancelled the current trip. 
            //TBD: All the captains who ever accepted the trip can be excluded using ReroutedRidesLog table

            if (!string.IsNullOrEmpty(bookingRN.PreviousCaptainId))
            {
                lstNormalCaptains.RemoveAll(x => x.driverID.Equals(bookingRN.PreviousCaptainId));
                lstPreferredCaptains.RemoveAll(x => x.driverID.Equals(bookingRN.PreviousCaptainId));
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
            await SetTripDispatchedStatus(tripId, bookingRN.IsDispatchedRide);

            string applicationId = ConfigurationManager.AppSettings["ApplicationID"].ToString();
            var applicationSettings = await ApplicationSettingService.GetApplicationSettings(applicationId);
            var distanceInterval = applicationSettings.RequestRadiusInterval;
            int? minDistanceRange = 0;
            var maxDistanceRange = distanceInterval;
            var requestSearchRange = bool.Parse(bookingRN.IsLaterBooking) ? (int)(applicationSettings.LaterBookingRequestSearchRange * 1000) : (int)(applicationSettings.RequestSearchRange * 1000);
            var captainMinRating = applicationSettings.CaptainMinRating; 
            var pickPosition = new GeoCoordinate(Convert.ToDouble(bookingRN.PickUpLatitude), Convert.ToDouble(bookingRN.PickUpLongitude));

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

                foreach (var dr in lstPreferredCaptains)
                {
                    var capDetail = lstPreferredCaptainsDetail.Where(c => c.CaptainID.ToString().Equals(dr.driverID)).FirstOrDefault();
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
                                lstFilteredPreferredCaptains.Add(capDetail.DeviceToken, Convert.ToBoolean(bookingRN.IsLaterBooking) ? capDetail.LaterBookingNotificationTone : capDetail.NormalBookingNotificationTone);

                                lstRequestLog.Add(new TripRequestLogDTO
                                {
                                    CaptainID = Guid.Parse(dr.driverID),
                                    RequestLogID = Guid.NewGuid(),
                                    CaptainLocationLatitude = dr.location.l[0].ToString(),
                                    CaptainLocationLongitude = dr.location.l[1].ToString(),
                                    DistanceToPickUpLocation = distance,
                                    isReRouteRequest = bool.Parse(bookingRN.IsReRouteRequest),
                                    TimeStamp = DateTime.UtcNow,
                                    TripID = Guid.Parse(tripId)
                                });
                            }
                        }
                    }
                }

                //if request is already accpeted then no need to save current drivers list in log, so break loop 
                if (await IsRequestAccepted(tripId))
                {
                    break;
                }
                else if (lstFilteredPreferredCaptains.Any())
                {
                    await PushyService.BroadCastNotification(lstFilteredPreferredCaptains, bookingRN);
                    await TripsManagerService.LogBookRequestRecipientDrivers(lstRequestLog);
                    Thread.Sleep(Convert.ToInt32(applicationSettings.RequestWaitingTime * 1000));
                }

                //TBD: Loop Optimization - Remove currently selected drivers from lstPreferredOnlineDriver

                lstRequestLog = new List<TripRequestLogDTO>();

                foreach (var dr in lstNormalCaptains)
                {
                    var capDetail = lstNormalCaptainsDetail.Where(c => c.CaptainID.ToString().Equals(dr.driverID)).FirstOrDefault();
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
                                lstFilteredNormalCaptains.Add(capDetail.DeviceToken, Convert.ToBoolean(bookingRN.IsLaterBooking) ? capDetail.LaterBookingNotificationTone : capDetail.NormalBookingNotificationTone);

                                lstRequestLog.Add(new TripRequestLogDTO
                                {
                                    CaptainID = Guid.Parse(dr.driverID),
                                    RequestLogID = Guid.NewGuid(),
                                    CaptainLocationLatitude = dr.location.l[0].ToString(),
                                    CaptainLocationLongitude = dr.location.l[1].ToString(),
                                    DistanceToPickUpLocation = distance,
                                    isReRouteRequest = bool.Parse(bookingRN.IsReRouteRequest),
                                    TimeStamp = DateTime.UtcNow,
                                    TripID = Guid.Parse(tripId)
                                });
                            }
                        }
                    }
                }

                //TBD: Loop Optimization - Remove currently selected drivers from lstOnlineDriver

                //if request is already accpeted then no need to save current drivers list in log, so break loop 
                if (await IsRequestAccepted(tripId))
                {
                    break;
                }
                else if (lstFilteredNormalCaptains.Any())
                {
                    await PushyService.BroadCastNotification(lstFilteredNormalCaptains, bookingRN);
                    await TripsManagerService.LogBookRequestRecipientDrivers(lstRequestLog);
                    Thread.Sleep(Convert.ToInt32(applicationSettings.RequestWaitingTime * 1000));
                }

                minDistanceRange = maxDistanceRange;
                maxDistanceRange = maxDistanceRange + distanceInterval > requestSearchRange ? requestSearchRange : maxDistanceRange + distanceInterval;

            } while (!await IsRequestAccepted(tripId) && minDistanceRange < requestSearchRange);

            return true; //Ride is accepted

            #region ApplicationSettingsFiltersBeforeSendingRideRequest

            //using (CanTaxiResellerEntities context = new CanTaxiResellerEntities())
            //{
            //    //Get preferred captain details from db - device token and ringtone .
            //    var lstPreferredCaptains = new List<spGetOnlineDriver_Result>();
            //    if (!string.IsNullOrEmpty(preferredCaptainIDs))
            //        lstPreferredCaptains = context.spGetOnlineDriver(preferredCaptainIDs).ToList();

            //    Dictionary<string, string> lstPrefer1OnlineDriverPreferred = new Dictionary<string, string>();
            //    Dictionary<string, string> lstPrefer2OnlineDriverPreferred = new Dictionary<string, string>();
            //    Dictionary<string, string> lstPrefer3OnlineDriverPreferred = new Dictionary<string, string>();
            //    Dictionary<string, string> lstWithOutPreferOnlineDriverPreferred = new Dictionary<string, string>();

            //    //Get captain details from db - device token and ringtone .
            //    var lstcaptains = new List<spGetOnlineDriver_Result>();
            //    if (!string.IsNullOrEmpty(captainIDs))
            //        lstcaptains = context.spGetOnlineDriver(captainIDs).ToList();

            //    Dictionary<string, string> lstPrefer1OnlineDriver = new Dictionary<string, string>();
            //    Dictionary<string, string> lstPrefer2OnlineDriver = new Dictionary<string, string>();
            //    Dictionary<string, string> lstPrefer3OnlineDriver = new Dictionary<string, string>();
            //    Dictionary<string, string> lstWithOutPreferOnlineDriver = new Dictionary<string, string>();

            //    var applicationSettings = context.ApplicationSettings.Where(a => a.ApplicationID.ToString().Equals(applicationID)).FirstOrDefault();

            //    var distanceRange1 = applicationSettings.Preference1Distance.Split(';');
            //    var distanceRange2 = applicationSettings.Preference2Distance.Split(';');
            //    var distanceRange3 = applicationSettings.Preference3Distance.Split(';');

            //    //List of drivers who have active priority hours
            //    if (lstPreferredOnlineDriver.Count > 0)
            //    {
            //        foreach (var dr in lstPreferredOnlineDriver)
            //        {
            //            var capDetail = lstPreferredCaptains.Where(c => c.CaptainID.ToString().Equals(dr.driverID)).FirstOrDefault();
            //            if (capDetail != null)
            //            {
            //                //find distance
            //                var driverPosition = new GeoCoordinate(dr.location.l[0], dr.location.l[1]);
            //                var pickPosition = new GeoCoordinate(Convert.ToDouble(tp.PickupLocationLatitude), Convert.ToDouble(tp.PickupLocationLongitude));
            //                var distance = driverPosition.GetDistanceTo(pickPosition) / 1000;

            //                if (distance <= applicationSettings.RequestSearchRange)
            //                {
            //                    //If priority hour is active but not in priority area range then add in last preference of priority drivers
            //                    if (distance <= applicationSettings.PriorityAreaRange)
            //                    {
            //                        if (hotelSetting == null)
            //                        {
            //                            //check preference1
            //                            if (capDetail.Rating >= applicationSettings.Preference1Rating && dr.makeID == applicationSettings.Preference1Make && distance >= Convert.ToDouble(distanceRange1[0]) && distance <= Convert.ToDouble(distanceRange1[1]) && Convert.ToInt32(dr.seatingCapacity) >= reqSeatingCapacity)
            //                            {
            //                                lstPrefer1OnlineDriverPreferred.Add(capDetail.DeviceToken, Convert.ToBoolean(tp.isLaterBooking) ? capDetail.LaterBookingNotificationTone : capDetail.NormalBookingNotificationTone);
            //                            }
            //                            //check preference2
            //                            else if (capDetail.Rating >= applicationSettings.Preference2Rating && dr.makeID == applicationSettings.Preference2Make && distance >= Convert.ToDouble(distanceRange2[0]) && distance <= Convert.ToDouble(distanceRange2[1]) && Convert.ToInt32(dr.seatingCapacity) >= reqSeatingCapacity)
            //                            {
            //                                lstPrefer2OnlineDriverPreferred.Add(capDetail.DeviceToken, Convert.ToBoolean(tp.isLaterBooking) ? capDetail.LaterBookingNotificationTone : capDetail.NormalBookingNotificationTone);
            //                            }
            //                            //check preference3
            //                            else if (capDetail.Rating >= applicationSettings.Preference3Rating && dr.makeID == applicationSettings.Preference3Make && distance >= Convert.ToDouble(distanceRange3[0]) && distance <= Convert.ToDouble(distanceRange3[1]) && Convert.ToInt32(dr.seatingCapacity) >= reqSeatingCapacity)
            //                            {
            //                                lstPrefer3OnlineDriverPreferred.Add(capDetail.DeviceToken, Convert.ToBoolean(tp.isLaterBooking) ? capDetail.LaterBookingNotificationTone : capDetail.NormalBookingNotificationTone);
            //                            }
            //                            //add in without preference
            //                            else if (Convert.ToInt32(dr.seatingCapacity) >= reqSeatingCapacity)
            //                            {
            //                                lstWithOutPreferOnlineDriverPreferred.Add(capDetail.DeviceToken, Convert.ToBoolean(tp.isLaterBooking) ? capDetail.LaterBookingNotificationTone : capDetail.NormalBookingNotificationTone);
            //                            }
            //                        }
            //                        else
            //                        {
            //                            //check preference1
            //                            if (capDetail.Rating >= hotelSetting.HotelPreference1Rating && dr.makeID == hotelSetting.HotelPreference1Make && distance >= Convert.ToDouble(distanceRange1[0]) && distance <= Convert.ToDouble(distanceRange1[1]) && Convert.ToInt32(dr.seatingCapacity) >= reqSeatingCapacity)
            //                            {
            //                                lstPrefer1OnlineDriverPreferred.Add(capDetail.DeviceToken, Convert.ToBoolean(tp.isLaterBooking) ? capDetail.LaterBookingNotificationTone : capDetail.NormalBookingNotificationTone);
            //                            }
            //                            //check preference2
            //                            else if (capDetail.Rating >= hotelSetting.HotelPreference2Rating && dr.makeID == hotelSetting.HotelPreference2Make && distance >= Convert.ToDouble(distanceRange2[0]) && distance <= Convert.ToDouble(distanceRange2[1]) && Convert.ToInt32(dr.seatingCapacity) >= reqSeatingCapacity)
            //                            {
            //                                lstPrefer2OnlineDriverPreferred.Add(capDetail.DeviceToken, Convert.ToBoolean(tp.isLaterBooking) ? capDetail.LaterBookingNotificationTone : capDetail.NormalBookingNotificationTone);
            //                            }
            //                            //check preference3
            //                            else if (capDetail.Rating >= hotelSetting.HotelPreference3Rating && dr.makeID == hotelSetting.HotelPreference3Make && distance >= Convert.ToDouble(distanceRange3[0]) && distance <= Convert.ToDouble(distanceRange3[1]) && Convert.ToInt32(dr.seatingCapacity) >= reqSeatingCapacity)
            //                            {
            //                                lstPrefer3OnlineDriverPreferred.Add(capDetail.DeviceToken, Convert.ToBoolean(tp.isLaterBooking) ? capDetail.LaterBookingNotificationTone : capDetail.NormalBookingNotificationTone);
            //                            }
            //                            //add in without preference
            //                            else if (Convert.ToInt32(dr.seatingCapacity) >= reqSeatingCapacity)
            //                            {
            //                                lstWithOutPreferOnlineDriverPreferred.Add(capDetail.DeviceToken, Convert.ToBoolean(tp.isLaterBooking) ? capDetail.LaterBookingNotificationTone : capDetail.NormalBookingNotificationTone);
            //                            }
            //                        }
            //                    }
            //                    else
            //                    {
            //                        lstWithOutPreferOnlineDriverPreferred.Add(capDetail.DeviceToken, Convert.ToBoolean(tp.isLaterBooking) ? capDetail.LaterBookingNotificationTone : capDetail.NormalBookingNotificationTone);
            //                    }
            //                }
            //            }
            //        }
            //    }

            //    //List of drivers whose priority hour is not active
            //    if (lstOnlineDriver.Count > 0)
            //    {
            //        foreach (var dr in lstOnlineDriver)
            //        {
            //            var capDetail = lstcaptains.Where(c => c.CaptainID.ToString().Equals(dr.driverID)).FirstOrDefault();
            //            if (capDetail != null)
            //            {
            //                //find distance
            //                var driverPosition = new GeoCoordinate(dr.location.l[0], dr.location.l[1]);
            //                var pickPosition = new GeoCoordinate(Convert.ToDouble(tp.PickupLocationLatitude), Convert.ToDouble(tp.PickupLocationLongitude));
            //                var distance = driverPosition.GetDistanceTo(pickPosition) / 1000;

            //                if (distance <= applicationSettings.RequestSearchRange)
            //                {
            //                    if (hotelSetting == null)
            //                    {
            //                        //check preference1
            //                        if (capDetail.Rating >= applicationSettings.Preference1Rating && dr.makeID == applicationSettings.Preference1Make && distance >= Convert.ToDouble(distanceRange1[0]) && distance <= Convert.ToDouble(distanceRange1[1]) && Convert.ToInt32(dr.seatingCapacity) >= reqSeatingCapacity)
            //                        {
            //                            lstPrefer1OnlineDriver.Add(capDetail.DeviceToken, Convert.ToBoolean(tp.isLaterBooking) ? capDetail.LaterBookingNotificationTone : capDetail.NormalBookingNotificationTone);
            //                        }
            //                        //check preference2
            //                        else if (capDetail.Rating >= applicationSettings.Preference2Rating && dr.makeID == applicationSettings.Preference2Make && distance >= Convert.ToDouble(distanceRange2[0]) && distance <= Convert.ToDouble(distanceRange2[1]) && Convert.ToInt32(dr.seatingCapacity) >= reqSeatingCapacity)
            //                        {
            //                            lstPrefer2OnlineDriver.Add(capDetail.DeviceToken, Convert.ToBoolean(tp.isLaterBooking) ? capDetail.LaterBookingNotificationTone : capDetail.NormalBookingNotificationTone);
            //                        }
            //                        //check preference3
            //                        else if (capDetail.Rating >= applicationSettings.Preference3Rating && dr.makeID == applicationSettings.Preference3Make && distance >= Convert.ToDouble(distanceRange3[0]) && distance <= Convert.ToDouble(distanceRange3[1]) && Convert.ToInt32(dr.seatingCapacity) >= reqSeatingCapacity)
            //                        {
            //                            lstPrefer3OnlineDriver.Add(capDetail.DeviceToken, Convert.ToBoolean(tp.isLaterBooking) ? capDetail.LaterBookingNotificationTone : capDetail.NormalBookingNotificationTone);
            //                        }
            //                        //add in without preference
            //                        else if (Convert.ToInt32(dr.seatingCapacity) >= reqSeatingCapacity)
            //                        {
            //                            lstWithOutPreferOnlineDriver.Add(capDetail.DeviceToken, Convert.ToBoolean(tp.isLaterBooking) ? capDetail.LaterBookingNotificationTone : capDetail.NormalBookingNotificationTone);
            //                        }
            //                    }
            //                    else
            //                    {
            //                        //check preference1
            //                        if (capDetail.Rating >= hotelSetting.HotelPreference1Rating && dr.makeID == hotelSetting.HotelPreference1Make && distance >= Convert.ToDouble(distanceRange1[0]) && distance <= Convert.ToDouble(distanceRange1[1]) && Convert.ToInt32(dr.seatingCapacity) >= reqSeatingCapacity)
            //                        {
            //                            lstPrefer1OnlineDriver.Add(capDetail.DeviceToken, Convert.ToBoolean(tp.isLaterBooking) ? capDetail.LaterBookingNotificationTone : capDetail.NormalBookingNotificationTone);
            //                        }
            //                        //check preference2
            //                        else if (capDetail.Rating >= hotelSetting.HotelPreference2Rating && dr.makeID == hotelSetting.HotelPreference2Make && distance >= Convert.ToDouble(distanceRange2[0]) && distance <= Convert.ToDouble(distanceRange2[1]) && Convert.ToInt32(dr.seatingCapacity) >= reqSeatingCapacity)
            //                        {
            //                            lstPrefer2OnlineDriver.Add(capDetail.DeviceToken, Convert.ToBoolean(tp.isLaterBooking) ? capDetail.LaterBookingNotificationTone : capDetail.NormalBookingNotificationTone);
            //                        }
            //                        //check preference3
            //                        else if (capDetail.Rating >= hotelSetting.HotelPreference3Rating && dr.makeID == hotelSetting.HotelPreference3Make && distance >= Convert.ToDouble(distanceRange3[0]) && distance <= Convert.ToDouble(distanceRange3[1]) && Convert.ToInt32(dr.seatingCapacity) >= reqSeatingCapacity)
            //                        {
            //                            lstPrefer3OnlineDriver.Add(capDetail.DeviceToken, Convert.ToBoolean(tp.isLaterBooking) ? capDetail.LaterBookingNotificationTone : capDetail.NormalBookingNotificationTone);
            //                        }
            //                        //add in without preference
            //                        else if (Convert.ToInt32(dr.seatingCapacity) >= reqSeatingCapacity)
            //                        {
            //                            lstWithOutPreferOnlineDriver.Add(capDetail.DeviceToken, Convert.ToBoolean(tp.isLaterBooking) ? capDetail.LaterBookingNotificationTone : capDetail.NormalBookingNotificationTone);
            //                        }
            //                    }
            //                }
            //            }
            //        }
            //    }

            //    //Final data to be sent to drivers in FCM
            //    RequestResponse response = new RequestResponse
            //    {
            //        tripID = tp.TripID.ToString(),
            //        lat = tp.PickupLocationLatitude,
            //        lan = tp.PickupLocationLongitude,
            //        dropOfflatitude = tp.DropOffLocationLatitude,
            //        dropOfflongitude = tp.DropOffLocationLongitude,
            //        pickUpLocation = tp.PickUpLocation,
            //        dropOffLocation = tp.DropOffLocation,
            //        numberOfPerson = Convert.ToInt32(tp.NoOfPerson),
            //        paymentMethod = tp.TripPaymentMode,
            //        isLaterBooking = Convert.ToBoolean(pr.isLaterBooking),
            //        pickUpDateTime = pr.pickUpDateTime.ToString(Common.dateFormat),
            //        isWeb = pr.isWeb,
            //        requiredFacilities = pr.requiredFacilities,
            //        facilities = pr.facilities,
            //        discountType = pr.discountType,
            //        discountAmount = pr.discountAmount,
            //        isReRouteRequest = pr.isReRouteRequest,
            //        isDispatchedRide = "false",
            //        description = "",
            //        fav = fav
            //    };

            //    int counter = 0;

            //    await updateDiscountTypeAndAmount(false, path, response.tripID, response.discountAmount ?? "", response.discountType ?? "", response.isDispatchedRide ?? "");

            //    //var task = Task.Run(async () =>
            //    //{
            //    //	await updateDiscountTypeAndAmount(false, path, response.tripID, response.discountAmount ?? "", response.discountType ?? "", response.isDispatchedRide ?? "");
            //    //});

            //    //Send priority based FCMs
            //    do
            //    {
            //        if (counter == 0)
            //        {
            //            if (lstPrefer1OnlineDriverPreferred.Count > 0)
            //            {
            //                NotifyAsync(lstPrefer1OnlineDriverPreferred, response);
            //                Thread.Sleep(Convert.ToInt32(applicationSettings.RequestWaitingTime * 1000));
            //            }
            //            counter++;
            //        }
            //        else if (counter == 1 && !Enumration.isRequestAccepted)
            //        {
            //            if (lstPrefer2OnlineDriverPreferred.Count > 0)
            //            {
            //                NotifyAsync(lstPrefer2OnlineDriverPreferred, response);
            //                Thread.Sleep(Convert.ToInt32(applicationSettings.RequestWaitingTime * 1000));
            //            }
            //            counter++;
            //        }
            //        else if (counter == 2 && !Enumration.isRequestAccepted)
            //        {
            //            if (lstPrefer3OnlineDriverPreferred.Count > 0)
            //            {
            //                NotifyAsync(lstPrefer3OnlineDriverPreferred, response);
            //                Thread.Sleep(Convert.ToInt32(applicationSettings.RequestWaitingTime * 1000));
            //            }
            //            counter++;
            //        }
            //        else if (counter == 3 && !Enumration.isRequestAccepted)
            //        {
            //            if (lstWithOutPreferOnlineDriverPreferred.Count > 0)
            //            {
            //                NotifyAsync(lstWithOutPreferOnlineDriverPreferred, response);
            //                Thread.Sleep(Convert.ToInt32(applicationSettings.RequestWaitingTime * 1000));
            //            }
            //            counter++;
            //        }
            //        else if (counter == 4 && !Enumration.isRequestAccepted)
            //        {
            //            if (lstPrefer1OnlineDriver.Count > 0)
            //            {
            //                NotifyAsync(lstPrefer1OnlineDriver, response);
            //                Thread.Sleep(Convert.ToInt32(applicationSettings.RequestWaitingTime * 1000));
            //            }
            //            counter++;
            //        }
            //        else if (counter == 5 && !Enumration.isRequestAccepted)
            //        {
            //            if (lstPrefer2OnlineDriver.Count > 0)
            //            {
            //                NotifyAsync(lstPrefer2OnlineDriver, response);
            //                Thread.Sleep(Convert.ToInt32(applicationSettings.RequestWaitingTime * 1000));
            //            }
            //            counter++;
            //        }
            //        else if (counter == 6 && !Enumration.isRequestAccepted)
            //        {
            //            if (lstPrefer3OnlineDriver.Count > 0)
            //            {
            //                NotifyAsync(lstPrefer3OnlineDriver, response);
            //                Thread.Sleep(Convert.ToInt32(applicationSettings.RequestWaitingTime * 1000));
            //            }
            //            counter++;
            //        }
            //        else
            //        {
            //            if (lstWithOutPreferOnlineDriver.Count > 0)
            //            {
            //                NotifyAsync(lstWithOutPreferOnlineDriver, response);
            //            }
            //            Enumration.isRequestAccepted = true;
            //        }

            //    } while (!Enumration.isRequestAccepted);

            //    return true; //Ride is accepted
            //}

            #endregion
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
                var customerTrips = JsonConvert.DeserializeObject<FirebaseDriver>(oldResp.Body);

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
            FirebaseDriver onlineDriver = await GetOnlineDriver(driverId);
            if (onlineDriver != null)
            {
                string path = "OnlineDriver/" + driverId;

                //Dictionary<string, string> dic = new Dictionary<string, string>();
                if (string.IsNullOrEmpty(onlineDriver.ongoingRide))
                {
                    //dic.Add("OngoingRide", "");
                    //dic.Add("isBusy", "false");
                    await FirebaseIntegration.Update(path, new DriverStatus { isBusy = "false" });
                }
                else if (onlineDriver.ongoingRide.Equals(tripId))
                {
                    //dic.Add("OngoingRide", "");
                    //dic.Add("isBusy", "false");
                    await FirebaseIntegration.Update(path, new DriverStatus { isBusy = "false" });
                }
            }
        }

        public static async Task SetDriverBusy(string driverId, string tripId)
        {
            string path = "OnlineDriver/" + driverId;
            //Dictionary<string, string> dic = new Dictionary<string, string>();
            //dic.Add("OngoingRide", tripId);
            //dic.Add("isBusy", "true");
            await FirebaseIntegration.Update(path, new DriverStatus { ongoingRide = tripId, isBusy = "true" });
        }

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

        public static async Task SetPassengerLaterBookingCancelReasons()
        {
            await FirebaseIntegration.Write("CancelReasonsLaterBooking/Passenger", await CancelReasonsService.GetCancelReasons(false, true, false));
        }

        public static async Task SetDriverLaterBookingCancelReasons()
        {
            await FirebaseIntegration.Write("CancelReasonsLaterBooking/Captain", await CancelReasonsService.GetCancelReasons(false, true, true));
        }

        #endregion

        //#region Request Dispatched / ReRouted

        public static async Task sendNotificationsAfterDispatchingRide(string deviceToken, string driverId, string tripId)
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

        //#endregion

        //#region Request Accepted

        public static async Task SetEstimateDistanceToPickUpLocation(string driverId, string estDistToPickUpLoc)
        {
            await FirebaseIntegration.Write("OnlineDriver/" + driverId + "/EstDistToPickUpLoc/", estDistToPickUpLoc);
        }
        
        public async static Task UpdateTripsAndNotifyPassengerOnRequestAcceptd(string driverId, string passengerId, AcceptRideDriverModel arm, string tripId, bool isWeb)
        {
            var fd = await GetOnlineDriver(driverId);
            var driverVehiclDetail = await DriverService.GetDriverVehicleDetail(driverId, fd == null ? "" : fd.vehicleID, passengerId, isWeb);

            var notificationPayLoad = new PassengerRequestAcceptedNotification
            {
                TripId = tripId,
                IsWeb = isWeb.ToString(),
                DriverId = driverId,
                DriverName = driverVehiclDetail.Name,
                DriverContactNumber = driverVehiclDetail.ContactNumber,
                DriverRating = driverVehiclDetail.Rating.ToString(),
                VehicleRating = driverVehiclDetail.vehicleRating.ToString(),
                DriverPicture = driverVehiclDetail.Picture,
                Make = driverVehiclDetail.Make,
                Model = driverVehiclDetail.Model,// + " " + driverVehiclDetail.PlateNumber,
                VehicleNumber = driverVehiclDetail.PlateNumber,
                PickUpLatitude = arm.pickupLocationLatitude,
                PickUpLongitude = arm.pickupLocationLongitude,
                MidwayStop1Latitude = "",
                MidwayStop1Longitude = "",
                DropOffLatitude = arm.dropOffLocationLatitude,
                DropOffLongitude = arm.dropOffLocationLongitude,
                IsLaterBooking = arm.isLaterBooking.ToString(),
                IsDispatchedRide = arm.isDispatchedRide,
                lstCancel = await CancelReasonsService.GetCancelReasons(!arm.isLaterBooking, arm.isLaterBooking, false),
                IsReRouteRequest = arm.isReRouteRequest,
                SeatingCapacity = arm.numberOfPerson,
                LaterBookingPickUpDateTime = arm.laterBookingPickUpDateTime,
                Description = arm.description,
                VoucherCode = arm.voucherCode,
                VoucherAmount = arm.voucherAmount
            };

            //driver data
            await WriteTripDriverDetails(arm, driverId);

            //passenger data
            await UpdateTripPassengerDetailsOnAccepted(notificationPayLoad, passengerId);

            await SetTripStatus(tripId, Enum.GetName(typeof(TripStatuses), TripStatuses.OnTheWay));

            if (!isWeb)
            {
                await PushyService.UniCast(driverVehiclDetail.DeviceToken, notificationPayLoad, NotificationKeys.pas_rideAccepted);
            }
        }

        //#endregion

        //#region Driver Arrived

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

        //#endregion

        //#region Ride started



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

        public static async Task sendFCMRideDetailPassengerAfterEndRide(string tripID, string distance, EndDriverRideModel edr, string driverID, string paymentMode, string userID, bool isWeb)
        {
            
        }

        //public string getTripDistance(string tripPath)
        //{
        //    client = new FireSharp.FirebaseClient(config);
        //    var fbResponse = client.Get(tripPath);
        //    if (!string.IsNullOrEmpty(fbResponse.Body) && !fbResponse.Body.Equals("null"))
        //        return fbResponse.Body.Replace("\"", "");

        //    return "0";
        //}
        //public Dictionary<string, LocationUpdate> getTripPolyLineDetails(string tripPath)
        //{
        //    client = new FireSharp.FirebaseClient(config);
        //    var reponse = client.Get(tripPath);
        //    if (!string.IsNullOrEmpty(reponse.Body) && !reponse.Body.Equals("null"))
        //    {
        //        //Key: RandomID, Value: Location Properties
        //        return JsonConvert.DeserializeObject<Dictionary<string, LocationUpdate>>(reponse.Body);
        //    }

        //    return new Dictionary<string, LocationUpdate>();
        //}

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

        //public async Task<bool> sendFCMForCreditCardPaymentToPassenger(string fleetID, string isOverride, string vehicleID, string estimatedFare, string walletUsedAmount,
        //    string walletTotalAmount, string voucherUsedAmount, string promoDiscountAmount, string totalFare, string duration, string distance, string paymentMode, string tripID, string driverID)
        //{
        //    using (CanTaxiResellerEntities context = new CanTaxiResellerEntities())
        //    {
        //        var result = context.spGetTripPassengerDetailsByTripID(tripID).FirstOrDefault();

        //        if (result == null)
        //        {
        //            return false;
        //        }

        //        MobilePayment pa = PopulateMobilePaymentObject(fleetID, isOverride, vehicleID, estimatedFare, walletUsedAmount, walletTotalAmount, voucherUsedAmount, promoDiscountAmount, totalFare, duration, distance, paymentMode, tripID, driverID, result.UserID);

        //        string path = "Trips/" + tripID + "/";

        //        await FirebaseIntegration.RideDataWriteOnFireBase(App_Start.Enumration.returnRideFirebaseStatus(App_Start.RideFirebaseStatus.PaymentRequested), true, path, pa, "", result.UserID, false);
        //        await FirebaseIntegration.RideDataWriteOnFireBase(App_Start.Enumration.returnRideFirebaseStatus(App_Start.RideFirebaseStatus.PaymentRequested), false, path, pa, driverID, "", false);

        //        await sentSingleFCM(result.DeviceToken, pa, "pas_creditCardPayment");
        //        return true;
        //    }
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


        //public bool fareAlreadyPaidFreeUserAndDriver(string tripID, string userID, string driverID)
        //{
        //    //delete trip node
        //    addDeleteNode(true, "", "Trips/" + tripID);

        //    //free driver
        //    updateDriverStatus(driverID, "false", tripID);

        //    //free user
        //    freeUserFromTrip(tripID, userID);

        //    return true;
        //}

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

        public static async Task AddPendingLaterBookings(string userID, string tripID, string pickupDateTime, string numberOfPerson)
        {
            await FirebaseIntegration.Write("PendingLaterBookings/" + tripID, new PendingLaterBooking
            {
                userID = userID,
                pickupDateTime = pickupDateTime,
                numberOfPerson = numberOfPerson
            });
        }

        public static async Task DeletePendingLaterBooking(string tripId)
        {
            await FirebaseIntegration.Delete("PendingLaterBookings/" + tripId);
        }

        public static async Task WriteTripPassengerDetails(DriverBookingRequestNotification data, string passengerId)
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
         
        public static async Task DeleteTrip(string tripId)
        {
            await FirebaseIntegration.Delete("Trips/" + tripId);
        }

        public static async Task AddUpcomingLaterBooking(string driverId, UpcomingLaterBooking data)
        {
            await FirebaseIntegration.Write("UpcomingLaterBooking/" + driverId, data);
        }
        
        public static async Task DeleteUpcomingLaterBooking(string driverId)
        {
            await FirebaseIntegration.Delete("UpcomingLaterBooking/" + driverId);
        }

        public static async Task SetTripCancelReasonsForPassenger(string tripId, string passengerId, Dictionary<string, dynamic> data)
        {
            await FirebaseIntegration.Write("Trips/" + tripId + "/" + passengerId + "/", data);
        }

        //public void getUpcomingLaterBooking(Dictionary<string, LaterBooking> dic)
        //{
        //    using (CanTaxiResellerEntities context = new CanTaxiResellerEntities())
        //    {
        //        //Need to reconsider where clause, scenario not clear:
        //        //select * from cte where rn =1 

        //        var lstLaterBooking = context.spGetUpcomingLaterBooking(Common.getUtcDateTime().ToString(), (int)TripStatus.LaterBookingAccepted).ToList();
        //        if (lstLaterBooking.Count > 0)
        //        {
        //            foreach (var laterBooking in lstLaterBooking)
        //            {
        //                LaterBooking lb = new LaterBooking
        //                {
        //                    tripID = laterBooking.TripID.ToString(),
        //                    pickUpDateTime = Convert.ToDateTime(laterBooking.PickUpBookingDateTime).ToString(Common.dateFormat),
        //                    seatingCapacity = Convert.ToInt32(laterBooking.Noofperson),
        //                    pickUplatitude = laterBooking.PickupLocationLatitude,
        //                    pickUplongitude = laterBooking.PickupLocationLongitude,
        //                    pickUpLocation = laterBooking.PickUpLocation,
        //                    dropOfflatitude = laterBooking.DropOffLocationLatitude,
        //                    dropOfflongitude = laterBooking.DropOffLocationLongitude,
        //                    dropOffLocation = laterBooking.DropOffLocation,
        //                    passengerName = laterBooking.Name,
        //                    isSend30MinutSendFCM = (Convert.ToDateTime(laterBooking.PickUpBookingDateTime) - Common.getUtcDateTime()).TotalMinutes <= 30 ? true : false,
        //                    isSend20MinutSendFCM = (Convert.ToDateTime(laterBooking.PickUpBookingDateTime) - Common.getUtcDateTime()).TotalMinutes <= 20 ? true : false,
        //                    isWeb = laterBooking.isWeb

        //                };
        //                string path = "UpcomingLaterBooking/" + laterBooking.CaptainID;

        //                //if there are some later bookings on firebase
        //                if (dic != null)
        //                {
        //                    //if this booking already exists on firebase then no need to update the node
        //                    var checkOldLaterBooking = dic.Any(d => d.Value.tripID == laterBooking.TripID.ToString());
        //                    if (!checkOldLaterBooking)
        //                    {
        //                        addDeleteNode(false, lb, path);
        //                    }
        //                }
        //                else
        //                {
        //                    addDeleteNode(false, lb, path);
        //                }
        //            }
        //        }
        //    }
        //}


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

        //public bool setPriorityHourStatus(bool isActive, string priorityHourRemainingTime, string captainID, string priorityHourEndTime, string earnedPoints)
        //{
        //    client = new FireSharp.FirebaseClient(config);
        //    Dictionary<string, dynamic> dic = new Dictionary<string, dynamic>
        //        {
        //            { "isPriorityHoursActive", isActive },
        //            { "priorityHourEndTime", priorityHourEndTime },
        //            { "earningPoints", earnedPoints },
        //            { "priorityHourRemainingTime", priorityHourRemainingTime }
        //        };
        //    client.Update("OnlineDriver/" + captainID + "/", dic);
        //    return true;
        //}

        //#endregion


        //#region Utilities



        //public void UpdateFlags(
        //            string iOSPassengerForceUpdate,
        //            string iOSPassengerAppVersion,
        //            string iOSPassengerShowAlertMessage,
        //            string iOSPassengerAlertMessage,

        //            string iOSCaptainForceUpdate,
        //            string iOSCaptainAppVersion,
        //            string iOSCaptainShowAlertMessage,
        //            string iOSCaptainAlertMessage,

        //            string androidPassengerForceUpdate,
        //            string androidPassengerAppVersion,
        //            string andriodPassengerShowAlertMessage,
        //            string andriodPassengerAlertMessage,

        //            string androidCaptainForceUpdate,
        //            string androidCaptainAppVersion,
        //            string andriodCaptainShowAlertMessage,
        //            string andriodCaptainAlertMessage)
        //{
        //    client = new FireSharp.FirebaseClient(config);

        //    #region ForceUpdate

        //    if (!string.IsNullOrEmpty(androidCaptainForceUpdate))
        //    {
        //        client.Set("GolbalSettings/Captain/Android/ForceUpdate", androidCaptainForceUpdate.ToLower());
        //    }

        //    if (!string.IsNullOrEmpty(androidPassengerForceUpdate))
        //    {
        //        client.Set("GolbalSettings/Passenger/Android/ForceUpdate", androidPassengerForceUpdate.ToLower());
        //    }

        //    if (!string.IsNullOrEmpty(iOSCaptainForceUpdate))
        //    {
        //        client.Set("GolbalSettings/Captain/IOS/ForceUpdate", iOSCaptainForceUpdate.ToLower());
        //    }

        //    if (!string.IsNullOrEmpty(iOSPassengerForceUpdate))
        //    {
        //        client.Set("GolbalSettings/Passenger/IOS/ForceUpdate", iOSPassengerForceUpdate.ToLower());
        //    }

        //    #endregion

        //    #region AppVersion

        //    if (!string.IsNullOrEmpty(androidCaptainAppVersion))
        //    {
        //        client.Set("GolbalSettings/Captain/Android/AppVersion", androidCaptainAppVersion);
        //    }

        //    if (!string.IsNullOrEmpty(androidPassengerAppVersion))
        //    {
        //        client.Set("GolbalSettings/Passenger/Android/AppVersion", androidPassengerAppVersion);
        //    }

        //    if (!string.IsNullOrEmpty(iOSCaptainAppVersion))
        //    {
        //        client.Set("GolbalSettings/Captain/IOS/AppVersion", iOSCaptainAppVersion);
        //    }

        //    if (!string.IsNullOrEmpty(iOSPassengerAppVersion))
        //    {
        //        client.Set("GolbalSettings/Passenger/IOS/AppVersion", iOSPassengerAppVersion);
        //    }

        //    #endregion

        //    #region ShowAlertMessage

        //    if (!string.IsNullOrEmpty(andriodCaptainShowAlertMessage))
        //    {
        //        client.Set("GolbalSettings/Captain/Android/ShowAlertMessage", andriodCaptainShowAlertMessage);
        //    }

        //    if (!string.IsNullOrEmpty(andriodPassengerShowAlertMessage))
        //    {
        //        client.Set("GolbalSettings/Passenger/Android/ShowAlertMessage", andriodPassengerShowAlertMessage);
        //    }

        //    if (!string.IsNullOrEmpty(iOSCaptainShowAlertMessage))
        //    {
        //        client.Set("GolbalSettings/Captain/IOS/ShowAlertMessage", iOSCaptainShowAlertMessage);
        //    }

        //    if (!string.IsNullOrEmpty(iOSPassengerShowAlertMessage))
        //    {
        //        client.Set("GolbalSettings/Passenger/IOS/ShowAlertMessage", iOSPassengerShowAlertMessage);
        //    }

        //    #endregion

        //    #region AlertMessage

        //    if (!string.IsNullOrEmpty(andriodCaptainAlertMessage))
        //    {
        //        client.Set("GolbalSettings/Captain/Android/AlertMessage", andriodCaptainAlertMessage);
        //    }

        //    if (!string.IsNullOrEmpty(andriodPassengerAlertMessage))
        //    {
        //        client.Set("GolbalSettings/Passenger/Android/AlertMessage", andriodPassengerAlertMessage);
        //    }

        //    if (!string.IsNullOrEmpty(iOSCaptainAlertMessage))
        //    {
        //        client.Set("GolbalSettings/Captain/IOS/AlertMessage", iOSCaptainAlertMessage);
        //    }

        //    if (!string.IsNullOrEmpty(iOSPassengerAlertMessage))
        //    {
        //        client.Set("GolbalSettings/Passenger/IOS/AlertMessage", iOSPassengerAlertMessage);
        //    }

        //    #endregion

        //}

        //#endregion

    }
}
