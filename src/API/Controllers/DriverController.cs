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
using System.Data.Entity;
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
    [Authorize]
    [RoutePrefix("api/Driver")]
    public class DriverController : BaseController
    {
        ResponseEntity response = new ResponseEntity();
        Dictionary<dynamic, dynamic> dic;

        #region Authentication Flow

        [HttpPost]
        [AllowAnonymous]
        [Route("phoneVerification")]    //Register Captain UI
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
        [AllowAnonymous]
        [Route("setNewPassword")]   //Set password after phone verification for first time login
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
        [Route("loginDriver")]
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
                            DriverLoginResponse captainModel = new DriverLoginResponse
                            {
                                userID = captain.CaptainID.ToString(),
                                UserName = model.UserName,
                                phone = user.PhoneNumber,
                                Name = captain.Name,
                                EarningPoints = captain.EarningPoints == null ? "0.0" : captain.EarningPoints.ToString(),
                                Email = captain.Email,
                                IsPriorityHoursActive = captain.IsPriorityHoursActive != true ? false : true,
                                priorityHourEndTime = captain.LastPriorityHourEndTime != null ? DateTime.Parse(captain.LastPriorityHourEndTime.ToString()).ToString(Formats.DateTimeFormat) : "",
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

        [HttpPost]
        [AllowAnonymous]
        [Route("resetPassword")]//Forgot Password UI
        public async Task<HttpResponseMessage> resetPassword([FromBody] DriverResetPasswordRequest model)
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

        [HttpGet]
        [Route("getVehicleList")]
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
        [Route("changeVehicleStatus")]
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
                    vh.Facilities, cap.Facilities, cap.DeviceToken, cap.UserName, cap.PhoneNumber, vh.ModelID, vh.PlateNumber, vh.Category, vh.Categories,
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
        [Route("logOutDriver")]
        public async Task<HttpResponseMessage> logOutDriver([FromBody] DriverLogOutRequest model)
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

        [HttpPost]          //Change Password UI
        [Route("chagePassword")]
        public async Task<HttpResponseMessage> chagePassword([FromBody] DriverChangePasswordRequest model)
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

        [HttpPost]
        [Route("verifyDeviceToken")]
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

        [HttpGet]
        [Route("tokenValidation")]
        public HttpResponseMessage tokenValidation()
        {
            return Request.CreateResponse(HttpStatusCode.OK);
        }
        #endregion

        #region Booked Ride Scenario

        [HttpPost]      //accept pending later booking
        [Route("acceptLateBooking")]
        public async Task<HttpResponseMessage> acceptLateBooking([FromBody] DriverAcceptLaterBookingRequest model)
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
                                    { "alreadyBooked", DateTime.Parse(conf.pickUpDateTime).ToString(Formats.DateTimeFormat) }
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

        [HttpPost]      //Start later booking / accept normal request
        [Route("acceptRequest")]
        public async Task<HttpResponseMessage> acceptRequest([FromBody] DriverAcceptTripRequest model)
        {
            //int bookingStatus = 0;

            ////Get trip current status before any status change
            //if (model.isLaterBooking)
            //{
            //    bookingStatus = (int)TripStatuses.LaterBookingAccepted;
            //}
            //else
            //{
            //    if (model.isDispatchedRide.ToLower().Equals("true"))
            //        bookingStatus = (int)TripStatuses.OnTheWay;
            //    else if (model.isReRouteRequest)
            //        bookingStatus = (int)TripStatuses.Cancel;
            //    else
            //        bookingStatus = (int)TripStatuses.RequestSent;
            //}

            //In case of dispatched ride save id and device token of previous captain for logging purpose.
            string previousDriverId = "";
            string previousDriverDeviceToken = "";

            var trip = await TripsManagerService.GetTripById(model.tripID);// context.Trips.Where(t => t.TripID.ToString().Equals(model.tripID)).FirstOrDefault();
            if (model.isDispatchedRide.ToLower().Equals("true"))
            {
                previousDriverId = trip.CaptainID.ToString();
                previousDriverDeviceToken = await DriverService.GetDriverDeviceToken(trip.CaptainID.ToString());
            }

            //In case of laterbooking the flag is not maintained appropriately

            model.isWeb = trip.BookingModeID == (int)BookingModes.UserApplication ? false : true;

            //Update trip status in database
            var detail = await DriverService.GetUpdatedTripDataOnAcceptRide(model.tripID, model.driverID, model.vehicleID, model.fleetID, model.isLaterBooking == true ? 1 : 0);

            if (detail != null)
            {
                var driverCancelReasons = await CancelReasonsService.GetDriverCancelReasons(!model.isLaterBooking, model.isLaterBooking, true);

                //Object to be used to populate driver firebase node
                AcceptRideDriverModel arm = new AcceptRideDriverModel
                {
                    tripID = model.tripID,
                    isWeb = model.isWeb,
                    isLaterBooking = model.isLaterBooking,
                    isDispatchedRide = model.isDispatchedRide,

                    pickupLocationLatitude = detail.PickupLocationLatitude,
                    pickupLocationLongitude = detail.PickupLocationLongitude,
                    pickUpLocation = detail.PickUpLocation,
                    midwayStop1Location = detail.MidwayStop1Location,
                    midwayStop1LocationLatitude = detail.MidwayStop1Latitude,
                    midwayStop1LocationLongitude = detail.MidwayStop1Longitude,
                    dropOffLocationLatitude = detail.dropoffLocationLatitude,
                    dropOffLocationLongitude = detail.dropofflocationLongitude,
                    dropOffLocation = detail.DropOffLocation,
                    vehicleCategory = detail.VehicleCategory,
                    totalFare = detail.TotalFare.ToString(),
                    requestTime = detail.RequestWaitingTime,

                    //UPDATE: After reverting google directions API, detail.ArrivedTime is distance in Meters - To enable arrived button on captain app before reaching pickup location

                    minDistance = detail.ArrivedTime,
                    passengerID = detail.UserID.ToString(),
                    passengerName = detail.PassengerName,
                    phone = detail.PhoneNumber,
                    lstCancel = driverCancelReasons,
                    laterBookingPickUpDateTime = Convert.ToDateTime(detail.PickUpBookingDateTime).ToString(Formats.DateTimeFormat),
                    distanceTraveled = "0.00",
                    isReRouteRequest = detail.isReRouted.ToString(),
                    description = detail.description,
                    voucherCode = detail.VoucherCode,
                    voucherAmount = detail.VoucherAmount.ToString(),
                    isFareChangePermissionGranted = "false",

                    numberOfPerson = trip.NoOfPerson.ToString(),
                    bookingMode = trip.BookingModeID == (int)BookingModes.Karhoo ? Enum.GetName(typeof(BookingModes), (int)BookingModes.Karhoo).ToLower() : ""
                };

                await FirebaseService.SetDriverBusy(model.driverID, model.tripID);
                await FirebaseService.SetEstimateDistanceToPickUpLocation(model.driverID, string.IsNullOrEmpty(model.distanceToPickUpLocation) ? "0" : model.distanceToPickUpLocation);

                var fd = await FirebaseService.GetOnlineDriverById(model.driverID);
                var driverVehiclDetail = await DriverService.GetDriverVehicleDetail(model.driverID, fd == null ? "" : fd.vehicleID, detail.UserID.ToString(), model.isWeb);

                await FirebaseService.WriteTripDriverDetails(arm, model.driverID);

                var fbPassenger = await FirebaseService.GetTripPassengerDetails(model.tripID, trip.UserID.ToString());

                //Object to be used to populate passenger FCM and firebase node data

                var notificationPayLoad = new PassengerRequestAcceptedNotification
                {
                    TripId = model.tripID,
                    IsWeb = model.isWeb.ToString(),
                    DriverId = model.driverID,

                    IsLaterBooking = arm.isLaterBooking.ToString(),
                    IsDispatchedRide = arm.isDispatchedRide,
                    IsReRouteRequest = arm.isReRouteRequest,
                    PickUpDateTime = arm.laterBookingPickUpDateTime,
                    Description = arm.description,
                    VoucherCode = arm.voucherCode,
                    VoucherAmount = arm.voucherAmount,
                    TotalFare = arm.totalFare,
                    PickUpLatitude = arm.pickupLocationLatitude,
                    PickUpLongitude = arm.pickupLocationLongitude,
                    PickUpLocation = arm.pickUpLocation,
                    MidwayStop1Latitude = arm.midwayStop1LocationLatitude,
                    MidwayStop1Longitude = arm.midwayStop1LocationLongitude,
                    MidwayStop1Location = arm.midwayStop1Location,
                    DropOffLatitude = arm.dropOffLocationLatitude,
                    DropOffLongitude = arm.dropOffLocationLongitude,
                    DropOffLocation = arm.dropOffLocation,
                    SeatingCapacity = arm.numberOfPerson,
                    VehicleCategory = arm.vehicleCategory,
                    VehicleCategoryId = detail.VehicleCategoryId.ToString(),

                    VehicleNumber = driverVehiclDetail.PlateNumber,
                    DriverName = driverVehiclDetail.Name.Split(' ')[0],
                    DriverContactNumber = driverVehiclDetail.ContactNumber,
                    DriverRating = driverVehiclDetail.Rating.ToString(),
                    DriverPicture = driverVehiclDetail.Picture,
                    VehicleRating = driverVehiclDetail.vehicleRating.ToString(),
                    Make = driverVehiclDetail.Make,
                    Model = driverVehiclDetail.Model,// + " " + driverVehiclDetail.PlateNumber,
                    Color= driverVehiclDetail.Color,
                    FleetAddress= driverVehiclDetail.FleetAddress,
                    FleetName = driverVehiclDetail.FleetName,

                    PaymentModeId = fbPassenger.PaymentModeId,
                    PaymentMethod = fbPassenger.PaymentMethod,
                    CustomerId = fbPassenger.CustomerId,
                    CardId = fbPassenger.CardId,
                    Brand = fbPassenger.Brand,
                    Last4Digits = fbPassenger.Last4Digits,
                    WalletBalance = fbPassenger.WalletBalance,
                    AvailableWalletBalance = fbPassenger.AvailableWalletBalance,

                    CancelReasons = await CancelReasonsService.GetPassengerCancelReasons(!arm.isLaterBooking, arm.isLaterBooking, false),
                    Facilities = await FacilitiesService.GetPassengerFacilitiesDetailByIds(trip.facilities)
                };

                await FirebaseService.UpdateTripPassengerDetailsOnAccepted(notificationPayLoad, detail.UserID.ToString());

                await FirebaseService.SetTripStatus(model.tripID, Enum.GetName(typeof(TripStatuses), TripStatuses.OnTheWay));

                if (!model.isWeb)
                {
                    await PushyService.UniCast(driverVehiclDetail.DeviceToken, notificationPayLoad, NotificationKeys.pas_rideAccepted);
                }

                if (model.isDispatchedRide.ToLower().Equals("true"))
                {
                    await FirebaseService.SendNotificationsAfterDispatchingRide(previousDriverDeviceToken, previousDriverId, model.tripID);

                    //TBD: Log previous captain priority points
                    await TripsManagerService.LogDispatchedTrips(new DispatchedRideLogDTO
                    {
                        DispatchedBy = Guid.Parse(model.dispatcherID),
                        CaptainID = Guid.Parse(previousDriverId),
                        DispatchLogID = Guid.NewGuid(),
                        LogTime = DateTime.UtcNow,
                        TripID = Guid.Parse(model.tripID)
                    });

                    var cap = await DriverService.GetDriverById(previousDriverId);
                    if (!(bool)cap.IsPriorityHoursActive)
                    {
                        //TBD: Fetch distance traveled from driver node and update points accordingly.

                        await FirebaseService.UpdateDriverEarnedPoints(previousDriverId, cap.EarningPoints.ToString());
                    }
                }

                //API response data
                dic = new Dictionary<dynamic, dynamic>
                                    {
                                        { "lat", detail.PickupLocationLatitude },
                                        { "lon", detail.PickupLocationLongitude },
                                        { "passengerID", detail.UserID },
                                        { "minDistance", detail.ArrivedTime },
                                        { "requestTime", detail.RequestWaitingTime },
                                        { "phone", detail.PhoneNumber },
                                        { "tripID", model.tripID },
                                        { "isWeb", model.isWeb },
                                        { "laterBookingPickUpDateTime", Convert.ToDateTime(detail.PickUpBookingDateTime).ToString(Formats.DateTimeFormat) },
                                        { "isLaterBooking", model.isLaterBooking },
                                        { "cancel_reasons", driverCancelReasons },
                                        { "passengerName",  detail.PassengerName},
                                        { "bookingMode", arm.bookingMode}
                                    };

                //In case of later booking update captain UpcomingLaterBookings
                if (model.isLaterBooking)
                {
                    await FirebaseService.DeleteUpcomingLaterBooking(model.driverID);

                    Dictionary<string, dynamic> dic = new Dictionary<string, dynamic>
                                                                    {
                                                                        { "lstCancel", driverCancelReasons }
                                                                    };

                    /*VERIFY:
                     * If setting firebase node removes existing data which is not avaiable in new data then discount node should remove here.
                     */

                    /*REFACTOR
                     * If trip node is not delted on accepting pending later booking then
                     */

                    //Writer later booking on firebase in Trips node to deal ride flow as normal booking
                    await FirebaseService.SetTripCancelReasonsForPassenger(model.tripID, detail.UserID.ToString(), dic);

                    //NEW IMPLEMENTATION(No need to reset / recheck reset upcoming later bookings for all drivers, just adjust upcoming of current driver)

                    await FirebaseService.AddUpcomingLaterBooking(model.driverID, await DriverService.GetUpcomingLaterBooking(model.driverID));

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

        [HttpPost]
        [Route("cancelRide")]
        public async Task<HttpResponseMessage> cancelRide([FromBody] DriverCancelTripRequest model, HttpRequestMessage request = null)
        {
            using (CangooEntities context = new CangooEntities())
            {
                //TBD: Add new trip status Cancel
                var tp = context.spCaptainCancelRide(model.tripID, model.driverID, (int)TripStatuses.Cancel, model.cancelID, model.isWeb, bool.Parse(model.isAtPickupLocation), DateTime.UtcNow).FirstOrDefault();

                if (tp == null)
                {
                    response.error = true;
                    response.message = ResponseKeys.tripNotFound;
                    return Request.CreateResponse(HttpStatusCode.OK, response);
                }

                if ((bool)tp.isCaptainArrived)
                {
                    if (tp.PaymentModeId == (int)PaymentModes.CreditCard)
                    {
                        await PaymentsServices.CancelAuthorizedPayment(tp.CreditCardPaymentIntent);
                    }
                    else if (tp.PaymentModeId == (int)PaymentModes.Wallet)
                    {
                        await PaymentsServices.ReleaseWalletScrewedAmount(tp.UserID.ToString(), await FareManagerService.GetTripCalculatedFare(tp.TripID.ToString()));
                        context.SaveChanges();
                    }
                    else if (tp.BookingModeID == (int)BookingModes.Voucher)
                    {
                        //VoucherService.RefundFullVoucherAmount(tp);
                    }
                }

                //Regardless of ride status (Accepted / On The Way / Arrived), if it is cancelled at least 2 min before pick up time 
                //then request should be rerouted as new later booking

                model.isLaterBooking = (bool)tp.isLaterBooking ?
                        (((DateTime)tp.PickUpBookingDateTime) - DateTime.UtcNow).TotalMinutes > 2 ? true : false
                    : false;

                //From app laterbooking of less than 5 min is not allowed, but rerouted later booking can be of at least 2 min.
                if (model.isLaterBooking)
                {
                    //Step 1: Refresh pending later bookings (not accepted by any driver) node on firebase
                    await FirebaseService.AddPendingLaterBookings(tp.UserID.ToString(), tp.TripID.ToString(), ((DateTime)tp.PickUpBookingDateTime).ToString(Formats.DateTimeFormat), tp.NoOfPerson.ToString());

                    //Step 2: Refresh current driver upcoming later booking on firebase
                    await FirebaseService.DeleteUpcomingLaterBooking(model.driverID);
                    await FirebaseService.AddUpcomingLaterBooking(model.driverID, await DriverService.GetUpcomingLaterBooking(model.driverID));
                }

                await FirebaseService.SetDriverFree(model.driverID, model.tripID);
                await FirebaseService.UpdateDriverEarnedPoints(model.driverID, tp.EarningPoints.ToString());

                dic = new Dictionary<dynamic, dynamic>
                        {
                            { "tripID", model.tripID },
                            { "isLaterBooking", model.isLaterBooking }
                        };

                //in normal booking if captain is arrived and cancels the ride then don't reroute
                //in later booking if less than or equal to 2 min are remainig, captain cancels trip after arrival then don't reroute

                if (!model.isLaterBooking && tp.ArrivalDateTime != null)
                {
                    await FirebaseService.FreePassengerFromCurrentTrip(tp.TripID.ToString(), tp.UserID.ToString());

                    await PushyService.UniCast(tp.DeviceToken,
                        new PassengerCancelRequestNotification { TripId = model.tripID, IsLaterBooking = model.isLaterBooking.ToString()},
                        NotificationKeys.pas_rideCancel);

                    await FirebaseService.DeleteTrip(tp.TripID.ToString());
                }
                else
                {
                    //Reroute Request
                    response.data = (await TripsManagerService.BookNewTrip(await TripsManagerService.GetCancelledTripRequestObject(model, tp))).Data;
                }

                response.error = false;
                response.data = dic;
                response.message = ResponseKeys.msgSuccess;

                //If later booking was over due and cancelled by cron-job (if driver app was not active)
                if (Request == null)
                    return request.CreateResponse(HttpStatusCode.OK, response);
                else
                    return Request.CreateResponse(HttpStatusCode.OK, response);
            }
        }

        [HttpPost]
        [Route("driverArrived")]
        public async Task<HttpResponseMessage> driverArrived([FromBody] DriverArrivedRequest model)
        {
            using (CangooEntities context = new CangooEntities())
            {
                var estDist = await FirebaseService.GetTripEstimatedDistanceOnArrival(model.driverID);

                //Update earned points in case of normal booking only.
                var trip = context.spGetUpdateTripOnCaptainArrived(DateTime.UtcNow,
                    (double.Parse(model.distanceToPickUpLocation) <= estDist ? double.Parse(model.distanceToPickUpLocation) : estDist) / 100,
                    model.tripID,
                    model.driverID,
                    model.isWeb).FirstOrDefault();

                if (trip != null)
                {
                    ArrivedDriverRideModel adr = new ArrivedDriverRideModel()
                    {
                        passengerName = trip.Name,
                        passengerRating = trip.Rating,
                        dropOffLatitude = trip.DropOffLocationLatitude,
                        dropOffLongitude = trip.DropOffLocationLongitude,
                        passenger_Pic = trip.pic,
                        //bookingMode = trip.BookingModeID == (int)TripBookingMod.Karhoo ? Enum.GetName(typeof(TripBookingMod), (int)App_Start.TripBookingMod.Karhoo).ToLower() : ""
                        bookingMode = Enum.GetName(typeof(BookingModes), (int)trip.BookingModeID).ToLower()
                    };

                    //NEW IMPLEMENTATION

                    await FirebaseService.UpdateTripDriverDetailsOnArrival(adr, model.tripID, model.driverID, trip.EarningPoints.ToString());
                    await FirebaseService.SetTripStatus(model.tripID, Enum.GetName(typeof(TripStatuses), (int)TripStatuses.Arrived));

                    if (!model.isWeb)
                    {
                        await PushyService.UniCast(trip.DeviceToken, new Dictionary<dynamic, dynamic>
                            {
                                { "driverID", model.driverID }
                            }, NotificationKeys.pas_driverReached);
                    }

                    dic = new Dictionary<dynamic, dynamic>
                            {
                                { "Name", trip.Name },
                                { "Rating", trip.Rating },
                                { "dropOffLat", trip.DropOffLocationLatitude != null ? trip.DropOffLocationLatitude : "" },
                                { "dropOffLon", trip.DropOffLocationLongitude != null ? trip.DropOffLocationLongitude : "" },
                                { "passenger_Pic", trip.pic },
                                { "cancel_reasons", await CancelReasonsService.GetDriverCancelReasons(true, false, true)},
                                { "bookingMode", adr.bookingMode}
                            };

                    response.error = false;
                    response.message = ResponseKeys.msgSuccess;
                    response.data = dic;
                    return Request.CreateResponse(HttpStatusCode.OK, response);
                }
                else
                {
                    response.error = true;
                    response.message = ResponseKeys.tripNotFound;
                    return Request.CreateResponse(HttpStatusCode.OK, response);
                }
            }
        }

        [HttpPost]
        [Route("startTrip")]
        public async Task<HttpResponseMessage> startTrip([FromBody] DriverStartTripRequest model)
        {
            using (CangooEntities context = new CangooEntities())
            {
                var trip = context.spGetUserUpdateTripOnStartRide(DateTime.UtcNow, model.tripID, model.driverID, model.isWeb).FirstOrDefault();
                if (trip != null)
                {
                    startDriverRideModel sdr = new startDriverRideModel()
                    {
                        dropOffLatitude = trip.DropOffLocationLatitude,
                        dropOffLongitude = trip.DropOffLocationLongitude,
                        //bookingMode = trip.BookingModeID == (int)TripBookingMod.Karhoo ? Enum.GetName(typeof(TripBookingMod), (int)App_Start.TripBookingMod.Karhoo).ToLower() : ""
                        bookingMode = Enum.GetName(typeof(BookingModes), (int)trip.BookingModeID).ToLower()
                    };

                    dic = new Dictionary<dynamic, dynamic>
                                {
                                    { "destination_lat", trip.DropOffLocationLatitude },
                                    { "destination_lon", trip.DropOffLocationLongitude },
                                    { "passengerName",  trip.Name},
                                    { "bookingMode", sdr.bookingMode}
                                };

                    await FirebaseService.UpdateTripDriverDetailsOnStart(sdr, model.tripID, model.driverID);
                    await FirebaseService.SetTripStatus(model.tripID, Enum.GetName(typeof(TripStatuses), (int)TripStatuses.Picked));
                    //await FirebaseService.updateGo4Module(trip.CaptainName, trip.Name, trip.Go4ModuleDeviceToken);

                    if (!model.isWeb)
                    {
                        await PushyService.UniCast(trip.DeviceToken, new Dictionary<dynamic, dynamic>
                            {
                                 { "driverID", model.driverID }
                            }, NotificationKeys.pas_rideStarted);
                    }

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
        }

        [HttpPost]
        [Route("endTrip")]
        public async Task<HttpResponseMessage> endTrip([FromBody] DriverEndTripRequest model)
        {
            using (CangooEntities context = new CangooEntities())
            {
                var trip = context.Trips.Where(t => t.TripID.ToString() == model.tripID && t.CaptainID.ToString() == model.driverID).FirstOrDefault();

                //Pickup and Drop off details are already set.
                //DistanceTraveled and DropOff details are already set, should be updated in this API, but in that case polyline should be updated as well

                if (trip != null)
                {
                    //Step 1 : Set WaitingFare (PerKm, BaseFare, BookingFare already set at booking time)

                    decimal totalFare = await FareManagerService.GetTripCalculatedFare(model.tripID);

                    DriverEndTripResponse result = new DriverEndTripResponse();

                    //get waiting time
                    DateTime arrivalTime = model.isLaterBooking ? Convert.ToDateTime(trip.PickUpBookingDateTime) : Convert.ToDateTime(trip.ArrivalDateTime);
                    DateTime startTime = Convert.ToDateTime(trip.TripStartDatetime);
                    TimeSpan waitingDur = startTime.Subtract(arrivalTime);

                    //in case of later booking if driver arrived and started ride before scheduled pickup time then no waiting charges
                    if (waitingDur.Seconds <= 0)
                    {
                        waitingDur = new TimeSpan();
                    }

                    trip.TripEndDatetime = DateTime.UtcNow;//utc date time
                    trip.WaitingMinutes = waitingDur.TotalMinutes;

                    //Step 2 : Check if sepcial promotion fare was applied then no need to apply promo code else calculate and set the discount amount

                    //if (trip.isSpecialPromotionApplied == false && trip.PromoCodeID != null)
                    //{
                    //    var promotionDetails = PromoCodeService.GetUserPromoDiscountAmount(ApplicationID, trip.UserID.ToString(), trip.PromoCodeID.ToString());

                    //    if (promotionDetails.DiscountType.Equals(Enum.GetName(typeof(DiscountTypes), (int)DiscountTypes.Fixed).ToLower()))
                    //        trip.PromoDiscount = decimal.Parse(promotionDetails.DiscountAmount);

                    //    if (promotionDetails.DiscountType.Equals(Enum.GetName(typeof(DiscountTypes), (int)DiscountTypes.Percentage).ToLower()))
                    //    {
                    //        trip.PromoDiscount = (totalFare * decimal.Parse(promotionDetails.DiscountAmount)) / 100.0M;
                    //    }

                    //    result.discountType = promotionDetails.DiscountType;
                    //    result.discountAmount = promotionDetails.DiscountAmount;
                    //}

                    result.estimatedPrice = string.Format("{0:0.00}", totalFare);

                    //Step 3 : Update earned points, priority hour and booking type check applied

                    var captain = context.Captains.Where(c => c.CaptainID == trip.CaptainID).FirstOrDefault();

                    if ((bool)trip.isLaterBooking)
                    {
                        if (captain.IsPriorityHoursActive != null)
                        {
                            if (!(bool)captain.IsPriorityHoursActive)
                            {
                                trip.DriverEarnedPoints = 50;
                                captain.EarningPoints = captain.EarningPoints == null ? 50 :
                                    captain.EarningPoints + 50 <= 300 ? captain.EarningPoints + 50 : 300;
                            }
                        }
                        else
                        {
                            trip.DriverEarnedPoints = 50;
                            captain.EarningPoints = captain.EarningPoints == null ? 50 :
                                captain.EarningPoints + 50 <= 300 ? captain.EarningPoints + 50 : 300;
                        }
                    }

                    await FirebaseService.UpdateDriverEarnedPoints(captain.CaptainID.ToString(), captain.EarningPoints.ToString());

                    //Step 4 : Send notifications and update firebase accordingly

                    DateTime startRideTime = Convert.ToDateTime(trip.TripStartDatetime);
                    DateTime endRideTime = Convert.ToDateTime(DateTime.UtcNow);//utc date time
                    TimeSpan totalRideDuration = endRideTime.Subtract(startRideTime);

                    //Object to update data in trip's driver node
                    EndDriverRideModel edr = new EndDriverRideModel
                    {
                        travelCharges = "0.00",// string.Format("{0:0.00}", result.discountType.Equals(Enum.GetName(typeof(DiscountTypes), (int)DiscountTypes.Special).ToLower()) ? "0.00" : trip.PerKMFare.ToString()),
                        waitingCharges = "0.00",//  string.Format("{0:0.00}", result.discountType.Equals(Enum.GetName(typeof(DiscountTypes), (int)DiscountTypes.Special).ToLower()) ? "0.00" : trip.WaitingFare.ToString()),
                        bookingCharges = "0.00",//  string.Format("{0:0.00}", trip.BookingFare.ToString()),
                        baseCharges = "0.00",//  string.Format("{0:0.00}", trip.BaseFare.ToString()),
                        estimatedPrice = string.Format("{0:0.00}", result.estimatedPrice),
                        paymentMethod = trip.TripPaymentMode,
                        paymentModeId = trip.PaymentModeId.ToString(),
                        distance = string.Format("{0:0.00}", trip.DistanceTraveled.ToString()),
                        duration = totalRideDuration.TotalMinutes,
                        isPaymentRequested = false,
                        isFavUser = false,// userFav == null ? false : (userFav.IsFavByCaptain == null ? false : (bool)userFav.IsFavByCaptain),
                        discountAmount = result.discountAmount,
                        discountType = result.discountType,
                        availableWalletBalance = result.availableWalletBalance,
                        isWalletPreferred = result.isWalletPreferred.ToString(),
                        isFareChangePermissionGranted = (bool)trip.isFareChangePermissionGranted,

                        InBoundDistanceInMeters = string.Format("{0:0.00}", result.discountType.Equals(Enum.GetName(typeof(DiscountTypes), (int)DiscountTypes.Special).ToLower()) ? "0.00" : trip.InBoundDistanceInMeters.ToString()),
                        OutBoundDistanceInMeters = string.Format("{0:0.00}", result.discountType.Equals(Enum.GetName(typeof(DiscountTypes), (int)DiscountTypes.Special).ToLower()) ? "0.00" : trip.OutBoundDistanceInMeters.ToString()),

                        InBoundTimeInSeconds = string.Format("{0:0.00}", result.discountType.Equals(Enum.GetName(typeof(DiscountTypes), (int)DiscountTypes.Special).ToLower()) ? "0.00" : trip.InBoundTimeInSeconds.ToString()),
                        OutBoundTimeInSeconds = string.Format("{0:0.00}", result.discountType.Equals(Enum.GetName(typeof(DiscountTypes), (int)DiscountTypes.Special).ToLower()) ? "0.00" : trip.OutBoundTimeInSeconds.ToString()),

                        InBoundDistanceFare = string.Format("{0:0.00}", result.discountType.Equals(Enum.GetName(typeof(DiscountTypes), (int)DiscountTypes.Special).ToLower()) ? "0.00" : trip.InBoundDistanceFare.ToString()),
                        OutBoundDistanceFare = string.Format("{0:0.00}", result.discountType.Equals(Enum.GetName(typeof(DiscountTypes), (int)DiscountTypes.Special).ToLower()) ? "0.00" : trip.OutBoundDistanceFare.ToString()),

                        InBoundTimeFare = string.Format("{0:0.00}", result.discountType.Equals(Enum.GetName(typeof(DiscountTypes), (int)DiscountTypes.Special).ToLower()) ? "0.00" : trip.InBoundTimeFare.ToString()),
                        OutBoundTimeFare = string.Format("{0:0.00}", result.discountType.Equals(Enum.GetName(typeof(DiscountTypes), (int)DiscountTypes.Special).ToLower()) ? "0.00" : trip.OutBoundTimeFare.ToString()),

                        InBoundSurchargeAmount = string.Format("{0:0.00}", result.discountType.Equals(Enum.GetName(typeof(DiscountTypes), (int)DiscountTypes.Special).ToLower()) ? "0.00" : trip.InBoundSurchargeAmount.ToString()),
                        OutBoundSurchargeAmount = string.Format("{0:0.00}", result.discountType.Equals(Enum.GetName(typeof(DiscountTypes), (int)DiscountTypes.Special).ToLower()) ? "0.00" : trip.OutBoundSurchargeAmount.ToString()),

                        bookingMode = Enum.GetName(typeof(BookingModes), (int)trip.BookingModeID).ToLower(),

                        isVoucherApplied = "false",
                        voucherAmount = "0.00",
                        voucherCode = ""

                    };

                    //No need to set / check voucher - for now

                    //var voucher = context.CompanyVouchers.Where(v => v.VoucherID == trip.VoucherID &&
                    //v.ApplicationID.ToString().ToLower().Equals(ApplicationID.ToLower())).FirstOrDefault();
                    //if (voucher != null)
                    //{
                    //    edr.isVoucherApplied = "true";
                    //    edr.voucherAmount = voucher.Amount.ToString();
                    //    edr.voucherCode = voucher.VoucherCode;
                    //}
                    //else
                    //{
                    //    edr.isVoucherApplied = "false";
                    //    edr.voucherAmount = "0.00";
                    //    edr.voucherCode = "";
                    //}

                    var pas = (from up in context.UserProfiles
                               join anu in context.AspNetUsers on up.UserID equals anu.Id
                               where anu.Id.Equals(trip.UserID.ToString())
                               select new
                               {
                                   up.FirstName,
                                   up.LastName,
                                   anu.Email
                               }).FirstOrDefault();

                    if (pas != null)
                    {
                        result.passengerName = pas.FirstName + " " + pas.LastName;
                        edr.isUserProfileUpdated = !string.IsNullOrEmpty(pas.Email);
                    }
                    else
                    {
                        result.passengerName = "";
                        edr.isUserProfileUpdated = false;
                    }

                    result.travelCharges = edr.travelCharges;
                    result.waitingCharges = edr.waitingCharges;
                    result.bookingCharges = edr.bookingCharges;
                    result.baseCharges = edr.baseCharges;

                    result.paymentMethod = edr.paymentMethod;
                    result.distance = edr.distance;

                    result.duration = edr.duration;
                    result.isFavUser = edr.isFavUser;

                    result.isVoucherApplied = edr.isVoucherApplied;
                    result.voucherAmount = edr.voucherAmount;
                    result.voucherCode = edr.voucherCode;

                    result.isUserProfileUpdated = edr.isUserProfileUpdated;
                    result.isFareChangePermissionGranted = edr.isFareChangePermissionGranted;
                    result.bookingMode = edr.bookingMode;
                    result.isWeb = model.isWeb;

                    //driver data
                    await FirebaseService.UpdateTripDriverDetailsOnEnd(edr, model.tripID, model.driverID);

                    PaymentPendingPassenger pp = new PaymentPendingPassenger()
                    {
                        isPaymentRequested = edr.isPaymentRequested,
                        isFareChangePermissionGranted = edr.isFareChangePermissionGranted,
                        PaymentMode = edr.paymentMethod,

                        //IsPaymentRequested = edr.isPaymentRequested.ToString(),
                        //IsFareChangePermissionGranted = edr.isFareChangePermissionGranted.ToString(),
                        //PaymentMethod = edr.paymentMethod,
                        PaymentModeId = edr.paymentModeId
                    };

                    //user data
                    await FirebaseService.UpdateTripPassengerDetailsOnEnd(pp, model.tripID, trip.UserID.ToString());

                    if (!model.isWeb)
                    {
                        var trp = await TripsManagerService.GetRideDetail(model.tripID, model.isWeb);
                        if (trp != null)
                        {
                            EndRideFCM efc = new EndRideFCM()
                            {
                                TripId = model.tripID,
                                DriverName = trp.Name,
                                DriverImage = trp.Picture,
                                PickUpLatitude = trp.PickupLocationLatitude,
                                PickUpLongitude = trp.PickupLocationLongitude,
                                MidwayStop1Latitude = trip.MidwayStop1Latitude,
                                MidwayStop1Longitude = trip.MidwayStop1Longitude,
                                DropOffLatitude = trp.DropOffLocationLatitude,
                                DropOffLongitude = trp.DropOffLocationLongitude,
                                TotalFare = ((trp.BaseFare != null ? trp.BaseFare : 0) + (trp.BookingFare != null ? trp.BookingFare : 0) + Convert.ToDecimal(trp.travelledFare != null ? trp.travelledFare : 0)).ToString(),
                                BookingDateTime = trp.BookingDateTime.ToString(),
                                EndTripDateTime = trp.TripEndDatetime.ToString(),
                                TotalRewardPoints = (trp.RewardPoints + (int.Parse(model.distance) / 500)).ToString(),
                                TripRewardPoints = (int.Parse(model.distance) / 500).ToString(),
                                Distance = model.distance,
                                //Date = string.Format("{0:dd MM yyyy}", trp.BookingDateTime),
                                //Time = string.Format("{0:hh:mm tt}", trp.BookingDateTime),
                                Date = ((DateTime)trp.BookingDateTime).ToString(Formats.DateFormat),
                                Time = ((DateTime)trp.BookingDateTime).ToString(Formats.TimeFormat),
                                PaymentMode = edr.paymentMethod,
                                PaymentModeId = ((int)Enum.Parse(typeof(PaymentModes), edr.paymentMethod)).ToString(),
                                IsFavorite = trp.favorite == 1 ? true.ToString() : false.ToString()
                            };
                            await PushyService.UniCast(trp.DeviceToken, efc, NotificationKeys.pas_endRideDetail);
                        }
                    }

                    if (trip.BookingModeID == (int)BookingModes.Karhoo ||
                        (trip.BookingModeID == (int)BookingModes.Dispatcher && trip.TripPaymentMode.ToLower().Equals("wallet")))
                    {
                        //Quik fix to avoid application side changes.
                        result.bookingMode = "karhoo";

                        trip.isOverRided = false;
                        trip.TripStatusID = (int)TripStatuses.Completed;
                        trip.CompanyID = captain.CompanyID;
                        trip.TripPaymentMode = "Wallet";

                        Transaction tr = new Transaction()
                        {
                            TransactionID = Guid.NewGuid(),
                            DebitedFrom = Guid.Parse(trip.UserID.ToString()),
                            CreditedTo = Guid.Parse(ApplicationID),
                            DateTime = DateTime.UtcNow,
                            Amount = Convert.ToDecimal(edr.estimatedPrice), //Adjusted wallet amount and voucher amount is considered as mobile payment - RECEIVABLE
                            PaymentModeID = (int)PaymentModes.Wallet,
                            Reference = trip.BookingModeID == (int)BookingModes.Karhoo ? "Karhoo trip completed." : "Dispatcher mobile payment trip completed."
                        };
                        context.Transactions.Add(tr);

                        await FirebaseService.SetDriverFree(model.driverID, model.tripID);
                        await FirebaseService.FreePassengerFromCurrentTrip(model.tripID, trip.UserID.ToString());
                        await FirebaseService.DeleteTrip(model.tripID);
                    }
                    else    //Step 5 : Update trip status - PaymentPending
                    {
                        trip.TripStatusID = (int)TripStatuses.PaymentPending;
                        await FirebaseService.SetTripStatus(model.tripID, Enum.GetName(typeof(TripStatuses), (int)TripStatuses.PaymentPending));
                    }

                    trip.PolyLine = "";

                    foreach (var location in await FirebaseService.GetTripPolyLineDetails(model.tripID, model.driverID))
                    {
                        trip.PolyLine += location.Value.latitude + "," + location.Value.longitude + "|";
                    }

                    trip.PolyLine = trip.PolyLine.Length > 0 ? trip.PolyLine.Remove(trip.PolyLine.Length - 1) : "";

                    await context.SaveChangesAsync();

                    response.error = false;
                    response.message = ResponseKeys.msgSuccess;
                    response.data = result;

                    return Request.CreateResponse(HttpStatusCode.OK, response);
                }
                else
                {
                    response.error = true;
                    response.message = ResponseKeys.tripNotFound;
                    return Request.CreateResponse(HttpStatusCode.OK, response);
                }
            }
        }

        [HttpPost]
        [Route("collectPayment")]
        public async Task<HttpResponseMessage> CashPayment([FromBody] CollectPaymentRequest model)
        {
            using (CangooEntities context = new CangooEntities())
            {
                string passengerDeviceToken = "";
                var trip = context.Trips.Where(t => t.TripID.ToString().Equals(model.tripID)).FirstOrDefault();

                trip.CompanyID = Guid.Parse(model.fleetID);
                trip.isOverRided = false;
                trip.TripPaymentMode = Enum.GetName(typeof(PaymentModes), (int)PaymentModes.Cash);
                trip.PaymentModeId = (int)PaymentModes.Cash;
                trip.Tip = 0;
                trip.TripStatusID = (int)TripStatuses.Completed;

                model.userID = trip.UserID.ToString();

                Transaction tr = new Transaction()
                {
                    TransactionID = Guid.NewGuid(),
                    DebitedFrom = Guid.Parse(trip.UserID.ToString()),
                    CreditedTo = Guid.Parse(ApplicationID),
                    DateTime = DateTime.UtcNow,
                    Amount = Convert.ToDecimal(model.collectedAmount),
                    PaymentModeID = (int)PaymentModes.Cash,
                    Reference = "Trip cash payment received."
                };

                context.Transactions.Add(tr);
                context.SaveChanges();

                passengerDeviceToken = context.UserProfiles.Where(up => up.UserID.Equals(trip.UserID.ToString())).FirstOrDefault().DeviceToken;

                var notificationPayload = new PaymentNotification()
                {
                    TripId = model.tripID,
                    DriverId = model.driverID,
                    IsDriverFavorite = context.UserFavoriteCaptains.Any(ufc => ufc.CaptainID == trip.CaptainID && ufc.UserID.Equals(trip.UserID.ToString())).ToString(),
                    PaymentModeId = trip.PaymentModeId.ToString(),
                    SelectedTipAmount = await FirebaseService.GetTipAmount(model.tripID),
                    TotalFare = model.collectedAmount,
                    DiscountedFare = model.collectedAmount
                    //DiscountedFare = (decimal.Parse(model.collectedAmount) + decimal.Parse(model.walletUsedAmount) + decimal.Parse(model.promoDiscountAmount)).ToString(),
                    //TotalFare = (decimal.Parse(model.collectedAmount) + decimal.Parse(model.walletUsedAmount) + decimal.Parse(model.promoDiscountAmount)).ToString()
                };

                await FreePassengerAndDriver(model.driverID, model.tripID, trip.UserID.ToString());

                //In case of Request from Business it'll be empty. isWeb check can be applied here.
                if (!string.IsNullOrEmpty(passengerDeviceToken))
                    await PushyService.UniCast(passengerDeviceToken, notificationPayload, NotificationKeys.pas_CashPaymentPaid);


                response.error = false;
                response.message = ResponseKeys.msgSuccess;
                return Request.CreateResponse(HttpStatusCode.OK, response);
            }
        }

        [HttpPost]
        [Route("collectCreditCardPayment")]
        public async Task<HttpResponseMessage> MobilePayment([FromBody] MobilePaymentRequest model)
        {
            Dictionary<dynamic, dynamic> dic = new Dictionary<dynamic, dynamic>();

            using (var context = new CangooEntities())
            {
                var trip = context.Trips.Where(t => t.TripID.ToString().Equals(model.tripID)).FirstOrDefault();

                decimal chargeblePayment = decimal.Parse(model.totalFare);

                chargeblePayment -= (decimal)trip.PromoDiscount;

                var transactionId = "";

                if (trip.PaymentModeId == (int)PaymentModes.CreditCard)
                {
                    var paymentDetails = await PaymentsServices.CaptureAuthorizedPaymentPartially(trip.CreditCardPaymentIntent, chargeblePayment.ToString());

                    if (paymentDetails.Status.Equals(TransactionStatus.succeeded))
                    {
                        context.SaveChanges();

                        transactionId = "Trip CreditCard payment received. Stripe transactionId = " + paymentDetails.PaymentIntentId;
                    }
                    else
                    {
                        response.error = true;
                        response.message = ResponseKeys.paymentGetwayError;
                        response.data = new Dictionary<dynamic, dynamic>
                                        {
                                            { "Status", paymentDetails.Status },
                                            { "ClientSecret", paymentDetails.ClientSecret },
                                            { "FailureMessage", paymentDetails.Description }
                                        };
                        return Request.CreateResponse(HttpStatusCode.OK, response);
                    }
                }
                else if (trip.PaymentModeId == (int)PaymentModes.Wallet)
                {
                    //Can be moved to spAfterMobilePayment sp
                    var userProfile = await context.UserProfiles.Where(up => up.UserID.Equals(trip.UserID.ToString())).FirstOrDefaultAsync();
                    userProfile.WalletBalance -= chargeblePayment;
                    userProfile.AvailableWalletBalance += (decimal.Parse(model.totalFare) - chargeblePayment);
                    context.SaveChanges();
                }
                else if (trip.PaymentModeId == (int)PaymentModes.Paypal)
                {

                }

                var updatedTrip = context.spAfterMobilePayment(model.tripID, transactionId, (int)TripStatuses.Completed,
                    trip.UserID.ToString(), ApplicationID,
                   model.totalFare, DateTime.UtcNow, (int)PaymentStatuses.Paid).FirstOrDefault();

                if (!string.IsNullOrEmpty(updatedTrip.PassengerDeviceToken))
                {
                    var notificationPayload = new PaymentNotification()
                    {
                        TripId = model.tripID,
                        DriverId = model.driverID,
                        PaymentModeId = trip.PaymentModeId.ToString(),
                        TotalFare = model.totalFare,
                        PromoDiscountAmount = ((decimal)trip.PromoDiscount).ToString("0.00"),
                        DiscountedFare = (decimal.Parse(model.totalFare) - (decimal)trip.PromoDiscount).ToString("0.00"),
                        SelectedTipAmount = await FirebaseService.GetTipAmount(model.tripID),
                        WalletBalance = ((decimal)updatedTrip.WalletBalance).ToString("0.00"),
                        AvailableWalletBalance = ((decimal)updatedTrip.AvailableWalletBalance).ToString("0.00"),
                        Brand = trip.CreditCardBrand,
                        Last4Digits = trip.CreditCardLast4Digits,
                        IsDriverFavorite = context.UserFavoriteCaptains.Any(ufc => ufc.CaptainID == trip.CaptainID && ufc.UserID.Equals(trip.UserID.ToString())).ToString()
                    };

                    await PushyService.UniCast(updatedTrip.PassengerDeviceToken, notificationPayload, NotificationKeys.pas_MobilePaymentPaid);
                }

                await FreePassengerAndDriver(model.driverID, model.tripID, trip.UserID.ToString());

                await SendInvoice(new InvoiceModel
                {
                    CashAmount = "0.00",
                    TotalAmount = model.totalFare,
                    WalletUsedAmount = trip.PaymentModeId == (int)PaymentModes.Wallet ? (decimal.Parse(model.totalFare) - (decimal)trip.PromoDiscount).ToString("0.00") : "0.00",
                    PromoDiscountAmount = trip.PromoDiscount.ToString(),
                    CustomerEmail = updatedTrip.CustomerEmail,
                    CaptainName = updatedTrip.CaptainName,
                    CustomerName = updatedTrip.CustomerName,
                    TripDate = updatedTrip.TripDate,
                    InvoiceNumber = updatedTrip.InvoiceNumber,
                    FleetName = updatedTrip.FleetName,
                    ATUNumber = updatedTrip.FleetATUNumber,
                    Street = updatedTrip.FleetAddress,
                    BuildingNumber = updatedTrip.FleetBuildingNumber,
                    PostCode = updatedTrip.FleetPostalCode,
                    City = updatedTrip.FleetCity,
                    PickUpAddress = updatedTrip.PickUpLocation,
                    MidwayStop1Address = updatedTrip.MidwayStop1Location ?? "",
                    DropOffAddress = updatedTrip.DropOffLocation,
                    CaptainUserName = updatedTrip.CaptainUserName,
                    Distance = updatedTrip.DistanceInKM.ToString("0.00"),
                    VehicleNumber = updatedTrip.PlateNumber,
                    FleetEmail = updatedTrip.FleetEmail
                });

                response.error = false;
                response.message = ResponseKeys.msgSuccess;
                return Request.CreateResponse(HttpStatusCode.OK, response);
            }
        }

        [HttpPost]
        [Route("passengerRating")]
        public HttpResponseMessage passengerRating([FromBody] PassengerRatingRequest model)
        {
            //if (model != null && model.customerRating > 0 && !string.IsNullOrEmpty(model.tripID) && !string.IsNullOrEmpty(model.driverID))
            //{
            using (CangooEntities context = new CangooEntities())
            {
                var tp = context.Trips.Where(t => t.TripID.ToString() == model.tripID).FirstOrDefault();
                if (tp != null)
                {
                    tp.UserRating = Convert.ToInt32(model.customerRating);
                    tp.DriverSubmittedFeedback = model.description;

                    var user = context.UserProfiles.Where(u => u.UserID == tp.UserID.ToString()).FirstOrDefault();

                    //Verify if ride was booked by business portal
                    if (user != null)
                    {
                        int userTrips = (int)(user.NoOfTrips == null ? 0 : user.NoOfTrips);
                        user.Rating = Math.Round((double)((((user.Rating == null ? 0 : user.Rating) * (userTrips - 1)) + tp.UserRating) / userTrips), 1, MidpointRounding.ToEven);
                    }

                    context.SaveChanges();
                    response.error = false;
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
            //}
            //else
            //{
            //    response.error = true;
            //    response.message = ResponseKeys.invalidParameters;
            //    return Request.CreateResponse(HttpStatusCode.OK, response);
            //}
        }

        [HttpPost]
        [Route("passengerFavUnFav")]
        public HttpResponseMessage passengerFavUnFav([FromBody] PassengerFavUnFavRequest model)
        {
            //if (model != null && !string.IsNullOrEmpty(model.tripID) && !string.IsNullOrEmpty(model.driverID))
            //{
            using (CangooEntities context = new CangooEntities())
            {
                var tp = context.Trips.Where(t => t.TripID.ToString() == model.tripID).FirstOrDefault();
                if (tp != null)
                {
                    //if (Request.Headers.Contains("ApplicationID"))
                    //{
                    //    ApplicationID = Request.Headers.GetValues("ApplicationID").First();
                    //}
                    var capt = context.Captains.Where(c => c.CaptainID.ToString().Equals(model.driverID)).FirstOrDefault();
                    var usr = context.UserFavoriteCaptains.Where(f => f.UserID == tp.UserID.ToString() && f.CaptainID.ToString() == model.driverID).FirstOrDefault();
                    if (usr == null)
                    {
                        UserFavoriteCaptain uf = new UserFavoriteCaptain
                        {
                            ID = Guid.NewGuid(),
                            UserID = tp.UserID.ToString(),
                            CaptainID = tp.CaptainID,
                            IsFavByPassenger = false,
                            IsFavByCaptain = true,
                            ApplicationID = Guid.Parse(ApplicationID)
                        };

                        capt.NumberOfFavoriteUser = capt.NumberOfFavoriteUser == null ? 1 : (int)capt.NumberOfFavoriteUser + 1;
                        context.UserFavoriteCaptains.Add(uf);
                    }
                    else
                    {
                        if ((bool)usr.IsFavByCaptain && (bool)usr.IsFavByPassenger)
                        {
                            usr.IsFavByCaptain = false;
                            capt.NumberOfFavoriteUser = capt.NumberOfFavoriteUser == 1 ? 0 : (int)capt.NumberOfFavoriteUser - 1;
                        }
                        else if ((bool)usr.IsFavByCaptain)
                        {
                            context.UserFavoriteCaptains.Remove(usr);
                            capt.NumberOfFavoriteUser = capt.NumberOfFavoriteUser == 1 ? 0 : (int)capt.NumberOfFavoriteUser - 1;
                        }
                        else
                        {
                            usr.IsFavByCaptain = true;
                            capt.NumberOfFavoriteUser = capt.NumberOfFavoriteUser == null ? 1 : (int)capt.NumberOfFavoriteUser + 1;
                        }
                    }
                    context.SaveChanges();
                    response.error = false;
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
            //}
            //else
            //{
            //    response.error = true;
            //    response.message = ResponseKeys.invalidParameters;
            //    return Request.CreateResponse(HttpStatusCode.OK, response);
            //}
        }

        #endregion

        #region Priority Hour

        [HttpPost]
        [Route("activatePriorityHour")]
        public async Task<HttpResponseMessage> activatePriorityHour([FromBody] ActivatePriorityHourRequest model)
        {
            //if (model != null && model.captainID != string.Empty && model.duration > 0)
            //{
            using (CangooEntities context = new CangooEntities())
            {

                var captain = context.Captains.Where(c => c.CaptainID.ToString().Equals(model.captainID)).FirstOrDefault();
                if (captain != null)
                {
                    var settings = context.ApplicationSettings.Where(a => a.ApplicationID.ToString().ToLower().Equals(ApplicationID.ToLower())).FirstOrDefault();

                    captain.IsPriorityHoursActive = true;
                    captain.LastPriorityHourStartTime = DateTime.UtcNow;
                    captain.LastPriorityHourEndTime = DateTime.UtcNow.AddHours(model.duration);
                    captain.EarningPoints -= model.duration * (settings.AwardpointsDeduction != null ? (int)settings.AwardpointsDeduction : 100);
                    context.SaveChanges();

                    var priorityHourRemainingTime = ((int)(((DateTime)captain.LastPriorityHourEndTime).
                                                    Subtract((DateTime)captain.LastPriorityHourStartTime).TotalMinutes)).ToString();

                    await FirebaseService.SetPriorityHourStatus(true, priorityHourRemainingTime, model.captainID, DateTime.Parse(captain.LastPriorityHourEndTime.ToString()).ToString(Formats.DateTimeFormat), captain.EarningPoints.ToString());

                    response.error = false;
                    response.message = ResponseKeys.msgSuccess;
                    dic = new Dictionary<dynamic, dynamic>
                                    {
                                        { "priorityHour", model }
                                    };
                    response.data = dic;
                    return Request.CreateResponse(HttpStatusCode.OK, response);
                }
                else
                {
                    response.message = ResponseKeys.captainNotFound;
                    response.error = true;
                    return Request.CreateResponse(HttpStatusCode.OK, response);
                }
            }
            //}
            //else
            //{
            //    response.message = ResponseKeys.invalidParameters;
            //    response.error = true;
            //    return Request.CreateResponse(HttpStatusCode.OK, response);
            //}
        }


        [HttpGet]
        [Route("getCaptainEarnedPoints")]
        public HttpResponseMessage getCaptainEarnedPoints([FromUri] GetDriverEarnedPointsRequest model)
        {
            //if (!string.IsNullOrEmpty(model.captainID))
            //{
            using (CangooEntities context = new CangooEntities())
            {
                var captain = context.Captains.Where(c => c.CaptainID.ToString().Equals(model.captainID)).FirstOrDefault();
                if (captain != null)
                {
                    response.error = false;
                    response.message = ResponseKeys.msgSuccess;
                    dic = new Dictionary<dynamic, dynamic>
                                    {
                                        { "earnedPoints", captain.EarningPoints }
                                    };
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
            //}
            //else
            //{
            //    response.message = ResponseKeys.invalidParameters;
            //    response.error = true;
            //    return Request.CreateResponse(HttpStatusCode.OK, response);
            //}
        }

        #endregion

        #region Trip History/Upcoming

        [HttpGet]
        [Route("getAllUnAcceptedLaterBooking")]
        public async Task<HttpResponseMessage> getAllUnAcceptedLaterBooking([FromUri] GetDriverPendingLaterBookingsRequest model)
        {
            //if (model.offset > 0 && model.limit > 0 && model.vehicleSeatingCapacity > 0)
            //{
            using (CangooEntities context = new CangooEntities())
            {
                //UserController uc = new UserController();
                var lstLaterBooking = context.spGetAllUnAcceptedLateBooking(ResellerID, ApplicationID, DateTime.UtcNow, (int)TripStatuses.RequestSent, model.vehicleSeatingCapacity, model.offset, model.limit).ToList(); //date time utc
                if (lstLaterBooking.Count > 0)
                {
                    List<ScheduleBookingResponse> lstSB = new List<ScheduleBookingResponse>();
                    foreach (var item in lstLaterBooking)
                    {
                        //dic = new Dictionary<dynamic, dynamic>() {
                        //        { "discountType", "normal"},
                        //        { "discountAmount", "0.00" }
                        //    };

                        var result = await FareManagerService.IsSpecialPromotionApplicable(item.pickupLocationLatitude, item.pickuplocationlongitude, item.DropOffLocationLatitude,
                             item.DropOffLocationLongitude, ApplicationID, true, item.pickUpBookingDateTime);

                        //dic = new Dictionary<dynamic, dynamic>() {
                        //            { "discountType", result.DiscountType },
                        //            { "discountAmount", result.DiscountAmount }
                        //        };

                        var lstTripFacilities = await FacilitiesService.GetDriverFacilitiesDetailByIds(item.facilities);
                        //if (item.NoOfPerson <= vehicleSeatingCapacity)
                        //{
                        ScheduleBookingResponse sb = new ScheduleBookingResponse
                        {
                            tripID = item.TripID.ToString(),
                            pickUplatitude = item.pickupLocationLatitude,
                            pickUplongitude = item.pickuplocationlongitude,
                            pickUpLocation = item.PickUpLocation,
                            dropOfflatitude = item.DropOffLocationLatitude,
                            dropOfflongitude = item.DropOffLocationLongitude,
                            dropOffLocation = item.DropOffLocation,
                            isLaterBooking = Convert.ToBoolean(item.isLaterBooking),
                            passengerID = item.UserID.ToString(),
                            passengerName = item.passengName,
                            rating = item.Rating.ToString(),
                            tripPaymentMode = item.TripPaymentMode,
                            pickUpDateTime = Convert.ToDateTime(item.pickUpBookingDateTime).ToString(Formats.DateTimeFormat),
                            isFav = item.favorite != null ? (bool)item.favorite : false,
                            seatingCapacity = (int)item.NoOfPerson,
                            estimatedDistance = item.DistanceTraveled.ToString(),
                            facilities = lstTripFacilities,
                            discountType = result.DiscountType,// dic["discountType"],
                            discountAmount = result.DiscountAmount// dic["discountAmount"]
                        };
                        lstSB.Add(sb);
                        //}
                    }

                    dic = new Dictionary<dynamic, dynamic>
                            {
                                { "pendingLaterBooking", lstSB },
                                { "totalRecords", lstLaterBooking.FirstOrDefault().totalRecord }
                            };

                    response.error = false;
                    response.message = ResponseKeys.msgSuccess;
                    response.data = dic;
                    return Request.CreateResponse(HttpStatusCode.OK, response);
                }
                else
                {
                    dic = new Dictionary<dynamic, dynamic>
                            {
                                { "pendingLaterBooking", new List<ScheduleBookingResponse>() },
                                { "totalRecords", 0 }
                            };

                    response.error = false;
                    response.message = ResponseKeys.msgSuccess;
                    response.data = dic;
                    return Request.CreateResponse(HttpStatusCode.OK, response);
                }
            }
            //}
            //else
            //{
            //    response.error = true;
            //    response.message = ResponseKeys.invalidParameters;
            //    return Request.CreateResponse(HttpStatusCode.BadRequest, response);
            //}
        }

        [HttpPost]      //Get accepted upcoming later bookings
        [Route("getDriverLaterBooking")]
        public async Task<HttpResponseMessage> getDriverLaterBooking([FromBody] DriverGetUpComingBookingsRequest model)
        {
            using (CangooEntities context = new CangooEntities())
            {
                //if (model != null && model.userID != string.Empty && model.offset > 0 && model.limit > 0)
                //{
                //UserController uc = new UserController();

                var lstScheduleRides = context.spCaptainLaterTrips(DateTime.UtcNow, model.userID, (int)TripStatuses.LaterBookingAccepted, model.offset, model.limit).ToList();
                if (lstScheduleRides.Count > 0)
                {
                    List<ScheduleBookingResponse> lstSB = new List<ScheduleBookingResponse>();
                    foreach (var item in lstScheduleRides)
                    {
                        var result = await FareManagerService.IsSpecialPromotionApplicable(item.pickupLocationLatitude, item.pickuplocationlongitude, item.DropOffLocationLatitude,
                            item.DropOffLocationLongitude, ApplicationID, true, item.pickUpBookingDateTime);

                        //dic = new Dictionary<dynamic, dynamic>() {
                        //            { "discountType", result.DiscountType },
                        //            { "discountAmount", result.DiscountAmount }
                        //        };

                        //dic = new Dictionary<dynamic, dynamic>() {
                        //        { "discountType", "normal"},
                        //        { "discountAmount", "0.00" }
                        //    };


                        //Common.GetFacilities(ResellerID, ApplicationID, context, item.facilities, out List<Facilities> lstTripFacilities);
                        var lstTripFacilities = await FacilitiesService.GetDriverFacilitiesDetailByIds(item.facilities);
                        ScheduleBookingResponse sb = new ScheduleBookingResponse
                        {
                            tripID = item.TripID.ToString(),
                            pickUplatitude = item.pickupLocationLatitude,
                            pickUplongitude = item.pickuplocationlongitude,
                            pickUpLocation = item.PickUpLocation,
                            dropOfflatitude = item.DropOffLocationLatitude,
                            dropOfflongitude = item.DropOffLocationLongitude,
                            dropOffLocation = item.DropOffLocation,
                            isLaterBooking = Convert.ToBoolean(item.isLaterBooking),
                            passengerID = item.UserID.ToString(),
                            passengerName = item.passengName,
                            rating = item.Rating.ToString(),
                            tripPaymentMode = item.TripPaymentMode,
                            isFav = item.favorite != null ? (bool)item.favorite : false,
                            pickUpDateTime = Convert.ToDateTime(item.pickUpBookingDateTime).ToString(Formats.DateTimeFormat),
                            seatingCapacity = (int)item.NoOfPerson,
                            estimatedDistance = item.DistanceTraveled.ToString(),
                            facilities = lstTripFacilities,
                            isWeb = item.isWeb,
                            discountType = result.DiscountType,//dic["discountType"],
                            discountAmount = result.DiscountAmount,//dic["discountAmount"],
                            remainingTime = ((DateTime)item.pickUpBookingDateTime - DateTime.UtcNow).TotalSeconds
                        };
                        lstSB.Add(sb);
                    }

                    dic = new Dictionary<dynamic, dynamic>
                            {
                                { "laterBooking", lstSB },
                                { "totalRecords", lstScheduleRides.FirstOrDefault().totalRecord }
                            };

                    response.error = false;
                    response.message = ResponseKeys.msgSuccess;
                    response.data = dic;
                    return Request.CreateResponse(HttpStatusCode.OK, response);
                }
                else
                {
                    dic = new Dictionary<dynamic, dynamic>
                            {
                                { "laterBooking", new List<ScheduleBookingResponse>() },
                                { "totalRecords", 0 }
                            };

                    response.error = false;
                    response.message = ResponseKeys.msgSuccess;
                    response.data = dic;
                    return Request.CreateResponse(HttpStatusCode.OK, response);
                }
                //}
                //else
                //{
                //    response.error = true;
                //    response.message = ResponseKeys.invalidParameters;
                //    return Request.CreateResponse(HttpStatusCode.OK, response);
                //}
            }
        }

        [HttpGet]
        [Route("getDriverBookingHistory")]
        public async Task<HttpResponseMessage> getDriverBookingHistory([FromUri] GetDriverBookingHistoryRequest model)
        {
            //if (!string.IsNullOrEmpty(captainID) && !string.IsNullOrEmpty(pageSize) && !string.IsNullOrEmpty(pageNo) &&
            //    !string.IsNullOrEmpty(dateFrom) && !string.IsNullOrEmpty(dateTo))
            //{
            using (CangooEntities context = new CangooEntities())
            {

                var result = context.spCaptainTripHistory(model.captainID, int.Parse(model.pageNo), int.Parse(model.pageSize),
                    Convert.ToDateTime(model.dateFrom), Convert.ToDateTime(model.dateTo)).ToList();
                if (result.Count > 0)
                {
                    List<DriverTripsDTO> lstTrips = new List<DriverTripsDTO>();

                    foreach (var temp in result)
                    {
                        //Common.GetFacilities(ResellerID, ApplicationID, context, item.facilities, out List<Facilities> lstTripFacilities);
                        var lstTripFacilities = await FacilitiesService.GetDriverFacilitiesDetailByIds(temp.facilities);
                        DriverTripsDTO trip = new DriverTripsDTO()
                        {
                            tripID = temp.tripID.ToString(),
                            pickupLocationLatitude = temp.PickupLocationLatitude,
                            pickupLocationLongitude = temp.PickupLocationLongitude,
                            pickupLocation = temp.PickUpLocation,
                            dropOffLocationLatitude = temp.DropOffLocationLatitude,
                            dropOffLocationLongitude = temp.DropOffLocationLongitude,
                            dropOffLocation = temp.DropOffLocation,
                            bookingDateTime = Convert.ToDateTime(temp.BookingDateTime).ToString(Formats.DateTimeFormat),
                            pickUpBookingDateTime = Convert.ToDateTime(temp.PickUpBookingDateTime).ToString(Formats.DateTimeFormat),
                            tripArrivalDatetime = Convert.ToDateTime(temp.ArrivalDateTime).ToString(Formats.DateTimeFormat),
                            tripStartDatetime = Convert.ToDateTime(temp.TripStartDatetime).ToString(Formats.DateTimeFormat),
                            tripEndDatetime = Convert.ToDateTime(temp.TripEndDatetime).ToString(Formats.DateTimeFormat),
                            tripStatus = temp.Status,
                            facilities = lstTripFacilities,
                            tip = temp.Tip.ToString(),
                            fare = temp.Fare.ToString(),
                            cashPayment = temp.TripCashPayment.ToString(),
                            mobilePayment = temp.TripMobilePayment.ToString(),
                            bookingType = temp.BookingType,
                            bookingMode = temp.BookingMode,
                            paymentMode = temp.PaymentMode,
                            passengerName = temp.PassengerName,
                            make = temp.make,
                            model = temp.Model,
                            plateNumber = temp.PlateNumber,
                            distanceTraveled = temp.DistanceTraveled.ToString(),
                            vehicleRating = temp.VehicleRating.ToString(),
                            driverEarnedPoints = temp.DriverEarnedPoints.ToString(),
                            driverRating = temp.DriverRating.ToString()
                        };
                        lstTrips.Add(trip);
                    }

                    DriverTripsHistoryResponse history = new DriverTripsHistoryResponse()
                    {
                        avgDriverRating = result.FirstOrDefault().avgDriverRating.ToString(),
                        avgVehicleRating = result.FirstOrDefault().avgVehicleRating.ToString(),
                        totalTrips = result.FirstOrDefault().totalTrips.ToString(),
                        totalFare = (result.FirstOrDefault().totalTip + result.FirstOrDefault().totalFare).ToString(),
                        totalTip = result.FirstOrDefault().totalTip.ToString(),
                        totalEarnedPoints = result.FirstOrDefault().totalEarnedPoints.ToString(),
                        totalCashEarning = result.FirstOrDefault().totalCashEarning.ToString(),
                        totalMobilePayEarning = result.FirstOrDefault().totalMobilePayEarning.ToString(),
                        trips = lstTrips
                    };
                    response.data = history;
                }
                else
                {
                    response.data = new DriverTripsHistoryResponse()
                    {
                        avgDriverRating = "0",
                        avgVehicleRating = "0",
                        totalEarnedPoints = "0",
                        totalFare = "0",
                        totalTip = "0",
                        totalTrips = "0",
                        trips = new List<DriverTripsDTO>()
                    };
                }
                response.error = false;
                response.message = ResponseKeys.msgSuccess;
                return Request.CreateResponse(HttpStatusCode.OK, response);
            }
            //}
            //else
            //{
            //    response.error = true;
            //    response.message = ResponseKeys.invalidParameters;
            //    return Request.CreateResponse(HttpStatusCode.OK, response);
            //}
        }

        #endregion

        #region profile / settings

        [HttpGet]
        [Route("captainProfile")]
        public async Task<HttpResponseMessage> captainProfile([FromUri] GetDriverProfileRequest model)
        {
            //if (!string.IsNullOrEmpty(captainID))
            //{

            using (CangooEntities context = new CangooEntities())
            {
                var cap = context.spCaptainProfile(model.captainID, model.vehicleID, new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1)).FirstOrDefault();

                if (cap != null)
                {
                    //Common.GetFacilities(ResellerID, ApplicationID, context, cap.Facilities, out List<Facilities> lstCaptainFacilities);
                    //Common.GetFacilities(ResellerID, ApplicationID, context, cap.VehicleFacilities, out List<Facilities> lstVehicleFacilities);

                    var lstCaptainFacilities = await FacilitiesService.GetDriverFacilitiesDetailByIds(cap.Facilities);
                    var lstVehicleFacilities = await FacilitiesService.GetDriverFacilitiesDetailByIds(cap.VehicleFacilities);

                    var profile = new CaptainProfileResponse()
                    {
                        name = cap.Name,
                        email = cap.Email,
                        phone = cap.PhoneNumber,
                        shareCode = cap.ShareCode,
                        captainFacilitiesList = lstCaptainFacilities,
                        make = cap.Make,
                        model = cap.Model,
                        number = cap.PlateNumber,
                        seatingCapacity = cap.SeatingCapacity.ToString(),
                        vehicleFacilitiesList = lstVehicleFacilities
                    };
                    response.data = profile;
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
            //}
            //else
            //{
            //    response.error = true;
            //    response.message = ResponseKeys.invalidParameters;
            //    return Request.CreateResponse(HttpStatusCode.OK, response);
            //}
        }

        [HttpGet]
        [Route("captainStats")]
        public HttpResponseMessage captainStats([FromUri] GetDriverStatsRequest model)
        {
            ////if (!string.IsNullOrEmpty(captainID) && !string.IsNullOrEmpty(vehicleID))
            ////{
            using (CangooEntities context = new CangooEntities())
            {
                var cap = context.spCaptainProfile(model.captainID, model.vehicleID, new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1)).FirstOrDefault();

                if (cap != null)
                {
                    var stats = new CaptainStatsResponse()
                    {
                        captainRating = string.Format("{0:0.00}", cap.Rating.ToString()),
                        vehicleRating = string.Format("{0:0.00}", cap.VehicleRating.ToString()),
                        cashRides = cap.CashTrips.ToString(),
                        mobilePayRides = cap.MobilePayTrips.ToString(),
                        cashEarning = string.Format("{0:0.00}", cap.CashEarning.ToString()),
                        mobilePayEarning = string.Format("{0:0.00}", cap.MobilePayEarning.ToString()),
                        favPassengers = cap.NumberOfFavoriteUser.ToString(),
                        memberSince = DateTime.Parse(cap.MemberSince.ToString()).ToString(Formats.DateTimeFormat),
                        avgCashEarning = string.Format("{0:0.00}", cap.AverageCashEarning.ToString()),
                        avgMobilePayEarning = string.Format("{0:0.00}", cap.AverageMobilePayEarning.ToString()),
                        currentMonthAcceptanceRate = string.Format("{0:0.00}", cap.currentMonthAcceptanceRate),
                        currentMonthOnlineHours = string.Format("{0:0.00}", cap.AverageMobilePayEarning)
                    };

                    response.data = stats;
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
            //}
            //else
            //{
            //    response.error = true;
            //    response.message = ResponseKeys.invalidParameters;
            //    return Request.CreateResponse(HttpStatusCode.OK, response);
            //}
        }

        [HttpGet]
        [Route("getCaptainSettings")]
        public HttpResponseMessage getCaptainSettings([FromUri] GetDriverSettingsRequest model)
        {
            //if (!string.IsNullOrEmpty(captainID))
            //{
            using (CangooEntities context = new CangooEntities())
            {
                var cap = context.Captains.Where(c => c.CaptainID.ToString().Equals(model.captainID)).FirstOrDefault();

                if (cap != null)
                {
                    var settings = new CaptainSettingsDTO()
                    {
                        laterBookingNotificationTone = string.IsNullOrEmpty(cap.LaterBookingNotificationTone) ? "6.mp3" : cap.LaterBookingNotificationTone,
                        normalBookingNotificationTone = string.IsNullOrEmpty(cap.NormalBookingNotificationTone) ? "6.mp3" : cap.NormalBookingNotificationTone,
                        requestRadius = cap.RideRadius != null ? cap.RideRadius.ToString() : "N/A",
                        showOtherVehicles = cap.ShowOtherVehicles.ToString().ToLower(),
                        captainID = model.captainID
                    };

                    response.data = settings;
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
            //}
            //else
            //{
            //    response.error = true;
            //    response.message = ResponseKeys.invalidParameters;
            //    return Request.CreateResponse(HttpStatusCode.OK, response);
            //}
        }

        [HttpPost]
        [Route("saveCaptainSettings")]
        public HttpResponseMessage saveCaptainSettings([FromBody] SaveCaptainSettingsRequest model)
        {
            //if (!string.IsNullOrEmpty(model.captainID))
            //{
            using (CangooEntities context = new CangooEntities())
            {
                var cap = context.Captains.Where(c => c.CaptainID.ToString().Equals(model.captainID)).FirstOrDefault();

                if (cap != null)
                {
                    if (!string.IsNullOrEmpty(model.laterBookingNotificationTone))
                        cap.LaterBookingNotificationTone = model.laterBookingNotificationTone;
                    if (!string.IsNullOrEmpty(model.normalBookingNotificationTone))
                        cap.NormalBookingNotificationTone = model.normalBookingNotificationTone;
                    if (!string.IsNullOrEmpty(model.requestRadius))
                        cap.RideRadius = double.Parse(model.requestRadius);
                    if (!string.IsNullOrEmpty(model.showOtherVehicles))
                        cap.ShowOtherVehicles = bool.Parse(model.showOtherVehicles.ToLower());

                    context.SaveChanges();

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
            //}
            //else
            //{
            //    response.error = true;
            //    response.message = ResponseKeys.invalidParameters;
            //    return Request.CreateResponse(HttpStatusCode.OK, response);
            //}
        }

        #endregion

        #region Misc.

        [HttpGet]
        [Route("getContactDetails")]
        public HttpResponseMessage getContactDetails()
        {

            using (CangooEntities context = new CangooEntities())
            {
                var contact = context.ContactDetails.Where(c => c.ApplicationID.ToString().ToLower().Equals(ApplicationID.ToLower())).FirstOrDefault();
                if (contact != null)
                {
                    response.error = false;
                    response.message = ResponseKeys.msgSuccess;
                    dic = new Dictionary<dynamic, dynamic>
                        {
                            { "businessName", contact.BusinessName },
                            { "openingTime", contact.OpeningTime },
                            { "closingTime", contact.ClosingTime },
                            { "contactPersonName", contact.ContactPersonName },
                            { "Email", contact.Email },
                            { "telephone", contact.Telephone },
                            { "state", contact.State },
                            { "city", contact.City },
                            { "street", contact.Street },
                            { "zipCode", contact.ZipCode }
                        };
                    response.data = dic;
                    return Request.CreateResponse(HttpStatusCode.OK, response);
                }
                else
                {
                    response.error = true;
                    response.message = ResponseKeys.contactDetailsNotFound;
                    return Request.CreateResponse(HttpStatusCode.OK, response);
                }
            }
        }

        [HttpGet]
        [Route("getAgreementTypes")]
        public HttpResponseMessage getAgreementTypes()
        {

            using (CangooEntities context = new CangooEntities())
            {
                var agreement = context.AgreementTypes.Where(at => at.ApplicationUserTypeID == (int)SystemRoles.Captain
                && at.ApplicationID.ToString().ToLower().Equals(ApplicationID.ToLower())
                ).ToList();

                if (agreement != null)
                {
                    List<AgreementTypeResponse> lst = new List<AgreementTypeResponse>();

                    foreach (var item in agreement)
                    {
                        lst.Add(new AgreementTypeResponse
                        {
                            TypeId = item.ID,
                            Name = item.TypeName
                        });
                    }
                    response.error = false;
                    response.message = ResponseKeys.msgSuccess;
                    response.data = lst;
                    return Request.CreateResponse(HttpStatusCode.OK, response);
                }
                else
                {
                    response.error = true;
                    response.message = ResponseKeys.agreementTypesNotFound;
                    return Request.CreateResponse(HttpStatusCode.OK, response);
                }
            }
        }

        [HttpGet]
        [Route("getAgreements")]
        public HttpResponseMessage getAgreements([FromUri] GetAgreementsRequest model)
        {
            using (CangooEntities context = new CangooEntities())
            {
                int id = int.Parse(model.agreementTypeId);

                var agreement = context.Agreements.Where(a => a.AgreementTypeID == id
                && a.ApplicationID.ToString().ToLower().Equals(ApplicationID.ToLower())
                ).ToList();
                if (agreement != null)
                {
                    List<AgreementResponse> lst = new List<AgreementResponse>();

                    foreach (var item in agreement)
                    {
                        lst.Add(new AgreementResponse
                        {
                            AgreementId = item.AgreementID,
                            Title = item.Name,
                            Description = item.Detail
                        });
                    }
                    response.error = false;
                    response.message = ResponseKeys.msgSuccess;
                    response.data = lst;
                    return Request.CreateResponse(HttpStatusCode.OK, response);
                }
                else
                {
                    response.error = true;
                    response.message = ResponseKeys.agreementsNotFound;
                    return Request.CreateResponse(HttpStatusCode.OK, response);
                }
            }
        }

        [HttpGet]
        [Route("getFAQs")]
        public HttpResponseMessage getFAQs()
        {

            using (CangooEntities context = new CangooEntities())
            {
                var faqs = context.FAQs.Where(f => f.ApplicationUserTypeID == (int)SystemRoles.Captain
                && f.ApplicationID.ToString().ToLower().Equals(ApplicationID.ToLower())
                ).ToList();

                if (faqs != null)
                {
                    List<FAQResponse> lst = new List<FAQResponse>();

                    foreach (var item in faqs)
                    {
                        lst.Add(new FAQResponse
                        {
                            FaqId = item.ID,
                            Question = item.Question,
                            Answer = item.Answer
                        });
                    }
                    response.error = false;
                    response.message = ResponseKeys.msgSuccess;
                    response.data = lst;
                    return Request.CreateResponse(HttpStatusCode.OK, response);
                }
                else
                {
                    response.error = true;
                    response.message = ResponseKeys.faqNotFound;
                    return Request.CreateResponse(HttpStatusCode.OK, response);
                }
            }
        }

        [HttpGet]
        [Route("getNewsFeed")]
        public HttpResponseMessage getNewsFeed()
        {
            using (CangooEntities context = new CangooEntities())
            {
                var feed = context.Notifications.Where(nf => nf.ApplicationUserTypeID == (int)SystemRoles.Captain
                && nf.ExpiryDate > DateTime.UtcNow
                && nf.ApplicationID.ToString().ToLower().Equals(ApplicationID.ToLower())).ToList().OrderByDescending(nf => nf.CreationDate);
                if (feed != null)
                {
                    List<NewsFeedResponse> lst = new List<NewsFeedResponse>();

                    foreach (var item in feed)
                    {
                        lst.Add(new NewsFeedResponse
                        {
                            FeedId = item.FeedID,
                            ShortDescrption = item.ShortDescription,
                            Detail = item.Detail
                        });
                    }
                    response.error = false;
                    response.message = ResponseKeys.msgSuccess;
                    response.data = lst;
                    return Request.CreateResponse(HttpStatusCode.OK, response);
                }
                else
                {
                    response.error = true;
                    response.message = ResponseKeys.feedNotFound;
                    return Request.CreateResponse(HttpStatusCode.OK, response);
                }
            }
        }

        [HttpGet]
        [Route("getCurrentUTCDateTime")]
        public HttpResponseMessage getCurrentUTCDateTime()
        {
            response.error = false;
            response.data = new Dictionary<dynamic, dynamic>
                            {
                                {"currentDateTime", DateTime.UtcNow.ToString(Formats.DateTimeFormat) }
                            };
            response.message = ResponseKeys.msgSuccess;
            return Request.CreateResponse(HttpStatusCode.OK, response);
        }

        #endregion

        #region Deprecated

        //[HttpPost]
        //[Route("mobilePaymentTimeout")]
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
        //public HttpResponseMessage searchUserByPhone([FromBody] RequestModel model)
        //{
        //    try
        //    {
        //        if (model != null && !string.IsNullOrEmpty(model.phoneNumber))
        //        {
        //            using (CangooEntities context = new CangooEntities())
        //            {
        //                var up = context.spSearchUserByPhone(model.phoneNumber).ToList();
        //                if (up.Any())
        //                {
        //                    if (string.IsNullOrEmpty(up.FirstOrDefault().Email))
        //                    {
        //                        response.error = true;
        //                        response.message = ResponseKeys.userNotVerified;
        //                        return Request.CreateResponse(HttpStatusCode.OK, response);
        //                    }

        //                    response.data = up;
        //                    response.error = false;
        //                    response.message = ResponseKeys.msgSuccess;
        //                    return Request.CreateResponse(HttpStatusCode.OK, response);
        //                }
        //                else
        //                {
        //                    response.error = true;
        //                    response.message = ResponseKeys.userNotFound;
        //                    return Request.CreateResponse(HttpStatusCode.OK, response);
        //                }
        //            }
        //        }
        //        else
        //        {
        //            response.error = true;
        //            response.message = ResponseKeys.invalidParameters;
        //            return Request.CreateResponse(HttpStatusCode.OK, response);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        response.error = true;
        //        Logger.WriteLog(ex);
        //        Logger.WriteLog(model);
        //        response.message = ResponseKeys.serverError;
        //        return Request.CreateResponse(HttpStatusCode.OK, response);
        //    }
        //}

        //[HttpPost]
        //public HttpResponseMessage WalkInFareEstimate([FromBody] RequestModel model)
        //{
        //    try
        //    {
        //        //TBD: Inherit this controller from another and set following proerties in that controller to remove redundancy

        //        if (Request.Headers.Contains("ApplicationID"))
        //        {
        //            ApplicationID = Request.Headers.GetValues("ApplicationID").First();
        //        }

        //        if (model != null && !string.IsNullOrEmpty(model.distance) && !string.IsNullOrEmpty(model.resellerID) &&
        //            !string.IsNullOrEmpty(model.resellerArea) && !string.IsNullOrEmpty(model.pickUplatitude) &&
        //            !string.IsNullOrEmpty(model.pickUplongitude) && !string.IsNullOrEmpty(model.lat.ToString()) && !string.IsNullOrEmpty(model.lon.ToString()))
        //        {
        //            using (CangooEntities context = new CangooEntities())
        //            {
        //                decimal totalFare = 0.0M;
        //                FareManager fareManag = new FareManager();

        //                dic = new Dictionary<dynamic, dynamic>() {
        //                    { "discountType", "normal"},
        //                    { "discountAmount", "0.00" }
        //                };

        //                //TBD: Vehcile capacity to be fetched based on driver current vehicle
        //                totalFare = Common.CalculateEstimatedFare(4, true, false, "", false, false, null, ApplicationID, model.pickUplatitude, model.pickUplongitude, model.lat.ToString(), model.lon.ToString(), ref fareManag, ref fareManag, ref dic); //(Convert.ToDecimal(model.distance) / 1000).ToString(), null

        //                dic.Add("estimatedPrice", string.Format("{0:0.00}", totalFare));

        //                response.error = false;
        //                response.data = dic;
        //                response.message = ResponseKeys.msgSuccess;
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
        //    catch (Exception ex)
        //    {
        //        response.error = true;
        //        Logger.WriteLog(ex);
        //        Logger.WriteLog(model);
        //        response.message = ResponseKeys.serverError;
        //        return Request.CreateResponse(HttpStatusCode.OK, response);
        //    }
        //}

        //[HttpPost]
        //public HttpResponseMessage WalkInCollectMobilePayment([FromBody] RequestModel model)
        //{
        //    try
        //    {
        //        //TBD: Inherit this controller from another and set following proerties in that controller to remove redundancy

        //        if (Request.Headers.Contains("ApplicationID"))
        //        {
        //            ApplicationID = Request.Headers.GetValues("ApplicationID").First();
        //        }

        //        if (model != null && !string.IsNullOrEmpty(model.driverID) && !string.IsNullOrEmpty(model.userID) && !string.IsNullOrEmpty(model.vehicleID) &&
        //            !string.IsNullOrEmpty(model.distance) && !string.IsNullOrEmpty(model.fleetID) &&
        //            !string.IsNullOrEmpty(model.estimatedFare) && !string.IsNullOrEmpty(model.isOverride) &&
        //            !string.IsNullOrEmpty(model.passengerName) && !string.IsNullOrEmpty(model.paymentMode) &&
        //            !string.IsNullOrEmpty(model.dropOfflatitude) && !string.IsNullOrEmpty(model.dropOfflongitude) &&
        //            !string.IsNullOrEmpty(model.pickUplatitude) && !string.IsNullOrEmpty(model.pickUplongitude))
        //        {
        //            using (CangooEntities context = new CangooEntities())
        //            {
        //                if (!string.IsNullOrEmpty(model.tripID))
        //                {
        //                    dic = new Dictionary<dynamic, dynamic>();
        //                    if (CheckIfAlreadyPaid(model.estimatedFare, model.tripID, model.driverID, ref dic, true))
        //                    {
        //                        FireBaseController fc = new FireBaseController();
        //                        fc.freeUserFromWalkInTrip(model.userID, model.tripID);

        //                        response.data = dic;
        //                        response.error = false;
        //                        response.message = ResponseKeys.fareAlreadyPaid;
        //                        return Request.CreateResponse(HttpStatusCode.OK, response);
        //                    }
        //                }

        //                var user = context.UserProfiles.Where(u => u.UserID.ToString() == model.userID).FirstOrDefault();
        //                if (user == null)
        //                {
        //                    response.error = true;
        //                    response.message = ResponseKeys.userNotFound;
        //                    return Request.CreateResponse(HttpStatusCode.OK, response);
        //                }

        //                dic = new Dictionary<dynamic, dynamic>() {
        //                        { "discountType", "normal"},
        //                        { "discountAmount", "0.00" }
        //                    };

        //                walkInPassengerPaypalPaymentFCM pfcm = new walkInPassengerPaypalPaymentFCM()
        //                {
        //                    isPaymentRequested = true,
        //                    paymentRequestTime = Common.getUtcDateTime().ToString(Common.dateFormat),
        //                    userID = model.userID,
        //                    pickUplatitude = model.pickUplatitude,
        //                    pickUplongitude = model.pickUplongitude,
        //                    dropOfflatitude = model.dropOfflatitude,
        //                    dropOfflongitude = model.dropOfflongitude,
        //                    driverID = model.driverID,
        //                    ressellerID = ApplicationID,
        //                    estimatedFare = model.estimatedFare,
        //                    distance = model.distance,
        //                    isOverride = model.isOverride,
        //                    vehicleID = model.vehicleID,
        //                    paymentMode = model.paymentMode,
        //                    fleetID = model.fleetID,
        //                    walletTotalAmount = user.WalletBalance != null ? string.Format("{0:0.00}", user.WalletBalance.ToString()) : "0.00",
        //                    newTripID = string.IsNullOrEmpty(model.tripID) ? Guid.NewGuid().ToString() : model.tripID,
        //                    passengerName = model.passengerName,
        //                    discountType = dic["discountType"],
        //                    discountAmount = dic["discountAmount"],
        //                    promoCodeID = ""
        //                };

        //                //UserController uc = new UserController();

        //                string promotionId = Common.IsSpecialPromotionApplicable(model.pickUplatitude, model.pickUplongitude, model.dropOfflatitude, model.dropOfflongitude, ApplicationID, context, ref dic);

        //                if (!string.IsNullOrEmpty(promotionId))
        //                {
        //                    pfcm.discountType = dic["discountType"];
        //                    pfcm.discountAmount = dic["discountAmount"];
        //                    pfcm.promoCodeID = promotionId;
        //                }
        //                else
        //                {
        //                    promotionId = Common.ApplyPromoCode(ApplicationID, model.userID, context, ref dic);
        //                    if (!string.IsNullOrEmpty(promotionId))
        //                    {
        //                        pfcm.discountType = dic["discountType"];
        //                        pfcm.discountAmount = dic["discountAmount"];
        //                        pfcm.promoCodeID = promotionId;
        //                    }
        //                }

        //                FireBaseController fb = new FireBaseController();

        //                //By mistake request was sent to wrong user, free the user
        //                if (!string.IsNullOrEmpty(model.walkInOldUserID))
        //                {
        //                    fb.freeUserFromWalkInTrip(model.walkInOldUserID, pfcm.newTripID.ToString());
        //                }

        //                fb.setWalkInPaymentData(model.driverID, model.userID, pfcm);

        //                var task = Task.Run(async () =>
        //                {
        //                    FireBaseController fc = new FireBaseController();
        //                    if (model.paymentMode.ToLower().Equals("paypal"))
        //                        await fc.sentSingleFCM(user.DeviceToken, pfcm, "pas_WalkInPassengerPaypalPayment");
        //                    else
        //                        await fc.sentSingleFCM(user.DeviceToken, pfcm, "pas_WalkInPassengerCreditCardPayment");
        //                });

        //                dic = new Dictionary<dynamic, dynamic> {
        //                    {"newTripID", pfcm.newTripID}
        //                };
        //            }

        //            response.error = false;
        //            response.data = dic;
        //            response.message = ResponseKeys.msgSuccess;
        //            return Request.CreateResponse(HttpStatusCode.OK, response);
        //        }
        //        else
        //        {
        //            response.error = true;
        //            response.message = ResponseKeys.invalidParameters;
        //            return Request.CreateResponse(HttpStatusCode.OK, response);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        response.error = true;
        //        Logger.WriteLog(ex);
        //        Logger.WriteLog(model);
        //        response.message = ResponseKeys.serverError;
        //        return Request.CreateResponse(HttpStatusCode.OK, response);
        //    }
        //}

        //[HttpPost]
        //[Route("sendChangeFareRequest")]
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
        //[Route("collectPaypalPayment")]
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

        #endregion

        #region HelperFunctions

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

        //private void ApplyPromoCode(string promoDiscountAmount, Trip trip, CangooEntities context)
        //{
            //If voucher is applied - Wallet and PromoDiscount can't be applied

            //if (Convert.ToDecimal(voucherUsedAmount) == 0)
            //{
            //    VoucherService.RefundFullVoucherAmount(trip);
            //}

            //if (Convert.ToDecimal(voucherUsedAmount) > 0)
            //{
            //    trip.PromoDiscount = 0;
            //    trip.WalletAmountUsed = 0;

            //    var voucher = context.CompanyVouchers.Where(cv => cv.VoucherID == trip.VoucherID && cv.isUsed == false).FirstOrDefault();
            //    if (voucher != null)
            //    {
            //        //Add extra voucher amount back to company balance.
            //        if (voucher.Amount > Convert.ToDecimal(voucherUsedAmount))
            //        {
            //            var company = context.Companies.Where(c => c.CompanyID == voucher.CompanyID).FirstOrDefault();
            //            company.CompanyBalance += (voucher.Amount - Convert.ToDecimal(voucherUsedAmount));
            //            voucher.Amount = Convert.ToDecimal(voucherUsedAmount);
            //        }
            //        voucher.isUsed = true;
            //    }
            //}
            //else
            //{
            //    trip.VoucherID = null;

                //trip.PromoDiscount = Convert.ToDecimal(promoDiscountAmount);

                //if (Convert.ToDecimal(promoDiscountAmount) > 0)
                //{
                //    var userPromo = context.UserPromos.Where(up => up.PromoID.ToString().Equals(trip.PromoCodeID.ToString())
                //    && up.UserID.ToString().ToLower().Equals(trip.UserID.ToString().ToLower())
                //    && up.isActive == true).FirstOrDefault();
                //    if (userPromo != null)
                //    {
                //        userPromo.NoOfUsage += 1;
                //    }
                //}
            //}

            //trip.WalletAmountUsed = Convert.ToDecimal(walletUsedAmount);

            //if (Convert.ToDecimal(walletUsedAmount) > 0)
            //{
            //    var user = context.UserProfiles.Where(up => up.UserID.Equals(trip.UserID.ToString())).FirstOrDefault();
            //    if (user != null)
            //    {
            //        user.WalletBalance -= Convert.ToDecimal(walletUsedAmount);
            //    }
            //}

            //TBD: Check if any of the amounts is > 0, only then save db changes

            //context.SaveChanges();
        //}

        //private async Task<bool> CheckIfAlreadyPaid(string totalFare, string tripID, string driverID, Dictionary<dynamic, dynamic> dic, bool isWalkIn)
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
        //                await FirebaseService.FareAlreadyPaidFreeUserAndDriver(tripID, trip.UserID.ToString(), driverID);
        //                dic.Add("tripID", trip.TripID.ToString());
        //                dic.Add("tip", trip.Tip == null ? "0.00" : trip.Tip.ToString());
        //                dic.Add("amount", string.Format("{0:0.00}", Convert.ToDouble(totalFare)));
        //            }

        //            return true;
        //        }
        //        else
        //        {
        //            return false;
        //        }
        //    }
        //}

        //private void CheckWalletBalance(string userID, CangooEntities context, ref DriverEndTripResponse dic)
        //{
        //    var user = context.UserProfiles.Where(u => u.UserID.ToString().Equals(userID)).FirstOrDefault();
        //    //When ride was booked by some hotel / company 
        //    if (user != null)
        //    {
        //        dic.isWalletPreferred = user.isWalletPreferred;

        //        if (user.WalletBalance != null)
        //            dic.availableWalletBalance = string.Format("{0:0.00}", (decimal)user.WalletBalance);
        //        else
        //            dic.availableWalletBalance = string.Format("{0:0.00}", 0);
        //    }
        //    else
        //    {
        //        dic.isWalletPreferred = false;
        //        dic.availableWalletBalance = string.Format("{0:0.00}", 0);
        //    }
        //}

        private LaterBookingConflictDTO checkLaterBookingDate(string captainID, DateTime pickUpDateTime)
        {
            using (CangooEntities context = new CangooEntities())
            {
                LaterBookingConflictDTO lbc = new LaterBookingConflictDTO();
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

        private async Task SendInvoice(InvoiceModel model)
        {
            var headerLink = this.Url.Link("Default", new { Controller = "Invoice", Action = "Header" });
            var footerLink = this.Url.Link("Default", new { Controller = "Invoice", Action = "Footer" });

            System.Web.Routing.RouteData route = new System.Web.Routing.RouteData();
            route.Values.Add("action", "SendInvoice");
            route.Values.Add("controller", "Invoice");

            InvoiceController controllerObj = new InvoiceController();
            System.Web.Mvc.ControllerContext newContext = new System.Web.Mvc.ControllerContext(new HttpContextWrapper(System.Web.HttpContext.Current), route, controllerObj);
            controllerObj.ControllerContext = newContext;

            await controllerObj.SendInvoice(model, headerLink, footerLink);
        }

        private static async Task FreePassengerAndDriver(string driverId, string tripId, string passengerId)
        {
            await FirebaseService.SetDriverFree(driverId, tripId);
            await FirebaseService.FreePassengerFromCurrentTrip(passengerId, tripId);
            await FirebaseService.DeleteTrip(tripId);
        }

        #endregion
    }
}