using API.Filters;
using Constants;
using DatabaseModel;
using DTOs.API;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Owin.Security;
using Newtonsoft.Json;
using Services;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace API.Controllers
{
    [AllowAnonymous]
    [RoutePrefix("api/CronJobs")]
    public class CronJobsController : BaseController
    {
        [HttpGet]
        [Route("UpdateTimer")]
        public async Task<HttpResponseMessage> UpdateTimer()
        {
            await FirebaseService.SetCurrentTime();

            return Request.CreateResponse(HttpStatusCode.OK, new ResponseEntity
            {
                error = false,
                message = ResponseKeys.msgSuccess
            });
        }

        [HttpGet]
        [Route("UpdateGlobalSettings")]
        public async Task<HttpResponseMessage> UpdateGlobalSettings()
        {
            await FirebaseService.SetGlobalSettings();

            return Request.CreateResponse(HttpStatusCode.OK, new ResponseEntity
            {
                error = false,
                message = ResponseKeys.msgSuccess
            });
        }

        [HttpGet]
        [Route("OfflineInActiveDrivers")]
        public async Task<HttpResponseMessage> OfflineInActiveDrivers()
        {
            var onlineDrivers = await FirebaseService.GetOnlineDrivers();

            foreach (var od in onlineDrivers)
            {
                var applicationSettings = await ApplicationSettingService.GetApplicationSettings(ConfigurationManager.AppSettings["ApplicationID"].ToString());

                FirebaseDriver driver = od.Value;// JsonConvert.DeserializeObject<FirebaseDriver>(JsonConvert.SerializeObject(od.Value));

                if (string.IsNullOrEmpty(driver.driverID)) //Dirty data
                {
                    await SetDriverOffline(od.Key, driver.vehicleID);
                }
                //Cushion for new online drivers - wait for location update
                else if ((DateTime.UtcNow - DateTime.Parse(driver.onlineSince)).TotalSeconds > applicationSettings.CaptainAllowedIdleTimeInSeconds)
                {
                    if (string.IsNullOrEmpty(driver.lastUpdated))
                    {
                        await SetDriverOffline(driver.driverID, driver.vehicleID);
                    }
                    else
                    {
                        DateTime currentLocationUpdateTime = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(driver.lastUpdated)).UtcDateTime;
                        if ((DateTime.UtcNow - currentLocationUpdateTime).TotalSeconds > applicationSettings.CaptainAllowedIdleTimeInSeconds)
                        {
                            await SetDriverOffline(driver.driverID, driver.vehicleID);
                        }
                    }
                }
            }

            return Request.CreateResponse(HttpStatusCode.OK, new ResponseEntity
            {
                error = false,
                message = ResponseKeys.msgSuccess
            });
        }

        [HttpGet]
        [Route("UpdateActivatedPriorityHours")]
        public async Task<HttpResponseMessage> UpdateActivatedPriorityHours()
        {
            var onlineDrivers = await FirebaseService.GetOnlineDrivers();

            foreach (var od in onlineDrivers)
            {
                FirebaseDriver driver = od.Value;// JsonConvert.DeserializeObject<FirebaseDriver>(JsonConvert.SerializeObject(od.Value));
                if (string.IsNullOrEmpty(driver.driverID) || driver.location == null) //Dirty data
                {
                    continue;
                }
                else
                {
                    //make sure priortiy hour is active
                    if (!(driver.isPriorityHoursActive != true))
                    {
                        if (DateTime.Compare(DateTime.Parse(driver.priorityHourEndTime), DateTime.Parse(DateTime.UtcNow.ToString(Formats.DateFormat))) <= 0)
                        {
                            await FirebaseService.SetPriorityHourStatus(false, "", driver.driverID, "",
                                (await DriverService.UpdatePriorityHourLog(Guid.Parse(driver.driverID))).ToString());
                        }
                        else
                        {
                            var priorityHourRemainingTime = ((int)(DateTime.Parse(driver.priorityHourEndTime).Subtract(DateTime.Parse(DateTime.UtcNow.ToString(Formats.DateFormat))).TotalMinutes)).ToString();
                            await FirebaseService.UpdatePriorityHourTime(driver.driverID, priorityHourRemainingTime);
                        }
                    }
                }
            }

            return Request.CreateResponse(HttpStatusCode.OK, new ResponseEntity
            {
                error = false,
                message = ResponseKeys.msgSuccess
            });
        }

        [HttpGet]
        [Route("UpdatePendingLaterBookings")]
        public async Task<HttpResponseMessage> UpdatePendingLaterBookings()
        {
            var pendingLaterBookings = await FirebaseService.GetPendingLaterBookings();

            foreach (var booking in pendingLaterBookings)
            {
                //TimeOut later booking atleast 2 minutes before pickup time if no captain accepts the booking

                if ((DateTime.Parse(booking.Value.pickUpDateTime) - DateTime.UtcNow).TotalMinutes <= 2)
                {
                    var tp = await TripsManagerService.GetTripById(booking.Key);
                    if (tp != null)
                    {
                        tp.TripStatusID = (int)TripStatuses.TimeOut;

                        var user = await UserService.GetProfileAsync(booking.Value.userID, ApplicationID, ResellerID);

                        //user will be null if ride is booked by hotel / company.No need to send notifications on portals.
                        string deviceToken = user != null ? user.DeviceToken : "";

                        //Refund voucher amount
                        if (user == null && tp.BookingModeID == (int)BookingModes.Voucher)
                        {
                            VoucherService.RefundFullVoucherAmount(tp);
                        }

                        await FirebaseService.DeletePendingLaterBooking(tp.TripID.ToString());
                        await FirebaseService.DeleteTrip(tp.TripID.ToString());
                        await PushyService.UniCast(deviceToken, "Later booking timed out. No captain accepted your request.", NotificationKeys.pas_LaterBookingTimeOut);
                    }
                }
            }

            return Request.CreateResponse(HttpStatusCode.OK, new ResponseEntity
            {
                error = false,
                message = ResponseKeys.msgSuccess
            });
        }

        [HttpGet]
        [Route("UpdateUpcomingLaterBookings")]
        public async Task<HttpResponseMessage> UpdateUpcomingLaterBookings()
        {
            Dictionary<string, UpcomingLaterBooking> dic = await FirebaseService.GetUpcomingLaterBookings();

            if (dic != null)
            {
                foreach (var item in dic)
                {
                    var diff = (Convert.ToDateTime(item.Value.pickUpDateTime) - DateTime.UtcNow).TotalMinutes;

                    var tokens = new spGetDriverUserDeviceTokens_Result();
                    if (Convert.ToInt32(diff) == 30 && Convert.ToInt32(diff) > 0 && !item.Value.isSend30MinutSendFCM)
                    {
                        await FirebaseService.UpdateUpcomingBooking30MinuteFlag(item.Key);
                        tokens = await TripsManagerService.GetDriverAndPassengerDeviceToken(item.Value.tripID);

                        //captain fcm
                        await PushyService.UniCast(tokens.captainDeviceToken, tokens.captainName + " 30 minutes left your later booking.", NotificationKeys.cap_30MinutesLeft);

                        //user fcm
                        await PushyService.UniCast(tokens.userDeviceToken, tokens.userName + " 30 minutes left your later booking.", NotificationKeys.pas_30MinutesLeft);
                    }
                    else if (Convert.ToInt32(diff) == 20 && Convert.ToInt32(diff) > 0 && !item.Value.isSend20MinutSendFCM)
                    {
                        tokens = await TripsManagerService.GetDriverAndPassengerDeviceToken(item.Value.tripID);

                        await FirebaseService.UpdateUpcomingBooking20MinuteFlag(item.Key);

                        //captain fcm
                        await PushyService.UniCast(tokens.captainDeviceToken, tokens.captainName + " 20 minutes left your later booking.", NotificationKeys.cap_20MinutesLeft);

                        //user fcm
                        await PushyService.UniCast(tokens.userDeviceToken, tokens.userName + " 20 minutes left your later booking.", NotificationKeys.pas_20MinutesLeft);
                    }

                    /*Upcoming later booking is cancelled from application if captain don't start 15 min before pick up, if somehow
                    application fails to cancel upcoming later booking then in next min, server will cancel and re-route the booking*/

                    else if (Convert.ToInt32(diff) < 14)// && Convert.ToInt32(diff) > 0)
                    {
                        var trip = await TripsManagerService.GetUpcomingLaterBookingDetailsForCancel(item.Value.tripID);

                        //If later booking was of less than 15 min, then it is started as soon as driver accepts it.
                        //    Following check is applied to make precise decision - to be discussed.

                        if (trip != null && trip.diff > 15)
                        {
                            var req = new DriverCancelTripRequest()
                            {
                                isLaterBooking = trip.isLaterBooking,
                                resellerID = trip.ResellerID.ToString(),
                                resellerArea = trip.AuthorizedArea,
                                timeZoneOffset = "0", //Will not be considered, in case of reroute PickUpBookingDateTime is being fetched from db
                                isWeb = trip.isWeb == 1,
                                isAtPickupLocation = "false",
                                driverID = trip.CaptainID.ToString(),
                                cancelID = 1,
                                tripID = item.Value.tripID,
                                isDispatchedRide = "false",
                                isReRouteRequest = true   //Flag to ReRoute "On The Way" cancelled ride
                            };

                            DriverController dc = new DriverController();
                            await dc.cancelRide(req, Request);
                        }
                    }
                }
            }
            else
            {
                //Need to reconsider where clause, scenario not clear:
                //select * from cte where rn =1 

                var lstLaterBooking = await TripsManagerService.GetUpcomingLaterBookings();
                foreach (var laterBooking in lstLaterBooking)
                {
                    UpcomingLaterBooking lb = new UpcomingLaterBooking
                    {
                        tripID = laterBooking.TripID.ToString(),
                        pickUpDateTime = Convert.ToDateTime(laterBooking.PickUpBookingDateTime).ToString(Formats.DateFormat),
                        seatingCapacity = Convert.ToInt32(laterBooking.Noofperson),
                        pickUplatitude = laterBooking.PickupLocationLatitude,
                        pickUplongitude = laterBooking.PickupLocationLongitude,
                        pickUpLocation = laterBooking.PickUpLocation,
                        dropOfflatitude = laterBooking.DropOffLocationLatitude,
                        dropOfflongitude = laterBooking.DropOffLocationLongitude,
                        dropOffLocation = laterBooking.DropOffLocation,
                        passengerName = laterBooking.Name,
                        isSend30MinutSendFCM = (Convert.ToDateTime(laterBooking.PickUpBookingDateTime) - DateTime.UtcNow).TotalMinutes <= 30 ? true : false,
                        isSend20MinutSendFCM = (Convert.ToDateTime(laterBooking.PickUpBookingDateTime) - DateTime.UtcNow).TotalMinutes <= 20 ? true : false,
                        isWeb = laterBooking.isWeb

                    };

                    //if there are some later bookings on firebase
                    if (dic != null)
                    {
                        //if this booking already exists on firebase then no need to update the node
                        var checkOldLaterBooking = dic.Any(d => d.Value.tripID == laterBooking.TripID.ToString());
                        if (!checkOldLaterBooking)
                        {
                            await FirebaseService.AddUpcomingLaterBooking(laterBooking.CaptainID.ToString(), lb);
                        }
                    }
                    else
                    {
                        await FirebaseService.AddUpcomingLaterBooking(laterBooking.CaptainID.ToString(), lb);
                    }
                }
            }

            return Request.CreateResponse(HttpStatusCode.OK, new ResponseEntity
            {
                error = false,
                message = ResponseKeys.msgSuccess
            });
        }

        [HttpGet]
        [Route("UpdateFlags")]
        public async Task UpdateFlags(
                    string iOSPassengerForceUpdate,
                    string iOSPassengerAppVersion,
                    string iOSPassengerShowAlertMessage,
                    string iOSPassengerAlertMessage,

                    string iOSCaptainForceUpdate,
                    string iOSCaptainAppVersion,
                    string iOSCaptainShowAlertMessage,
                    string iOSCaptainAlertMessage,

                    string androidPassengerForceUpdate,
                    string androidPassengerAppVersion,
                    string andriodPassengerShowAlertMessage,
                    string andriodPassengerAlertMessage,

                    string androidCaptainForceUpdate,
                    string androidCaptainAppVersion,
                    string andriodCaptainShowAlertMessage,
                    string andriodCaptainAlertMessage)
        {
            #region ForceUpdate

            if (!string.IsNullOrEmpty(androidCaptainForceUpdate))
            {
                await FirebaseService.UpdateUtilities("GolbalSettings/Captain/Android/ForceUpdate", androidCaptainForceUpdate.ToLower());
            }

            if (!string.IsNullOrEmpty(androidPassengerForceUpdate))
            {
                await FirebaseService.UpdateUtilities("GolbalSettings/Passenger/Android/ForceUpdate", androidPassengerForceUpdate.ToLower());
            }

            if (!string.IsNullOrEmpty(iOSCaptainForceUpdate))
            {
                await FirebaseService.UpdateUtilities("GolbalSettings/Captain/IOS/ForceUpdate", iOSCaptainForceUpdate.ToLower());
            }

            if (!string.IsNullOrEmpty(iOSPassengerForceUpdate))
            {
                await FirebaseService.UpdateUtilities("GolbalSettings/Passenger/IOS/ForceUpdate", iOSPassengerForceUpdate.ToLower());
            }

            #endregion

            #region AppVersion

            if (!string.IsNullOrEmpty(androidCaptainAppVersion))
            {
                await FirebaseService.UpdateUtilities("GolbalSettings/Captain/Android/AppVersion", androidCaptainAppVersion);
            }

            if (!string.IsNullOrEmpty(androidPassengerAppVersion))
            {
                await FirebaseService.UpdateUtilities("GolbalSettings/Passenger/Android/AppVersion", androidPassengerAppVersion);
            }

            if (!string.IsNullOrEmpty(iOSCaptainAppVersion))
            {
                await FirebaseService.UpdateUtilities("GolbalSettings/Captain/IOS/AppVersion", iOSCaptainAppVersion);
            }

            if (!string.IsNullOrEmpty(iOSPassengerAppVersion))
            {
                await FirebaseService.UpdateUtilities("GolbalSettings/Passenger/IOS/AppVersion", iOSPassengerAppVersion);
            }

            #endregion

            #region ShowAlertMessage

            if (!string.IsNullOrEmpty(andriodCaptainShowAlertMessage))
            {
                await FirebaseService.UpdateUtilities("GolbalSettings/Captain/Android/ShowAlertMessage", andriodCaptainShowAlertMessage);
            }

            if (!string.IsNullOrEmpty(andriodPassengerShowAlertMessage))
            {
                await FirebaseService.UpdateUtilities("GolbalSettings/Passenger/Android/ShowAlertMessage", andriodPassengerShowAlertMessage);
            }

            if (!string.IsNullOrEmpty(iOSCaptainShowAlertMessage))
            {
                await FirebaseService.UpdateUtilities("GolbalSettings/Captain/IOS/ShowAlertMessage", iOSCaptainShowAlertMessage);
            }

            if (!string.IsNullOrEmpty(iOSPassengerShowAlertMessage))
            {
                await FirebaseService.UpdateUtilities("GolbalSettings/Passenger/IOS/ShowAlertMessage", iOSPassengerShowAlertMessage);
            }

            #endregion

            #region AlertMessage

            if (!string.IsNullOrEmpty(andriodCaptainAlertMessage))
            {
                await FirebaseService.UpdateUtilities("GolbalSettings/Captain/Android/AlertMessage", andriodCaptainAlertMessage);
            }

            if (!string.IsNullOrEmpty(andriodPassengerAlertMessage))
            {
                await FirebaseService.UpdateUtilities("GolbalSettings/Passenger/Android/AlertMessage", andriodPassengerAlertMessage);
            }

            if (!string.IsNullOrEmpty(iOSCaptainAlertMessage))
            {
                await FirebaseService.UpdateUtilities("GolbalSettings/Captain/IOS/AlertMessage", iOSCaptainAlertMessage);
            }

            if (!string.IsNullOrEmpty(iOSPassengerAlertMessage))
            {
                await FirebaseService.UpdateUtilities("GolbalSettings/Passenger/IOS/AlertMessage", iOSPassengerAlertMessage);
            }

            #endregion
        }


        #region Helper Function

        private async Task SetDriverOffline(string driverID, string vehicleID)
        {
            await FirebaseService.OfflineDriver(driverID);
            DriverService.OnlineOfflineDriver(driverID, string.IsNullOrEmpty(vehicleID) ? "" : vehicleID, false, Guid.Parse(ConfigurationManager.AppSettings["ApplicationID"].ToString()));
        }

        #endregion
    }
}