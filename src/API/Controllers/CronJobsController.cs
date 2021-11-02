using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace API.Controllers
{
    public class CronJobsController : BaseController
    {
        //[HttpGet]
        //[AllowAnonymous]
        //public HttpResponseMessage UpdateTimer()
        //{
        //    try
        //    {
        //        client = new FireSharp.FirebaseClient(config);

        //        client.Set("CurenntDateTime/", Common.getUtcDateTime().ToString(Common.dateFormat));

        //        //UpdateGlobalSettings();
        //        //UpdateActivatedPriorityHours();
        //        //UpdatePendingLaterBookings();
        //        //UpdateUpcomingLaterBookings();

        //        response.error = false;
        //        response.message = AppMessage.msgSuccess;
        //        return Request.CreateResponse(HttpStatusCode.OK, response);
        //    }
        //    catch (Exception ex)
        //    {
        //        response.error = true;
        //        Logger.WriteLog(ex);
        //        response.message = AppMessage.serverError;
        //        response.data = ex.InnerException;
        //        return Request.CreateResponse(HttpStatusCode.OK, response);
        //    }
        //}

        //[HttpGet]
        //[AllowAnonymous]
        //public HttpResponseMessage UpdateGlobalSettings()
        //{
        //    try
        //    {
        //        using (CanTaxiResellerEntities context = new CanTaxiResellerEntities())
        //        {
        //            var applicationId = ConfigurationManager.AppSettings["ApplicationID"].ToString();
        //            client = new FireSharp.FirebaseClient(config);
        //            var settings = context.ApplicationSettings.Where(a => a.ApplicationID.ToString().ToUpper().Equals(applicationId)).FirstOrDefault();
        //            client.Set("GolbalSettings/PriorityHourEnableThreshold", settings.AwardPointsThreshold != null ? settings.AwardPointsThreshold.ToString() : "100");
        //        }

        //        response.error = false;
        //        response.message = AppMessage.msgSuccess;
        //        return Request.CreateResponse(HttpStatusCode.OK, response);
        //    }
        //    catch (Exception ex)
        //    {
        //        response.error = true;
        //        Logger.WriteLog(ex);
        //        response.message = AppMessage.serverError;
        //        response.data = ex.InnerException;
        //        return Request.CreateResponse(HttpStatusCode.OK, response);
        //    }
        //}

        //[HttpGet]
        //[AllowAnonymous]
        //public HttpResponseMessage UpdateActivatedPriorityHours()
        //{
        //    try
        //    {
        //        var onlineDrivers = GetOnlineDrivers();

        //        foreach (var od in onlineDrivers)
        //        {
        //            FirbaseDriver driver = JsonConvert.DeserializeObject<FirbaseDriver>(JsonConvert.SerializeObject(od.Value));
        //            if (string.IsNullOrEmpty(driver.driverID) || driver.location == null) //Dirty data
        //            {
        //                continue;
        //            }
        //            else
        //            {
        //                //make sure priortiy hour is active
        //                if (!(driver.isPriorityHoursActive != true))
        //                {
        //                    var remainingTimeDictionary = new Dictionary<string, string>
        //                                                        {
        //                                                            { "priorityHourRemainingTime", "0" }
        //                                                        };

        //                    if (DateTime.Compare(DateTime.Parse(driver.priorityHourEndTime), DateTime.Parse(Common.getUtcDateTime().ToString(Common.dateFormat))) <= 0)
        //                    {
        //                        using (var context = new CanTaxiResellerEntities())
        //                        {
        //                            var captain = context.Captains.Where(c => c.CaptainID.ToString().Equals(driver.driverID)).FirstOrDefault();
        //                            captain.IsPriorityHoursActive = false;
        //                            context.PriorityHourLogs.Add(new PriorityHourLog
        //                            {
        //                                CaptainID = Guid.Parse(driver.driverID),
        //                                PriorityHourEndTime = DateTime.Parse(captain.LastPriorityHourEndTime.ToString()),
        //                                PriorityHourStartTime = DateTime.Parse(captain.LastPriorityHourStartTime.ToString()),
        //                                PriorityHourLogID = Guid.NewGuid()
        //                            });

        //                            setPriorityHourStatus(false, "", driver.driverID, "", captain.EarningPoints.ToString());
        //                            context.SaveChanges();
        //                        }
        //                    }
        //                    else
        //                    {
        //                        var priorityHourRemainingTime = ((int)(DateTime.Parse(driver.priorityHourEndTime).
        //                        Subtract(DateTime.Parse(Common.getUtcDateTime().ToString(Common.dateFormat))).TotalMinutes)).ToString();
        //                        remainingTimeDictionary["priorityHourRemainingTime"] = priorityHourRemainingTime;
        //                    }

        //                    client = new FireSharp.FirebaseClient(config);
        //                    client.Update("OnlineDriver/" + driver.driverID + "/", remainingTimeDictionary);
        //                }
        //            }
        //        }

        //        response.error = false;
        //        response.message = AppMessage.msgSuccess;
        //        return Request.CreateResponse(HttpStatusCode.OK, response);
        //    }
        //    catch (Exception ex)
        //    {
        //        response.error = true;
        //        Logger.WriteLog(ex);
        //        response.message = AppMessage.serverError;
        //        response.data = ex.InnerException;
        //        return Request.CreateResponse(HttpStatusCode.OK, response);
        //    }
        //}

        //[HttpGet]
        //[AllowAnonymous]
        //public HttpResponseMessage UpdatePendingLaterBookings()
        //{
        //    try
        //    {
        //        //TBD: Remove nodes from pending and trips if difference is negative.
        //        client = new FireSharp.FirebaseClient(config);
        //        FirebaseResponse pendingResp = client.Get("PendingLaterBookings");
        //        if (!string.IsNullOrEmpty(pendingResp.Body) && !pendingResp.Body.Equals("null"))
        //        {
        //            Dictionary<string, LaterBooking> pendingLaterBookings = JsonConvert.DeserializeObject<Dictionary<string, LaterBooking>>(pendingResp.Body);
        //            if (pendingLaterBookings.Any())
        //            {
        //                foreach (var booking in pendingLaterBookings)
        //                {
        //                    //TimeOut later booking atleast 2 minutes before pickup time if no captain accepts the booking

        //                    //if (DateTime.Compare(DateTime.Parse(booking.Value.pickUpDateTime),
        //                    //	DateTime.Parse(Common.getUtcDateTime().ToString(Common.dateFormat)).AddMinutes(2)) <= 0)

        //                    if ((DateTime.Parse(booking.Value.pickUpDateTime) - Common.getUtcDateTime()).TotalMinutes <= 2)
        //                    {
        //                        using (CanTaxiResellerEntities context = new CanTaxiResellerEntities())
        //                        {
        //                            var tp = context.Trips.Where(t => t.TripID.ToString() == booking.Key).FirstOrDefault();
        //                            if (tp != null)
        //                            {
        //                                tp.TripStatusID = (int)App_Start.TripStatus.TimeOut;

        //                                var user = context.UserProfiles.Where(u => u.UserID.ToString().Equals(booking.Value.userID)).FirstOrDefault();

        //                                //user will be null if ride is booked by hotel / company. No need to send notifications on portals.
        //                                string deviceToken = user != null ? user.DeviceToken : "";

        //                                //Refund voucher amount
        //                                if (user == null && tp.BookingModeID == (int)TripBookingMod.Voucher)
        //                                {
        //                                    Common.RefundFullVoucherAmount(tp, context);
        //                                }
        //                                context.SaveChanges();

        //                                var task = Task.Run(async () =>
        //                                {
        //                                    addRemovePendingLaterBookings(false, "", tp.TripID.ToString(), "", 0);
        //                                    await delTripNode(tp.TripID.ToString());
        //                                    await sentSingleFCM(deviceToken, "Later booking timed out. No captain accepted your request.", "pas_LaterBookingTimeOut");
        //                                });
        //                            }
        //                        }
        //                    }
        //                }
        //            }
        //        }

        //        response.error = false;
        //        response.message = AppMessage.msgSuccess;
        //        return Request.CreateResponse(HttpStatusCode.OK, response);
        //    }
        //    catch (Exception ex)
        //    {
        //        response.error = true;
        //        Logger.WriteLog(ex);
        //        response.message = AppMessage.serverError;
        //        response.data = ex.InnerException;
        //        return Request.CreateResponse(HttpStatusCode.OK, response);
        //    }
        //}

        //[HttpGet]
        //[AllowAnonymous]
        //public HttpResponseMessage UpdateUpcomingLaterBookings()
        //{
        //    try
        //    {
        //        Dictionary<string, LaterBooking> dic = new Dictionary<string, LaterBooking>();
        //        //send fcm 30/20 minutes fcm
        //        client = new FireSharp.FirebaseClient(config);
        //        FirebaseResponse resp = client.Get("UpcomingLaterBooking");
        //        if (!string.IsNullOrEmpty(resp.Body) && !resp.Body.Equals("null"))
        //        {
        //            dic = JsonConvert.DeserializeObject<Dictionary<string, LaterBooking>>(resp.Body);

        //            //TBD: Need to reconsider this function call for the sake of cron-job preformance. Seems to be un-necessary.
        //            //getUpcomingLaterBooking(dic);

        //            if (dic != null)
        //            {
        //                foreach (var item in dic)
        //                {
        //                    //DateTime dt = Convert.ToDateTime(item.Value.pickUpDateTime);
        //                    //TimeSpan ts = dt.Subtract(Common.getUtcDateTime());//utc datetime
        //                    //var diff = ts.TotalMinutes;

        //                    var diff = (Convert.ToDateTime(item.Value.pickUpDateTime) - Common.getUtcDateTime()).TotalMinutes;

        //                    using (CanTaxiResellerEntities context = new CanTaxiResellerEntities())
        //                    {
        //                        var tokens = new spGetDriverUserDeviceTokens_Result();
        //                        if (Convert.ToInt32(diff) == 30 && Convert.ToInt32(diff) > 0 && !item.Value.isSend30MinutSendFCM)
        //                        {
        //                            client.Set("UpcomingLaterBooking/" + item.Key + "/isSend30MinutSendFCM", true);
        //                            tokens = context.spGetDriverUserDeviceTokens(item.Value.tripID).FirstOrDefault();
        //                            var task = Task.Run(async () =>
        //                            {
        //                                //captain fcm
        //                                await sentSingleFCM(tokens.captainDeviceToken, tokens.captainName + " 30 minutes left your later booking.", "cap_30MinutesLeft");

        //                                //user fcm
        //                                await sentSingleFCM(tokens.userDeviceToken, tokens.userName + " 30 minutes left your later booking.", "pas_30MinutesLeft");
        //                            });
        //                        }
        //                        else if (Convert.ToInt32(diff) == 20 && Convert.ToInt32(diff) > 0 && !item.Value.isSend20MinutSendFCM)
        //                        {
        //                            tokens = context.spGetDriverUserDeviceTokens(item.Value.tripID).FirstOrDefault();
        //                            var task = Task.Run(async () =>
        //                            {
        //                                client.Set("UpcomingLaterBooking/" + item.Key + "/isSend20MinutSendFCM", true);

        //                                //captain fcm
        //                                await sentSingleFCM(tokens.captainDeviceToken, tokens.captainName + " 20 minutes left your later booking.", "cap_20MinutesLeft");

        //                                //user fcm
        //                                await sentSingleFCM(tokens.userDeviceToken, tokens.userName + " 20 minutes left your later booking.", "pas_20MinutesLeft");
        //                            });
        //                        }

        //                        //Upcoming later booking is cancelled from application if captain don't start 15 min before pick up, if somehow
        //                        //application fails to cancel upcoming later booking then in next min, server will cancel and re-route the booking

        //                        else if (Convert.ToInt32(diff) < 14)// && Convert.ToInt32(diff) > 0)
        //                        {
        //                            var trip = context.spUpcomingLaterBookingDetailsForCancel(item.Value.tripID).FirstOrDefault();

        //                            //If later booking was of less than 15 min, then it is started as soon as driver accepts it.
        //                            //Following check is applied to make precise decision - to be discussed.

        //                            if (trip != null && trip.diff > 15)
        //                            {
        //                                var req = new RequestModel()
        //                                {
        //                                    isLaterBooking = trip.isLaterBooking,
        //                                    //resellerID = this.ResellerID,
        //                                    //resellerID = trip.ResellerID.ToString(),
        //                                    resellerArea = trip.AuthorizedArea,
        //                                    timeZoneOffset = "0", //Will not be considered, in case of reroute PickUpBookingDateTime is being fetched from db
        //                                    isWeb = trip.isWeb == 1,
        //                                    isAtPickupLocation = "false",
        //                                    driverID = trip.CaptainID.ToString(),
        //                                    cancelID = 1,
        //                                    tripID = item.Value.tripID,
        //                                    isDispatchedRide = "false",
        //                                    isReRouteRequest = true   //Flag to ReRoute "On The Way" cancelled ride
        //                                };

        //                                DriverController dc = new DriverController();
        //                                dc.cancelRide(req, Request);
        //                            }
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //        else
        //        {
        //            //if somehow upcoming later booking trips node was removed from firebase.
        //            getUpcomingLaterBooking(dic);
        //        }

        //        response.error = false;
        //        response.message = AppMessage.msgSuccess;
        //        return Request.CreateResponse(HttpStatusCode.OK, response);
        //    }
        //    catch (Exception ex)
        //    {
        //        response.error = true;
        //        Logger.WriteLog(ex);
        //        response.message = AppMessage.serverError;
        //        response.data = ex.InnerException;
        //        return Request.CreateResponse(HttpStatusCode.OK, response);
        //    }
        //}

        //[HttpGet]
        //[AllowAnonymous]
        //public HttpResponseMessage OfflineInActiveDrivers()
        //{
        //    try
        //    {
        //        var onlineDrivers = GetOnlineDrivers();

        //        foreach (var od in onlineDrivers)
        //        {
        //            using (CanTaxiResellerEntities context = new CanTaxiResellerEntities())
        //            {
        //                string applicationID = ConfigurationManager.AppSettings["ApplicationID"].ToString();
        //                var applicationSettings = context.ApplicationSettings.Where(a => a.ApplicationID.ToString().Equals(applicationID)).FirstOrDefault();

        //                FirbaseDriver driver = JsonConvert.DeserializeObject<FirbaseDriver>(JsonConvert.SerializeObject(od.Value));
        //                if (string.IsNullOrEmpty(driver.driverID)) //Dirty data
        //                {
        //                    SetDriverOffline(od.Key, driver.vehicleID);
        //                }
        //                //Cushion for new online drivers - wait for location update
        //                else if ((Common.getUtcDateTime() - DateTime.Parse(driver.onlineSince)).TotalSeconds > applicationSettings.CaptainAllowedIdleTimeInSeconds)
        //                {
        //                    if (string.IsNullOrEmpty(driver.lastUpdated))
        //                    {
        //                        SetDriverOffline(driver.driverID, driver.vehicleID);
        //                    }
        //                    else
        //                    {
        //                        DateTime currentLocationUpdateTime = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(driver.lastUpdated)).UtcDateTime;
        //                        if ((Common.getUtcDateTime() - currentLocationUpdateTime).TotalSeconds > applicationSettings.CaptainAllowedIdleTimeInSeconds)
        //                        {
        //                            SetDriverOffline(driver.driverID, driver.vehicleID);
        //                        }
        //                    }
        //                }
        //            }
        //        }

        //        response.error = false;
        //        response.message = AppMessage.msgSuccess;
        //        return Request.CreateResponse(HttpStatusCode.OK, response);
        //    }
        //    catch (Exception ex)
        //    {
        //        response.error = true;
        //        Logger.WriteLog(ex);
        //        response.message = AppMessage.serverError;
        //        response.data = ex.InnerException;
        //        return Request.CreateResponse(HttpStatusCode.OK, response);
        //    }
        //}

        //private void SetDriverOffline(string driverID, string vehicleID)
        //{
        //    insertDeleteOnlineDriver(false, driverID, "", 0, "", "", "", "", "", null, "", "", "", "", "", "", "", 0, "", "", 0, "", "");
        //    Common.OnlineOfflineDriver(driverID, string.IsNullOrEmpty(vehicleID) ? "" : vehicleID, false, Guid.Parse(ConfigurationManager.AppSettings["ApplicationID"].ToString()));
        //}

        ////make sure every upcoming laterbooking exists on firebase by verifing from database
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

    }
}