using API.Filters;
using Constants;
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
    [Authorize]
    [RoutePrefix("api/User")]
    public class UserController : BaseController
    {
        #region auth / profile

        [HttpPost]
        [AllowAnonymous]
        [Route("generate-otp")]
        public async Task<HttpResponseMessage> GenerateOTP([FromBody] GenerateOTPRequest model)
        {
            if (await AuthenticationService.IsAppliactionBlockedAsync(ApplicationID))
                return Request.CreateResponse(HttpStatusCode.Forbidden, new ResponseWrapper
                {
                    Message = ResponseKeys.serviceNotAvailable,
                });

            if (!AuthenticationService.IsValidApplicationUser(ApplicationID))
                return Request.CreateResponse(HttpStatusCode.Forbidden, new ResponseWrapper
                {
                    Message = ResponseKeys.invalidUser,
                });

            model.PhoneNumber = model.PhoneNumber.Replace(" ", "");

            var user = await UserService.GetByUserNameAsync(model.PhoneNumber);

            var isUserProfileUpdated = user == null ? false : !string.IsNullOrEmpty(user.Email);

            ResponseWrapper.Data = new GenerateOTPResponse();

            if (isUserProfileUpdated)
            {
                ResponseWrapper.Data = new GenerateOTPResponse
                {
                    IsUserProfileUpdated = isUserProfileUpdated.ToString()
                };
            }
            else
            {
                string verificationCode = AuthenticationService.GenerateOTP();

                TextMessageService.SendSms(string.Format("Deine cangoo-TAN lautet\n{0}", verificationCode), model.PhoneNumber);

                ResponseWrapper.Data = new GenerateOTPResponse
                {
                    OTP = verificationCode,
                    IsUserProfileUpdated = isUserProfileUpdated.ToString()
                };
            }

            ResponseWrapper.Error = false;
            ResponseWrapper.Message = ResponseKeys.msgSuccess;

            return Request.CreateResponse(HttpStatusCode.OK, ResponseWrapper);
        }

        [HttpPost]
        [AllowAnonymous]
        [Route("register")]
        public async Task<HttpResponseMessage> Register([FromBody] PassengerRegisterRequest model)
        {
            if (await AuthenticationService.IsAppliactionBlockedAsync(ApplicationID))
                return Request.CreateResponse(HttpStatusCode.Forbidden, new ResponseWrapper
                {
                    Message = ResponseKeys.serviceNotAvailable,
                });

            if (!AuthenticationService.IsValidApplicationUser(ApplicationID))
                return Request.CreateResponse(HttpStatusCode.Forbidden, new ResponseWrapper
                {
                    Message = ResponseKeys.invalidUser,
                });

            model.PhoneNumber = model.PhoneNumber.Replace(" ", "");

            //Step 1 : Check if user already exists?
            var userStore = new UserStore<IdentityUser>();
            var userManager = new UserManager<IdentityUser>(userStore);
            var user = userManager.FindByName(model.PhoneNumber);

            if (user == null)
            {
                //Step 2 : If user don't exist, create user (firebase user id will be received as password from application), 
                //			return profileFlag and authentication token.

                userManager.UserValidator = new UserValidator<IdentityUser>(userManager) { AllowOnlyAlphanumericUserNames = false };
                string hashedNewPassword = userManager.PasswordHasher.HashPassword(model.Password);
                user = new IdentityUser() { UserName = model.PhoneNumber, PhoneNumber = model.PhoneNumber, PhoneNumberConfirmed = true, PasswordHash = hashedNewPassword };
                IdentityResult identityResult = userManager.Create(user);

                if (identityResult.Succeeded)
                {
                    //var roleresult = userManager.AddToRole(user.Id, App_Start.Enumration.returnRoleDes(SysetmRoles.User));

                    userManager.AddToRole(user.Id, Enum.GetName(typeof(SystemRoles), SystemRoles.User));

                    var authenticationManager = HttpContext.Current.GetOwinContext().Authentication;
                    var userIdentity = userManager.CreateIdentity(user, DefaultAuthenticationTypes.ApplicationCookie);
                    authenticationManager.SignIn(new AuthenticationProperties() { }, userIdentity);

                    string verificationCode = userManager.GenerateChangePhoneNumberToken(user.Id, user.PhoneNumber);

                    await UserService.CreateUserProfileAsync(user.Id, verificationCode, model.CountryCode, model.DeviceToken, ApplicationID, ResellerID);

                    //SendSMS.SendSms("Herzlich willkommen bei cangoo!", model.PhoneNumber);
                    TextMessageService.SendSms("Herzlich willkommen bei cangoo!", model.PhoneNumber);

                    //EmailManager.SendEmail(pasngr.email, "Welcome to Cangoo !!<br /> Your Cangoo account veification code is:" + verificationCode, "New User Welcome Email Subject", "support@cangoo.at", "Support Cangoo");

                    var response = await UserService.GetAccessTokenAndPassengerProfileData(user.Id, user.UserName, model.Password, model.DeviceToken, user.Email, user.PhoneNumber, ApplicationID, ResellerID, false, false, false);

                    //On register user password can't be wrong.
                    //if (response.Message.Equals(ResponseKeys.authenticationFailed))
                    //    return Request.CreateResponse(HttpStatusCode.Unauthorized, response);

                    //else //if (response.Message.Equals(ResponseKeys.msgSuccess))
                    return Request.CreateResponse(HttpStatusCode.OK, response);
                }
                else
                {
                    //response.error = true;
                    //response.message = AppMessage.failedToRegisterUser;
                    return Request.CreateResponse(HttpStatusCode.OK, new ResponseWrapper { Message = ResponseKeys.failedToRegisterUser });
                }
            }
            else
            {
                //Step 2.1 : If user exists check if profile is updated, return flag with profile data

                //if ((await UserService.GetUserProfileAsync(user.Id)) == null)
                //{
                //    return Request.CreateResponse(HttpStatusCode.NotFound, new ResponseWrapper { Message = ResponseKeys.userProfileNotFound });
                //}

                var response = await UserService.GetAccessTokenAndPassengerProfileData(user.Id, user.UserName, model.Password, model.DeviceToken, user.Email, user.PhoneNumber, ApplicationID, ResellerID, true, !string.IsNullOrEmpty(user.Email), false);

                if (response.Message.Equals(ResponseKeys.userBlocked))
                    return Request.CreateResponse(HttpStatusCode.Forbidden, response);

                else if (response.Message.Equals(ResponseKeys.authenticationFailed))
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, response);

                else //if (response.Message.Equals(ResponseKeys.msgSuccess))
                    return Request.CreateResponse(HttpStatusCode.OK, response);
            }
        }

        [HttpPost]
        [Route("complete-profile")]
        public async Task<HttpResponseMessage> CompleteProfile()
        {
            PassengerProfileRequest model = new PassengerProfileRequest
            {
                FirstName = HttpContext.Current.Request.Form["firstName"],
                LastName = HttpContext.Current.Request.Form["lastName"],
                Email = HttpContext.Current.Request.Form["email"],
                Password = HttpContext.Current.Request.Form["password"],
                PhoneNumber = HttpContext.Current.Request.Form["phoneNumber"]
            };

            if (model != null && !string.IsNullOrEmpty(model.FirstName) && !string.IsNullOrEmpty(model.LastName) &&
                !string.IsNullOrEmpty(model.Email) && !string.IsNullOrEmpty(model.Password) && !string.IsNullOrEmpty(model.PhoneNumber))
            {
                model.PhoneNumber = model.PhoneNumber.Replace(" ", "");
                var store = new UserStore<IdentityUser>();
                var userManager = new UserManager<IdentityUser>(store);
                var user = userManager.FindByName(model.PhoneNumber);

                if (user == null)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, new ResponseWrapper { Message = ResponseKeys.authenticationFailed });
                }

                string newPassword = model.Password;
                string hashedNewPassword = userManager.PasswordHasher.HashPassword(newPassword);

                await store.SetPasswordHashAsync(user, hashedNewPassword);

                user.Email = model.Email;
                await store.UpdateAsync(user);

                await UserService.UpdateNameAsync(model.FirstName, model.LastName, user.Id);

                ResponseWrapper.Data = new PassengerProfileResponse
                {
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Email = model.Email,
                    PhoneNumber = model.PhoneNumber,
                    ProfilePicture = "",
                    IsUserProfileUpdated = true.ToString()
                };

                HttpPostedFile postedFile = HttpContext.Current.Request.Files.Count > 0 ? HttpContext.Current.Request.Files[0] : null;

                if (postedFile != null && postedFile.ContentLength > 0)
                {
                    //Extension check is applied on application end.

                    //IList<string> AllowedFileExtensions = new List<string> { ".jpg", ".gif", ".png" };
                    //var extension = postedFile.FileName.Substring(postedFile.FileName.LastIndexOf('.')).ToLower();

                    //if (!AllowedFileExtensions.Contains(extension))
                    //{
                    //    response.data = dic;
                    //    response.error = true;
                    //    response.message = AppMessage.invalidFileExtension;
                    //    return Request.CreateResponse(HttpStatusCode.OK, response);
                    //}
                    //else
                    //{

                    //string path = "~/Images/User/" + psng.UserID + extension;
                    //var filePath = HttpContext.Current.Server.MapPath(path);
                    //postedFile.SaveAs(filePath);
                    //psng.ProfilePicture = path;
                    //psng.OriginalPicture = psng.UserID + extension;

                    //context.SaveChanges();
                    //dic["originalPicture"] =  path;

                    string uploadedFilePath = "~/Images/User/" + FilesManagerService.SaveFile(postedFile, "~/Images/User/", user.Id);

                    await UserService.UpdateImageAsync(uploadedFilePath, postedFile.FileName, user.Id);

                    ResponseWrapper.Data = new PassengerProfileResponse
                    {
                        FirstName = model.FirstName,
                        LastName = model.LastName,
                        Email = model.Email,
                        PhoneNumber = model.PhoneNumber,
                        ProfilePicture = uploadedFilePath,
                        IsUserProfileUpdated = true.ToString()
                    };
                    //}
                }

                ResponseWrapper.Error = false;
                ResponseWrapper.Message = ResponseKeys.msgSuccess;
                //response.data = dic;
                //    response.error = false;
                //    response.message = AppMessage.msgSuccess;
                return Request.CreateResponse(HttpStatusCode.OK, ResponseWrapper);
                //}
            }
            else
            {
                ResponseWrapper.Message = ResponseKeys.invalidParameters;
                return Request.CreateResponse(HttpStatusCode.OK, ResponseWrapper);
            }
        }

        [HttpPost]
        [AllowAnonymous]
        [Route("login")]
        public async Task<HttpResponseMessage> Login([FromBody] PassengerLoginRequest model)
        {
            //Step 1 : This end point is accessed only if user exists and profile is updated. Get Access Token and return user data.
            var store = new UserStore<IdentityUser>();
            var userManager = new UserManager<IdentityUser>(store);
            var user = userManager.FindByName(model.PhoneNumber);

            if (user == null)
            {
                return Request.CreateResponse(HttpStatusCode.Unauthorized, new ResponseWrapper { Message = ResponseKeys.authenticationFailed });
            }

            if ((await UserService.GetProfileAsync(user.Id)) == null)
            {
                return Request.CreateResponse(HttpStatusCode.NotFound, new ResponseWrapper { Message = ResponseKeys.userProfileNotFound });
            }

            var response = await UserService.GetAccessTokenAndPassengerProfileData(user.Id, user.UserName, model.Password, model.DeviceToken, user.Email, user.PhoneNumber, ApplicationID, ResellerID, true, true, true);

            if (response.Message.Equals(ResponseKeys.userBlocked))
                return Request.CreateResponse(HttpStatusCode.Forbidden, response);

            else if (response.Message.Equals(ResponseKeys.authenticationFailed))
                return Request.CreateResponse(HttpStatusCode.Unauthorized, response);

            else
                return Request.CreateResponse(HttpStatusCode.OK, response);

        }

        [HttpPost]
        [AllowAnonymous]
        [Route("forgot-password")]
        public async Task<HttpResponseMessage> ForgotPassword([FromBody] PassengerForgetPasswordRequest model)
        {
            var store = new UserStore<IdentityUser>();
            var userManager = new UserManager<IdentityUser>(store);
            IdentityUser user = userManager.FindByName(model.PhoneNumber);

            if (user == null)
            {
                return Request.CreateResponse(HttpStatusCode.Unauthorized, new ResponseWrapper { Message = ResponseKeys.authenticationFailed });
            }

            string newPassword = AuthenticationService.GetRandomPassword();
            string hashedNewPassword = userManager.PasswordHasher.HashPassword(newPassword);

            await store.SetPasswordHashAsync(user, hashedNewPassword);
            await store.UpdateAsync(user);

            TextMessageService.SendSms(string.Format("Das Passwort für dein cangoo - Konto wurde nun zurückgesetzt.\nDein neues Passwort lautet {0}", newPassword), model.PhoneNumber);

            return Request.CreateResponse(HttpStatusCode.OK, new ResponseWrapper { Error = false, Message = ResponseKeys.msgSuccess });
        }

        [HttpPost]
        [Route("change-password")]
        public async Task<HttpResponseMessage> ChangePassword([FromBody] PassengerChangePasswordRequest model)
        {
            var store = new UserStore<IdentityUser>();
            var userManager = new UserManager<IdentityUser>(store);

            IdentityUser user = await userManager.FindAsync(model.PhoneNumber, model.CurrentPassword);
            if (user == null)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResponseWrapper { Message = ResponseKeys.failedToResetPassword });
            }

            string hashedNewPassword = userManager.PasswordHasher.HashPassword(model.NewPassword);
            await store.SetPasswordHashAsync(user, hashedNewPassword);
            await store.UpdateAsync(user);

            return Request.CreateResponse(HttpStatusCode.OK, new ResponseWrapper { Message = ResponseKeys.msgSuccess, Error = false });
        }

        [HttpPost]
        [Route("update-profile-image")]
        public async Task<HttpResponseMessage> UpdateProfileImage()
        {
            //if (!string.IsNullOrEmpty(HttpContext.Current.Request.Params["pid"]))
            if (!string.IsNullOrEmpty(HttpContext.Current.Request.Params["PassengerId"]))
            {
                var userId = HttpContext.Current.Request.Params["PassengerId"];

                HttpPostedFile postedFile = HttpContext.Current.Request.Files.Count > 0 ? HttpContext.Current.Request.Files[0] : null;

                if (postedFile != null && postedFile.ContentLength > 0)
                {
                    //Extension check is applied on application end.

                    string uploadedFilePath = "~/Images/User/" + FilesManagerService.SaveFile(postedFile, "~/Images/User/", userId);

                    await UserService.UpdateImageAsync(uploadedFilePath, postedFile.FileName, userId);

                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResponseWrapper
                    {
                        Message = ResponseKeys.profileImageNotFound,
                        Error = false,
                        Data = new UpdateProfileImageResponse
                        {
                            ProfilePicture = uploadedFilePath
                        }
                    });
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResponseWrapper { Message = ResponseKeys.profileImageNotFound });
                }
            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResponseWrapper { Message = ResponseKeys.invalidParameters });
            }
        }

        [HttpPost]
        [Route("update-name")]
        public async Task<HttpResponseMessage> UpdateName(UpdatePassengerNameRequest model)
        {
            await UserService.UpdateNameAsync(model.FirstName, model.LastName, model.PassengerId);
            return Request.CreateResponse(HttpStatusCode.OK, new ResponseWrapper
            {
                Error = false,
                Message = ResponseKeys.msgSuccess,
                Data = new UpdatePassengerNameResponse
                {
                    LastName = model.LastName,
                    FirstName = model.FirstName
                }
            });
        }

        [HttpPost]
        [Route("update-email")]
        public async Task<HttpResponseMessage> UpdateEmail(UpdatePassengerEmailRequest model)
        {
            await UserService.UpdateEmailAsync(model.Email, model.PassengerId);
            return Request.CreateResponse(HttpStatusCode.OK, new ResponseWrapper
            {
                Error = false,
                Message = ResponseKeys.msgSuccess,
                Data = new UpdatePassengerEmailResponse
                {
                    Email = model.Email
                }
            });
        }

        [HttpPost]
        [Route("update-phone-number")]
        public async Task<HttpResponseMessage> UpdatePhoneNumber(UpdatePassengerPhoneNumberRequest model)
        {
            var user = await UserService.GetByUserNameAsync(model.PhoneNumber);

            if (user != null)
                return Request.CreateResponse(HttpStatusCode.Conflict, new ResponseWrapper { Message = ResponseKeys.userAlreadyRegistered });

            await UserService.UpdatePhoneNumberAsync(model.PhoneNumber, model.CountryCode, model.PassengerId);

            return Request.CreateResponse(HttpStatusCode.Conflict, new ResponseWrapper
            {
                Error = false,
                Message = ResponseKeys.msgSuccess,
                Data = new UpdatePassengerPhoneNumberResponse
                {
                    CountryCode = model.CountryCode,
                    PhoneNumber = model.PhoneNumber
                }
            });
        }

        [HttpPost]
        [Route("logout")]
        public async Task<HttpResponseMessage> LogOutPassenger([FromBody] PassengerLogOutRequest model)
        {
            await UserService.UpdateDeviceTokenAsync(null, model.PassengerId);
            return Request.CreateResponse(HttpStatusCode.OK, new ResponseWrapper
            {
                Error = false,
                Message = ResponseKeys.msgSuccess
            });
        }

        #endregion

        #region Profile

        //[HttpGet]
        //public HttpResponseMessage passengerStats(string passengerID)
        //{
        //	try
        //	{
        //		if (!string.IsNullOrEmpty(passengerID))
        //		{
        //			using (CanTaxiResellerEntities context = new CanTaxiResellerEntities())
        //			{
        //				var pas = (from up in context.UserProfiles
        //						   join anu in context.AspNetUsers on up.UserID equals anu.Id
        //						   where anu.Id == passengerID
        //						   select new
        //						   {
        //							   up.Rating,
        //							   up.NumberDriverFavourites,
        //							   up.FirstName,
        //							   up.LastName,
        //							   anu.Email,
        //							   anu.PhoneNumber
        //						   }).FirstOrDefault();

        //				if (pas != null)
        //				{
        //					dic = new Dictionary<dynamic, dynamic>
        //						{
        //							{ "rating", pas.Rating },
        //							{ "numberDriverFavourites", pas.NumberDriverFavourites },
        //							{ "firstName", pas.FirstName },
        //							{ "lastName", pas.LastName },
        //							{ "email", pas.Email },
        //							{ "phoneNumber", pas.PhoneNumber }
        //						};

        //					response.error = false;
        //					response.data = dic;
        //					response.message = AppMessage.msgSuccess;
        //					return Request.CreateResponse(HttpStatusCode.OK, response);
        //				}
        //				else
        //				{
        //					response.error = true;
        //					response.message = AppMessage.userNotFound;
        //					return Request.CreateResponse(HttpStatusCode.OK, response);
        //				}
        //			}
        //		}
        //		else
        //		{
        //			response.error = true;
        //			response.message = AppMessage.invalidParameters;
        //			return Request.CreateResponse(HttpStatusCode.OK, response);
        //		}
        //	}
        //	catch (Exception ex)
        //	{
        //		response.error = true;
        //		Logger.WriteLog(ex);
        //		response.message = AppMessage.serverError;
        //		return Request.CreateResponse(HttpStatusCode.OK, response);
        //	}
        //}

        #endregion

        #region Trips

        [HttpGet]
        [Route("completed-trips")]
        public async Task<HttpResponseMessage> CompletedTrips(string passengerId, int offSet, int limit)
        {
            if (passengerId != string.Empty && offSet > 0 && limit > 0)
            {
                var lstTrips = await TripsManagerService.GetPassengerCompletedTrips(passengerId, offSet, limit);

                return Request.CreateResponse(HttpStatusCode.OK, new ResponseWrapper
                {
                    Error = false,
                    Message = ResponseKeys.msgSuccess,
                    Data = new PassengerTripsListResponse
                    {
                        TotalRecords = lstTrips.Count > 0 ? lstTrips.FirstOrDefault().TotalRecord.ToString() : "0",
                        Trips = lstTrips
                    }
                });
            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResponseWrapper { Message = ResponseKeys.invalidParameters });
            }
        }

        [HttpGet]
        [Route("cancelled-trips")]
        public async Task<HttpResponseMessage> CancelledTrips(string passengerId, int offSet, int limit)
        {
            if (passengerId != string.Empty && offSet > 0 && limit > 0)
            {
                var lstTrips = await TripsManagerService.GetPassengerCancelledTrips(passengerId, offSet, limit);

                return Request.CreateResponse(HttpStatusCode.OK, new ResponseWrapper
                {
                    Error = false,
                    Message = ResponseKeys.msgSuccess,
                    Data = new PassengerTripsListResponse
                    {
                        TotalRecords = lstTrips.Count > 0 ? lstTrips.FirstOrDefault().TotalRecord.ToString() : "0",
                        Trips = lstTrips
                    }
                });
            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResponseWrapper { Message = ResponseKeys.invalidParameters });
            }
        }

        [HttpGet]
        [Route("scheduled-trips")]
        public async Task<HttpResponseMessage> ScheduledTrips(string passengerId, int offSet, int limit)
        {
            if (passengerId != string.Empty && offSet > 0 && limit > 0)
            {
                var lstTrips = await TripsManagerService.GetPassengerScheduledTrips(passengerId, offSet, limit);

                return Request.CreateResponse(HttpStatusCode.OK, new ResponseWrapper
                {
                    Error = false,
                    Message = ResponseKeys.msgSuccess,
                    Data = new PassengerTripsListResponse
                    {
                        TotalRecords = lstTrips.Count > 0 ? lstTrips.FirstOrDefault().TotalRecord.ToString() : "0",
                        Trips = lstTrips
                    }
                });
            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResponseWrapper { Message = ResponseKeys.invalidParameters });
            }
        }

        [HttpGet]
        [Route("trip-details")]
        public async Task<HttpResponseMessage> TripDetails(string tripId)
        {
            if (!string.IsNullOrEmpty(tripId))
            {
                return Request.CreateResponse(HttpStatusCode.OK, new ResponseWrapper
                {
                    Error = false,
                    Message = ResponseKeys.msgSuccess,
                    Data = await TripsManagerService.GetTripDetails(tripId)
                });
            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResponseWrapper { Message = ResponseKeys.invalidParameters });
            }
        }

        #endregion

    }
}