using Constants;
using DTOs.API;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.Owin.Security;
using Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace API.Controllers
{
    [Authorize]
    [RoutePrefix("api/User")]
    public class UserController : BaseController
    {
        #region Authentication

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
                ResponseWrapper.Data = new GenerateOTPResponse
                {
                    OTP = await TextMessageService.SendAuthenticationOTP(model.PhoneNumber),
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
                    await TextMessageService.SendWelcomeSMS(model.PhoneNumber);

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

            if ((await UserService.GetProfileByIdAsync(user.Id, ApplicationID, ResellerID)) == null)
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
                return Request.CreateResponse(HttpStatusCode.NotFound, new ResponseWrapper { Message = ResponseKeys.userNotFound });
            }

            string newPassword = AuthenticationService.GetRandomPassword();
            string hashedNewPassword = userManager.PasswordHasher.HashPassword(newPassword);

            await store.SetPasswordHashAsync(user, hashedNewPassword);
            await store.UpdateAsync(user);

            await TextMessageService.SendForgotPasswordSMS(newPassword, model.PhoneNumber);

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
                return Request.CreateResponse(HttpStatusCode.OK, new ResponseWrapper { Message = ResponseKeys.inCorrectCurrentPassword });
            }

            string hashedNewPassword = userManager.PasswordHasher.HashPassword(model.NewPassword);
            await store.SetPasswordHashAsync(user, hashedNewPassword);
            await store.UpdateAsync(user);

            return Request.CreateResponse(HttpStatusCode.OK, new ResponseWrapper { Message = ResponseKeys.msgSuccess, Error = false });
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

        [HttpPost]
        [Route("verify-device-token")]
        public async Task<HttpResponseMessage> VerifyDeviceToken([FromBody] PassengerVerifyDeviceTokenRequest model)
        {
            var userProfile = await UserService.GetProfileByIdAsync(model.PassengerId, ApplicationID, ResellerID);

            if ((await UserService.GetProfileByIdAsync(model.PassengerId, ApplicationID, ResellerID)) == null)
            {
                return Request.CreateResponse(HttpStatusCode.NotFound, new ResponseWrapper { Message = ResponseKeys.userProfileNotFound });
            }

            return Request.CreateResponse(HttpStatusCode.OK,
                new ResponseWrapper
                {
                    Error = false,
                    Message = ResponseKeys.msgSuccess,
                    Data = new PassengerVerifyDeviceTokenResponse
                    {
                        IsTokenVerified = userProfile.DeviceToken.Equals(model.DeviceToken).ToString()
                    }
                });
        }

        [HttpGet]
        [Route("auth-token-validation")]
        public HttpResponseMessage TokenValidation()
        {
            return Request.CreateResponse(HttpStatusCode.OK);
        }
        
        #endregion

        #region Profile

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
                }

                ResponseWrapper.Error = false;
                ResponseWrapper.Message = ResponseKeys.msgSuccess;
                return Request.CreateResponse(HttpStatusCode.OK, ResponseWrapper);
            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResponseWrapper { Message = ResponseKeys.invalidParameters });
            }
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
        public async Task<HttpResponseMessage> UpdateName([FromBody] UpdatePassengerNameRequest model)
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

        [HttpGet]
        [Route("update-email-otp")]
        public async Task<HttpResponseMessage> GetUpdateEmailOTP([FromUri] UpdatePassengerEmailOTPRequest model)
        {
            return Request.CreateResponse(HttpStatusCode.OK, new ResponseWrapper
            {
                Error = false,
                Message = ResponseKeys.msgSuccess,
                Data = new UpdatePassengerEmailOTPResponse
                {
                    OTP = await EmailService.SendEmailOTPAsync(model.Email)
                }
            });
        }

        [HttpPost]
        [Route("update-email")]
        public async Task<HttpResponseMessage> UpdateEmail([FromBody] UpdatePassengerEmailRequest model)
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

        [HttpGet]
        [Route("update-phone-number-otp")]
        public async Task<HttpResponseMessage> UpdatePhoneNumberOTP([FromUri] UpdatePassengerPhoneNumberOTPRequest model)
        {
            var user = await UserService.GetByUserNameAsync(model.PhoneNumber);

            if (user != null)
                return Request.CreateResponse(HttpStatusCode.OK, new ResponseWrapper { Message = ResponseKeys.userAlreadyRegistered });

            return Request.CreateResponse(HttpStatusCode.OK, new ResponseWrapper
            {
                Error = false,
                Message = ResponseKeys.msgSuccess,
                Data = new UpdatePassengerPhoneNumberOTPResponse
                {
                    OTP = await TextMessageService.SendChangePhoneNumberOTP(model.PhoneNumber)
                }
            });
        }

        [HttpPost]
        [Route("update-phone-number")]
        public async Task<HttpResponseMessage> UpdatePhoneNumber([FromBody] UpdatePassengerPhoneNumberRequest model)
        {
            await UserService.UpdatePhoneNumberAsync(model.PhoneNumber, model.CountryCode, model.PassengerId);

            return Request.CreateResponse(HttpStatusCode.OK, new ResponseWrapper
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

        #endregion

        #region Places

        [HttpPost]
        [Route("add-place")]
        public async Task<HttpResponseMessage> AddPlace([FromBody] AddPassengerPlaceRequest model)
        {
            var result = await PassengerPlacesService.AddPlace(model);
            if (result > 0)
            {
                return Request.CreateResponse(HttpStatusCode.OK, new ResponseWrapper
                {
                    Error = false,
                    Message = ResponseKeys.msgSuccess
                });
            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.Forbidden, new ResponseWrapper { Message = ResponseKeys.failedToAdd });
            }
        }

        [HttpPost]
        [Route("update-place")]
        public async Task<HttpResponseMessage> UpdatePlace([FromBody] UpdatePassengerPlaceRequest model)
        {
            var result = await PassengerPlacesService.UpdatePassengerPlaces(model);

            if (result == 0)
            {
                return Request.CreateResponse(HttpStatusCode.OK, new ResponseWrapper
                {
                    Message = ResponseKeys.failedToUpdate
                });
            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.OK, new ResponseWrapper
                {
                    Error = false,
                    Message = ResponseKeys.msgSuccess
                });
            }
        }


        [HttpGet]
        [Route("get-places")]
        public async Task<HttpResponseMessage> GetPlaces([FromUri] GetPassengerPlaceRequest model)
        {
            return Request.CreateResponse(HttpStatusCode.OK, new ResponseWrapper
            {
                Error = false,
                Message = ResponseKeys.msgSuccess,
                Data = await PassengerPlacesService.GetPassengerPlaces(model.PassengerId)
            });
        }

        #endregion

        #region Favorite Captain

        [HttpPost]
        [Route("search-drivers")]
        public async Task<HttpResponseMessage> SearchDrivers([FromBody] SearchDriversRequest model)
        {
            return Request.CreateResponse(HttpStatusCode.OK, new ResponseWrapper
            {
                Error = false,
                Message = ResponseKeys.msgSuccess,
                Data = await FavoritesService.GetDriversSerachResultListAsync(model.DriverUserName)
            });
        }

        [HttpPost]
        [Route("add-fav-driver")]
        public async Task<HttpResponseMessage> AddFavCaptain([FromBody] AddFavoriteDriverRequest model)
        {
            return Request.CreateResponse(HttpStatusCode.OK, new ResponseWrapper
            {
                Error = false,
                Message = ResponseKeys.msgSuccess,
                Data = await FavoritesService.AddFavoriteDriverAsync(model.DriverId, model.PassengerId, ApplicationID)
            });
        }

        [HttpPost]
        [Route("del-fav-driver")]
        public async Task<HttpResponseMessage> DelFavCaptain([FromBody] DeleteFavoriteDriverRequest model)
        {
            return Request.CreateResponse(HttpStatusCode.OK, new ResponseWrapper
            {
                Error = false,
                Message = ResponseKeys.msgSuccess,
                Data = await FavoritesService.DeleteFavoriteDriverAsync(model.DriverId, model.PassengerId)
            });
        }

        [HttpPost]
        [Route("get-fav-drivers")]
        public async Task<HttpResponseMessage> GetFavCaptains([FromBody] FavoriteDriversListRequest model)
        {
            return Request.CreateResponse(HttpStatusCode.OK, new ResponseWrapper
            {
                Error = false,
                Message = ResponseKeys.msgSuccess,
                Data = await FavoritesService.GetFavoriteDriversListAsync(model.PassengerId)
            });
        }

        #endregion

        #region Language

        [HttpGet]
        [Route("get-languages")]
        public async Task<HttpResponseMessage> GetLanguages()
        {
            return Request.CreateResponse(HttpStatusCode.OK, new ResponseWrapper
            {
                Error = false,
                Message = ResponseKeys.msgSuccess,
                Data = await LanguageService.GetAllLanguages()
            });
        }

        [HttpPost]
        [Route("update-language")]
        public async Task<HttpResponseMessage> UpdateLanguage([FromBody] UpdateLanguageRequest model)
        {
            return Request.CreateResponse(HttpStatusCode.OK, await LanguageService.UpdatePassengerLanguage(model));
        }

        #endregion

        #region TrustedContact

        [HttpGet]
        [Route("get-trusted-contact")]
        public async Task<HttpResponseMessage> GetTrustedContact([FromUri] GetTrustedContactRequest model)
        {
            return Request.CreateResponse(HttpStatusCode.OK, new ResponseWrapper
            {
                Error = false,
                Message = ResponseKeys.msgSuccess,
                Data = await TrustedContactManagerService.GetTrustedContact(model.PassengerId)
            });
        }

        [HttpPost]
        [Route("update-trusted-contact")]
        public async Task<HttpResponseMessage> UpdateTrustedContact([FromBody] UpdateTrustedContactRequest model)
        {
            var result = await TrustedContactManagerService.UpdateTrustedContact(model);

            if (result == 0)
            {
                return Request.CreateResponse(HttpStatusCode.NotFound, new ResponseWrapper
                {
                    Message = ResponseKeys.failedToUpdate
                });
            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.OK, new ResponseWrapper
                {
                    Error = false,
                    Message = ResponseKeys.msgSuccess,
                    Data = new UpdateTrustedContactResponse
                    {
                        FirstName = model.FirstName,
                        CountryCode = model.CountryCode,
                        MobileNo = model.MobileNo,
                        Email = model.Email
                    }
                });
            }
        }

        #endregion

        #region Reward Points

        [HttpGet]
        [Route("passenger-earned-reward-points")]
        public async Task<HttpResponseMessage> PassengerEarnedRewardPoints([FromUri] GetPassengerCangoosRequest model)
        {
            var result = await RewardPointService.GetPassengerRewardPoint(model.PassengerId);
            return Request.CreateResponse(HttpStatusCode.OK, new ResponseWrapper
            {
                Error = false,
                Message = ResponseKeys.msgSuccess,
                Data = new PassengerEarnedRewardRespose
                {
                    RewardPoint = result.RewardPoint
                }
            });
        }

        [HttpPost]
        [Route("redeem-reward-points")]
        public async Task<HttpResponseMessage> RedeemRewardPoints([FromBody] ReedemPassengerCangoosRequsest model)
        {
            var result = await RewardPointService.ReedemPassengerPoints(model);
            return Request.CreateResponse(HttpStatusCode.OK, new ResponseWrapper
            {
                Error = false,
                Message = ResponseKeys.msgSuccess,
                Data = new PassengerReedemRewardResponse
                {
                    RewardPoint = result.RewardPoint,
                    WalletBalance = result.WalletBalance,
                    AvailableWalletBalance = result.AvailableWalletBalance
                }
            });
        }

        [HttpGet]
        [Route("reward-points-list")]
        public async Task<HttpResponseMessage> RewardPointsList()
        {
            return Request.CreateResponse(HttpStatusCode.OK, new ResponseWrapper
            {
                Error = false,
                Message = ResponseKeys.msgSuccess,
                Data = new RewardPointResponse
                {
                    Rewards = await RewardPointService.GetRewards()
                }
            });
        }

        #endregion

        #region Trip Listings

        [HttpGet]
        [Route("completed-trips")]
        public async Task<HttpResponseMessage> CompletedTrips([FromUri] PassengerTripsListRequest model)
        {
            var lstTrips = await TripsManagerService.GetPassengerCompletedTrips(model.PassengerId, int.Parse(model.OffSet), int.Parse(model.Limit));

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

        [HttpGet]
        [Route("cancelled-trips")]
        public async Task<HttpResponseMessage> CancelledTrips([FromUri] PassengerTripsListRequest model)
        {
            var lstTrips = await TripsManagerService.GetPassengerCancelledTrips(model.PassengerId, int.Parse(model.OffSet), int.Parse(model.Limit));

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

        [HttpGet]
        [Route("scheduled-trips")]
        public async Task<HttpResponseMessage> ScheduledTrips([FromUri] PassengerTripsListRequest model)
        {
            var lstTrips = await TripsManagerService.GetPassengerScheduledTrips(model.PassengerId, int.Parse(model.OffSet), int.Parse(model.Limit));

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

        [HttpGet]
        [Route("trip-details")]
        public async Task<HttpResponseMessage> TripDetails([FromUri] PassengerTripDetailRequest model)
        {
            return Request.CreateResponse(HttpStatusCode.OK, new ResponseWrapper
            {
                Error = false,
                Message = ResponseKeys.msgSuccess,
                Data = await TripsManagerService.GetFullTripById(model.TripId)
            });
        }

        [HttpPost]
        [Route("update-trip-payment-method")]
        public async Task<HttpResponseMessage> UpdateTripPaymentMethod([FromBody] UpdateTripPaymentMethodRequest model)
        {
            return Request.CreateResponse(HttpStatusCode.OK, await TripsManagerService.UpdateTripPaymentMode(model));
        }

        [HttpPost]
        [Route("update-trip-tip-amount")]
        public async Task<HttpResponseMessage> UpdateTripTipAmount([FromBody] UpdateTripTipAmountRequest model)
        {
            await FirebaseService.SetTipAmount(model.TripId, model.TipAmount);
            return Request.CreateResponse(HttpStatusCode.OK, new ResponseWrapper
            {
                Error = false,
                Message = ResponseKeys.msgSuccess
            });
        }

        #endregion

        #region Promotions (Promo Codes)

        [HttpPost]
        [Route("add-promo-code")]
        public async Task<HttpResponseMessage> AddPromoCode([FromBody] AddPromoCodeRequest model)
        {
            var response = await PromoCodeService.AddUserPromoCode(model);

            if (response.Message.Equals(ResponseKeys.invalidPromo))
                return Request.CreateResponse(HttpStatusCode.NotFound, response);

            else if (response.Message.Equals(ResponseKeys.promoExpired))
                return Request.CreateResponse(HttpStatusCode.NoContent, response);

            else if (response.Message.Equals(ResponseKeys.promoLimitExceeded))
                return Request.CreateResponse(HttpStatusCode.NoContent, response);

            return Request.CreateResponse(HttpStatusCode.OK, response);
        }

        [HttpGet]
        [Route("get-promo-codes")]
        public async Task<HttpResponseMessage> GetPromoCodesList([FromUri] GetPassengerPromoRequest model)
        {
            return Request.CreateResponse(HttpStatusCode.OK, new ResponseWrapper
            {
                Error = false,
                Message = ResponseKeys.msgSuccess,
                Data = await PromoCodeService.GetPromoCodes(model.PassengerId)
            });
        }

        [HttpPost]
        [Route("apply-trip-promo-code")]
        public async Task<HttpResponseMessage> ApplyTripPromoCode([FromBody] ApplyPromoCodeRequest model)
        {
            return Request.CreateResponse(HttpStatusCode.OK,
                await PromoCodeService.UpdateTripPromo(model.CurrentUserPromoCodeId, model.NewUserPromoCodeId, model.PromoCodeId, model.TripId, model.PassengerId));
        }

        #endregion

        #region Payment

        [HttpGet]
        [Route("get-wallet")]
        public async Task<HttpResponseMessage> GetWalletDetails([FromUri] WalletDetailsRequest model)
        {
            var response = await PaymentsServices.GetUserWalletDetails(model.PassengerId, ApplicationID, ResellerID);

            if (response.Message.Equals(ResponseKeys.paymentGetwayError))
                return Request.CreateResponse(HttpStatusCode.NoContent, response);

            return Request.CreateResponse(HttpStatusCode.OK, response);
        }

        #region Wallet Recharge

        #region Coupon Code Recharge

        [HttpPost]
        [Route("redeem-coupon-code")]
        public async Task<HttpResponseMessage> RedeemCouponCode([FromBody] RedeemCouponCodeRequest model)
        {
            return Request.CreateResponse(HttpStatusCode.OK, await PaymentsServices.RedeemCouponCode(model.PassengerId, model.CouponCode));
        }

        #endregion

        #region Recharge using Card | Apple Pay | Google Pay

        [HttpGet]   //Required to make payment on client side
        [Route("get-payment-intent-client-secret")]
        public async Task<HttpResponseMessage> GetStripePaymentIntentClientSecret([FromUri] StripePaymentIntentClientSecretRequest model)
        {
            return Request.CreateResponse(HttpStatusCode.OK, new ResponseWrapper
            {
                Error = false,
                Message = ResponseKeys.msgSuccess,
                Data = new StripeClientSecretResponse
                {
                    ClientSecret = await PaymentsServices.GetPaymentIntentSecret(model.Email, model.PassengerId, model.Amount)
                }
            });
        }


        [HttpPost]
        [Route("mobile-payment-wallet-recharge")]
        public async Task<HttpResponseMessage> CardsWalletRecharge([FromBody] MobilePaymentWalletRechargeRequest model)
        {
            return Request.CreateResponse(HttpStatusCode.OK, await PaymentsServices.MobilePaymentWalletRecharge(model));
        }

        #endregion

        #region In-App transfer

        [HttpPost]
        [Route("check-application-user")]
        public async Task<HttpResponseMessage> CheckApplicationUser([FromBody] CheckAppUserRequest model)
        {
            return Request.CreateResponse(HttpStatusCode.OK, await UserService.IsAppUserExist(model.ReceiverMobileNo));
        }

        [HttpPost]
        [Route("transfer-wallet-balance")]
        public async Task<HttpResponseMessage> TransferWalletBalance([FromBody] ShareWalletBalanceRequest model)
        {
            return Request.CreateResponse(HttpStatusCode.OK, await PaymentsServices.TransferUsingMobile(model));
        }

        #endregion

        #endregion

        #region Add/Delete Cards

        [HttpGet]   //Required to add card on client side
        [Route("get-setup-intent-client-secret")]
        public async Task<HttpResponseMessage> GetStripeSetupIntentClientSecret([FromUri] StripeSetupIntentClientSecretRequest model)
        {
            return Request.CreateResponse(HttpStatusCode.OK, new ResponseWrapper
            {
                Error = false,
                Message = ResponseKeys.msgSuccess,
                Data = new StripeClientSecretResponse
                {
                    ClientSecret = await PaymentsServices.GetSetupIntentSecret(model.CustomerId, model.Email, model.PassengerId)
                }
            });
        }

        //[HttpPost]
        //[Route("update-default-credit-card")]
        //public async Task<HttpResponseMessage> UpdateDefaultCreditCard([FromBody] UpdateDefaultCreditCardRequest model)
        //{
        //    return Request.CreateResponse(HttpStatusCode.OK, await WalletServices.UpdateDefaultCreditCard(model.CardToken, model.CustomerId));
        //}

        [HttpPost]
        [Route("delete-credit-card")]
        public async Task<HttpResponseMessage> DeleteCreditCard([FromBody] DeleteCreditCardRequest model)
        {
            return Request.CreateResponse(HttpStatusCode.OK, PaymentsServices.DeleteCreditCard(model.CardToken, model.CustomerId)) ;
            
            
        }

        #endregion

        #region PayPal

        [HttpPost]  //Used to complete payment with PayPal / Wallet.
        [Route("add-paypal-account")]
        public async Task<HttpResponseMessage> AddPayPalAccount([FromBody] string model)
        {
            return Request.CreateResponse(HttpStatusCode.OK);
        }

        [HttpPost]  //Used to complete payment with PayPal / Wallet.
        [Route("paypal-payment")]
        public async Task<HttpResponseMessage> PayPalPayment([FromBody] string model)
        {
            return Request.CreateResponse(HttpStatusCode.OK);
            //req.paymentAmount shows total amount payable. TripFare + Tip - WalletUsedAmount - PromoDiscountAmount

            //if (model != null && !string.IsNullOrEmpty(model.tripID) && !string.IsNullOrEmpty(model.pID) &&
            //    !string.IsNullOrEmpty(model.fleetID) && !string.IsNullOrEmpty(model.isOverride) &&
            //    !string.IsNullOrEmpty(model.paymentTip) && !string.IsNullOrEmpty(model.paymentAmount) &&
            //    !string.IsNullOrEmpty(model.promoDiscountAmount) && !string.IsNullOrEmpty(model.walletUsedAmount.ToString()) &&
            //    !string.IsNullOrEmpty(model.paypalTransactionID))
            //{
            //    using (CanTaxiResellerEntities context = new CanTaxiResellerEntities())
            //    {
            //        //unlike captain function, just checks trip status, don't do anything else
            //        bool isAlreadyPaid = CheckIfAlreadyPaid(model.tripID);

            //        string paypalTransID = "";
            //        if (!isAlreadyPaid)
            //        {
            //            if (model.isBrainTree)
            //            {
            //                if (!ProcessBrainTreePayment(model.paypalTransactionID, Convert.ToDecimal(model.paymentAmount), out string transactionID))
            //                {
            //                    response.error = true;
            //                    response.message = AppMessage.paymentGetwayError;
            //                    return Request.CreateResponse(HttpStatusCode.OK, response);
            //                }
            //                paypalTransID = "Trip paypal payment received. Braintree transactionId = " + transactionID;
            //            }
            //            else
            //            {
            //                paypalTransID = "Trip paypal payment received. Paypal transactionId = " + model.paypalTransactionID;
            //            }
            //        }

            //        //If already paid, trip will not update the trip data but returns required info.

            //        var trip = context.spAfterMobilePayment(false,//Convert.ToBoolean(model.isOverride), 
            //            model.tripID,
            //            paypalTransID,
            //            (int)App_Start.TripStatus.Completed,
            //            model.pID,
            //            this.ApplicationID,
            //            (Convert.ToDouble(model.paymentAmount) - Convert.ToDouble(model.paymentTip)).ToString(),
            //            "0.00",
            //            model.promoDiscountAmount,
            //            model.walletUsedAmount,
            //            model.paymentTip,
            //            Common.getUtcDateTime(),
            //            decimal.Parse(model.walletUsedAmount) > 0 ? (int)App_Start.PaymentMode.Wallet : (int)App_Start.PaymentMode.Paypal,
            //            (int)App_Start.ResellerPaymentStatus.Paid,
            //            model.fleetID).FirstOrDefault();

            //        dic = new Dictionary<dynamic, dynamic>
            //        {
            //            { "tripID", model.tripID },
            //            { "tip", model.paymentTip },
            //            { "amount", string.Format("{0:0.00}", (Convert.ToDouble(model.paymentAmount) - Convert.ToDouble(model.paymentTip)) + Convert.ToDouble(model.walletUsedAmount) + Convert.ToDouble(model.promoDiscountAmount)) }
            //        };

            //        FireBaseController fc = new FireBaseController();
            //        var task = Task.Run(async () =>
            //        {
            //            if (!isAlreadyPaid) //may be captain have engaged in another trip
            //                await fc.sentSingleFCM(trip.DeviceToken, dic, "cap_paymentSuccess");

            //            await fc.delTripNode(model.tripID);
            //        });

            //        //to avoid login on another device during trip
            //        fc.addDeleteNode(true, "", "CustomerTrips/" + model.pID.ToString());

            //        //driver set as free
            //        fc.updateDriverStatus(trip.CaptainID.ToString(), "false", model.tripID);

            //        if (!isAlreadyPaid)
            //            SendInvoice(new InvoiceModel
            //            {
            //                CustomerEmail = trip.CustomerEmail, //context.AspNetUsers.Where(u => u.Id.Equals(model.pID)).FirstOrDefault().Email,
            //                TotalAmount = (Convert.ToDouble(model.paymentAmount) + Convert.ToDouble(model.walletUsedAmount) + Convert.ToDouble(model.promoDiscountAmount)).ToString(),
            //                CashAmount = "0",
            //                WalletUsedAmount = model.walletUsedAmount,
            //                PromoDiscountAmount = model.promoDiscountAmount,
            //                CaptainName = trip.CaptainName,
            //                CustomerName = trip.CustomerName,
            //                TripDate = trip.TripDate,
            //                InvoiceNumber = trip.InvoiceNumber,
            //                FleetName = trip.FleetName,
            //                ATUNumber = trip.FleetATUNumber,
            //                Street = trip.FleetAddress,
            //                BuildingNumber = trip.FleetBuildingNumber,
            //                PostCode = trip.FleetPostalCode,
            //                City = trip.FleetCity,
            //                PickUpAddress = trip.PickUpLocation,
            //                DropOffAddress = trip.DropOffLocation,
            //                CaptainUserName = trip.CaptainUserName,
            //                Distance = trip.DistanceInKM.ToString("0.00"),
            //                VehicleNumber = trip.PlateNumber,
            //                FleetEmail = trip.FleetEmail
            //            });

            //        response.message = AppMessage.msgSuccess;
            //        response.error = false;
            //        return Request.CreateResponse(HttpStatusCode.OK, response);
            //    }
            //}
            //else
            //{
            //    response.message = AppMessage.invalidParameters;
            //    response.error = true;
            //    return Request.CreateResponse(HttpStatusCode.OK, response);
            //}
        }

        #endregion

        #endregion

        #region Notifications

        [HttpGet]
        [Route("notifications-list")]
        public async Task<HttpResponseMessage> GetNotificationsList([FromUri] GetNotificationsListRequest model)
        {
            return Request.CreateResponse(HttpStatusCode.OK, new ResponseWrapper
            {
                Error = false,
                Message = ResponseKeys.msgSuccess,
                Data = await NotificationServices.GetValidNotifications(((int)ApplicationUserTypes.Passenger).ToString(), model.PassengerId)
            });
        }

        [HttpPost]
        [Route("read-notification")]
        public async Task<HttpResponseMessage> ReadNotification([FromBody] ReadNotificationRequest model)
        {
            return Request.CreateResponse(HttpStatusCode.OK, new ResponseWrapper
            {
                Error = false,
                Message = ResponseKeys.msgSuccess,
                Data = await NotificationServices.GetNotificationdetails(model.FeedId, model.PassengerId)
            });
        }

        #endregion  

        #region Invitation

        [HttpPost]
        [Route("apply-invite-code")]
        public async Task<HttpResponseMessage> ApplyInviteCode([FromBody] ApplyInviteCodeRequest model)
        {
            if (InvitationService.IsUserInviteCodeApplicable(model.PassengerId))
            {
                return Request.CreateResponse(HttpStatusCode.OK, new ResponseWrapper
                {
                    Error = true,
                    Message = ResponseKeys.inviteCodeNotApplicable,
                });
            }
            else if (InvitationService.IsUserInviteCodeAlreadyApplied(model.PassengerId))
            {
                return Request.CreateResponse(HttpStatusCode.OK, new ResponseWrapper
                {
                    Error = true,
                    Message = ResponseKeys.inviteCodeAlreadyApplied,
                });
            }
            else
            {
                var result = await InvitationService.ApplyInvitation(model);
                if (result > 0)
                {
                    return Request.CreateResponse(HttpStatusCode.OK, new ResponseWrapper
                    {
                        Error = false,
                        Message = ResponseKeys.msgSuccess,
                    });
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.OK, new ResponseWrapper
                    {
                        Error = true,
                        Message = ResponseKeys.invalidInviteCode,
                    });
                }
            }
        }

        #endregion

        #region Booking

        [HttpGet]
        [Route("recent-locations")]
        public async Task<HttpResponseMessage> GetRecentLocations([FromUri] RecentLocationsListRequest model)
        {
            return Request.CreateResponse(HttpStatusCode.OK, new ResponseWrapper
            {
                Error = false,
                Message = ResponseKeys.msgSuccess,
                Data = await PassengerPlacesService.GetRecentTripsLocations(model.PassengerId)
            });
        }

        [HttpPost]
        [Route("estimate-fare")]
        public async Task<HttpResponseMessage> EstimateFare([FromBody] EstimateFareRequest model)
        {
            if (PolygonService.IsLatLonExistsInPolygon(PolygonService.ConvertLatLonObjectsArrayToPolygonString(model.ApplicationAuthorizeArea), model.PickUpLatitude, model.PickUpLongitude))
            {
                return Request.CreateResponse(HttpStatusCode.OK, new ResponseWrapper
                {
                    Error = false,
                    Message = ResponseKeys.msgSuccess,
                    Data = await FareManagerService.GetFareEstimate(model.PickUpPostalCode, model.PickUpLatitude, model.PickUpLongitude,
                    model.MidwayStop1PostalCode, model.MidwayStop1Latitude, model.MidwayStop1Longitude,
                    model.DropOffPostalCode, model.DropOffLatitude, model.DropOffLongitude,
                    model.PolyLine, model.InBoundTimeInSeconds, model.InBoundDistanceInMeters, model.OutBoundTimeInSeconds, model.OutBoundDistanceInMeters)
                });
            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.Forbidden, new ResponseWrapper
                {
                    Error = true,
                    Message = ResponseKeys.userOutOfRange,
                });
            }
        }

        [HttpPost]
        [Route("book-trip")]
        public async Task<HttpResponseMessage> BookTrip([FromBody] BookTripRequest model)
        {
            var response = await TripsManagerService.BookNewTrip(model);

            if (response.Message.Equals(ResponseKeys.invalidParameters))
                return Request.CreateResponse(HttpStatusCode.Forbidden, response);

            else
                return Request.CreateResponse(HttpStatusCode.OK, response);
        }

        [HttpPost]  //Cancel normal booking which is not accepted yet
        [Route("time-out")]
        public async Task<HttpResponseMessage> TimeOut([FromBody] TripTimeOutRequest model)
        {
            return Request.CreateResponse(HttpStatusCode.OK, await TripsManagerService.TimeOutTrip(model.TripId, model.PassengerId));
        }

        [HttpPost]
        [Route("cancel-trip")]
        public async Task<HttpResponseMessage> CancelTrip([FromBody] CancelTripRequest model)
        {
            return Request.CreateResponse(HttpStatusCode.OK, await TripsManagerService.CancelTripByPassenger(model.TripId, model.PassengerId, model.DistanceTravelled, model.CancelID, model.IsLaterBooking));
        }

        [HttpPost]
        [Route("submit-feedback")]
        public async Task<HttpResponseMessage> SubmitFeedback([FromBody] UpdateTripUserFeedbackRequest model)
        {
            return Request.CreateResponse(HttpStatusCode.OK, await TripsManagerService.UserSubmitFeedback(model));
        }

        [HttpGet]
        [Route("dashboard")]
        public async Task<HttpResponseMessage> GetMiscData()
        {
            return Request.CreateResponse(HttpStatusCode.OK, new ResponseWrapper
            {
                Error = false,
                Message = ResponseKeys.msgSuccess,
                Data = new Dictionary<dynamic, dynamic>
                            {
                                {"TotalNotifications", await NotificationServices.GetValidNotificationsCount() },
                                {"CurrentUTCDateTime", DateTime.UtcNow.ToString(Formats.DateTimeFormat) },
                            }
            });
        }


        //[HttpGet]
        //[Route("current-utc-datetime")]
        //public HttpResponseMessage getCurrentUTCDateTime()
        //{
        //    return Request.CreateResponse(HttpStatusCode.OK, new ResponseWrapper
        //    {
        //        Error = false,
        //        Message = ResponseKeys.msgSuccess,
        //        Data = new Dictionary<dynamic, dynamic>
        //                    {
        //                        {"currentDateTime", DateTime.UtcNow.ToString(Formats.DateTimeFormat) }
        //                    }
        //    });
        //}

        #endregion
    }
}