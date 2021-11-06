using API.Filters;
using Constants;
using DatabaseModel;
using DTOs.API;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Owin.Security;
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
    public class ResponseEntity
    {
        public bool error { get; set; }
        public string message { get; set; }
        public dynamic data { get; set; }
    }

    [Authorize]
    [RoutePrefix("api/Driver")]
    public class DriverController : BaseController
    {
        ResponseEntity response = new ResponseEntity();
        Dictionary<dynamic, dynamic> dic;
        [HttpPost]
        public HttpResponseMessage verifyDeviceToken([FromBody] DriverVerifyDeviceTokenRequest model)
        {
            using (CangooEntities context = new CangooEntities())
            {
                var cap = context.Captains.Where(c => c.CaptainID.ToString().Equals(model.driverID)
                && c.ApplicationID.ToString().ToLower().Equals(ApplicationID.ToLower())
                && c.ResellerID.ToString().ToLower().Equals(ResellerID.ToLower())
                ).FirstOrDefault();

                response.error = false;
                response.message = ResponseKeys.msgSuccess;
                response.data = new DriverVerifyDeviceTokenResponse
                {
                    isTokenVerified = cap.DeviceToken.ToLower().Equals(model.DeviceToken.ToLower())
                };
                return Request.CreateResponse(HttpStatusCode.OK, response);
            }
        }

        #region Authentication Flow

        [HttpPost]
        [AllowAnonymous]            //Register Captain UI
        public HttpResponseMessage phoneVerification([FromBody] DriverVerifyPhoneNumberRequest model)
        {
            using (CangooEntities context = new CangooEntities())
            {
                var store = new UserStore<IdentityUser>();
                var userManager = new UserManager<IdentityUser>(store);

                IdentityUser cUser = userManager.FindByName(model.UserName);
                if (cUser != null)
                {
                    if (cUser.PhoneNumberConfirmed)
                    {
                        response.error = true;
                        response.message = ResponseKeys.userAlreadyVerified;
                        return Request.CreateResponse(HttpStatusCode.OK, response);
                    }

                    dic = new Dictionary<dynamic, dynamic>();

                    string role = Enum.GetName(typeof(SystemRoles), SystemRoles.Captain); // Enumration.returnRoleDes(App_Start.Roles.Captain);
                    var codeUserID = context.spPhoneVerication(model.UserName, role).FirstOrDefault();
                    var code = codeUserID.code;
                    var userID = codeUserID.userID;

                    if (!string.IsNullOrEmpty(code))
                    {
                        //dic.Add("code", code);
                        //dic.Add("user_id", userID);
                        //dic.Add("is_user_register", true);

                        response.data = new DriverVerifyPhoneNumberResponse
                        {
                            code = code,
                            user_id = userID,
                            is_user_register = true
                        };

                        response.error = false;
                        response.message = ResponseKeys.msgSuccess;
                    }
                    else
                    {
                        //dic.Add("code", "");
                        //dic.Add("user_id", "");
                        //dic.Add("is_user_register", false);

                        response.data = new DriverVerifyPhoneNumberResponse
                        {
                            is_user_register = false
                        };

                        response.error = true;
                        response.message = ResponseKeys.captainNotRegistered;
                    }
                    response.data = dic;
                    return Request.CreateResponse(HttpStatusCode.OK, response);
                }
                else
                {
                    response.error = true;
                    response.message = ResponseKeys.captainNotFound;
                    return Request.CreateResponse(HttpStatusCode.OK, response);
                }
            }
        }

        [HttpPost]
        [AllowAnonymous]            //Set password after phone verification for first time login
        public async Task<HttpResponseMessage> setNewPassword([FromBody] DriverSetNewPasswordRequest model)
        {
            if (model != null && !string.IsNullOrEmpty(model.userID) && !string.IsNullOrEmpty(model.password))
            {

                var store = new UserStore<IdentityUser>();
                IdentityUser cUser = await store.FindByIdAsync(model.userID);
                if (cUser != null)
                {
                    var userManager = new UserManager<IdentityUser>(store);

                    await store.SetPasswordHashAsync(cUser, userManager.PasswordHasher.HashPassword(model.password));
                    cUser.PhoneNumberConfirmed = true;
                    await store.UpdateAsync(cUser);
                    await TextMessageService.SendDriverMessages("Das neue Passwort für cangoo-Konto lautet " + model.password, cUser.PhoneNumber);
                    //SendSMS.SendSms("Das neue Passwort für cangoo-Konto lautet " + model.password, cUser.PhoneNumber);

                    response.error = false;
                    response.message = ResponseKeys.msgSuccess;
                    return Request.CreateResponse(HttpStatusCode.OK, response);
                }
                else
                {
                    response.error = true;
                    response.message = ResponseKeys.captainNotFound;
                    return Request.CreateResponse(HttpStatusCode.OK, response);
                }
            }
            else
            {
                response.error = true;
                response.message = ResponseKeys.invalidParameters;
                return Request.CreateResponse(HttpStatusCode.OK, response);
            }
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<HttpResponseMessage> loginDriver([FromBody] DriverLoginRequest model)
        {
            if (!ApplicationID.ToString().ToUpper().Equals(ConfigurationManager.AppSettings["ApplicationID"].ToString().ToUpper()))
            {
                response.error = true;
                response.message = ResponseKeys.authenticationFailed;
                return Request.CreateResponse(HttpStatusCode.OK, response);
            }
            else
            {
                using (var context = new CangooEntities())
                {
                    var app = context.Applications.Where(a => a.ApplicationID.ToString().Equals(ApplicationID.ToString())).FirstOrDefault();
                    if (app.isBlocked)
                    {
                        response.error = true;
                        response.message = ResponseKeys.authenticationFailed;
                        return Request.CreateResponse(HttpStatusCode.OK, response);
                    }
                }
            }

            //TBD: Apply is phone number verified check, will be required when captain phone number change option will be available.

            using (CangooEntities context = new CangooEntities())
            {
                var store = new UserStore<IdentityUser>();
                var userManager = new UserManager<IdentityUser>(store);
                var user = userManager.FindByName(model.UserName);

                if (user == null)
                {
                    response.error = true;
                    response.message = ResponseKeys.authenticationFailed;
                    return Request.CreateResponse(HttpStatusCode.OK, response);
                }

                var captain = context.Captains.Where(c => c.CaptainID.ToString().Equals(user.Id)).FirstOrDefault();

                if (captain != null)
                {
                    var request = HttpContext.Current.Request;
                    var tokenServiceUrl = request.Url.GetLeftPart(UriPartial.Authority) + request.ApplicationPath + "/Token";
                    using (var client = new HttpClient())
                    {
                        var requestParams = new List<KeyValuePair<string, string>>
                                        {
                                            new KeyValuePair<string, string>("grant_type", "password"),
                                            new KeyValuePair<string, string>("username", model.UserName),
                                            new KeyValuePair<string, string>("password", model.password)
                                        };
                        var requestParamsFormUrlEncoded = new FormUrlEncodedContent(requestParams);
                        var tokenServiceResponse = await client.PostAsync(tokenServiceUrl, requestParamsFormUrlEncoded);
                        var responseString = await tokenServiceResponse.Content.ReadAsStringAsync();
                        var code = "";
                        if (responseString.Contains("access_token"))
                        {
                            if (!user.PhoneNumberConfirmed)
                            {
                                response.error = true;
                                response.message = ResponseKeys.phoneNumberNotConfirmed;
                                return Request.CreateResponse(HttpStatusCode.OK, response);
                            }

                            if (captain.isActive != true)
                            {
                                response.error = true;
                                response.message = ResponseKeys.captainBlocked;
                                dic = new Dictionary<dynamic, dynamic>
                                                {
                                                    { "captainID", captain.CaptainID },
                                                    { "isBlocked", true }
                                                };
                                response.data = dic;
                                return Request.CreateResponse(HttpStatusCode.OK, response);
                            }

                            var subString = responseString.Split(':');
                            code = subString[1].Split(',')[0];

                            var applicationData = context.spGetApplicationArea(ApplicationID).FirstOrDefault();

                            var isCaptanInTrip = await FirebaseService.isDriverInTrip(captain.CaptainID.ToString());
                            DriverModel captainModel = new DriverModel
                            {
                                userID = captain.CaptainID.ToString(),
                                UserName = model.UserName,
                                phone = user.PhoneNumber,
                                Name = captain.Name,
                                EarningPoints = captain.EarningPoints == null ? "0.0" : captain.EarningPoints.ToString(),
                                Email = captain.Email,
                                IsPriorityHoursActive = captain.IsPriorityHoursActive != true ? false : true,
                                priorityHourEndTime = captain.LastPriorityHourEndTime != null ? DateTime.Parse(captain.LastPriorityHourEndTime.ToString()).ToString(Formats.DateFormat) : "",
                                NumberOfFavoriteUser = captain.NumberOfFavoriteUser == null ? "0" : captain.NumberOfFavoriteUser.ToString(),
                                TotalOnlineHours = captain.TotalOnlineHours == null ? "0.0" : captain.TotalOnlineHours.ToString(),
                                ShareCode = captain.ShareCode,
                                verificationCode = captain.VerificationCode,
                                access_Token = code.Replace("\"", ""),
                                LastLogin = captain.LastLogin.ToString(),
                                MemberSince = captain.MemberSince.ToString(),
                                rating = captain.Rating,
                                totalEarnings = captain.Earning == null ? 0 : captain.Earning,
                                totalRides = captain.NoOfTrips == null ? 0 : captain.NoOfTrips,
                                Picture = captain.Picture,
                                DrivingLicense = captain.DrivingLicense,
                                normalBookingNotificationTone = string.IsNullOrEmpty(captain.NormalBookingNotificationTone) ? "Notification Tone 1" : captain.NormalBookingNotificationTone,
                                laterBookingNotificationTone = string.IsNullOrEmpty(captain.LaterBookingNotificationTone) ? "Notification Tone 1" : captain.LaterBookingNotificationTone,
                                showOtherVehicles = captain.ShowOtherVehicles.ToString(),
                                resellerID = ResellerID,
                                applicationID = ApplicationID,
                                fleetID = captain.CompanyID.ToString(),
                                isAlreadyInTrip = isCaptanInTrip
                            };

                            if (isCaptanInTrip)
                            {
                                captainModel.vehicleDetails = context.spGetCaptainOccupiedVehicleDetails(ResellerID, ApplicationID, user.Id).FirstOrDefault();

                                LogOutAlreadyLoggedInCaptain(model.DeviceToken, user.Id, captain.DeviceToken, true);
                                await PushyService.UniCast(captain.DeviceToken, dic, NotificationKeys.cap_NewDeviceLoggedIn);

                                captain.DeviceToken = model.DeviceToken;
                                context.SaveChanges();
                            }

                            response.error = false;
                            response.message = ResponseKeys.msgSuccess;
                            dic = new Dictionary<dynamic, dynamic>
                                    {
                                        { "Captain", captainModel },
                                        { "resellerID", applicationData.ApplicationID },
                                        { "resellerAuthorizeArea", applicationData.AuthorizedArea }
                                    };
                            response.data = dic;
                            return Request.CreateResponse(HttpStatusCode.OK, response);
                        }
                        else
                        {
                            response.error = true;
                            response.message = ResponseKeys.authenticationFailed;
                            return Request.CreateResponse(HttpStatusCode.OK, response);
                        }
                    }
                }
                else
                {
                    response.error = true;
                    response.message = ResponseKeys.captainNotFound;
                    return Request.CreateResponse(HttpStatusCode.OK, response);
                }
            }
        }

        [HttpGet]
        public async Task<HttpResponseMessage> getVehicleList([FromUri] GetVehicleListRequest model)
        {
            using (CangooEntities context = new CangooEntities())
            {
                var onlineDrivers = await FirebaseService.GetOnlineDrivers();

                var fleetOnlineDrivers = onlineDrivers.Where(d => d.Value.companyID == model.fleetID).ToList();

                var dbCaptains = context.Captains.Where(c => c.CompanyID.ToString().Equals(model.fleetID)).ToList();

                var lstFleetOfflineDrivres = dbCaptains.Where(dc => !fleetOnlineDrivers.Any(od => od.Value.driverID == dc.CaptainID.ToString()));
                foreach (var driver in lstFleetOfflineDrivres)
                {
                    DriverService.OnlineOfflineDriver(driver.CaptainID.ToString(), "", false, Guid.Parse(ApplicationID));
                }

                //var lstFleetVehicleDetails = context.spGetCompanyVehicle(model.fleetID).ToList();

                //dic = new Dictionary<dynamic, dynamic>
                //        {
                //            { "vehicle", lstFleetVehicleDetails }
                //        };

                response.error = false;
                response.message = ResponseKeys.msgSuccess;
                response.data = new GetVehicleListResponse
                {
                    vehicle = context.spGetCompanyVehicle(model.fleetID).ToList()
                };
                return Request.CreateResponse(HttpStatusCode.OK, response);
            }
        }

        [HttpPost]
        public async Task<HttpResponseMessage> changeVehicleStatus([FromBody] ChangeVehicleStatusRequest model)
        {
            using (CangooEntities context = new CangooEntities())
            {
                var cap = (from c in context.Captains
                           join u in context.AspNetUsers on c.CaptainID.ToString() equals u.Id
                           where c.CaptainID.ToString().Equals(model.driverID)
                           && c.ApplicationID.ToString().ToLower().Equals(ApplicationID.ToLower())
                           && c.ResellerID.ToString().ToLower().Equals(ResellerID.ToLower())
                           select new
                           {
                               c.DeviceToken,
                               c.IsPriorityHoursActive,
                               c.LastPriorityHourEndTime,
                               c.EarningPoints,
                               c.Facilities,
                               u.UserName,
                               u.PhoneNumber
                           }).FirstOrDefault();

                //var cap = context.Captains.Where(c => c.CaptainID.ToString().Equals(model.driverID)
                //&& c.ApplicationID.ToString().ToLower().Equals(ApplicationID.ToLower())
                //&& c.ResellerID.ToString().ToLower().Equals(ResellerID.ToLower())
                //).FirstOrDefault();

                //First time login check
                if (!string.IsNullOrEmpty(cap.DeviceToken))
                {
                    //If this endpoint is accessed then we have already confirmed that captain was not in trip.
                    LogOutAlreadyLoggedInCaptain(model.DeviceToken, model.driverID, cap.DeviceToken, await FirebaseService.isDriverInTrip(model.driverID));
                    await PushyService.UniCast(cap.DeviceToken, dic, NotificationKeys.cap_NewDeviceLoggedIn);
                }

                var vh = context.spVehicleBookedUnBooked(model.driverID, model.DeviceToken.Trim(), model.vehicleID, DateTime.UtcNow, model.isBooked).FirstOrDefault();

                await FirebaseService.OnlineDriver(model.driverID, model.vehicleID, Convert.ToInt32(vh.MakeID), vh.CompanyID.ToString(),
                    vh.SeatingCapacity.ToString(), vh.Make, vh.Model, model.driverName, cap.IsPriorityHoursActive,
                    cap.LastPriorityHourEndTime.ToString(), cap.EarningPoints.ToString(),
                    vh.Facilities, cap.Facilities, cap.DeviceToken, cap.UserName, cap.PhoneNumber, vh.ModelID, vh.PlateNumber, vh.Category, vh.CategoryID,
                    vh.RegistrationYear, vh.Color);

                //This endpoint is accessed only when vehicle is booked. model.isBooked = true
                DriverService.OnlineOfflineDriver(model.driverID, model.vehicleID, model.isBooked, Guid.Parse(ApplicationID));

                response.error = false;
                response.message = ResponseKeys.msgSuccess;
                //dic = new Dictionary<dynamic, dynamic>
                //        {
                //            { "vehicle", vh }
                //        };
                response.data = new ChangeVehicleStatusResponse
                {
                    vehicle = vh
                };
                return Request.CreateResponse(HttpStatusCode.OK, response);
            }
        }

        [HttpPost]
        //[AllowAnonymous] //TBD: Remove once Authentication failure issue is fixed.
        public async Task<HttpResponseMessage> logOutDriver([FromBody] VehicleDetail model)
        {
            if (model != null && !string.IsNullOrEmpty(model.driverID) && !string.IsNullOrEmpty(model.DeviceToken))
            {
                using (CangooEntities context = new CangooEntities())
                {
                    //If driver logout directly, or app is killed then remove device token.
                    var cap = context.Captains.Where(c => c.CaptainID.ToString().Equals(model.driverID)).FirstOrDefault();

                    //if (cap.DeviceToken != null)
                    //{
                    if (cap.DeviceToken.ToLower().Equals(model.DeviceToken.ToLower()))
                    {
                        cap.DeviceToken = null;
                        context.SaveChanges();
                    }
                    //}

                    if (!string.IsNullOrEmpty(model.vehicleID))
                    {
                        await FirebaseService.OfflineDriver(model.driverID);
                        DriverService.OnlineOfflineDriver(model.driverID, model.vehicleID, false, Guid.Parse(ApplicationID));
                    }

                    response.error = false;
                    response.message = ResponseKeys.msgSuccess;
                    return Request.CreateResponse(HttpStatusCode.OK, response);
                }
            }
            else
            {
                response.error = true;
                response.message = ResponseKeys.invalidParameters;
                return Request.CreateResponse(HttpStatusCode.OK, response);
            }
        }

        [HttpPost]
        [AllowAnonymous]    //Forgot Password UI
        public async Task<HttpResponseMessage> resetPassword([FromBody] DriverModel model)
        {
            if (model != null && !string.IsNullOrEmpty(model.UserName))
            {
                //using (CangooEntities context = new CangooEntities())
                //{
                //	var checkRole = context.spCheckUserWithRole(driver.phone, App_Start.Enumration.returnRoleDes(App_Start.Roles.Captain));

                //	if (checkRole.FirstOrDefault().Value == 0)
                //	{
                //		response.error = true;
                //		response.message = ResponseKeys.authenticationFailed;
                //		return Request.CreateResponse(HttpStatusCode.OK, response);
                //	}
                //}

                var store = new UserStore<IdentityUser>();
                IdentityUser cUser = await store.FindByNameAsync(model.UserName);
                if (cUser != null)
                {
                    var userManager = new UserManager<IdentityUser>(store);

                    string newPassword = AuthenticationService.GetRandomPassword();
                    string hashedNewPassword = userManager.PasswordHasher.HashPassword(newPassword);

                    await store.SetPasswordHashAsync(cUser, hashedNewPassword);
                    await store.UpdateAsync(cUser);

                    //SendSMS.SendSms("Dear captain, your Cangoo account new password is:\n" + newPassword, cUser.PhoneNumber);
                    await TextMessageService.SendDriverMessages("Das Passwort für dein cangoo-Fahrer wurde nun zurückgesetzt.\nDein neues Passwort lautet " + newPassword, cUser.PhoneNumber);

                    response.error = false;
                    response.message = ResponseKeys.msgSuccess;
                    return Request.CreateResponse(HttpStatusCode.OK, response);
                }
                else
                {
                    response.error = true;
                    response.message = ResponseKeys.captainNotFound;
                    return Request.CreateResponse(HttpStatusCode.OK, response);
                }
            }
            else
            {
                response.error = true;
                response.message = ResponseKeys.invalidParameters;
                return Request.CreateResponse(HttpStatusCode.OK, response);
            }
        }

        [HttpPost]          //Change Password UI
        public async Task<HttpResponseMessage> chagePassword([FromBody] DriverModel model)
        {
            if (model != null && !string.IsNullOrEmpty(model.UserName) && !string.IsNullOrEmpty(model.password) && !string.IsNullOrEmpty(model.oldPassword))
            {
                CangooEntities context = new CangooEntities();
                var store = new UserStore<IdentityUser>();
                var userManager = new UserManager<IdentityUser>(store);

                IdentityUser cUser = await userManager.FindAsync(model.UserName, model.oldPassword);
                if (cUser == null)
                {
                    response.error = true;
                    response.message = ResponseKeys.failedToResetPassword;
                    return Request.CreateResponse(HttpStatusCode.OK, response);
                }
                else
                {
                    string hashedNewPassword = userManager.PasswordHasher.HashPassword(model.password);
                    await store.SetPasswordHashAsync(cUser, hashedNewPassword);
                    await store.UpdateAsync(cUser);

                    response.error = false;
                    response.message = ResponseKeys.msgSuccess;
                    return Request.CreateResponse(HttpStatusCode.OK, response);
                }
            }
            else
            {
                response.error = true;
                response.message = ResponseKeys.invalidParameters;
                return Request.CreateResponse(HttpStatusCode.OK, response);
            }
        }

        #endregion

        #region Booked Ride Scenario

        [HttpPost]      //accept pending later booking
        public async Task<HttpResponseMessage> acceptLateBooking([FromBody] RequestModel model)
        {
            if (model != null && !string.IsNullOrEmpty(model.tripID) && !string.IsNullOrEmpty(model.driverID))
            {
                //App_Start.Enumration.isRequestAccepted = false;
                using (CangooEntities context = new CangooEntities())
                {
                    if (model.isCheckLaterBookingConflict)
                    {
                        var conf = checkLaterBookingDate(model.driverID, Convert.ToDateTime(model.pickUpDateTime));
                        if (conf.isConflict)
                        {
                            dic = new Dictionary<dynamic, dynamic>
                                {
                                    { "alreadyBooked", DateTime.Parse(conf.pickUpDateTime).ToString(Formats.DateFormat) }
                                };
                            response.data = dic;
                            response.error = false;
                            response.message = ResponseKeys.laterBookingConflict;
                            return Request.CreateResponse(HttpStatusCode.OK, response);
                        }
                    }

                    var tp = context.Trips.Where(t => t.TripID.ToString() == model.tripID && t.TripStatusID == (int)TripStatuses.RequestSent && t.isLaterBooking == true).FirstOrDefault();
                    if (tp == null)
                    {
                        response.error = true;
                        response.message = ResponseKeys.tripAlreadyBooked;
                        return Request.CreateResponse(HttpStatusCode.OK, response);
                    }
                    tp.TripStatusID = (int)TripStatuses.LaterBookingAccepted;
                    tp.CaptainID = new Guid(model.driverID);
                    await context.SaveChangesAsync();

                    //New Implemention
                    //var tripDiscountDetails = await FirebaseService.GetTripDiscountDetails(model.tripID);
                    //var tripDispatchedStatus = await FirebaseService.GetTripDispatchedStatus(model.tripID);
                    //await FirebaseService.DeleteTrip(model.tripID);

                    await FirebaseService.DeletePendingLaterBooking(model.tripID);
                    await FirebaseService.SetDriverLaterBookingCancelReasons();
                    await FirebaseService.AddUpcomingLaterBooking(model.driverID, await DriverService.GetUpcomingLaterBooking(model.driverID));

                    /* REFACTOR
                    * If we stop deleting trip node then following look up implementation will be no more needed. */

                    //New Implementation (Trip node is not delted, so discont details already exist)
                    //await FirebaseService.UpdateDiscountTypeAndAmount(model.tripID, tripDiscountDetails.DiscountAmount, tripDiscountDetails.DiscountType);
                    //await FirebaseService.SetTripDispatchedStatus(model.tripID, tripDispatchedStatus);

                    response.error = false;
                    response.message = ResponseKeys.msgSuccess;
                    return Request.CreateResponse(HttpStatusCode.OK, response);
                }
            }
            else
            {
                response.error = true;
                response.message = ResponseKeys.invalidParameters;
                return Request.CreateResponse(HttpStatusCode.OK, response);
            }
        }

        [HttpPost]      //Start later booking / accept normal request
        public async Task<HttpResponseMessage> acceptRequest([FromBody] RequestModel model)
        {
            int bookingStatus = 0;

            //Get trip current status before any status change
            if (model.isLaterBooking)
            {
                bookingStatus = (int)TripStatuses.LaterBookingAccepted;
            }
            else
            {
                //App_Start.Enumration.isRequestAccepted = true;

                if (model.isDispatchedRide.ToLower().Equals("true"))
                    bookingStatus = (int)TripStatuses.OnTheWay;
                else if (model.isReRouteRequest)
                    bookingStatus = (int)TripStatuses.Cancel;
                else
                    bookingStatus = (int)TripStatuses.RequestSent;
            }

            //In case of dispatched ride save id and device token of previous captain for logging purpose.
            string driverId = "";
            string driverDeviceToken = "";

            var trip = await TripsManagerService.GetTripById(model.tripID);// context.Trips.Where(t => t.TripID.ToString().Equals(model.tripID)).FirstOrDefault();
            if (model.isDispatchedRide.ToLower().Equals("true"))
            {
                driverId = trip.CaptainID.ToString();
                driverDeviceToken = await DriverService.GetDriverDeviceToken(trip.CaptainID.ToString());
            }

            //In case of laterbooking the flag is not maintained appropriately

            model.isWeb = trip.BookingModeID == (int)BookingModes.UserApplication ? false : true;

            //Update trip status in database
            var detail = await DriverService.GetUpdateTripDataOnAcceptRide(model.tripID, model.driverID, model.vehicleID, bookingStatus, model.isLaterBooking == true ? 1 : 0);

            if (detail != null)
            {
                var lstcr = await CancelReasonsService.GetCancelReasons(!model.isLaterBooking, model.isLaterBooking, true);

                //Object to be used to populate passenger FCM object
                AcceptRideDriverModel arm = new AcceptRideDriverModel
                {
                    tripID = model.tripID,
                    pickupLocationLatitude = detail.PickupLocationLatitude,
                    pickupLocationLongitude = detail.PickupLocationLongitude,
                    midwayStop1LocationLatitude = "",
                    midwayStop1LocationLongitude = "",
                    dropOffLocationLatitude = detail.dropoffLocationLatitude,
                    dropOffLocationLongitude = detail.dropofflocationLongitude,
                    requestTime = detail.RequestWaitingTime,
                    //UPDATE: After reverting google directions API, detail.ArrivedTime is distance in Meters - To enable arrived button on captain app before reaching pickup location
                    minDistance = detail.ArrivedTime,
                    passengerID = detail.UserID.ToString(),
                    passengerName = detail.PassengerName,
                    phone = detail.PhoneNumber,
                    isWeb = model.isWeb,
                    lstCancel = lstcr,
                    isLaterBooking = model.isLaterBooking,
                    laterBookingPickUpDateTime = Convert.ToDateTime(detail.PickUpBookingDateTime).ToString(Formats.DateFormat),
                    isDispatchedRide = model.isDispatchedRide,
                    distanceTraveled = "0.00",
                    isReRouteRequest = detail.isReRouted.ToString(),
                    numberOfPerson = trip.NoOfPerson.ToString(),
                    description = detail.description,
                    voucherCode = detail.VoucherCode,
                    voucherAmount = detail.VoucherAmount.ToString(),
                    isFareChangePermissionGranted = "false",
                    bookingMode = trip.BookingModeID == (int)BookingModes.Karhoo ? Enum.GetName(typeof(BookingModes), (int)BookingModes.Karhoo).ToLower() : ""
                };

                await FirebaseService.SetDriverBusy(model.driverID, model.tripID);
                await FirebaseService.SetEstimateDistanceToPickUpLocation(model.driverID, string.IsNullOrEmpty(model.distanceToPickUpLocation) ? "0" : model.distanceToPickUpLocation);

                await FirebaseService.UpdateTripsAndNotifyPassengerOnRequestAcceptd(model.driverID, detail.UserID.ToString(), arm, model.tripID, model.isWeb);

                if (model.isDispatchedRide.ToLower().Equals("true"))
                {
                    await FirebaseService.sendNotificationsAfterDispatchingRide(driverDeviceToken, driverId, model.tripID);

                    //TBD: Log previous captain priority points
                    await TripsManagerService.LogDispatchedTrips(new DispatchedRideLogDTO
                    {
                        DispatchedBy = Guid.Parse(model.dispatcherID),
                        CaptainID = Guid.Parse(driverId),
                        DispatchLogID = Guid.NewGuid(),
                        LogTime = DateTime.UtcNow,
                        TripID = Guid.Parse(model.tripID)
                    });

                    var cap = await DriverService.GetDriverById(driverId);
                    if (!(bool)cap.IsPriorityHoursActive)
                    {
                        //TBD: Fetch distance traveled from driver node and update points accordingly.

                        await FirebaseService.UpdateDriverEarnedPoints(driverId, cap.EarningPoints.ToString());
                    }
                }

                //API response data
                dic = new Dictionary<dynamic, dynamic>
                                    {
                                //REFACTOR - These lat long keys are no more in use. Remove these keys.
                                        { "lat", detail.PickupLocationLatitude },
                                        { "lon", detail.PickupLocationLongitude },
                                        { "passengerID", detail.UserID },
                                        { "minDistance", detail.ArrivedTime },
                                        { "requestTime", detail.RequestWaitingTime },
                                        { "phone", detail.PhoneNumber },
                                        { "tripID", model.tripID },
                                        { "isWeb", model.isWeb },
                                        { "laterBookingPickUpDateTime", Convert.ToDateTime(detail.PickUpBookingDateTime).ToString(Formats.DateFormat) },
                                        { "isLaterBooking", model.isLaterBooking },
                                        { "cancel_reasons", lstcr },
                                        { "passengerName",  detail.PassengerName},
                                        { "bookingMode", arm.bookingMode}
                                    };

                //In case of later booking update captain UpcomingLaterBookings
                if (model.isLaterBooking)
                {
                    await FirebaseService.DeleteUpcomingLaterBooking(model.driverID);

                    Dictionary<string, dynamic> dic = new Dictionary<string, dynamic>
                                                                    {
                                                                        { "lstCancel", lstcr }
                                                                    };

                    /*VERIFY:
                     * If setting firebase node removes existing data which is not avaiable in new data then discount node should remove here.
                     */

                    /*REFACTOR
                     * If trip node is not delted on accepting pending later booking then
                     */

                    //Writer later booking on firebase in Trips node to deal ride flow as normal booking
                    await FirebaseService.SetTripCancelReasonsForPassenger(model.tripID, detail.UserID.ToString(), dic);

                    //NEW IMPLEMENTATION

                    //Dictionary<string, LaterBooking> dicLaterBooking = new Dictionary<string, LaterBooking>();
                    ////check if driver have any other later booking then save the record on fb as UpcomingLaterBooking
                    //fc.getUpcomingLaterBooking(dicLaterBooking);
                }

                //For user application state management
                await FirebaseService.SetPassengerTrip(detail.UserID.ToString(), model.tripID);

                response.error = false;
                response.data = dic;
                response.message = ResponseKeys.msgSuccess;
                return Request.CreateResponse(HttpStatusCode.OK, response);
            }
            else
            {
                response.error = true;
                response.message = ResponseKeys.tripNotFound;
                return Request.CreateResponse(HttpStatusCode.OK, response);
            }
        }


        //[HttpPost]
        //public HttpResponseMessage cancelRide([FromBody] RequestModel model, HttpRequestMessage request = null)
        //{
        //        if (model != null && !string.IsNullOrEmpty(model.tripID) && model.cancelID > 0 && !string.IsNullOrEmpty(model.driverID) &&
        //            !string.IsNullOrEmpty(model.isAtPickupLocation) && !string.IsNullOrEmpty(model.resellerArea))
        //        {
        //            using (CangooEntities context = new CangooEntities())
        //            {
        //                //TBD: Add new trip status Cancel
        //                var tp = context.spCaptainCancelRide(model.tripID, model.driverID, (int)App_Start.TripStatus.Cancel, model.cancelID, model.isWeb, bool.Parse(model.isAtPickupLocation), (DateTime)Common.getUtcDateTime()).FirstOrDefault();

        //                if (tp == null)
        //                {
        //                    response.error = true;
        //                    response.message = ResponseKeys.tripNotFound;
        //                    return Request.CreateResponse(HttpStatusCode.OK, response);
        //                }

        //                UserController uc = new UserController();
        //                FireBaseController fc = new FireBaseController();

        //                //Regardless of ride statud (Accepted / On The Way / Arrived), if it is cancelled at least 2 min before pick up time 
        //                //then request should be rerouted as new later booking

        //                model.isLaterBooking = (bool)tp.isLaterBooking ?
        //                        (((DateTime)tp.PickUpBookingDateTime) - DateTime.UtcNow).TotalMinutes > 2 ? true : false
        //                    : false;

        //                //From app laterbooking of less than 5 min is not allowed, but rerouted later booking can be of at least 2 min.
        //                if (model.isLaterBooking)
        //                {
        //                    //Step 1: Refresh pending later bookings (not accepted by any driver) node on firebase
        //                    fc.addRemovePendingLaterBookings(true, tp.UserID.ToString(), tp.TripID.ToString(), ((DateTime)tp.PickUpBookingDateTime).ToString(Common.dateFormat), (int)tp.NoOfPerson);

        //                    //Step 2: Refresh current driver upcoming later booking on firebase
        //                    fc.addDeleteNode(true, null, "UpcomingLaterBooking/" + model.driverID);
        //                    Common.getUpcomingLaterBookingByDriverID(model.driverID);
        //                }

        //                fc.updateDriverStatus(model.driverID, "false", model.tripID);
        //                fc.updateDriverEarnedPoints(model.driverID, tp.EarningPoints.ToString());

        //                dic = new Dictionary<dynamic, dynamic>
        //                {
        //                    { "tripID", model.tripID }
        //                };

        //                //in normal booking if captain is arrived and cancels the ride then don't reroute
        //                //in later booking if less than or equal to 2 min are remainig, captain cancels trip after arrival then don't reroute

        //                if (!model.isLaterBooking && tp.ArrivalDateTime != null)
        //                {
        //                    fc.freeUserFromTrip(tp.TripID.ToString(), tp.UserID.ToString());

        //                    var task = Task.Run(async () =>
        //                    {
        //                        await fc.sentSingleFCM(tp.DeviceToken, dic, "pas_rideCancel");
        //                        await fc.delTripNode(tp.TripID.ToString());
        //                    });
        //                }
        //                else
        //                {
        //                    //Reroute Request
        //                    uc.rideRequest(GetCancelledTripRequestObject(model, tp), Request ?? request);
        //                }

        //                response.error = false;
        //                response.data = dic;
        //                response.message = ResponseKeys.msgSuccess;

        //                //If later booking was over due and cancelled by cron-job (if driver app was not active)
        //                if (Request == null)
        //                    return request.CreateResponse(HttpStatusCode.OK, response);
        //                else
        //                    return Request.CreateResponse(HttpStatusCode.OK, response);
        //            }
        //        }
        //        else
        //        {
        //            response.error = true;
        //            response.message = ResponseKeys.invalidParameters;

        //            if (Request == null)
        //                return request.CreateResponse(HttpStatusCode.OK, response);
        //            else
        //                return Request.CreateResponse(HttpStatusCode.OK, response);
        //        }
        //    //}
        //    //catch (Exception ex)
        //    //{
        //    //    response.error = true;
        //    //    Logger.WriteLog(ex);
        //    //    Logger.WriteLog(model);
        //    //    response.message = ResponseKeys.serverError;

        //    //    if (Request == null)
        //    //        return request.CreateResponse(HttpStatusCode.OK, response);
        //    //    else
        //    //        return Request.CreateResponse(HttpStatusCode.OK, response);
        //    //}
        //}

        //[HttpPost]
        //public HttpResponseMessage driverArrived([FromBody] RequestModel model)
        //{

        //    if (model != null && !string.IsNullOrEmpty(model.tripID) && !string.IsNullOrEmpty(model.driverID))
        //    {
        //        using (CangooEntities context = new CangooEntities())
        //        {

        //            FireBaseController fb = new FireBaseController();
        //            var estDist = fb.getTripEstimatedDistanceOnArrival(model.driverID);

        //            //Update earned points in case of normal booking only.
        //            var trip = context.spGetUpdateTripOnCaptainArrived(Common.getUtcDateTime(),
        //                ((double.Parse(model.distanceToPickUpLocation) <= estDist ? double.Parse(model.distanceToPickUpLocation) : estDist) / 100),
        //                model.tripID,
        //                model.driverID,
        //                model.isWeb).FirstOrDefault();
        //            if (trip != null)
        //            {
        //                ArrivedDriverRideModel adr = new ArrivedDriverRideModel()
        //                {
        //                    passengerName = trip.Name,
        //                    passengerRating = trip.Rating,
        //                    dropOffLatitude = trip.DropOffLocationLatitude,
        //                    dropOffLongitude = trip.DropOffLocationLongitude,
        //                    passenger_Pic = trip.pic,
        //                    //bookingMode = trip.BookingModeID == (int)TripBookingMod.Karhoo ? Enum.GetName(typeof(TripBookingMod), (int)App_Start.TripBookingMod.Karhoo).ToLower() : ""
        //                    bookingMode = Enum.GetName(typeof(TripBookingMod), (int)trip.BookingModeID).ToLower()
        //                };

        //                FireBaseController fc = new FireBaseController();
        //                var task = Task.Run(async () =>
        //                {
        //                    await fc.sendFCMAfterDriverArrived(model.driverID, trip.DeviceToken, adr, model.tripID, model.isWeb);
        //                });
        //                fc.updateDriverEarnedPoints(model.driverID, trip.EarningPoints.ToString());
        //                fc.updateArrivalTime(model.tripID, model.driverID, trip.EarningPoints.ToString());

        //                List<CanclReasonsModel> lstcr = Common.GetCancelReasons(context, true, false, true);

        //                dic = new Dictionary<dynamic, dynamic>
        //                    {
        //                        { "Name", trip.Name },
        //                        { "Rating", trip.Rating },
        //                        { "dropOffLat", trip.DropOffLocationLatitude != null ? trip.DropOffLocationLatitude : "" },
        //                        { "dropOffLon", trip.DropOffLocationLongitude != null ? trip.DropOffLocationLongitude : "" },
        //                        { "passenger_Pic", trip.pic },
        //                        { "cancel_reasons", lstcr},
        //                        { "bookingMode", adr.bookingMode}
        //                    };

        //                response.error = false;
        //                response.message = ResponseKeys.msgSuccess;
        //                response.data = dic;
        //                return Request.CreateResponse(HttpStatusCode.OK, response);
        //            }
        //            else
        //            {
        //                response.error = true;
        //                response.message = ResponseKeys.tripNotFound;
        //                return Request.CreateResponse(HttpStatusCode.OK, response);
        //            }
        //        }
        //    }
        //    else
        //    {
        //        response.error = true;
        //        response.message = ResponseKeys.invalidParameters;
        //        return Request.CreateResponse(HttpStatusCode.OK, response);
        //    }
        //}

        //[HttpPost]
        //public HttpResponseMessage sendChangeFareRequest([FromBody] RequestModel model)
        //{
        //    if (model != null && !string.IsNullOrEmpty(model.tripID) && !string.IsNullOrEmpty(model.driverID))
        //    {
        //        using (CangooEntities context = new CangooEntities())
        //        {
        //            var trip = (from t in context.Trips
        //                        join u in context.UserProfiles on t.UserID.ToString() equals u.UserID   //in case of hotel booking will be null
        //                        join c in context.Captains on t.CaptainID equals c.CaptainID
        //                        where t.TripID.ToString().Equals(model.tripID) && t.CaptainID.ToString().Equals(model.driverID)
        //                        select new
        //                        {
        //                            TripID = t.TripID.ToString(),
        //                            DeviceToken = u.DeviceToken,
        //                            PassengerName = u.FirstName + " " + u.LastName,
        //                            CaptainName = c.Name
        //                        }).FirstOrDefault();

        //            if (trip != null)
        //            {
        //                dic = new Dictionary<dynamic, dynamic> {
        //                        {"tripID", trip.TripID },
        //                        {"passengerName", trip.PassengerName},
        //                        {"captainName", trip.CaptainName}
        //                    };

        //                FireBaseController fc = new FireBaseController();

        //                var task = Task.Run(async () =>
        //                {
        //                    await fc.sentSingleFCM(trip.DeviceToken, dic, "pas_FareChangeRequested");
        //                });

        //                response.error = false;
        //                response.data = dic;
        //                response.message = ResponseKeys.msgSuccess;
        //                return Request.CreateResponse(HttpStatusCode.OK, response);
        //            }
        //            else
        //            {
        //                response.error = true;
        //                response.message = ResponseKeys.tripNotFound;
        //                return Request.CreateResponse(HttpStatusCode.OK, response);
        //            }
        //        }
        //    }
        //    else
        //    {
        //        response.error = true;
        //        response.message = ResponseKeys.invalidParameters;
        //        return Request.CreateResponse(HttpStatusCode.OK, response);
        //    }
        //}

        //[HttpPost]
        //public HttpResponseMessage startTrip([FromBody] RequestModel model)
        //{
        //    if (model != null && !string.IsNullOrEmpty(model.tripID) && model.driverID != string.Empty)
        //    {

        //        using (CangooEntities context = new CangooEntities())
        //        {
        //            var trip = context.spGetUserUpdateTripOnStartRide(Common.getUtcDateTime(), model.tripID, model.driverID, model.isWeb).FirstOrDefault(); //datetime utc
        //            if (trip != null)
        //            {
        //                startDriverRideModel sdr = new startDriverRideModel()
        //                {
        //                    dropOffLatitude = trip.DropOffLocationLatitude,
        //                    dropOffLongitude = trip.DropOffLocationLongitude,
        //                    //bookingMode = trip.BookingModeID == (int)TripBookingMod.Karhoo ? Enum.GetName(typeof(TripBookingMod), (int)App_Start.TripBookingMod.Karhoo).ToLower() : ""
        //                    bookingMode = Enum.GetName(typeof(TripBookingMod), (int)trip.BookingModeID).ToLower()
        //                };
        //                dic = new Dictionary<dynamic, dynamic>
        //                        {
        //                            { "destination_lat", trip.DropOffLocationLatitude },
        //                            { "destination_lon", trip.DropOffLocationLongitude },
        //                            { "passengerName",  trip.Name},
        //                            { "bookingMode", sdr.bookingMode}
        //                        };

        //                var task = Task.Run(async () =>
        //                {
        //                    FireBaseController fc = new FireBaseController();
        //                    await fc.sendFCMAfterRideStarted(model.tripID, model.driverID, trip.DeviceToken, sdr, model.isWeb);
        //                    await fc.updateGo4Module(trip.CaptainName, trip.Name, trip.Go4ModuleDeviceToken);

        //                });

        //                response.error = false;
        //                response.data = dic;
        //                response.message = ResponseKeys.msgSuccess;
        //                return Request.CreateResponse(HttpStatusCode.OK, response);
        //            }
        //            else
        //            {
        //                response.error = true;
        //                response.message = ResponseKeys.tripNotFound;
        //                return Request.CreateResponse(HttpStatusCode.OK, response);
        //            }
        //        }
        //    }
        //    else
        //    {
        //        response.error = true;
        //        response.message = ResponseKeys.invalidParameters;
        //        return Request.CreateResponse(HttpStatusCode.OK, response);
        //    }
        //}

        //[HttpPost]
        //public HttpResponseMessage endTrip([FromBody] RequestModel model)
        //{
        //    if (model != null && model.tripID != string.Empty && model.driverID != string.Empty && model.resellerID != string.Empty &&
        //        model.resellerArea != string.Empty && !string.IsNullOrEmpty(model.dropOffLocation) && !string.IsNullOrEmpty(model.distance) &&
        //        !string.IsNullOrEmpty(model.isAtDropOffLocation))
        //    {
        //        using (CangooEntities context = new CangooEntities())
        //        {
        //            var trip = context.Trips.Where(t => t.TripID.ToString() == model.tripID && t.CaptainID.ToString() == model.driverID).FirstOrDefault();
        //            if (trip != null)
        //            {
        //                decimal totalFare = 0;
        //                FareManager fareManag = new FareManager();
        //                FareManager dropOffAreafareManag = new FareManager();

        //                dic = new Dictionary<dynamic, dynamic>() {
        //                        { "discountType", "normal"},
        //                        { "discountAmount", "0.00" }
        //                    };

        //                //get waiting time
        //                DateTime arrivalTime = model.isLaterBooking ? Convert.ToDateTime(trip.PickUpBookingDateTime) : Convert.ToDateTime(trip.ArrivalDateTime);
        //                DateTime startTime = Convert.ToDateTime(trip.TripStartDatetime);
        //                TimeSpan waitingDur = startTime.Subtract(arrivalTime);

        //                //in case of later booking if driver arrived and started ride before scheduled pickup time then no waiting charges
        //                if (waitingDur.Seconds <= 0)
        //                {
        //                    waitingDur = new TimeSpan();
        //                }

        //                //save trip detail
        //                trip.DropOffLocation = model.dropOffLocation;
        //                trip.TripEndDatetime = Common.getUtcDateTime();//utc date time
        //                trip.TripStatusID = (int)App_Start.TripStatus.PaymentPending;
        //                trip.DropOffLocationLatitude = Convert.ToDouble(trip.DropOffLocationLatitude) == 0 ? model.lat.ToString() : trip.DropOffLocationLatitude;
        //                trip.DropOffLocationLongitude = Convert.ToDouble(trip.DropOffLocationLongitude) == 0 ? model.lon.ToString() : trip.DropOffLocationLongitude;
        //                trip.WaitingMinutes = waitingDur.TotalMinutes;
        //                //trip.DistanceTraveled = double.Parse(model.distance);

        //                CheckWalletBalance(trip.UserID.ToString(), context, ref dic);

        //                string promotionId = Common.IsSpecialPromotionApplicable(trip.PickupLocationLatitude, trip.PickupLocationLongitude, model.lat.ToString(), model.lon.ToString(), ApplicationID, context, ref dic);

        //                if (!string.IsNullOrEmpty(promotionId))
        //                {
        //                    //Special  promotion ID
        //                    trip.PromoCodeID = Guid.Parse(promotionId);
        //                    dic.Add("estimatedPrice", dic["discountAmount"]);

        //                    //trip.BaseFare = 0;
        //                    //trip.BookingFare = 0;
        //                    //trip.WaitingFare = 0;
        //                    //trip.PerKMFare = 0;

        //                    /*
        //                     * trip isOverridedFare scenario is deprecated, fare is not saved on                                
        //                     * api/driver/CollectCashPayment                                
        //                     * api/user/PayPalPayment                                
        //                     * api/user/creditCardPayment
        //                     */

        //                    trip.PerKMFare = decimal.Parse(dic["discountAmount"].ToString());
        //                    //No need to use totalFare, function is called just to calculate other required fields i.e. PolyLine, Distance, Time etc
        //                    totalFare = Common.CalculateEstimatedFare((int)trip.NoOfPerson, false, true, "", bool.Parse(model.isAtDropOffLocation), true, trip.TripID, ApplicationID, trip.PickupLocationLatitude, trip.PickupLocationLongitude, model.lat.ToString(), model.lon.ToString(), ref fareManag, ref dropOffAreafareManag, ref dic);//(Convert.ToDecimal(model.distance) / 1000).ToString(), waitingDur
        //                }
        //                else
        //                {
        //                    totalFare = Common.CalculateEstimatedFare((int)trip.NoOfPerson, false, true, "", bool.Parse(model.isAtDropOffLocation), (bool)trip.isFareChangePermissionGranted, trip.TripID, ApplicationID, trip.PickupLocationLatitude, trip.PickupLocationLongitude, model.lat.ToString(), model.lon.ToString(), ref fareManag, ref dropOffAreafareManag, ref dic);//(Convert.ToDecimal(model.distance) / 1000).ToString(), waitingDur

        //                    dic.Add("estimatedPrice", string.Format("{0:0.00}", totalFare));

        //                    promotionId = Common.ApplyPromoCode(ApplicationID, trip.UserID.ToString(), context, ref dic);

        //                    if (!string.IsNullOrEmpty(promotionId))
        //                    {
        //                        trip.PromoCodeID = Guid.Parse(promotionId);
        //                    }

        //                    //trip.BaseFare = fareManag.BaseFare;
        //                    //trip.BookingFare = fareManag.BookingFare;
        //                    //trip.WaitingFare = fareManag.WaitingFare * Convert.ToDecimal(waitingDur.TotalMinutes);
        //                    //trip.PerKMFare = totalFare - fareManag.BookingFare - (fareManag.WaitingFare * Convert.ToDecimal(waitingDur.TotalMinutes));

        //                    trip.BaseFare = decimal.Parse(dic["inBoundBaseFare"].ToString()) + decimal.Parse(dic["outBoundBaseFare"].ToString());
        //                    trip.BookingFare = decimal.Parse(dic["inBoundSurchargeAmount"].ToString()) + decimal.Parse(dic["outBoundSurchargeAmount"].ToString());
        //                    trip.WaitingFare = decimal.Parse(dic["inBoundTimeFare"].ToString()) + decimal.Parse(dic["outBoundTimeFare"].ToString());
        //                    trip.PerKMFare = decimal.Parse(dic["inBoundDistanceFare"].ToString()) + decimal.Parse(dic["outBoundDistanceFare"].ToString());
        //                }

        //                trip.FareManagerID = string.IsNullOrEmpty(trip.FareManagerID) ? fareManag.FareManagerID.ToString() : trip.FareManagerID;
        //                trip.DropOffFareMangerID = string.IsNullOrEmpty(trip.DropOffFareMangerID.ToString()) ? dropOffAreafareManag.FareManagerID : trip.DropOffFareMangerID;

        //                trip.InBoundDistanceInMeters = (int)(double.Parse(dic["inBoundDistanceInKM"].ToString()) * 1000);
        //                trip.InBoundTimeInSeconds = (int)(double.Parse(dic["inBoundTimeInMinutes"].ToString()) * 60);
        //                trip.OutBoundDistanceInMeters = (int)(double.Parse(dic["outBoundDistanceInKM"].ToString()) * 1000);
        //                trip.OutBoundTimeInSeconds = (int)(double.Parse(dic["outBoundTimeInMinutes"].ToString()) * 60);

        //                trip.InBoundBaseFare = decimal.Parse(dic["inBoundBaseFare"].ToString());
        //                trip.InBoundDistanceFare = decimal.Parse(dic["inBoundDistanceFare"].ToString());
        //                trip.InBoundTimeFare = decimal.Parse(dic["inBoundTimeFare"].ToString());
        //                trip.InBoundSurchargeAmount = decimal.Parse(dic["inBoundSurchargeAmount"].ToString());
        //                trip.OutBoundBaseFare = decimal.Parse(dic["outBoundBaseFare"].ToString());
        //                trip.OutBoundDistanceFare = decimal.Parse(dic["outBoundDistanceFare"].ToString());
        //                trip.OutBoundTimeFare = decimal.Parse(dic["outBoundTimeFare"].ToString());
        //                trip.OutBoundSurchargeAmount = decimal.Parse(dic["outBoundSurchargeAmount"].ToString());

        //                trip.DistanceTraveled = trip.InBoundDistanceInMeters + trip.OutBoundDistanceInMeters;
        //                trip.PolyLine = dic["polyLine"].ToString();
        //                model.distance = trip.DistanceTraveled.ToString();

        //                //Update earned points, priority hour and booking type check applied

        //                var captain = context.Captains.Where(c => c.CaptainID == trip.CaptainID).FirstOrDefault();

        //                if ((bool)trip.isLaterBooking)
        //                {
        //                    if (captain.IsPriorityHoursActive != null)
        //                    {
        //                        if (!(bool)captain.IsPriorityHoursActive)
        //                        {
        //                            trip.DriverEarnedPoints = 50;
        //                            captain.EarningPoints = captain.EarningPoints == null ? 50 :
        //                                captain.EarningPoints + 50 <= 300 ? captain.EarningPoints + 50 : 300;
        //                        }
        //                    }
        //                    else
        //                    {
        //                        trip.DriverEarnedPoints = 50;
        //                        captain.EarningPoints = captain.EarningPoints == null ? 50 :
        //                            captain.EarningPoints + 50 <= 300 ? captain.EarningPoints + 50 : 300;
        //                    }
        //                }

        //                context.SaveChanges();

        //                FireBaseController fc = new FireBaseController();
        //                fc.updateDriverEarnedPoints(captain.CaptainID.ToString(), captain.EarningPoints.ToString());

        //                DateTime startRideTime = Convert.ToDateTime(trip.TripStartDatetime);
        //                DateTime endRideTime = Convert.ToDateTime(Common.getUtcDateTime());//utc date time
        //                TimeSpan totalRideDuration = endRideTime.Subtract(startRideTime);

        //                var userFav = context.UserFavoriteCaptains.Where(u => u.UserID == trip.UserID.ToString() && u.CaptainID == trip.CaptainID).FirstOrDefault();

        //                //Object to update data in trip's driver node
        //                EndDriverRideModel edr = new EndDriverRideModel
        //                {
        //                    //What if totalFare was less than base fare?
        //                    //travelCharges = string.Format("{0:0.00}", dic["discountType"].ToString().Equals("special") ? "0.00" :
        //                    //(totalFare - fareManag.BookingFare - (fareManag.WaitingFare * Convert.ToDecimal(waitingDur.TotalMinutes))).ToString()),

        //                    //waitingCharges = string.Format("{0:0.00}", dic["discountType"].ToString().Equals("special") ? "0.00" :
        //                    //(fareManag.WaitingFare * Convert.ToDecimal(waitingDur.TotalMinutes)).ToString()),

        //                    //bookingCharges = string.Format("{0:0.00}", dic["discountType"].ToString().Equals("special") ? "0.00" :
        //                    //fareManag.BookingFare.ToString()),

        //                    //baseCharges = string.Format("{0:0.00}", fareManag.BaseFare != null ? fareManag.BaseFare : 0),

        //                    travelCharges = string.Format("{0:0.00}", dic["discountType"].ToString().Equals("special") ? "0.00" : trip.PerKMFare.ToString()),
        //                    waitingCharges = string.Format("{0:0.00}", dic["discountType"].ToString().Equals("special") ? "0.00" : trip.WaitingFare.ToString()),
        //                    bookingCharges = string.Format("{0:0.00}", "0.00"),
        //                    baseCharges = string.Format("{0:0.00}", trip.BaseFare.ToString()),
        //                    estimatedPrice = string.Format("{0:0.00}", dic["estimatedPrice"]),
        //                    paymentMethod = trip.TripPaymentMode,
        //                    distance = string.Format("{0:0.00}", trip.DistanceTraveled.ToString()),
        //                    duration = totalRideDuration.TotalMinutes,
        //                    isPaymentRequested = false,
        //                    isFavUser = userFav == null ? false : (userFav.IsFavByCaptain == null ? false : (bool)userFav.IsFavByCaptain),
        //                    discountAmount = dic["discountAmount"],
        //                    discountType = dic["discountType"],
        //                    availableWalletBalance = dic["availableWalletBalance"],
        //                    isWalletPreferred = dic["isWalletPreferred"].ToString(),
        //                    isFareChangePermissionGranted = (bool)trip.isFareChangePermissionGranted,
        //                    InBoundDistanceInMeters = string.Format("{0:0.00}", dic["discountType"].ToString().Equals("special") ? "0.00" : trip.InBoundDistanceInMeters.ToString()),
        //                    InBoundTimeInSeconds = string.Format("{0:0.00}", dic["discountType"].ToString().Equals("special") ? "0.00" : trip.InBoundTimeInSeconds.ToString()),
        //                    OutBoundDistanceInMeters = string.Format("{0:0.00}", dic["discountType"].ToString().Equals("special") ? "0.00" : trip.OutBoundDistanceInMeters.ToString()),
        //                    OutBoundTimeInSeconds = string.Format("{0:0.00}", dic["discountType"].ToString().Equals("special") ? "0.00" : trip.OutBoundTimeInSeconds.ToString()),
        //                    InBoundDistanceFare = string.Format("{0:0.00}", dic["discountType"].ToString().Equals("special") ? "0.00" : trip.InBoundDistanceFare.ToString()),
        //                    InBoundTimeFare = string.Format("{0:0.00}", dic["discountType"].ToString().Equals("special") ? "0.00" : trip.InBoundTimeFare.ToString()),
        //                    InBoundSurchargeAmount = string.Format("{0:0.00}", dic["discountType"].ToString().Equals("special") ? "0.00" : trip.InBoundSurchargeAmount.ToString()),
        //                    OutBoundDistanceFare = string.Format("{0:0.00}", dic["discountType"].ToString().Equals("special") ? "0.00" : trip.OutBoundDistanceFare.ToString()),
        //                    OutBoundTimeFare = string.Format("{0:0.00}", dic["discountType"].ToString().Equals("special") ? "0.00" : trip.OutBoundTimeFare.ToString()),
        //                    OutBoundSurchargeAmount = string.Format("{0:0.00}", dic["discountType"].ToString().Equals("special") ? "0.00" : trip.OutBoundSurchargeAmount.ToString()),
        //                    //bookingMode = trip.BookingModeID == (int)TripBookingMod.Karhoo ? Enum.GetName(typeof(TripBookingMod), (int)TripBookingMod.Karhoo).ToLower() : ""
        //                    bookingMode = Enum.GetName(typeof(TripBookingMod), (int)trip.BookingModeID).ToLower()
        //                };

        //                var voucher = context.CompanyVouchers.Where(v => v.VoucherID == trip.VoucherID &&
        //                v.ApplicationID.ToString().ToLower().Equals(ApplicationID.ToLower())).FirstOrDefault();
        //                if (voucher != null)
        //                {
        //                    edr.isVoucherApplied = "true";
        //                    edr.voucherAmount = voucher.Amount.ToString();
        //                    edr.voucherCode = voucher.VoucherCode;
        //                }
        //                else
        //                {
        //                    edr.isVoucherApplied = "false";
        //                    edr.voucherAmount = "0.00";
        //                    edr.voucherCode = "";
        //                }

        //                var pas = (from up in context.UserProfiles
        //                           join anu in context.AspNetUsers on up.UserID equals anu.Id
        //                           where anu.Id.Equals(trip.UserID.ToString())
        //                           select new
        //                           {
        //                               up.FirstName,
        //                               up.LastName,
        //                               anu.Email
        //                           }).FirstOrDefault();

        //                if (pas != null)
        //                {
        //                    dic.Add("passengerName", pas.FirstName + " " + pas.LastName);
        //                    edr.isUserProfileUpdated = !string.IsNullOrEmpty(pas.Email);
        //                }
        //                else
        //                {
        //                    dic.Add("passengerName", "");
        //                    edr.isUserProfileUpdated = false;
        //                }

        //                dic.Add("travelCharges", edr.travelCharges);
        //                dic.Add("waitingCharges", edr.waitingCharges);
        //                dic.Add("bookingCharges", edr.bookingCharges);
        //                dic.Add("baseCharges", edr.baseCharges);

        //                dic.Add("paymentMethod", edr.paymentMethod);
        //                dic.Add("distance", edr.distance);

        //                dic.Add("duration", edr.duration);
        //                dic.Add("isFavUser", edr.isFavUser);

        //                dic.Add("isVoucherApplied", edr.isVoucherApplied);
        //                dic.Add("voucherAmount", edr.voucherAmount);
        //                dic.Add("voucherCode", edr.voucherCode);

        //                dic.Add("isUserProfileUpdated", edr.isUserProfileUpdated);
        //                dic.Add("isFareChangePermissionGranted", edr.isFareChangePermissionGranted);
        //                dic.Add("bookingMode", edr.bookingMode);
        //                dic.Add("isWeb", model.isWeb);

        //                var task = Task.Run(async () =>
        //                {
        //                    await fc.sendFCMRideDetailPassengerAfterEndRide(model.tripID, model.distance, edr, model.driverID,
        //                        edr.paymentMethod, trip.UserID.ToString(), model.isWeb);
        //                });

        //                if (trip.BookingModeID == (int)TripBookingMod.Karhoo ||
        //                    (trip.BookingModeID == (int)TripBookingMod.Dispatcher && trip.TripPaymentMode.ToLower().Equals("wallet")))
        //                {

        //                    //Quik fix to avoid application side changes.
        //                    dic["bookingMode"] = "karhoo";

        //                    trip.isOverRided = false;
        //                    trip.TripStatusID = (int)TripStatus.Completed;
        //                    trip.CompanyID = captain.CompanyID;
        //                    trip.TripPaymentMode = "Wallet";

        //                    Transaction tr = new Transaction()
        //                    {
        //                        TransactionID = Guid.NewGuid(),
        //                        DebitedFrom = Guid.Parse(trip.UserID.ToString()),
        //                        CreditedTo = Guid.Parse(ApplicationID),
        //                        DateTime = Common.getUtcDateTime(),
        //                        Amount = Convert.ToDecimal(edr.estimatedPrice), //Adjusted wallet amount and voucher amount is considered as mobile payment - RECEIVABLE
        //                        PaymentModeID = (int)App_Start.PaymentMode.Wallet,
        //                        Reference = trip.BookingModeID == (int)TripBookingMod.Karhoo ? "Karhoo trip completed." : "Dispatcher mobile payment trip completed."
        //                    };
        //                    context.Transactions.Add(tr);
        //                    context.SaveChanges();

        //                    task = Task.Run(async () =>
        //                    {
        //                        fc.updateDriverStatus(model.driverID, "false", model.tripID);
        //                        fc.freeUserFromTrip(model.tripID, trip.UserID.ToString());

        //                        await fc.delTripNode(model.tripID);
        //                    });
        //                }

        //                response.error = false;
        //                response.message = ResponseKeys.msgSuccess;
        //                response.data = dic;

        //                return Request.CreateResponse(HttpStatusCode.OK, response);
        //            }
        //            else
        //            {
        //                response.error = true;
        //                response.message = ResponseKeys.tripNotFound;
        //                return Request.CreateResponse(HttpStatusCode.OK, response);
        //            }
        //        }
        //    }
        //    else
        //    {
        //        response.error = true;
        //        response.message = ResponseKeys.invalidParameters;
        //        return Request.CreateResponse(HttpStatusCode.OK, response);
        //    }
        //}

        //[HttpPost]
        //public HttpResponseMessage collectPayment([FromBody] RequestModel model)
        //{
        //    /*
        //     NEED TO UPDATE FormWebRequest method as well
        //     */

        //    if (model != null && !string.IsNullOrEmpty(model.tripID) && !string.IsNullOrEmpty(model.driverID) &&
        //        !string.IsNullOrEmpty(model.fleetID) && !string.IsNullOrEmpty(model.isOverride) &&
        //        !string.IsNullOrEmpty(model.totalFare) && !string.IsNullOrEmpty(model.tipAmount) &&
        //        !string.IsNullOrEmpty(model.walletUsedAmount) && !string.IsNullOrEmpty(model.voucherUsedAmount) &&
        //        !string.IsNullOrEmpty(model.promoDiscountAmount) && !string.IsNullOrEmpty(model.collectedAmount))
        //    {
        //        using (CangooEntities context = new CangooEntities())
        //        {
        //            dic = new Dictionary<dynamic, dynamic>();
        //            if (CheckIfAlreadyPaid(model.totalFare, model.tripID, model.driverID, ref dic, false))
        //            {
        //                //TBD: send fcm to user - if required.
        //                response.data = dic;
        //                response.error = true;
        //                response.message = ResponseKeys.fareAlreadyPaid;
        //                return Request.CreateResponse(HttpStatusCode.OK, response);
        //            }

        //            string passengerDeviceToken = "";
        //            var trip = context.Trips.Where(t => t.TripID.ToString().Equals(model.tripID)).FirstOrDefault();

        //            //In case of partial payment need to send invoice
        //            if ((decimal.Parse(model.walletUsedAmount) > 0.0M) || (decimal.Parse(model.promoDiscountAmount) > 0.0M))
        //            {
        //                var result = context.spAfterMobilePayment(false,//Convert.ToBoolean(model.isOverride), 
        //                    model.tripID,
        //                    "",
        //                    (int)App_Start.TripStatus.Completed,
        //                    trip.UserID.ToString(),
        //                    ApplicationID,
        //                    (decimal.Parse(model.totalFare) - decimal.Parse(model.tipAmount)).ToString(),
        //                    "0.00",
        //                    model.promoDiscountAmount,
        //                    model.walletUsedAmount,
        //                    model.tipAmount.ToString(),
        //                    Common.getUtcDateTime(),
        //                    (int)App_Start.PaymentMode.Cash,
        //                    (int)App_Start.ResellerPaymentStatus.Paid,
        //                    model.fleetID).FirstOrDefault();

        //                SendInvoice(new InvoiceModel
        //                {
        //                    CustomerEmail = result.CustomerEmail,// context.AspNetUsers.Where(u => u.Id.Equals(model.passengerID)).FirstOrDefault().Email,
        //                    TotalAmount = (decimal.Parse(model.collectedAmount) + decimal.Parse(model.walletUsedAmount) + decimal.Parse(model.promoDiscountAmount)).ToString(),  // model.totalFare,
        //                    CashAmount = model.collectedAmount,
        //                    WalletUsedAmount = model.walletUsedAmount,
        //                    PromoDiscountAmount = model.promoDiscountAmount,
        //                    CaptainName = result.CaptainName,
        //                    CustomerName = result.CustomerName,
        //                    TripDate = result.TripDate,
        //                    InvoiceNumber = result.InvoiceNumber,
        //                    FleetName = result.FleetName,
        //                    ATUNumber = result.FleetATUNumber,
        //                    Street = result.FleetAddress,
        //                    BuildingNumber = result.FleetBuildingNumber,
        //                    PostCode = result.FleetPostalCode,
        //                    City = result.FleetCity,
        //                    PickUpAddress = result.PickUpLocation,
        //                    DropOffAddress = result.DropOffLocation,
        //                    CaptainUserName = result.CaptainUserName,
        //                    Distance = result.DistanceInKM.ToString("0.00"),
        //                    VehicleNumber = result.PlateNumber,
        //                    FleetEmail = result.FleetEmail
        //                });

        //                passengerDeviceToken = result.PassengerDeviceToken;
        //            }
        //            else
        //            {
        //                trip.CompanyID = Guid.Parse(model.fleetID);

        //                //Fare details are calculated and saved on endTrip requests.

        //                trip.isOverRided = false;
        //                trip.TripPaymentMode = "Cash";
        //                trip.Tip = Convert.ToDecimal(model.tipAmount);

        //                model.userID = trip.UserID.ToString();

        //                ApplyPromoAdjustWalletUpdateVoucherAmount(model.voucherUsedAmount, model.walletUsedAmount, model.promoDiscountAmount, trip, context);

        //                Transaction tr = new Transaction()
        //                {
        //                    TransactionID = Guid.NewGuid(),
        //                    DebitedFrom = Guid.Parse(trip.UserID.ToString()),
        //                    CreditedTo = Guid.Parse(ApplicationID),
        //                    DateTime = Common.getUtcDateTime(),
        //                    Amount = Convert.ToDecimal(model.collectedAmount), //Adjusted wallet amount and voucher amount is considered as mobile payment - RECEIVABLE
        //                    PaymentModeID = (int)App_Start.PaymentMode.Cash,
        //                    Reference = "Trip cash payment received."
        //                };
        //                context.Transactions.Add(tr);
        //                context.SaveChanges();

        //                var result = context.spGetTripPassengerTokenByTripIDOnCollectPayment(model.tripID, (int)App_Start.TripStatus.Completed).FirstOrDefault();

        //                passengerDeviceToken = result.DeviceToken;
        //            }

        //            var paymentDetails = new cashPayment()
        //            {
        //                collectedAmount = model.collectedAmount,
        //                promoDiscountAmount = model.promoDiscountAmount,
        //                //voucherUsedAmount = model.voucherUsedAmount,  //In case of vouchered ride, user don't have passenger application
        //                walletAmountUsed = model.walletUsedAmount,
        //                totalFare = (decimal.Parse(model.collectedAmount) + decimal.Parse(model.walletUsedAmount) + decimal.Parse(model.promoDiscountAmount)).ToString()
        //            };


        //            FireBaseController fc = new FireBaseController();
        //            var task = Task.Run(async () =>
        //            {
        //                fc.updateDriverStatus(model.driverID, "false", model.tripID);
        //                fc.freeUserFromTrip(model.tripID, trip.UserID.ToString());

        //                await fc.delTripNode(model.tripID);

        //                    //In case of Request from Business it'll be empty. isWeb check can be applied here.
        //                    if (!string.IsNullOrEmpty(passengerDeviceToken))
        //                    await fc.sentSingleFCM(passengerDeviceToken, paymentDetails, "pas_CashPaymentPaid");
        //            });


        //            response.error = false;
        //            response.message = ResponseKeys.msgSuccess;
        //            return Request.CreateResponse(HttpStatusCode.OK, response);
        //        }
        //    }
        //    else
        //    {
        //        response.error = true;
        //        response.message = ResponseKeys.invalidParameters;
        //        return Request.CreateResponse(HttpStatusCode.OK, response);
        //    }
        //}

        //[HttpPost]
        //public HttpResponseMessage collectPaypalPayment([FromBody] RequestModel model)
        //{
        //    /*
        //     req.estimatedFare = discountType,
        //     req.promoDiscountAmount = discountAmount,
        //     req.walletUsedAmount = isWalletPreferred
        //     */
        //    if (model != null && !string.IsNullOrEmpty(model.isOverride) && model.driverID != string.Empty && model.estimatedFare != string.Empty &&
        //        model.duration != string.Empty && model.distance != string.Empty && model.tripID != string.Empty && model.fleetID != string.Empty &&
        //        model.paymentMode != string.Empty && model.vehicleID != string.Empty && !string.IsNullOrEmpty(model.walletUsedAmount) &&
        //        !string.IsNullOrEmpty(model.walletTotalAmount) && !string.IsNullOrEmpty(model.voucherUsedAmount) &&
        //        !string.IsNullOrEmpty(model.promoDiscountAmount) && !string.IsNullOrEmpty(model.totalFare))
        //    {
        //        dic = new Dictionary<dynamic, dynamic>();

        //        if (CheckIfAlreadyPaid(model.totalFare, model.tripID, model.driverID, ref dic, false))
        //        {
        //            response.data = dic;
        //            response.error = true;
        //            response.message = ResponseKeys.fareAlreadyPaid;
        //            return Request.CreateResponse(HttpStatusCode.OK, response);
        //        }
        //        else
        //        {
        //            using (var context = new CangooEntities())
        //            {
        //                var trip = context.Trips.Where(t => t.TripID.ToString().Equals(model.tripID)).FirstOrDefault();
        //                trip.TripStatusID = (int)TripStatus.PaymentRequested;
        //                context.SaveChanges();
        //            }

        //            var task = Task.Run(async () =>
        //            {
        //                FireBaseController fc = new FireBaseController();
        //                await fc.sendFCMForPaypalPaymentToPassenger(model.fleetID, model.isOverride, model.vehicleID, model.estimatedFare, model.walletUsedAmount, model.walletTotalAmount, model.voucherUsedAmount, model.promoDiscountAmount, model.totalFare, model.duration, model.distance, model.paymentMode, model.tripID, model.driverID);
        //            });

        //            response.error = false;
        //            response.message = ResponseKeys.msgSuccess;
        //            return Request.CreateResponse(HttpStatusCode.OK, response);
        //        }
        //    }
        //    else
        //    {
        //        response.error = true;
        //        response.message = ResponseKeys.invalidParameters;
        //        return Request.CreateResponse(HttpStatusCode.OK, response);
        //    }
        //}

        //[HttpPost]
        //public HttpResponseMessage collectCreditCardPayment([FromBody] RequestModel model)
        //{
        //    /*
        //     req.estimatedFare = discountType,
        //     req.promoDiscountAmount = discountAmount,
        //     req.walletUsedAmount = isWalletPreferred
        //     */
        //    if (model != null && !string.IsNullOrEmpty(model.isOverride) && model.driverID != string.Empty && model.estimatedFare != string.Empty &&
        //    model.duration != string.Empty && model.distance != string.Empty && model.tripID != string.Empty && model.fleetID != string.Empty &&
        //    model.paymentMode != string.Empty && model.vehicleID != string.Empty && !string.IsNullOrEmpty(model.walletUsedAmount) &&
        //    !string.IsNullOrEmpty(model.walletTotalAmount) && !string.IsNullOrEmpty(model.voucherUsedAmount) &&
        //    !string.IsNullOrEmpty(model.promoDiscountAmount) && !string.IsNullOrEmpty(model.totalFare))
        //    {
        //        dic = new Dictionary<dynamic, dynamic>();

        //        if (CheckIfAlreadyPaid(model.totalFare, model.tripID, model.driverID, ref dic, false))
        //        {
        //            response.data = dic;
        //            response.error = true;
        //            response.message = ResponseKeys.fareAlreadyPaid;
        //            return Request.CreateResponse(HttpStatusCode.OK, response);
        //        }
        //        else
        //        {
        //            using (var context = new CangooEntities())
        //            {
        //                var trip = context.Trips.Where(t => t.TripID.ToString().Equals(model.tripID)).FirstOrDefault();
        //                trip.TripStatusID = (int)TripStatus.PaymentRequested;
        //                context.SaveChanges();
        //            }

        //            var task = Task.Run(async () =>
        //            {
        //                FireBaseController fc = new FireBaseController();
        //                await fc.sendFCMForCreditCardPaymentToPassenger(model.fleetID, model.isOverride, model.vehicleID, model.estimatedFare, model.walletUsedAmount, model.walletTotalAmount, model.voucherUsedAmount, model.promoDiscountAmount, model.totalFare, model.duration, model.distance, model.paymentMode, model.tripID, model.driverID);
        //            });
        //            response.error = false;
        //            response.message = ResponseKeys.msgSuccess;
        //            return Request.CreateResponse(HttpStatusCode.OK, response);
        //        }
        //    }
        //    else
        //    {
        //        response.error = true;
        //        response.message = ResponseKeys.invalidParameters;
        //        return Request.CreateResponse(HttpStatusCode.OK, response);
        //    }
        //}

        //[HttpPost]
        //public async Task<HttpResponseMessage> mobilePaymentTimeout([FromBody] RequestModel model)
        //{
        //    if (!string.IsNullOrEmpty(model.userID) && !string.IsNullOrEmpty(model.driverID) && !string.IsNullOrEmpty(model.tripID))
        //    {
        //        using (var context = new CangooEntities())
        //        {
        //            FireBaseController fb = new FireBaseController();

        //            if (model.isWalkIn)
        //            {
        //                fb.freeUserFromWalkInTrip(model.userID, model.tripID);
        //                //fb.removeWalkInPaymentData(model.driverID, model.userID, model.tripID);
        //            }
        //            else
        //            {
        //                fb.changeTripStatus("Trips/" + model.tripID, App_Start.Enumration.returnRideFirebaseStatus(App_Start.RideFirebaseStatus.PaymentPending));
        //            }

        //            dic = new Dictionary<dynamic, dynamic> {
        //                    {"isWalkIn",model.isWalkIn.ToString().ToLower() },
        //                    {"tripID", model.tripID },
        //                    {"driverID", model.driverID }
        //                };

        //            var user = context.UserProfiles.Where(u => u.UserID.Equals(model.userID)).FirstOrDefault();
        //            await fb.sentSingleFCM(user.DeviceToken, dic, "pas_MobilePaymentTimeOut");
        //        }

        //        response.error = false;
        //        response.data = dic;
        //        response.message = ResponseKeys.msgSuccess;
        //        return Request.CreateResponse(HttpStatusCode.OK, response);
        //    }
        //    else
        //    {
        //        response.error = true;
        //        response.message = ResponseKeys.invalidParameters;
        //        return Request.CreateResponse(HttpStatusCode.OK, response);
        //    }
        //}

        //[HttpPost]
        //public HttpResponseMessage passengerRating([FromBody] RequestModel model)
        //{
        //    if (model != null && model.customerRating > 0 && !string.IsNullOrEmpty(model.tripID) && !string.IsNullOrEmpty(model.driverID))
        //    {
        //        using (CangooEntities context = new CangooEntities())
        //        {
        //            var tp = context.Trips.Where(t => t.TripID.ToString() == model.tripID).FirstOrDefault();
        //            if (tp != null)
        //            {
        //                tp.UserRating = Convert.ToInt32(model.customerRating);
        //                tp.DriverSubmittedFeedback = model.description;

        //                var user = context.UserProfiles.Where(u => u.UserID == tp.UserID.ToString()).FirstOrDefault();

        //                //Verify if ride was booked by business portal
        //                if (user != null)
        //                {
        //                    int userTrips = (int)(user.NoOfTrips == null ? 0 : user.NoOfTrips);
        //                    user.Rating = Math.Round((double)((((user.Rating == null ? 0 : user.Rating) * (userTrips - 1)) + tp.UserRating) / userTrips), 1, MidpointRounding.ToEven);
        //                }

        //                context.SaveChanges();
        //                response.error = false;
        //                response.message = ResponseKeys.msgSuccess;
        //                return Request.CreateResponse(HttpStatusCode.OK, response);
        //            }
        //            else
        //            {
        //                response.error = true;
        //                response.message = ResponseKeys.tripNotFound;
        //                return Request.CreateResponse(HttpStatusCode.OK, response);
        //            }
        //        }
        //    }
        //    else
        //    {
        //        response.error = true;
        //        response.message = ResponseKeys.invalidParameters;
        //        return Request.CreateResponse(HttpStatusCode.OK, response);
        //    }
        //}

        //[HttpPost]
        //public HttpResponseMessage passengerFavUnFav([FromBody] RequestModel model)
        //{
        //    if (model != null && !string.IsNullOrEmpty(model.tripID) && !string.IsNullOrEmpty(model.driverID))
        //    {
        //        using (CangooEntities context = new CangooEntities())
        //        {
        //            var tp = context.Trips.Where(t => t.TripID.ToString() == model.tripID).FirstOrDefault();
        //            if (tp != null)
        //            {
        //                if (Request.Headers.Contains("ApplicationID"))
        //                {
        //                    ApplicationID = Request.Headers.GetValues("ApplicationID").First();
        //                }
        //                var capt = context.Captains.Where(c => c.CaptainID.ToString().Equals(model.driverID)).FirstOrDefault();
        //                var usr = context.UserFavoriteCaptains.Where(f => f.UserID == tp.UserID.ToString() && f.CaptainID.ToString() == model.driverID).FirstOrDefault();
        //                if (usr == null)
        //                {
        //                    UserFavoriteCaptain uf = new UserFavoriteCaptain
        //                    {
        //                        ID = Guid.NewGuid(),
        //                        UserID = tp.UserID.ToString(),
        //                        CaptainID = tp.CaptainID,
        //                        IsFavByPassenger = false,
        //                        IsFavByCaptain = true,
        //                        ApplicationID = Guid.Parse(ApplicationID)
        //                    };

        //                    capt.NumberOfFavoriteUser = capt.NumberOfFavoriteUser == null ? 1 : (int)capt.NumberOfFavoriteUser + 1;
        //                    context.UserFavoriteCaptains.Add(uf);
        //                }
        //                else
        //                {
        //                    if ((bool)usr.IsFavByCaptain && (bool)usr.IsFavByPassenger)
        //                    {
        //                        usr.IsFavByCaptain = false;
        //                        capt.NumberOfFavoriteUser = capt.NumberOfFavoriteUser == 1 ? 0 : (int)capt.NumberOfFavoriteUser - 1;
        //                    }
        //                    else if ((bool)usr.IsFavByCaptain)
        //                    {
        //                        context.UserFavoriteCaptains.Remove(usr);
        //                        capt.NumberOfFavoriteUser = capt.NumberOfFavoriteUser == 1 ? 0 : (int)capt.NumberOfFavoriteUser - 1;
        //                    }
        //                    else
        //                    {
        //                        usr.IsFavByCaptain = true;
        //                        capt.NumberOfFavoriteUser = capt.NumberOfFavoriteUser == null ? 1 : (int)capt.NumberOfFavoriteUser + 1;
        //                    }
        //                }
        //                context.SaveChanges();
        //                response.error = false;
        //                response.message = ResponseKeys.msgSuccess;
        //                return Request.CreateResponse(HttpStatusCode.OK, response);
        //            }
        //            else
        //            {
        //                response.error = true;
        //                response.message = ResponseKeys.tripNotFound;
        //                return Request.CreateResponse(HttpStatusCode.OK, response);
        //            }
        //        }
        //    }
        //    else
        //    {
        //        response.error = true;
        //        response.message = ResponseKeys.invalidParameters;
        //        return Request.CreateResponse(HttpStatusCode.OK, response);
        //    }
        //}

        //#endregion

        //#region WalkIn Ride Scenario

        ////[HttpPost]
        ////public HttpResponseMessage searchUserByPhone([FromBody] RequestModel model)
        ////{
        ////    try
        ////    {
        ////        if (model != null && !string.IsNullOrEmpty(model.phoneNumber))
        ////        {
        ////            using (CangooEntities context = new CangooEntities())
        ////            {
        ////                var up = context.spSearchUserByPhone(model.phoneNumber).ToList();
        ////                if (up.Any())
        ////                {
        ////                    if (string.IsNullOrEmpty(up.FirstOrDefault().Email))
        ////                    {
        ////                        response.error = true;
        ////                        response.message = ResponseKeys.userNotVerified;
        ////                        return Request.CreateResponse(HttpStatusCode.OK, response);
        ////                    }

        ////                    response.data = up;
        ////                    response.error = false;
        ////                    response.message = ResponseKeys.msgSuccess;
        ////                    return Request.CreateResponse(HttpStatusCode.OK, response);
        ////                }
        ////                else
        ////                {
        ////                    response.error = true;
        ////                    response.message = ResponseKeys.userNotFound;
        ////                    return Request.CreateResponse(HttpStatusCode.OK, response);
        ////                }
        ////            }
        ////        }
        ////        else
        ////        {
        ////            response.error = true;
        ////            response.message = ResponseKeys.invalidParameters;
        ////            return Request.CreateResponse(HttpStatusCode.OK, response);
        ////        }
        ////    }
        ////    catch (Exception ex)
        ////    {
        ////        response.error = true;
        ////        Logger.WriteLog(ex);
        ////        Logger.WriteLog(model);
        ////        response.message = ResponseKeys.serverError;
        ////        return Request.CreateResponse(HttpStatusCode.OK, response);
        ////    }
        ////}

        ////[HttpPost]
        ////public HttpResponseMessage WalkInFareEstimate([FromBody] RequestModel model)
        ////{
        ////    try
        ////    {
        ////        //TBD: Inherit this controller from another and set following proerties in that controller to remove redundancy

        ////        if (Request.Headers.Contains("ApplicationID"))
        ////        {
        ////            ApplicationID = Request.Headers.GetValues("ApplicationID").First();
        ////        }

        ////        if (model != null && !string.IsNullOrEmpty(model.distance) && !string.IsNullOrEmpty(model.resellerID) &&
        ////            !string.IsNullOrEmpty(model.resellerArea) && !string.IsNullOrEmpty(model.pickUplatitude) &&
        ////            !string.IsNullOrEmpty(model.pickUplongitude) && !string.IsNullOrEmpty(model.lat.ToString()) && !string.IsNullOrEmpty(model.lon.ToString()))
        ////        {
        ////            using (CangooEntities context = new CangooEntities())
        ////            {
        ////                decimal totalFare = 0.0M;
        ////                FareManager fareManag = new FareManager();

        ////                dic = new Dictionary<dynamic, dynamic>() {
        ////                    { "discountType", "normal"},
        ////                    { "discountAmount", "0.00" }
        ////                };

        ////                //TBD: Vehcile capacity to be fetched based on driver current vehicle
        ////                totalFare = Common.CalculateEstimatedFare(4, true, false, "", false, false, null, ApplicationID, model.pickUplatitude, model.pickUplongitude, model.lat.ToString(), model.lon.ToString(), ref fareManag, ref fareManag, ref dic); //(Convert.ToDecimal(model.distance) / 1000).ToString(), null

        ////                dic.Add("estimatedPrice", string.Format("{0:0.00}", totalFare));

        ////                response.error = false;
        ////                response.data = dic;
        ////                response.message = ResponseKeys.msgSuccess;
        ////                return Request.CreateResponse(HttpStatusCode.OK, response);
        ////            }
        ////        }
        ////        else
        ////        {
        ////            response.error = true;
        ////            response.message = ResponseKeys.invalidParameters;
        ////            return Request.CreateResponse(HttpStatusCode.OK, response);
        ////        }
        ////    }
        ////    catch (Exception ex)
        ////    {
        ////        response.error = true;
        ////        Logger.WriteLog(ex);
        ////        Logger.WriteLog(model);
        ////        response.message = ResponseKeys.serverError;
        ////        return Request.CreateResponse(HttpStatusCode.OK, response);
        ////    }
        ////}

        ////[HttpPost]
        ////public HttpResponseMessage WalkInCollectMobilePayment([FromBody] RequestModel model)
        ////{
        ////    try
        ////    {
        ////        //TBD: Inherit this controller from another and set following proerties in that controller to remove redundancy

        ////        if (Request.Headers.Contains("ApplicationID"))
        ////        {
        ////            ApplicationID = Request.Headers.GetValues("ApplicationID").First();
        ////        }

        ////        if (model != null && !string.IsNullOrEmpty(model.driverID) && !string.IsNullOrEmpty(model.userID) && !string.IsNullOrEmpty(model.vehicleID) &&
        ////            !string.IsNullOrEmpty(model.distance) && !string.IsNullOrEmpty(model.fleetID) &&
        ////            !string.IsNullOrEmpty(model.estimatedFare) && !string.IsNullOrEmpty(model.isOverride) &&
        ////            !string.IsNullOrEmpty(model.passengerName) && !string.IsNullOrEmpty(model.paymentMode) &&
        ////            !string.IsNullOrEmpty(model.dropOfflatitude) && !string.IsNullOrEmpty(model.dropOfflongitude) &&
        ////            !string.IsNullOrEmpty(model.pickUplatitude) && !string.IsNullOrEmpty(model.pickUplongitude))
        ////        {
        ////            using (CangooEntities context = new CangooEntities())
        ////            {
        ////                if (!string.IsNullOrEmpty(model.tripID))
        ////                {
        ////                    dic = new Dictionary<dynamic, dynamic>();
        ////                    if (CheckIfAlreadyPaid(model.estimatedFare, model.tripID, model.driverID, ref dic, true))
        ////                    {
        ////                        FireBaseController fc = new FireBaseController();
        ////                        fc.freeUserFromWalkInTrip(model.userID, model.tripID);

        ////                        response.data = dic;
        ////                        response.error = false;
        ////                        response.message = ResponseKeys.fareAlreadyPaid;
        ////                        return Request.CreateResponse(HttpStatusCode.OK, response);
        ////                    }
        ////                }

        ////                var user = context.UserProfiles.Where(u => u.UserID.ToString() == model.userID).FirstOrDefault();
        ////                if (user == null)
        ////                {
        ////                    response.error = true;
        ////                    response.message = ResponseKeys.userNotFound;
        ////                    return Request.CreateResponse(HttpStatusCode.OK, response);
        ////                }

        ////                dic = new Dictionary<dynamic, dynamic>() {
        ////                        { "discountType", "normal"},
        ////                        { "discountAmount", "0.00" }
        ////                    };

        ////                walkInPassengerPaypalPaymentFCM pfcm = new walkInPassengerPaypalPaymentFCM()
        ////                {
        ////                    isPaymentRequested = true,
        ////                    paymentRequestTime = Common.getUtcDateTime().ToString(Common.dateFormat),
        ////                    userID = model.userID,
        ////                    pickUplatitude = model.pickUplatitude,
        ////                    pickUplongitude = model.pickUplongitude,
        ////                    dropOfflatitude = model.dropOfflatitude,
        ////                    dropOfflongitude = model.dropOfflongitude,
        ////                    driverID = model.driverID,
        ////                    ressellerID = ApplicationID,
        ////                    estimatedFare = model.estimatedFare,
        ////                    distance = model.distance,
        ////                    isOverride = model.isOverride,
        ////                    vehicleID = model.vehicleID,
        ////                    paymentMode = model.paymentMode,
        ////                    fleetID = model.fleetID,
        ////                    walletTotalAmount = user.WalletBalance != null ? string.Format("{0:0.00}", user.WalletBalance.ToString()) : "0.00",
        ////                    newTripID = string.IsNullOrEmpty(model.tripID) ? Guid.NewGuid().ToString() : model.tripID,
        ////                    passengerName = model.passengerName,
        ////                    discountType = dic["discountType"],
        ////                    discountAmount = dic["discountAmount"],
        ////                    promoCodeID = ""
        ////                };

        ////                //UserController uc = new UserController();

        ////                string promotionId = Common.IsSpecialPromotionApplicable(model.pickUplatitude, model.pickUplongitude, model.dropOfflatitude, model.dropOfflongitude, ApplicationID, context, ref dic);

        ////                if (!string.IsNullOrEmpty(promotionId))
        ////                {
        ////                    pfcm.discountType = dic["discountType"];
        ////                    pfcm.discountAmount = dic["discountAmount"];
        ////                    pfcm.promoCodeID = promotionId;
        ////                }
        ////                else
        ////                {
        ////                    promotionId = Common.ApplyPromoCode(ApplicationID, model.userID, context, ref dic);
        ////                    if (!string.IsNullOrEmpty(promotionId))
        ////                    {
        ////                        pfcm.discountType = dic["discountType"];
        ////                        pfcm.discountAmount = dic["discountAmount"];
        ////                        pfcm.promoCodeID = promotionId;
        ////                    }
        ////                }

        ////                FireBaseController fb = new FireBaseController();

        ////                //By mistake request was sent to wrong user, free the user
        ////                if (!string.IsNullOrEmpty(model.walkInOldUserID))
        ////                {
        ////                    fb.freeUserFromWalkInTrip(model.walkInOldUserID, pfcm.newTripID.ToString());
        ////                }

        ////                fb.setWalkInPaymentData(model.driverID, model.userID, pfcm);

        ////                var task = Task.Run(async () =>
        ////                {
        ////                    FireBaseController fc = new FireBaseController();
        ////                    if (model.paymentMode.ToLower().Equals("paypal"))
        ////                        await fc.sentSingleFCM(user.DeviceToken, pfcm, "pas_WalkInPassengerPaypalPayment");
        ////                    else
        ////                        await fc.sentSingleFCM(user.DeviceToken, pfcm, "pas_WalkInPassengerCreditCardPayment");
        ////                });

        ////                dic = new Dictionary<dynamic, dynamic> {
        ////                    {"newTripID", pfcm.newTripID}
        ////                };
        ////            }

        ////            response.error = false;
        ////            response.data = dic;
        ////            response.message = ResponseKeys.msgSuccess;
        ////            return Request.CreateResponse(HttpStatusCode.OK, response);
        ////        }
        ////        else
        ////        {
        ////            response.error = true;
        ////            response.message = ResponseKeys.invalidParameters;
        ////            return Request.CreateResponse(HttpStatusCode.OK, response);
        ////        }
        ////    }
        ////    catch (Exception ex)
        ////    {
        ////        response.error = true;
        ////        Logger.WriteLog(ex);
        ////        Logger.WriteLog(model);
        ////        response.message = ResponseKeys.serverError;
        ////        return Request.CreateResponse(HttpStatusCode.OK, response);
        ////    }
        ////}

        //#endregion

        //#region Priority Hour

        //[HttpPost]
        //public HttpResponseMessage activatePriorityHour([FromBody] PriorityHour model)
        //{

        //    using (CangooEntities context = new CangooEntities())
        //    {
        //        if (model != null && model.captainID != string.Empty && model.duration > 0)
        //        {
        //            var captain = context.Captains.Where(c => c.CaptainID.ToString().Equals(model.captainID)).FirstOrDefault();
        //            if (captain != null)
        //            {
        //                var settings = context.ApplicationSettings.Where(a => a.ApplicationID.ToString().ToLower().Equals(ApplicationID.ToLower())).FirstOrDefault();

        //                captain.IsPriorityHoursActive = true;
        //                captain.LastPriorityHourStartTime = Common.getUtcDateTime();
        //                captain.LastPriorityHourEndTime = Common.getUtcDateTime().AddHours(model.duration);
        //                captain.EarningPoints -= model.duration * (settings.AwardpointsDeduction != null ? (int)settings.AwardpointsDeduction : 100);
        //                context.SaveChanges();

        //                var priorityHourRemainingTime = ((int)(((DateTime)captain.LastPriorityHourEndTime).
        //                                                Subtract((DateTime)captain.LastPriorityHourStartTime).TotalMinutes)).ToString();
        //                FireBaseController fb = new FireBaseController();
        //                fb.setPriorityHourStatus(true, priorityHourRemainingTime, model.captainID, DateTime.Parse(captain.LastPriorityHourEndTime.ToString()).ToString(Common.dateFormat), captain.EarningPoints.ToString());

        //                response.error = false;
        //                response.message = ResponseKeys.msgSuccess;
        //                dic = new Dictionary<dynamic, dynamic>
        //                            {
        //                                { "priorityHour", model }
        //                            };
        //                response.data = dic;
        //                return Request.CreateResponse(HttpStatusCode.OK, response);
        //            }
        //            else
        //            {
        //                response.message = ResponseKeys.captainNotFound;
        //                response.error = true;
        //                return Request.CreateResponse(HttpStatusCode.OK, response);
        //            }
        //        }
        //        else
        //        {
        //            response.message = ResponseKeys.invalidParameters;
        //            response.error = true;
        //            return Request.CreateResponse(HttpStatusCode.OK, response);
        //        }
        //    }
        //}

        //[HttpGet]
        //public HttpResponseMessage getCaptainEarnedPoints(string captainID)
        //{
        //    if (!string.IsNullOrEmpty(captainID))
        //    {
        //        using (CangooEntities context = new CangooEntities())
        //        {
        //            var captain = context.Captains.Where(c => c.CaptainID.ToString().Equals(captainID)).FirstOrDefault();
        //            if (captain != null)
        //            {
        //                response.error = false;
        //                response.message = ResponseKeys.msgSuccess;
        //                dic = new Dictionary<dynamic, dynamic>
        //                            {
        //                                { "earnedPoints", captain.EarningPoints }
        //                            };
        //                response.data = dic;
        //                return Request.CreateResponse(HttpStatusCode.OK, response);
        //            }
        //            else
        //            {
        //                response.error = true;
        //                response.message = ResponseKeys.captainNotFound;
        //                return Request.CreateResponse(HttpStatusCode.OK, response);
        //            }
        //        }
        //    }
        //    else
        //    {
        //        response.message = ResponseKeys.invalidParameters;
        //        response.error = true;
        //        return Request.CreateResponse(HttpStatusCode.OK, response);
        //    }
        //}

        //#endregion

        //#region Trip History/Upcoming

        //[HttpGet]
        //public HttpResponseMessage getAllUnAcceptedLaterBooking(int offset, int limit, int vehicleSeatingCapacity)
        //{
        //    if (offset > 0 && limit > 0 && vehicleSeatingCapacity > 0)
        //    {
        //        using (CangooEntities context = new CangooEntities())
        //        {
        //            //UserController uc = new UserController();
        //            var lstLaterBooking = context.spGetAllUnAcceptedLateBooking(ResellerID, ApplicationID, Common.getUtcDateTime(), (int)App_Start.TripStatus.RequestSent, vehicleSeatingCapacity, offset, limit).ToList(); //date time utc
        //            if (lstLaterBooking.Count > 0)
        //            {
        //                List<ScheduleBooking> lstSB = new List<ScheduleBooking>();
        //                foreach (var item in lstLaterBooking)
        //                {
        //                    dic = new Dictionary<dynamic, dynamic>() {
        //                            { "discountType", "normal"},
        //                            { "discountAmount", "0.00" }
        //                        };

        //                    Common.IsSpecialPromotionApplicable(item.pickupLocationLatitude, item.pickuplocationlongitude, item.DropOffLocationLatitude,
        //                        item.DropOffLocationLongitude, ApplicationID, context, ref dic, true, item.pickUpBookingDateTime);


        //                    Common.GetFacilities(ResellerID, ApplicationID, context, item.facilities, out List<Facilities> lstTripFacilities);
        //                    //if (item.NoOfPerson <= vehicleSeatingCapacity)
        //                    //{
        //                    ScheduleBooking sb = new ScheduleBooking
        //                    {
        //                        tripID = item.TripID.ToString(),
        //                        pickUplatitude = item.pickupLocationLatitude,
        //                        pickUplongitude = item.pickuplocationlongitude,
        //                        pickUpLocation = item.PickUpLocation,
        //                        dropOfflatitude = item.DropOffLocationLatitude,
        //                        dropOfflongitude = item.DropOffLocationLongitude,
        //                        dropOffLocation = item.DropOffLocation,
        //                        isLaterBooking = Convert.ToBoolean(item.isLaterBooking),
        //                        passengerID = item.UserID.ToString(),
        //                        passengerName = item.passengName,
        //                        rating = item.Rating.ToString(),
        //                        tripPaymentMode = item.TripPaymentMode,
        //                        pickUpDateTime = Convert.ToDateTime(item.pickUpBookingDateTime).ToString(Common.dateFormat),
        //                        isFav = item.favorite != null ? (bool)item.favorite : false,
        //                        seatingCapacity = (int)item.NoOfPerson,
        //                        estimatedDistance = item.DistanceTraveled.ToString(),
        //                        facilities = lstTripFacilities,
        //                        discountType = dic["discountType"],
        //                        discountAmount = dic["discountAmount"]
        //                    };
        //                    lstSB.Add(sb);
        //                    //}
        //                }

        //                dic = new Dictionary<dynamic, dynamic>
        //                    {
        //                        { "pendingLaterBooking", lstSB },
        //                        { "totalRecords", lstLaterBooking.FirstOrDefault().totalRecord }
        //                    };

        //                response.error = false;
        //                response.message = ResponseKeys.msgSuccess;
        //                response.data = dic;
        //                return Request.CreateResponse(HttpStatusCode.OK, response);
        //            }
        //            else
        //            {
        //                dic = new Dictionary<dynamic, dynamic>
        //                    {
        //                        { "pendingLaterBooking", new List<ScheduleBooking>() },
        //                        { "totalRecords", 0 }
        //                    };

        //                response.error = false;
        //                response.message = ResponseKeys.msgSuccess;
        //                response.data = dic;
        //                return Request.CreateResponse(HttpStatusCode.OK, response);
        //            }
        //        }
        //    }
        //    else
        //    {
        //        response.error = true;
        //        response.message = ResponseKeys.invalidParameters;
        //        return Request.CreateResponse(HttpStatusCode.BadRequest, response);
        //    }
        //}

        //[HttpPost]      //Get accepted upcoming later bookings
        //public HttpResponseMessage getDriverLaterBooking([FromBody] DriverModel model)
        //{

        //    using (CangooEntities context = new CangooEntities())
        //    {
        //        if (model != null && model.userID != string.Empty && model.offset > 0 && model.limit > 0)
        //        {
        //            //UserController uc = new UserController();

        //            var lstScheduleRides = context.spCaptainLaterTrips(Common.getUtcDateTime(), model.userID, (int)App_Start.TripStatus.LaterBookingAccepted, model.offset, model.limit).ToList();
        //            if (lstScheduleRides.Count > 0)
        //            {
        //                List<ScheduleBooking> lstSB = new List<ScheduleBooking>();
        //                foreach (var item in lstScheduleRides)
        //                {
        //                    dic = new Dictionary<dynamic, dynamic>() {
        //                            { "discountType", "normal"},
        //                            { "discountAmount", "0.00" }
        //                        };

        //                    Common.IsSpecialPromotionApplicable(item.pickupLocationLatitude, item.pickuplocationlongitude, item.DropOffLocationLatitude,
        //                        item.DropOffLocationLongitude, ApplicationID, context, ref dic, true, item.pickUpBookingDateTime);

        //                    Common.GetFacilities(ResellerID, ApplicationID, context, item.facilities, out List<Facilities> lstTripFacilities);
        //                    ScheduleBooking sb = new ScheduleBooking
        //                    {
        //                        tripID = item.TripID.ToString(),
        //                        pickUplatitude = item.pickupLocationLatitude,
        //                        pickUplongitude = item.pickuplocationlongitude,
        //                        pickUpLocation = item.PickUpLocation,
        //                        dropOfflatitude = item.DropOffLocationLatitude,
        //                        dropOfflongitude = item.DropOffLocationLongitude,
        //                        dropOffLocation = item.DropOffLocation,
        //                        isLaterBooking = Convert.ToBoolean(item.isLaterBooking),
        //                        passengerID = item.UserID.ToString(),
        //                        passengerName = item.passengName,
        //                        rating = item.Rating.ToString(),
        //                        tripPaymentMode = item.TripPaymentMode,
        //                        isFav = item.favorite != null ? (bool)item.favorite : false,
        //                        pickUpDateTime = Convert.ToDateTime(item.pickUpBookingDateTime).ToString(Common.dateFormat),
        //                        seatingCapacity = (int)item.NoOfPerson,
        //                        estimatedDistance = item.DistanceTraveled.ToString(),
        //                        facilities = lstTripFacilities,
        //                        isWeb = item.isWeb,
        //                        discountType = dic["discountType"],
        //                        discountAmount = dic["discountAmount"],
        //                        remainingTime = ((DateTime)item.pickUpBookingDateTime - DateTime.UtcNow).TotalSeconds
        //                    };
        //                    lstSB.Add(sb);
        //                }

        //                dic = new Dictionary<dynamic, dynamic>
        //                    {
        //                        { "laterBooking", lstSB },
        //                        { "totalRecords", lstScheduleRides.FirstOrDefault().totalRecord }
        //                    };

        //                response.error = false;
        //                response.message = ResponseKeys.msgSuccess;
        //                response.data = dic;
        //                return Request.CreateResponse(HttpStatusCode.OK, response);
        //            }
        //            else
        //            {
        //                dic = new Dictionary<dynamic, dynamic>
        //                    {
        //                        { "laterBooking", new List<ScheduleBooking>() },
        //                        { "totalRecords", 0 }
        //                    };

        //                response.error = false;
        //                response.message = ResponseKeys.msgSuccess;
        //                response.data = dic;
        //                return Request.CreateResponse(HttpStatusCode.OK, response);
        //            }
        //        }
        //        else
        //        {
        //            response.error = true;
        //            response.message = ResponseKeys.invalidParameters;
        //            return Request.CreateResponse(HttpStatusCode.OK, response);
        //        }
        //    }
        //}

        //[HttpGet]
        //public HttpResponseMessage getDriverBookingHistory(string captainID, string pageNo, string pageSize, string dateTo, string dateFrom)
        //{
        //    if (!string.IsNullOrEmpty(captainID) && !string.IsNullOrEmpty(pageSize) && !string.IsNullOrEmpty(pageNo) &&
        //        !string.IsNullOrEmpty(dateFrom) && !string.IsNullOrEmpty(dateTo))
        //    {
        //        using (CangooEntities context = new CangooEntities())
        //        {

        //            var result = context.spCaptainTripHistory(captainID, int.Parse(pageNo), int.Parse(pageSize),
        //                Convert.ToDateTime(dateFrom), Convert.ToDateTime(dateTo)).ToList();
        //            if (result.Count > 0)
        //            {
        //                List<DriverTrips> lstTrips = new List<DriverTrips>();

        //                foreach (var temp in result)
        //                {
        //                    Common.GetFacilities(ResellerID, ApplicationID, context, temp.facilities, out List<Facilities> lstTripFacilities);
        //                    DriverTrips trip = new DriverTrips()
        //                    {
        //                        tripID = temp.tripID.ToString(),
        //                        pickupLocationLatitude = temp.PickupLocationLatitude,
        //                        pickupLocationLongitude = temp.PickupLocationLongitude,
        //                        pickupLocation = temp.PickUpLocation,
        //                        dropOffLocationLatitude = temp.DropOffLocationLatitude,
        //                        dropOffLocationLongitude = temp.DropOffLocationLongitude,
        //                        dropOffLocation = temp.DropOffLocation,
        //                        bookingDateTime = Convert.ToDateTime(temp.BookingDateTime).ToString(Common.dateFormat),
        //                        pickUpBookingDateTime = Convert.ToDateTime(temp.PickUpBookingDateTime).ToString(Common.dateFormat),
        //                        tripArrivalDatetime = Convert.ToDateTime(temp.ArrivalDateTime).ToString(Common.dateFormat),
        //                        tripStartDatetime = Convert.ToDateTime(temp.TripStartDatetime).ToString(Common.dateFormat),
        //                        tripEndDatetime = Convert.ToDateTime(temp.TripEndDatetime).ToString(Common.dateFormat),
        //                        tripStatus = temp.Status,
        //                        facilities = lstTripFacilities,
        //                        tip = temp.Tip.ToString(),
        //                        fare = temp.Fare.ToString(),
        //                        cashPayment = temp.TripCashPayment.ToString(),
        //                        mobilePayment = temp.TripMobilePayment.ToString(),
        //                        bookingType = temp.BookingType,
        //                        bookingMode = temp.BookingMode,
        //                        paymentMode = temp.PaymentMode,
        //                        passengerName = temp.PassengerName,
        //                        make = temp.make,
        //                        model = temp.Model,
        //                        plateNumber = temp.PlateNumber,
        //                        distanceTraveled = temp.DistanceTraveled.ToString(),
        //                        vehicleRating = temp.VehicleRating.ToString(),
        //                        driverEarnedPoints = temp.DriverEarnedPoints.ToString(),
        //                        driverRating = temp.DriverRating.ToString()
        //                    };
        //                    lstTrips.Add(trip);
        //                }

        //                DriverTripsHistory history = new DriverTripsHistory()
        //                {
        //                    avgDriverRating = result.FirstOrDefault().avgDriverRating.ToString(),
        //                    avgVehicleRating = result.FirstOrDefault().avgVehicleRating.ToString(),
        //                    totalTrips = result.FirstOrDefault().totalTrips.ToString(),
        //                    totalFare = (result.FirstOrDefault().totalTip + result.FirstOrDefault().totalFare).ToString(),
        //                    totalTip = result.FirstOrDefault().totalTip.ToString(),
        //                    totalEarnedPoints = result.FirstOrDefault().totalEarnedPoints.ToString(),
        //                    totalCashEarning = result.FirstOrDefault().totalCashEarning.ToString(),
        //                    totalMobilePayEarning = result.FirstOrDefault().totalMobilePayEarning.ToString(),
        //                    trips = lstTrips
        //                };
        //                response.data = history;
        //            }
        //            else
        //            {
        //                response.data = new DriverTripsHistory()
        //                {
        //                    avgDriverRating = "0",
        //                    avgVehicleRating = "0",
        //                    totalEarnedPoints = "0",
        //                    totalFare = "0",
        //                    totalTip = "0",
        //                    totalTrips = "0",
        //                    trips = new List<DriverTrips>()
        //                };
        //            }
        //            response.error = false;
        //            response.message = ResponseKeys.msgSuccess;
        //            return Request.CreateResponse(HttpStatusCode.OK, response);
        //        }
        //    }
        //    else
        //    {
        //        response.error = true;
        //        response.message = ResponseKeys.invalidParameters;
        //        return Request.CreateResponse(HttpStatusCode.OK, response);
        //    }
        //}

        //#endregion

        //#region profile / settings

        //[HttpGet]
        //public HttpResponseMessage captainProfile(string captainID, string vehicleID)
        //{
        //    if (!string.IsNullOrEmpty(captainID))
        //    {
        //        //TBD: Inherit this controller from another and set following proerties in that controller to remove redundancy
        //        if (Request.Headers.Contains("ResellerID"))
        //        {
        //            ResellerID = Request.Headers.GetValues("ResellerID").First();
        //        }

        //        if (Request.Headers.Contains("ApplicationID"))
        //        {
        //            ApplicationID = Request.Headers.GetValues("ApplicationID").First();
        //        }

        //        using (CangooEntities context = new CangooEntities())
        //        {
        //            var cap = context.spCaptainProfile(captainID, vehicleID, new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1)).FirstOrDefault();

        //            if (cap != null)
        //            {
        //                Common.GetFacilities(ResellerID, ApplicationID, context, cap.Facilities, out List<Facilities> lstCaptainFacilities);
        //                Common.GetFacilities(ResellerID, ApplicationID, context, cap.VehicleFacilities, out List<Facilities> lstVehicleFacilities);

        //                var profile = new CaptainProfile()
        //                {
        //                    name = cap.Name,
        //                    email = cap.Email,
        //                    phone = cap.PhoneNumber,
        //                    shareCode = cap.ShareCode,
        //                    captainFacilitiesList = lstCaptainFacilities,
        //                    make = cap.Make,
        //                    model = cap.Model,
        //                    number = cap.PlateNumber,
        //                    seatingCapacity = cap.SeatingCapacity.ToString(),
        //                    vehicleFacilitiesList = lstVehicleFacilities
        //                    //make = "",
        //                    //model = "",
        //                    //number = "",
        //                    //seatingCapacity = "",
        //                    //vehicleFacilitiesList = new List<Facilities>()
        //                };

        //                //if (!string.IsNullOrEmpty(vehicleID))
        //                //{
        //                //	var veh = context.spVehicleProfile(vehicleID).FirstOrDefault();

        //                //	if (veh != null)
        //                //	{
        //                //		GetFacilities(ResellerID, ApplicationID, context, veh.Facilities, out List<Facilities> lstVehicleFacilities);

        //                //		profile.make = veh.Make;
        //                //		profile.model = veh.Model;
        //                //		profile.number = veh.PlateNumber;
        //                //		profile.seatingCapacity = veh.SeatingCapacity.ToString();
        //                //		profile.vehicleFacilitiesList = lstVehicleFacilities;
        //                //	}
        //                //	else
        //                //	{
        //                //		response.error = true;
        //                //		response.message = ResponseKeys.vehicleNotFound;
        //                //		return Request.CreateResponse(HttpStatusCode.OK, response);
        //                //	}
        //                //}
        //                response.data = profile;
        //                response.error = false;
        //                response.message = ResponseKeys.msgSuccess;

        //                return Request.CreateResponse(HttpStatusCode.OK, response);
        //            }
        //            else
        //            {
        //                response.error = true;
        //                response.message = ResponseKeys.captainNotFound;
        //                return Request.CreateResponse(HttpStatusCode.OK, response);
        //            }
        //        }
        //    }
        //    else
        //    {
        //        response.error = true;
        //        response.message = ResponseKeys.invalidParameters;
        //        return Request.CreateResponse(HttpStatusCode.OK, response);
        //    }
        //}

        //[HttpGet]
        //public HttpResponseMessage captainStats(string captainID, string vehicleID)
        //{
        //    if (!string.IsNullOrEmpty(captainID) && !string.IsNullOrEmpty(vehicleID))
        //    {
        //        using (CangooEntities context = new CangooEntities())
        //        {
        //            var cap = context.spCaptainProfile(captainID, vehicleID, new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1)).FirstOrDefault();

        //            if (cap != null)
        //            {
        //                var stats = new CaptainStats()
        //                {
        //                    captainRating = string.Format("{0:0.00}", cap.Rating.ToString()),
        //                    vehicleRating = string.Format("{0:0.00}", cap.VehicleRating.ToString()),
        //                    cashRides = cap.CashTrips.ToString(),
        //                    mobilePayRides = cap.MobilePayTrips.ToString(),
        //                    cashEarning = string.Format("{0:0.00}", cap.CashEarning.ToString()),
        //                    mobilePayEarning = string.Format("{0:0.00}", cap.MobilePayEarning.ToString()),
        //                    favPassengers = cap.NumberOfFavoriteUser.ToString(),
        //                    memberSince = DateTime.Parse(cap.MemberSince.ToString()).ToString(Common.dateFormat),
        //                    avgCashEarning = string.Format("{0:0.00}", cap.AverageCashEarning.ToString()),
        //                    avgMobilePayEarning = string.Format("{0:0.00}", cap.AverageMobilePayEarning.ToString()),
        //                    currentMonthAcceptanceRate = string.Format("{0:0.00}", cap.currentMonthAcceptanceRate),
        //                    currentMonthOnlineHours = string.Format("{0:0.00}", cap.AverageMobilePayEarning)
        //                };

        //                response.data = stats;
        //                response.error = false;
        //                response.message = ResponseKeys.msgSuccess;

        //                return Request.CreateResponse(HttpStatusCode.OK, response);
        //            }
        //            else
        //            {
        //                response.error = true;
        //                response.message = ResponseKeys.captainNotFound;
        //                return Request.CreateResponse(HttpStatusCode.OK, response);
        //            }
        //        }
        //    }
        //    else
        //    {
        //        response.error = true;
        //        response.message = ResponseKeys.invalidParameters;
        //        return Request.CreateResponse(HttpStatusCode.OK, response);
        //    }
        //}

        //[HttpGet]
        //public HttpResponseMessage getCaptainSettings(string captainID)
        //{
        //    if (!string.IsNullOrEmpty(captainID))
        //    {
        //        using (CangooEntities context = new CangooEntities())
        //        {
        //            var cap = context.Captains.Where(c => c.CaptainID.ToString().Equals(captainID)).FirstOrDefault();

        //            if (cap != null)
        //            {
        //                var settings = new CaptainSettings()
        //                {
        //                    laterBookingNotificationTone = string.IsNullOrEmpty(cap.LaterBookingNotificationTone) ? "6.mp3" : cap.LaterBookingNotificationTone,
        //                    normalBookingNotificationTone = string.IsNullOrEmpty(cap.NormalBookingNotificationTone) ? "6.mp3" : cap.NormalBookingNotificationTone,
        //                    requestRadius = cap.RideRadius != null ? cap.RideRadius.ToString() : "N/A",
        //                    showOtherVehicles = cap.ShowOtherVehicles.ToString().ToLower(),
        //                    captainID = captainID
        //                };

        //                response.data = settings;
        //                response.error = false;
        //                response.message = ResponseKeys.msgSuccess;

        //                return Request.CreateResponse(HttpStatusCode.OK, response);
        //            }
        //            else
        //            {
        //                response.error = true;
        //                response.message = ResponseKeys.captainNotFound;
        //                return Request.CreateResponse(HttpStatusCode.OK, response);
        //            }
        //        }
        //    }
        //    else
        //    {
        //        response.error = true;
        //        response.message = ResponseKeys.invalidParameters;
        //        return Request.CreateResponse(HttpStatusCode.OK, response);
        //    }
        //}

        //[HttpPost]
        //public HttpResponseMessage saveCaptainSettings([FromBody] CaptainSettings model)
        //{
        //    if (!string.IsNullOrEmpty(model.captainID))
        //    {
        //        using (CangooEntities context = new CangooEntities())
        //        {
        //            var cap = context.Captains.Where(c => c.CaptainID.ToString().Equals(model.captainID)).FirstOrDefault();

        //            if (cap != null)
        //            {
        //                if (!string.IsNullOrEmpty(model.laterBookingNotificationTone))
        //                    cap.LaterBookingNotificationTone = model.laterBookingNotificationTone;
        //                if (!string.IsNullOrEmpty(model.normalBookingNotificationTone))
        //                    cap.NormalBookingNotificationTone = model.normalBookingNotificationTone;
        //                if (!string.IsNullOrEmpty(model.requestRadius))
        //                    cap.RideRadius = double.Parse(model.requestRadius);
        //                if (!string.IsNullOrEmpty(model.showOtherVehicles))
        //                    cap.ShowOtherVehicles = bool.Parse(model.showOtherVehicles.ToLower());

        //                context.SaveChanges();

        //                response.error = false;
        //                response.message = ResponseKeys.msgSuccess;
        //                return Request.CreateResponse(HttpStatusCode.OK, response);
        //            }
        //            else
        //            {
        //                response.error = true;
        //                response.message = ResponseKeys.captainNotFound;
        //                return Request.CreateResponse(HttpStatusCode.OK, response);
        //            }
        //        }
        //    }
        //    else
        //    {
        //        response.error = true;
        //        response.message = ResponseKeys.invalidParameters;
        //        return Request.CreateResponse(HttpStatusCode.OK, response);
        //    }
        //}

        //#endregion

        //#region Misc.

        //[HttpGet]
        //public HttpResponseMessage getContactDetails()
        //{

        //    using (CangooEntities context = new CangooEntities())
        //    {
        //        var contact = context.ContactDetails.Where(c => c.ApplicationID.ToString().ToLower().Equals(ApplicationID.ToLower())).FirstOrDefault();
        //        if (contact != null)
        //        {
        //            response.error = false;
        //            response.message = ResponseKeys.msgSuccess;
        //            dic = new Dictionary<dynamic, dynamic>
        //                {
        //                    { "businessName", contact.BusinessName },
        //                    { "openingTime", contact.OpeningTime },
        //                    { "closingTime", contact.ClosingTime },
        //                    { "contactPersonName", contact.ContactPersonName },
        //                    { "Email", contact.Email },
        //                    { "telephone", contact.Telephone },
        //                    { "state", contact.State },
        //                    { "city", contact.City },
        //                    { "street", contact.Street },
        //                    { "zipCode", contact.ZipCode }
        //                };
        //            response.data = dic;
        //            return Request.CreateResponse(HttpStatusCode.OK, response);
        //        }
        //        else
        //        {
        //            response.error = true;
        //            response.message = ResponseKeys.contactDetailsNotFound;
        //            return Request.CreateResponse(HttpStatusCode.OK, response);
        //        }
        //    }
        //}

        //[HttpGet]
        //public HttpResponseMessage getAgreementTypes()
        //{

        //    using (CangooEntities context = new CangooEntities())
        //    {
        //        var agreement = context.AgreementTypes.Where(at => at.ApplicationUserTypeID == (int)ApplicationUserTypes.Captain
        //        && at.ApplicationID.ToString().ToLower().Equals(ApplicationID.ToLower())
        //        ).ToList();

        //        if (agreement != null)
        //        {
        //            List<AgreementTypeModel> lst = new List<AgreementTypeModel>();

        //            foreach (var item in agreement)
        //            {
        //                lst.Add(new AgreementTypeModel
        //                {
        //                    TypeId = item.ID,
        //                    Name = item.TypeName
        //                });
        //            }
        //            response.error = false;
        //            response.message = ResponseKeys.msgSuccess;
        //            response.data = lst;
        //            return Request.CreateResponse(HttpStatusCode.OK, response);
        //        }
        //        else
        //        {
        //            response.error = true;
        //            response.message = ResponseKeys.agreementTypesNotFound;
        //            return Request.CreateResponse(HttpStatusCode.OK, response);
        //        }
        //    }
        //}

        //[HttpGet]
        //public HttpResponseMessage getAgreements(string agreementTypeId)
        //{
        //    using (CangooEntities context = new CangooEntities())
        //    {
        //        int id = int.Parse(agreementTypeId);

        //        var agreement = context.Agreements.Where(a => a.AgreementTypeID == id
        //        && a.ApplicationID.ToString().ToLower().Equals(ApplicationID.ToLower())
        //        ).ToList();
        //        if (agreement != null)
        //        {
        //            List<AgreementModel> lst = new List<AgreementModel>();

        //            foreach (var item in agreement)
        //            {
        //                lst.Add(new AgreementModel
        //                {
        //                    AgreementId = item.AgreementID,
        //                    Title = item.Name,
        //                    Description = item.Detail
        //                });
        //            }
        //            response.error = false;
        //            response.message = ResponseKeys.msgSuccess;
        //            response.data = lst;
        //            return Request.CreateResponse(HttpStatusCode.OK, response);
        //        }
        //        else
        //        {
        //            response.error = true;
        //            response.message = ResponseKeys.agreementsNotFound;
        //            return Request.CreateResponse(HttpStatusCode.OK, response);
        //        }
        //    }
        //}

        //[HttpGet]
        //public HttpResponseMessage getFAQs()
        //{

        //    using (CangooEntities context = new CangooEntities())
        //    {
        //        var faqs = context.FAQs.Where(f => f.ApplicationUserTypeID == (int)ApplicationUserTypes.Captain
        //        && f.ApplicationID.ToString().ToLower().Equals(ApplicationID.ToLower())
        //        ).ToList();

        //        if (faqs != null)
        //        {
        //            List<FAQModel> lst = new List<FAQModel>();

        //            foreach (var item in faqs)
        //            {
        //                lst.Add(new FAQModel
        //                {
        //                    FaqId = item.ID,
        //                    Question = item.Question,
        //                    Answer = item.Answer
        //                });
        //            }
        //            response.error = false;
        //            response.message = ResponseKeys.msgSuccess;
        //            response.data = lst;
        //            return Request.CreateResponse(HttpStatusCode.OK, response);
        //        }
        //        else
        //        {
        //            response.error = true;
        //            response.message = ResponseKeys.faqNotFound;
        //            return Request.CreateResponse(HttpStatusCode.OK, response);
        //        }
        //    }
        //}

        //[HttpGet]
        //public HttpResponseMessage getNewsFeed()
        //{
        //    using (CangooEntities context = new CangooEntities())
        //    {
        //        var feed = context.NewsFeeds.Where(nf => nf.ApplicationUserTypeID == (int)ApplicationUserTypes.Captain
        //        && nf.ExpiryDate > DateTime.UtcNow
        //        && nf.ApplicationID.ToString().ToLower().Equals(ApplicationID.ToLower())).ToList().OrderByDescending(nf => nf.CreationDate);
        //        if (feed != null)
        //        {
        //            List<NewsFeedModel> lst = new List<NewsFeedModel>();

        //            foreach (var item in feed)
        //            {
        //                lst.Add(new NewsFeedModel
        //                {
        //                    FeedId = item.FeedID,
        //                    ShortDescrption = item.ShortDescription,
        //                    Detail = item.Detail
        //                });
        //            }
        //            response.error = false;
        //            response.message = ResponseKeys.msgSuccess;
        //            response.data = lst;
        //            return Request.CreateResponse(HttpStatusCode.OK, response);
        //        }
        //        else
        //        {
        //            response.error = true;
        //            response.message = ResponseKeys.feedNotFound;
        //            return Request.CreateResponse(HttpStatusCode.OK, response);
        //        }
        //    }
        //}

        //[HttpGet]
        //public HttpResponseMessage getCurrentUTCDateTime()
        //{
        //    response.error = false;
        //    response.data = new Dictionary<dynamic, dynamic>
        //                    {
        //                        {"currentDateTime", Common.getUtcDateTime().ToString(Common.dateFormat) }
        //                    };
        //    response.message = ResponseKeys.msgSuccess;
        //    return Request.CreateResponse(HttpStatusCode.OK, response);
        //}

        //#endregion

        //#region HelperFunctions

        private void LogOutAlreadyLoggedInCaptain(string NewDeviceToken, string DriverID, string ExistingDeviceToken, bool isAlreadyInTrip)
        {
            //Pushy device token never changes. If user is already logged in on some other device then force logout.
            //If user was logged out (authentication failuer etc) and logging in  again then do nothing

            if (!NewDeviceToken.ToLower().Equals(ExistingDeviceToken.ToLower()))
            {
                dic = new Dictionary<dynamic, dynamic>
                                {
                                    { "driverID", DriverID },
                                    { "deviceToken", ExistingDeviceToken },
                                    { "isAlreadyInTrip", isAlreadyInTrip }
                                };

                //If already in trip, then no need to go offline and free up the vehicle.
                if (!isAlreadyInTrip)
                {
                    //free already booked vehicle
                    DriverService.OnlineOfflineDriver(DriverID, "", false, Guid.Parse(ApplicationID));
                }
            }
        }

        //public void ApplyPromoAdjustWalletUpdateVoucherAmount(string voucherUsedAmount, string walletUsedAmount, string promoDiscountAmount, Trip trip, CangooEntities context)
        //{
        //    //If voucher is applied - Wallet and PromoDiscount can't be applied

        //    if (Convert.ToDecimal(voucherUsedAmount) == 0)
        //    {
        //        Common.RefundFullVoucherAmount(trip, context);
        //    }

        //    if (Convert.ToDecimal(voucherUsedAmount) > 0)
        //    {
        //        trip.PromoDiscount = 0;
        //        trip.WalletAmountUsed = 0;

        //        var voucher = context.CompanyVouchers.Where(cv => cv.VoucherID == trip.VoucherID && cv.isUsed == false).FirstOrDefault();
        //        if (voucher != null)
        //        {
        //            //Add extra voucher amount back to company balance.
        //            if (voucher.Amount > Convert.ToDecimal(voucherUsedAmount))
        //            {
        //                var company = context.Companies.Where(c => c.CompanyID == voucher.CompanyID).FirstOrDefault();
        //                company.CompanyBalance += (voucher.Amount - Convert.ToDecimal(voucherUsedAmount));
        //                voucher.Amount = Convert.ToDecimal(voucherUsedAmount);
        //            }
        //            voucher.isUsed = true;
        //        }
        //    }
        //    else
        //    {
        //        trip.VoucherID = null;

        //        trip.PromoDiscount = Convert.ToDecimal(promoDiscountAmount);

        //        if (Convert.ToDecimal(promoDiscountAmount) > 0)
        //        {
        //            var userPromo = context.UserPromos.Where(up => up.PromoID.ToString().Equals(trip.PromoCodeID.ToString())
        //            && up.UserID.ToString().ToLower().Equals(trip.UserID.ToString().ToLower())
        //            && up.isActive == true).FirstOrDefault();
        //            if (userPromo != null)
        //            {
        //                userPromo.NoOfUsage += 1;
        //            }
        //        }
        //    }

        //    trip.WalletAmountUsed = Convert.ToDecimal(walletUsedAmount);

        //    if (Convert.ToDecimal(walletUsedAmount) > 0)
        //    {
        //        var user = context.UserProfiles.Where(up => up.UserID.Equals(trip.UserID.ToString())).FirstOrDefault();
        //        if (user != null)
        //        {
        //            user.WalletBalance -= Convert.ToDecimal(walletUsedAmount);
        //        }
        //    }

        //    //TBD: Check if any of the amounts is > 0, only then save db changes

        //    context.SaveChanges();
        //}

        //public bool CheckIfAlreadyPaid(string totalFare, string tripID, string driverID, ref Dictionary<dynamic, dynamic> dic, bool isWalkIn)
        //{
        //    using (CangooEntities context = new CangooEntities())
        //    {
        //        var trip = context.Trips.Where(t => t.TripID.ToString().Equals(tripID)).FirstOrDefault();
        //        if (trip == null)
        //            return false;

        //        if (trip.TripStatusID == (int)TripStatuses.Completed)
        //        {
        //            if (!isWalkIn)
        //            {
        //                FireBaseController fb = new FireBaseController();
        //                fb.fareAlreadyPaidFreeUserAndDriver(tripID, trip.UserID.ToString(), driverID);

        //                dic = new Dictionary<dynamic, dynamic>
        //                    {
        //                        { "tripID", trip.TripID.ToString() },
        //                        { "tip", trip.Tip == null ? "0.00" : trip.Tip.ToString() },
        //                        { "amount", string.Format("{0:0.00}", Convert.ToDouble(totalFare)) }
        //                    };
        //            }

        //            return true;
        //        }
        //        else
        //        {
        //            return false;
        //        }
        //    }
        //}

        //private void CheckWalletBalance(string userID, CangooEntities context, ref Dictionary<dynamic, dynamic> dic)
        //{
        //    var user = context.UserProfiles.Where(u => u.UserID.ToString().Equals(userID)).FirstOrDefault();
        //    //When ride was booked by some hotel / company 
        //    if (user != null)
        //    {
        //        dic.Add("isWalletPreferred", user.isWalletPreferred);

        //        if (user.WalletBalance != null)
        //            dic.Add("availableWalletBalance", string.Format("{0:0.00}", (decimal)user.WalletBalance));
        //        else
        //            dic.Add("availableWalletBalance", string.Format("{0:0.00}", 0));
        //    }
        //    else
        //    {
        //        dic.Add("isWalletPreferred", false);
        //        dic.Add("availableWalletBalance", string.Format("{0:0.00}", 0));
        //    }
        //}

        //private PassengerRequest GetCancelledTripRequestObject(RequestModel req, spCaptainCancelRide_Result tp)
        //{
        //    PassengerRequest pr = new PassengerRequest
        //    {
        //        pickUplatitude = tp.PickupLocationLatitude,
        //        pickUplongitude = tp.PickupLocationLongitude,
        //        pickUpLocation = tp.PickUpLocation,
        //        dropOfflatitude = tp.DropOffLocationLatitude,
        //        dropOfflongitude = tp.DropOffLocationLongitude,
        //        dropOffLocation = tp.DropOffLocation,
        //        pID = tp.UserID.ToString(),
        //        selectedPaymentMethod = tp.TripPaymentMode,//Enum.GetName(typeof(ResellerPaymentModes), tp.TripPaymentMode),
        //        timeZoneOffset = "0", //Will not be considered, in case of later booking reroute, PickUpBookingDateTime is being fetched from db
        //        resellerArea = req.resellerArea,
        //        isLaterBooking = req.isLaterBooking,
        //        laterBookingDate = tp.PickUpBookingDateTime.ToString(),
        //        isReRouteRequest = true,
        //        tripID = tp.TripID.ToString(),
        //        seatingCapacity = tp.NoOfPerson.ToString(),
        //        driverID = req.driverID,
        //        requiredFacilities = tp.facilities,
        //        deviceToken = tp.DeviceToken
        //    };

        //    string path = "Trips/" + req.tripID.ToString() + "/discount";

        //    client = new FireSharp.FirebaseClient(config);
        //    FirebaseResponse resp = client.Get(path);

        //    if (!string.IsNullOrEmpty(resp.Body) && !resp.Body.Equals("null"))
        //    {
        //        Dictionary<string, dynamic> discountDetails = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(resp.Body);
        //        pr.discountType = discountDetails["type"].ToString();
        //        pr.promoDiscountAmount = discountDetails["amount"].ToString();
        //    }

        //    return pr;
        //}

        private LaterBookingConflict checkLaterBookingDate(string captainID, DateTime pickUpDateTime)
        {
            using (CangooEntities context = new CangooEntities())
            {
                LaterBookingConflict lbc = new LaterBookingConflict();
                var lstLaterTrips = context.Trips.Where(t => t.CaptainID.ToString() == captainID && t.TripStatusID == (int)TripStatuses.LaterBookingAccepted && t.isLaterBooking == true).ToList();
                foreach (var item in lstLaterTrips)
                {
                    DateTime sDateTime = Convert.ToDateTime(item.PickUpBookingDateTime).AddHours(-1);
                    DateTime eDateTime = Convert.ToDateTime(item.PickUpBookingDateTime).AddHours(1);
                    if (pickUpDateTime >= sDateTime && pickUpDateTime <= eDateTime)
                    {
                        lbc.pickUpDateTime = item.PickUpBookingDateTime.ToString();
                        lbc.isConflict = true;
                        break;
                    }
                    else
                    {
                        lbc.isConflict = false;
                    }
                }
                return lbc;
            }
        }

        //private void SendInvoice(InvoiceModel model)
        //{
        //    var headerLink = this.Url.Link("Default", new { Controller = "Invoice", Action = "Header" });
        //    var footerLink = this.Url.Link("Default", new { Controller = "Invoice", Action = "Footer" });

        //    System.Web.Routing.RouteData route = new System.Web.Routing.RouteData();
        //    route.Values.Add("action", "SendInvoice");
        //    route.Values.Add("controller", "Invoice");

        //    InvoiceController controllerObj = new InvoiceController();
        //    System.Web.Mvc.ControllerContext newContext = new System.Web.Mvc.ControllerContext(new HttpContextWrapper(System.Web.HttpContext.Current), route, controllerObj);
        //    controllerObj.ControllerContext = newContext;

        //    controllerObj.SendInvoice(model, headerLink, footerLink);
        //}

        #endregion
    }
}