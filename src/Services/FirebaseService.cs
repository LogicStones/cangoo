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

        //public bool isCaptainInTrip(string DriverID)
        //{
        //    client = new FireSharp.FirebaseClient(config);

        //    string normalTripCheck = "OnlineDriver/" + DriverID + "/OngoingRide";
        //    string walkinTripCheck = "OnlineDriver/" + DriverID + "/WalkInCustomer";

        //    FirebaseResponse resp = client.Get(normalTripCheck);

        //    bool isInNormalTrip = true;
        //    bool isInWalkInTrip = true;

        //    if (string.IsNullOrEmpty(resp.Body) || resp.Body.Equals("null") || resp.Body.Equals("\"\""))
        //        isInNormalTrip = false;

        //    resp = client.Get(walkinTripCheck);

        //    if (string.IsNullOrEmpty(resp.Body) || resp.Body.Equals("null") || resp.Body.Equals("\"\""))
        //        isInWalkInTrip = false;

        //    if (isInNormalTrip || isInWalkInTrip)
        //        return true;

        //    return false;
        //}

        //#endregion

        #region Online / Offline Driver

        public static async Task<Dictionary<string, dynamic>> GetOnlineDrivers()
        {
            //get online driver from firebase
            //client = new FireSharp.FirebaseClient(config);
            //FirebaseResponse resp = client.Get();

           var fbOnlineDrivers =  await FirebaseIntegration.Read("OnlineDriver");

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

        //public void insertDeleteOnlineDriver(bool isBooked, string driverID, string vehicleID, int makeID, string companyID, string seatingCapacity, string make, string model, string driverName,
        //                                      Nullable<bool> isPriorityHoursActive, string priorityHourEndTime, string earningPoints, string vehicleFeatures, string driverFeatures,
        //                                      string deviceToken, string userName, string phoneNumber, int ModelID, string PlateNumber, string Category,
        //                                      int CategoryID, string RegistrationYear, string Color)
        //{
        //    using (CanTaxiResellerEntities context = new CanTaxiResellerEntities())
        //    {
        //        client = new FireSharp.FirebaseClient(config);
        //        if (isBooked)
        //        {
        //            FirbaseDriver fd = new FirbaseDriver
        //            {
        //                companyID = companyID,
        //                driverID = driverID,
        //                userName = userName,
        //                driverName = driverName,
        //                phoneNumber = phoneNumber,
        //                deviceToken = deviceToken,
        //                driverFacilities = driverFeatures,

        //                lat = 0.0,
        //                lon = 0.0,
        //                isBusy = "false",
        //                isPriorityHoursActive = isPriorityHoursActive != true ? false : true,
        //                priorityHourEndTime = priorityHourEndTime,
        //                onlineSince = Common.getUtcDateTime().ToString(Common.dateFormat),
        //                earningPoints = string.IsNullOrEmpty(earningPoints) ? "0.0" : earningPoints,
        //                priorityHourRemainingTime = isPriorityHoursActive == true ? ((int)(DateTime.Parse(priorityHourEndTime).
        //                Subtract(DateTime.Parse(Common.getUtcDateTime().ToString(Common.dateFormat))).TotalMinutes)).ToString() : "0",
        //                lastUpdated = "",
        //                OngoingRide = "",

        //                vehicleID = vehicleID,
        //                makeID = makeID,
        //                make = make,
        //                modelID = ModelID,
        //                model = model,
        //                categoryID = CategoryID,
        //                category = Category,
        //                color = Color,
        //                plateNumber = PlateNumber,
        //                seatingCapacity = seatingCapacity,
        //                registrationYear = RegistrationYear,
        //                vehicleFacilities = vehicleFeatures
        //            };
        //            //add in firebase
        //            SetResponse response = client.Set("OnlineDriver/" + driverID, fd);
        //        }
        //        else
        //        {
        //            DeleteResponse response = client.Delete("OnlineDriver/" + driverID);
        //        }
        //    }
        //}
        
        #endregion

        #region Send Request to Online Drivers

        public static async Task<bool> SendRideRequestToOnlineDrivers(string tripId, string passengerId, int reqSeatingCapacity, DriverBookingRequestNotification bookingRN, dynamic hotelSetting)
        {
            string path = "Trips/" + tripId + "/";

            //Passenger data
            if (bool.Parse(bookingRN.IsReRouteRequest))
            {
                if (!bool.Parse(bookingRN.IsLaterBooking))
                {
                    bookingRN.ReRouteRequestTime = DateTime.UtcNow.ToString(Formats.DateFormat);
                    await FirebaseIntegration.RideDataWriteOnFireBase(Enum.GetName(typeof(TripStatuses), TripStatuses.ReRouting), true, path, bookingRN, "", passengerId.ToString(), false);

                    // free captain > update user > send request 
                    await SendReRouteNotfication(bookingRN.BookingModeId, bookingRN.ReRouteRequestTime, bookingRN.RequestTimeOut.ToString(), bookingRN.PreviousCaptainId.ToString(), tripId, bookingRN.DeviceToken); //passengerId, 
                }
                else
                {
                    await FirebaseIntegration.RideDataWriteOnFireBase(Enum.GetName(typeof(TripStatuses), TripStatuses.RequestSent),
                        true, path, bookingRN, "", passengerId, false);

                    //UPDATE: Captain can canel inprocess later booking, so user / captain should be set free in every case.
                    // free captain > update user > send request 
                    await SendInProcessLaterBookingReRouteNotfication(bookingRN.BookingModeId, bookingRN.PreviousCaptainId.ToString(), tripId, passengerId, bookingRN.DeviceToken);
                }
            }
            else
                await FirebaseIntegration.RideDataWriteOnFireBase(Enum.GetName(typeof(TripStatuses), TripStatuses.RequestSent),
                        true, path, bookingRN, "", passengerId, false);


            #region MakePreferredAndNormalDriversList

            //Key: DriverID, Value: Online Driver Properties

            List<FirebaseDriver> lstNormalCaptains = new List<FirebaseDriver>();
            List<FirebaseDriver> lstPreferredCaptains = new List<FirebaseDriver>();

            string captainIDs = "";
            string preferredCaptainIDs = "";
            //App_Start.Enumration.isRequestAccepted = false;

            var rawOnlineDrivers = await GetOnlineDrivers();

            foreach (var od in rawOnlineDrivers)
            {
                FirebaseDriver driver = JsonConvert.DeserializeObject<FirebaseDriver>(JsonConvert.SerializeObject(od.Value));
                if (string.IsNullOrEmpty(driver.DriverID) || driver.Location == null) //Dirty data
                {
                    continue;
                }
                else
                {
                    //If user have requested some facilities then request should be sent to only eligible captains
                    if (!string.IsNullOrEmpty(bookingRN.RequiredFacilities))
                    {
                        var reqFac = bookingRN.RequiredFacilities.ToLower().Split(',');
                        var vehFac = driver.VehicleFacilities.ToLower().Split(',');
                        var capFac = driver.DriverFacilities.ToLower().Split(',');

                        var vehCheck = reqFac.Intersect(vehFac);
                        var capCheck = reqFac.Intersect(capFac);

                        //If both vehicle and captain don't have required facilities

                        if (!(reqFac.SequenceEqual(vehCheck) && reqFac.SequenceEqual(capCheck)))
                            continue;
                    }

                    if (driver.IsPriorityHoursActive)
                    {
                        preferredCaptainIDs = string.IsNullOrEmpty(preferredCaptainIDs) ? driver.DriverID : preferredCaptainIDs + "," + driver.DriverID;

                        //If its later booking then send request to captains even if he is already busy
                        if (bool.Parse(bookingRN.IsLaterBooking))
                        {
                            lstPreferredCaptains.Add(driver);
                        }
                        //In case of normal booking send request to free captains only
                        else
                        {
                            if (driver.IsBusy.ToLower().Equals("false"))
                            {
                                lstPreferredCaptains.Add(driver);
                            }
                        }
                    }
                    else
                    {
                        captainIDs = string.IsNullOrEmpty(captainIDs) ? driver.DriverID : captainIDs + "," + driver.DriverID;

                        //If it's later booking then send request to captains even if he is already busy
                        if (bool.Parse(bookingRN.IsLaterBooking))
                        {
                            lstNormalCaptains.Add(driver);
                        }
                        //In case of normal booking send request to free captains only
                        else
                        {
                            if (driver.IsBusy.ToLower().Equals("false"))
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
                lstNormalCaptains.RemoveAll(x => x.DriverID.Equals(bookingRN.PreviousCaptainId));
                lstPreferredCaptains.RemoveAll(x => x.DriverID.Equals(bookingRN.PreviousCaptainId));
            }

            //No driver available
            if (lstNormalCaptains.Count == 0 && lstPreferredCaptains.Count == 0)
                return false;


            /* REFACTOR
             * This function call seems to be unnecessary*/

            await UpdateDiscountTypeAndAmount(false, path, bookingRN.DiscountAmount ?? "0.00", bookingRN.DiscountType ?? "normal", bookingRN.IsDispatchedRide ?? "false");

            string applicationID = ConfigurationManager.AppSettings["ApplicationID"].ToString();
                var applicationSettings = await ApplicationSettingService.GetApplicationSettings(applicationID); 
                var distanceInterval = applicationSettings.RequestRadiusInterval; //int.Parse(ConfigurationManager.AppSettings["RequestRadiusInterval"].ToString());
                int? minDistanceRange = 0;
                var maxDistanceRange = distanceInterval;
                //var requestSearchRange = (int)(applicationSettings.RequestSearchRange * 1000);
                var requestSearchRange = bool.Parse(bookingRN.IsLaterBooking) ? (int)(applicationSettings.LaterBookingRequestSearchRange * 1000) : (int)(applicationSettings.RequestSearchRange * 1000);
                var captainMinRating = applicationSettings.CaptainMinRating; // int.Parse(ConfigurationManager.AppSettings["CaptainMinRating"].ToString());
                var pickPosition = new GeoCoordinate(Convert.ToDouble(bookingRN.PickUpLatitude), Convert.ToDouble(bookingRN.PickUpLongitude));

                var lstPreferredCaptainsDetail = new List<DatabaseOlineDriversDTO>();
                if (!string.IsNullOrEmpty(preferredCaptainIDs))
                    lstPreferredCaptainsDetail = await DriverService.GetDriversByIds(preferredCaptainIDs);

                var lstNormalCaptainsDetail = new List<DatabaseOlineDriversDTO>();
                if (!string.IsNullOrEmpty(captainIDs))
                    lstNormalCaptainsDetail = await DriverService.GetDriversByIds(captainIDs);

                do
                {
                    Dictionary<string, string> lstFilteredPreferredCaptains = new Dictionary<string, string>();
                    Dictionary<string, string> lstFilteredNormalCaptains = new Dictionary<string, string>();

                    var lstRequestLog = new List<TripRequestLogDTO>();

                    foreach (var dr in lstPreferredCaptains)
                    {
                        var capDetail = lstPreferredCaptainsDetail.Where(c => c.CaptainID.ToString().Equals(dr.DriverID)).FirstOrDefault();
                        if (capDetail != null)
                        {
                            if (dr.Location == null)
                                continue;

                            var driverPosition = new GeoCoordinate(dr.Location.l[0], dr.Location.l[1]);
                            var distance = driverPosition.GetDistanceTo(pickPosition);  //distance in meters

                            if (distance <= requestSearchRange)
                            {
                                if (capDetail.Rating >= captainMinRating && distance >= minDistanceRange && distance <= maxDistanceRange && Convert.ToInt32(dr.SeatingCapacity) >= reqSeatingCapacity)
                                {
                                    lstFilteredPreferredCaptains.Add(capDetail.DeviceToken, Convert.ToBoolean(bookingRN.IsLaterBooking) ? capDetail.LaterBookingNotificationTone : capDetail.NormalBookingNotificationTone);

                                    lstRequestLog.Add(new TripRequestLogDTO
                                    {
                                        CaptainID = Guid.Parse(dr.DriverID),
                                        RequestLogID = Guid.NewGuid(),
                                        CaptainLocationLatitude = dr.Location.l[0].ToString(),
                                        CaptainLocationLongitude = dr.Location.l[1].ToString(),
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
                        var capDetail = lstNormalCaptainsDetail.Where(c => c.CaptainID.ToString().Equals(dr.DriverID)).FirstOrDefault();
                        if (capDetail != null)
                        {
                            if (dr.Location == null)
                                continue;

                            var driverPosition = new GeoCoordinate(dr.Location.l[0], dr.Location.l[1]);
                            var distance = driverPosition.GetDistanceTo(pickPosition);  //distance in meters

                            if (distance <= requestSearchRange)
                            {
                                if (capDetail.Rating >= captainMinRating && distance >= minDistanceRange && distance <= maxDistanceRange && Convert.ToInt32(dr.SeatingCapacity) >= reqSeatingCapacity)
                                {
                                    lstFilteredNormalCaptains.Add(capDetail.DeviceToken, Convert.ToBoolean(bookingRN.IsLaterBooking) ? capDetail.LaterBookingNotificationTone : capDetail.NormalBookingNotificationTone);

                                    lstRequestLog.Add(new TripRequestLogDTO
                                    {
                                        CaptainID = Guid.Parse(dr.DriverID),
                                        RequestLogID = Guid.NewGuid(),
                                        CaptainLocationLatitude = dr.Location.l[0].ToString(),
                                        CaptainLocationLongitude = dr.Location.l[1].ToString(),
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

                    //if (!Enumration.isRequestAccepted && lstFilteredPreferredCaptains.Any())
                    //{
                    //    NotifyAsync(lstFilteredPreferredCaptains, response);
                    //    Thread.Sleep(Convert.ToInt32(applicationSettings.RequestWaitingTime * 1000));
                    //}

                    //if (!Enumration.isRequestAccepted && lstFilteredNormalCaptains.Any())
                    //{
                    //    NotifyAsync(lstFilteredNormalCaptains, response);
                    //    Thread.Sleep(Convert.ToInt32(applicationSettings.RequestWaitingTime * 1000));
                    //}

                    //if (lstFilteredPreferredCaptains.Any() || lstFilteredNormalCaptains.Any())
                    //{
                    //    context.TripRequestLogs.AddRange(lstRequestLog);
                    //    await context.SaveChangesAsync();
                    //}

                    minDistanceRange = maxDistanceRange;
                    maxDistanceRange = maxDistanceRange + distanceInterval > requestSearchRange ? requestSearchRange : maxDistanceRange + distanceInterval;

                } while (!await IsRequestAccepted(tripId) && minDistanceRange < requestSearchRange);
                //} while (!Enumration.isRequestAccepted && minDistanceRange < requestSearchRange);

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

        private static async Task<bool> IsRequestAccepted(string tripID)
        {
            var resp = await FirebaseIntegration.Read("Trips/" + tripID);

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

        private static async Task UpdateDiscountTypeAndAmount(bool isUpdate, string path, string discountAmount, string discountType, string isDispatchedRide)
        {
            Dictionary<string, string> dic = new Dictionary<string, string>
                {
                    { "/isDispatchedRide", isDispatchedRide },
                    { "/discount/amount", discountAmount },
                    { "/discount/type", discountType }
                };

            if (isUpdate)
                await FirebaseIntegration.Update(path, dic);
            else
                await FirebaseIntegration.Write(path, dic);
        }

        private static async Task SendInProcessLaterBookingReRouteNotfication(string bookingModeId, string captainId, string tripId, string userId, string deviceToken)
        {
            //step 1 : Free current captain

            await SetDriverFree(captainId, tripId);
            await RemoveDriverFromTrip(captainId, tripId);
            await DeletePassengerTrip(userId);

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
        
        public static async Task DeletePassengerTrip(string passengerId)
        {
            await FirebaseIntegration.Delete("CustomerTrips/" + passengerId);
        }

        private static async Task RemoveDriverFromTrip(string captainId, string tripId)
        {
            await FirebaseIntegration.Delete("Trips/" + tripId + "/" + captainId);
        }

        private static async Task SetDriverFree(string driverId, string tripId)
        {
            FirebaseDriver onlineDriver = await GetFirebaseDriver(driverId);
            string path = "OnlineDriver/" + driverId;

            //Dictionary<string, string> dic = new Dictionary<string, string>();
            if (string.IsNullOrEmpty(onlineDriver.OngoingRide))
            {
                //dic.Add("OngoingRide", "");
                //dic.Add("isBusy", "false");
                await FirebaseIntegration.Update(path, new DriverStatus { isBusy = "false" });
            }
            else if (onlineDriver.OngoingRide.Equals(tripId))
            {
                //dic.Add("OngoingRide", "");
                //dic.Add("isBusy", "false");
                await FirebaseIntegration.Update(path, new DriverStatus { isBusy = "false" });
            }
        }

        private static async Task<FirebaseDriver> GetFirebaseDriver(string driverId)
        {
            FirebaseDriver onlineDriver = new FirebaseDriver();

            //make sure driver is in the same trip we are trying to free
            var rawDriver = await FirebaseIntegration.Read("OnlineDriver/" + driverId);

            if (!string.IsNullOrEmpty(rawDriver.Body) && !rawDriver.Body.Equals("null"))
            
                 onlineDriver = JsonConvert.DeserializeObject<FirebaseDriver>(rawDriver.Body);

            return onlineDriver;
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
            await FirebaseIntegration.Write("CancelReasonsLaterBooking/Passenger", await CancelReasonsService.GetCancelReasons(false, true, false));
        }

        #endregion

        //#region Request Dispatched / ReRouted

        //public async Task<bool> sendNotificationsAfterDispatchingRide(string deviceToken, string captainID, string tripID)
        //{
        //    client = new FireSharp.FirebaseClient(config);

        //    using (CanTaxiResellerEntities context = new CanTaxiResellerEntities())
        //    {
        //        //step 1 : Free current captain
        //        client = new FireSharp.FirebaseClient(config);
        //        //string path = "OnlineDriver/" + captainID;
        //        //Dictionary<string, string> dic = new Dictionary<string, string>
        //        //{
        //        //    { "isBusy", "false" },
        //        //    { "OngoingRide", "" }
        //        //};
        //        //client.Update(path, dic);

        //        updateDriverStatus(captainID, "false", tripID);

        //        //step 2 : update captain about trip status

        //        Dictionary<string, string> dic = new Dictionary<string, string>
        //            {
        //                { "tripID", tripID }
        //            };

        //        await sentSingleFCM(deviceToken, dic, "cap_rideDispatched");

        //        //step 3 : update trip node
        //        //FirebaseResponse res = client.Get("Trips/" + tripID);
        //        client.Delete("Trips/" + tripID + "/" + captainID);

        //        return true;
        //    }
        //}

        //#endregion

        //#region Request Accepted

        //public bool addTripToDriverNode(string acceptedDriverID, string tripId, string estDistToPickUpLoc)
        //{
        //    client = new FireSharp.FirebaseClient(config);
        //    updateDriverStatus(acceptedDriverID, "true", tripId);

        //    SetResponse setResponse = client.Set("OnlineDriver/" + acceptedDriverID + "/EstDistToPickUpLoc/", estDistToPickUpLoc);
        //    return true;
        //}

        //public async Task<bool> sendFCMToUserAfterAcceptRideRequest(string driverID, string userID, AcceptRideDriverModel arm, string tripID, bool isWeb)
        //{
        //    client = new FireSharp.FirebaseClient(config);
        //    FirebaseResponse res = client.Get("OnlineDriver/" + driverID);
        //    var fd = JsonConvert.DeserializeObject<FirbaseDriver>(res.Body);
        //    using (CanTaxiResellerEntities context = new CanTaxiResellerEntities())
        //    {
        //        string path = "Trips/" + tripID + "/";

        //        //driver data
        //        await FirebaseIntegration.RideDataWriteOnFireBase(App_Start.Enumration.returnRideFirebaseStatus(App_Start.RideFirebaseStatus.OnTheWay), false, path, arm, driverID, "", isWeb);

        //        var driverVehiclDetail = context.spGetDriverVehicleDetail(driverID, fd.vehicleID, userID, isWeb).FirstOrDefault();

        //        AcceptRideFCM fcm = new AcceptRideFCM
        //        {
        //            tripID = tripID,
        //            isWeb = isWeb,
        //            driverID = driverID,
        //            driverName = driverVehiclDetail.Name,
        //            driverContactNumber = driverVehiclDetail.ContactNumber,
        //            driverRating = Convert.ToDouble(driverVehiclDetail.Rating),
        //            vehicleRating = Convert.ToDouble(driverVehiclDetail.vehicleRating),
        //            driverPicture = driverVehiclDetail.Picture,
        //            make = driverVehiclDetail.Make,
        //            model = driverVehiclDetail.Model,// + " " + driverVehiclDetail.PlateNumber,
        //            vehicleNumber = driverVehiclDetail.PlateNumber,
        //            pickUplatitude = arm.pickupLocationLatitude,
        //            pickUplongitude = arm.pickupLocationLongitude,
        //            dropOfflatitude = arm.dropOffLocationLatitude,
        //            dropOfflongitude = arm.dropOffLocationLongitude,
        //            isLaterBooking = arm.isLaterBooking,
        //            isDispatchedRide = arm.isDispatchedRide,
        //            lstCancel = Common.GetCancelReasons(context, !arm.isLaterBooking, arm.isLaterBooking, false),
        //            isReRouteRequest = arm.isReRouteRequest,
        //            numberOfPerson = arm.numberOfPerson,
        //            laterBookingPickUpDateTime = arm.laterBookingPickUpDateTime,
        //            description = arm.description,
        //            voucherCode = arm.voucherCode,
        //            voucherAmount = arm.voucherAmount
        //        };

        //        //passengerData
        //        await FirebaseIntegration.RideDataWriteOnFireBase(App_Start.Enumration.returnRideFirebaseStatus(App_Start.RideFirebaseStatus.OnTheWay), true, path, fcm, "", userID, isWeb);

        //        if (!isWeb)
        //        {
        //            await sentSingleFCM(driverVehiclDetail.DeviceToken, fcm, "pas_rideAccepted");
        //        }
        //        return true;
        //    }
        //}

        //#endregion

        //#region Driver Arrived

        //public async Task<bool> sendFCMAfterDriverArrived(string driverID, string deviceToken, ArrivedDriverRideModel adr, string tripID, bool isWeb)
        //{
        //    Dictionary<string, dynamic> dic = new Dictionary<string, dynamic>();
        //    dic.Add("driverID", driverID);
        //    if (!isWeb)
        //    {
        //        await sentSingleFCM(deviceToken, dic, "pas_driverReached");
        //    }
        //    client = new FireSharp.FirebaseClient(config);

        //    string path = "Trips/" + tripID + "/";
        //    //driver data
        //    await FirebaseIntegration.RideDataWriteOnFireBase(App_Start.Enumration.returnRideFirebaseStatus(App_Start.RideFirebaseStatus.Waiting), false, path, adr, driverID, "", isWeb);
        //    //SetResponse rs = await client.SetTaskAsync(path + driverID, adr);
        //    return true;
        //}

        public static async Task UpdateDriverEarnedPoints(string driverId, string earnedPoints)
        {
            await FirebaseIntegration.Update("OnlineDriver/" + driverId + "/", new DriverEarnedPoints
            {
                earningPoints = earnedPoints
            });
        }

        //public bool updateArrivalTime(string tripID, string driverID, string earnedPoints)
        //{
        //    client = new FireSharp.FirebaseClient(config);

        //    Dictionary<string, string> dic = new Dictionary<string, string>
        //    {
        //        { "arrivalTime", Common.getUtcDateTime().ToString(Common.dateFormat) }
        //    };
        //    client.Update("Trips/" + tripID + "/" + driverID + "/", dic);

        //    return true;
        //}

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

        //public async Task<bool> sendFCMAfterRideStarted(string tripID, string driverID, string deviceToken, startDriverRideModel sdr, bool isWeb)
        //{
        //    Dictionary<string, dynamic> dic = new Dictionary<string, dynamic>
        //    {
        //        { "driverID", driverID }
        //    };

        //    if (!isWeb)
        //    {
        //        await sentSingleFCM(deviceToken, dic, "pas_rideStarted");
        //    }
        //    client = new FireSharp.FirebaseClient(config);
        //    string path = "Trips/" + tripID + "/";

        //    await FirebaseIntegration.RideDataWriteOnFireBase(App_Start.Enumration.returnRideFirebaseStatus(App_Start.RideFirebaseStatus.Picked), false, path, sdr, driverID, "", isWeb);
        //    return true;
        //}

        //public async Task<bool> updateGo4Module(string captainName, string userName, string deviceToken)
        //{
        //    Go4ModuleModel fcm = new Go4ModuleModel()
        //    {
        //        captainName = captainName,
        //        passengerName = userName
        //    };
        //    await sentSingleFCM(deviceToken, fcm, "g4m_rideStarted");

        //    return true;
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

        //public async Task<bool> sendFCMRideDetailPassengerAfterEndRide(string tripID, string distance, EndDriverRideModel edr, string driverID, string paymentMode, string userID, bool isWeb)
        //{
        //    using (CanTaxiResellerEntities context = new CanTaxiResellerEntities())
        //    {
        //        var trp = context.spGetRideDetail(tripID, (Int32)App_Start.TripStatus.PaymentPending, isWeb).FirstOrDefault();
        //        if (trp != null)
        //        {
        //            string path = "Trips/" + tripID + "/";

        //            PaymentPendingPassenger pp = new PaymentPendingPassenger()
        //            {
        //                isPaymentRequested = false,
        //                isFareChangePermissionGranted = edr.isFareChangePermissionGranted,
        //                PaymentMode = paymentMode
        //            };

        //            //driver data
        //            await FirebaseIntegration.RideDataWriteOnFireBase(App_Start.Enumration.returnRideFirebaseStatus(App_Start.RideFirebaseStatus.PaymentPending), false, path, edr, driverID, "", isWeb);

        //            //user data
        //            await FirebaseIntegration.RideDataWriteOnFireBase(App_Start.Enumration.returnRideFirebaseStatus(App_Start.RideFirebaseStatus.PaymentPending), true, path, pp, "", userID, isWeb);

        //            if (!isWeb)
        //            {
        //                EndRideFCM efcm = new EndRideFCM()
        //                {
        //                    tripID = tripID,
        //                    driverName = trp.Name,
        //                    driverImage = trp.Picture,
        //                    pickLat = trp.PickupLocationLatitude,
        //                    pickLong = trp.PickupLocationLongitude,
        //                    dropLat = trp.DropOffLocationLatitude,
        //                    dropLong = trp.DropOffLocationLongitude,
        //                    estimateFare = Convert.ToDecimal((trp.BaseFare != null ? trp.BaseFare : 0) + (trp.BookingFare != null ? trp.BookingFare : 0) + Convert.ToDecimal(trp.travelledFare != null ? trp.travelledFare : 0)),
        //                    bookingDateTime = trp.BookingDateTime,
        //                    endRideDateTime = trp.TripEndDatetime,
        //                    totalRewardPoints = (trp.RewardPoints + (int.Parse(distance) / 500)).ToString(),
        //                    tripRewardPoints = (int.Parse(distance) / 500).ToString(),
        //                    distance = distance,
        //                    date = string.Format("{0:dd MM yyyy}", trp.BookingDateTime),
        //                    time = string.Format("{0:hh:mm tt}", trp.BookingDateTime),
        //                    paymentMode = paymentMode,
        //                    isFav = trp.favorite == 1 ? true : false
        //                };

        //                await sentSingleFCM(trp.DeviceToken, efcm, "pas_endRideDetail");
        //            }
        //        }
        //        return true;
        //    }
        //}

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
            //Dictionary<string, dynamic> dic = new Dictionary<string, dynamic>() {
            //                        {"userID",userID},
            //                        {"pickupDateTime", pickupDateTime },
            //                        {"numberOfPerson", numberOfPerson }
            //                    };

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
            await DeleteTrip(tripId);
        }

        public static async Task DeleteTrip(string tripId)
        {
            await FirebaseIntegration.Delete("Trips/" + tripId);
        }

        public static async Task DeleteUpcomingLaterBooking(string driverId)
        {
            await FirebaseIntegration.Delete("UpcomingLaterBooking/" + driverId);
        }

        public static async Task AddUpcomingLaterBooking(string driverId, UpcomingLaterBooking data)
        {
            await FirebaseIntegration.Write("UpcomingLaterBooking/" + driverId, data);
        }

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
