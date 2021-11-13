﻿using API.Filters;
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

            if ((await UserService.GetProfileAsync(user.Id, ApplicationID, ResellerID)) == null)
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
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResponseWrapper { Message = ResponseKeys.failedToResetPassword });
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
            var userProfile = await UserService.GetProfileAsync(model.PassengerId, ApplicationID, ResellerID);

            if ((await UserService.GetProfileAsync(model.PassengerId, ApplicationID, ResellerID)) == null)
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

        [HttpGet]
        [Route("update-phone-number-otp")]
        public async Task<HttpResponseMessage> UpdatePhoneNumberOTP([FromUri] UpdatePassengerPhoneNumberOTPRequest model)
        {
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
        public async Task<HttpResponseMessage> UpdatePhoneNumber(UpdatePassengerPhoneNumberRequest model)
        {
            var user = await UserService.GetByUserNameAsync(model.PhoneNumber);

            if (user != null)
                return Request.CreateResponse(HttpStatusCode.Conflict, new ResponseWrapper { Message = ResponseKeys.userAlreadyRegistered });

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

        #region Favorites

        [HttpPost]
        [Route("search-drivers")]
        public async Task<HttpResponseMessage> SearchDrivers(SearchDriversRequest model)
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
        public async Task<HttpResponseMessage> AddFavCaptain(AddFavoriteDriverRequest model)
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
        public async Task<HttpResponseMessage> DelFavCaptain(DeleteFavoriteDriverRequest model)
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
        public async Task<HttpResponseMessage> GetFavCaptains(FavoriteDriversListRequest model)
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
                Data = new GetLanguageRequestRespose
                {
                    Languages = await LanguageService.GetAllLanguages()
                }
            });
        }

        [HttpPost]
        [Route("update-language")]
        public async Task<HttpResponseMessage> UpdateLanguage( [FromBody] UpdateLanguageRequest model)
        {
            var result = await LanguageService.UpdatePassengerLanguage(model);

            if (result == 0)
            {
                return Request.CreateResponse(HttpStatusCode.NotModified, new ResponseWrapper
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

        #endregion

        #region TrustedContact

        [HttpGet]
        [Route("get-trusted-contact")]
        public async Task<HttpResponseMessage> GetTrustedContact([FromUri] GetTrustedContacts model)
        {
            var lstTrustedContact = await TrustedContactManagerService.GetTrustedContact(model.PassengerId);
            return Request.CreateResponse(HttpStatusCode.OK, new ResponseWrapper
            {
                Error = false,
                Message = ResponseKeys.msgSuccess,
                Data = new GetTrustedContactResponse
                {
                    Contact = lstTrustedContact
                }
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
        public async Task<HttpResponseMessage> PassengerEarnedRewardPoints([FromUri] GetPassengerEanedRewardPoints model)
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
        public async Task<HttpResponseMessage> RedeemRewardPoints([FromBody] PassengerReedemRewardRequsest model)
        {
            var result = await RewardPointService.ReedemPassengerPoints(model);
            return Request.CreateResponse(HttpStatusCode.OK, new ResponseWrapper
            {
                Error = false,
                Message = ResponseKeys.msgSuccess,
                Data = new PassengerReedemRewardResponse
                {
                    RewardPoint = result.RewardPoint,
                    WalletAmount = result.WalletAmount
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
        public async Task<HttpResponseMessage> UpdateTripPaymentMethod([FromBody] UpdateTripPaymentMethod model)
        {
            var result = await TripsManagerService.UpdateTripPaymentMode(model);

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
                    Message = ResponseKeys.msgSuccess
                });
            }
        }

        [HttpPost]
        [Route("update-trip-promo-code")]
        public async Task<HttpResponseMessage> UpdateTripPromoCode([FromBody] UpdateTripPromoCode model)
        {
            var result = await TripsManagerService.UpdateTripPromo(model);

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
                    Message = ResponseKeys.msgSuccess
                });
            }
        }

        [HttpPost]
        [Route("update-trip-tip-amount")]
        public async Task<HttpResponseMessage> UpdateTripTipAmount( [FromBody] UpdateTripTipAmount model)
        {
            await FirebaseService.SetTipAmount(model.TripId, model.TipAmount);
            return Request.CreateResponse(HttpStatusCode.OK, new ResponseWrapper
            {
                Error = false,
                Message = ResponseKeys.msgSuccess
            });
        }

        #endregion

        #region Promotions

        [HttpPost]
        [Route("add-promo-code")]
        public async Task<HttpResponseMessage> AddPromoCode([FromBody] AddPromoCode model)
        {
            var AddPromoResponse = await PromoCodeService.AddUserPromoCode(model);
            if (AddPromoResponse != null)
            {
                return Request.CreateResponse(HttpStatusCode.OK, new ResponseWrapper
                {
                    Error = false,
                    Message = ResponseKeys.msgSuccess,
                    Data = AddPromoResponse
                });
            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.OK, new ResponseWrapper
                {
                    Error = true,
                    Message = ResponseKeys.invalidPromo,
                });
            }
            
        }

        [HttpGet]
        [Route("get-promo-codes")]
        public async Task<HttpResponseMessage> GetPromoCodesList([FromUri] GetPassengerPromo model)
        {
            var lstPromoCodes = await PromoCodeService.GetPromoCodes(model.PassengerId);
            return Request.CreateResponse(HttpStatusCode.OK, new ResponseWrapper
            {
                Error = false,
                Message = ResponseKeys.msgSuccess,
                Data = lstPromoCodes
            });

        }
        #endregion

        #region Payment Methods

        #region Wallet

        [HttpGet]
        [Route("get-wallet")]
        public async Task<HttpResponseMessage> GetWalletDetails(string model)
        {
            return Request.CreateResponse(HttpStatusCode.OK);

            //Wallet Balance

            //PayPal Account

            //CreditCard Lists

            //if (string.IsNullOrEmpty(model.passengerID))
            //{
            //    response.error = true;
            //    response.message = AppMessage.invalidParameters;
            //    return Request.CreateResponse(HttpStatusCode.OK, response);
            //}

            //using (CanTaxiResellerEntities context = new CanTaxiResellerEntities())
            //{
            //    var user = context.UserProfiles.Where(u => u.UserID == model.passengerID).FirstOrDefault();
            //    if (user == null)
            //    {
            //        response.error = true;
            //        response.message = AppMessage.userNotFound;
            //        return Request.CreateResponse(HttpStatusCode.OK, response);
            //    }

            //    if (!string.IsNullOrEmpty(user.CreditCardCustomerID))
            //    {
            //        var customer = StripeIntegration.GetCustomer(user.CreditCardCustomerID);

            //        if (string.IsNullOrEmpty(customer.Id))
            //        {
            //            response.error = true;
            //            response.message = AppMessage.paymentGetwayError;
            //            return Request.CreateResponse(HttpStatusCode.OK, response);
            //        }

            //        StripeCustomer cust = new StripeCustomer()
            //        {
            //            cardsList = StripeIntegration.GetCardsList(customer.Id),
            //            customerId = customer.Id,
            //            defaultSourceId = customer.InvoiceSettings.DefaultPaymentMethodId
            //        };

            //        dic = new Dictionary<dynamic, dynamic>
            //                    {
            //                        { "creditCardDetails", cust }
            //                    };
            //        response.data = dic;
            //    }
            //    else
            //    {
            //        dic = new Dictionary<dynamic, dynamic>
            //                    {
            //                        { "creditCardDetails", null }
            //                    };
            //        response.data = dic;
            //    }
            //    response.error = false;
            //    response.message = AppMessage.msgSuccess;
            //    return Request.CreateResponse(HttpStatusCode.OK, response);
            //}
        }

        [HttpPost]
        [Route("redeem-coupon-code")]
        public async Task<HttpResponseMessage> RedeemCouponCode([FromBody] string Id)
        {
            return Request.CreateResponse(HttpStatusCode.OK);
            //if (!string.IsNullOrEmpty(model.passengerID) && !string.IsNullOrEmpty(model.couponCode))
            //{
            //    using (CanTaxiResellerEntities context = new CanTaxiResellerEntities())
            //    {
            //        var result = context.CouponsManagers.Where(co => co.Code.Equals(model.couponCode)
            //        && co.ApplicationID.ToString().ToLower().Equals(this.ApplicationID)
            //        ).FirstOrDefault();

            //        if (result == null)
            //        {
            //            response.error = true;
            //            response.message = AppMessage.invalidCouponCode;
            //            return Request.CreateResponse(HttpStatusCode.OK, response);
            //        }

            //        if (result.isUsed)
            //        {
            //            response.error = true;
            //            response.message = AppMessage.couponCodeAlreadyApplied;
            //            return Request.CreateResponse(HttpStatusCode.OK, response);
            //        }

            //        WalletTransfer wallet = new WalletTransfer
            //        {
            //            Amount = result.Amount,
            //            RechargeDate = DateTime.UtcNow,
            //            WalletTransferID = Guid.NewGuid(),
            //            Referrence = "Promo Code Applied - " + result.Code,
            //            TransferredBy = Guid.Parse(this.ApplicationID),
            //            TransferredTo = Guid.Parse(model.passengerID),
            //            ApplicationID = Guid.Parse(this.ApplicationID),
            //            ResellerID = Guid.Parse(this.ResellerID)
            //        };

            //        var profile = context.UserProfiles.Where(up => up.UserID.ToString().Equals(model.passengerID)).FirstOrDefault();
            //        if (profile != null)
            //        {
            //            profile.LastRechargedAt = wallet.RechargeDate;
            //            profile.WalletBalance = profile.WalletBalance == null ? result.Amount : profile.WalletBalance + result.Amount;
            //        }

            //        result.isUsed = true;
            //        result.UsedOn = wallet.RechargeDate;
            //        result.UsedBy = Guid.Parse(model.passengerID);

            //        context.WalletTransfers.Add(wallet);
            //        context.SaveChanges();

            //        response.error = false;
            //        response.data = new Dictionary<dynamic, dynamic> {
            //                            {"walletBalance", string.Format("{0:0.00}", profile.WalletBalance) }
            //                        };
            //        response.message = AppMessage.msgSuccess;
            //        return Request.CreateResponse(HttpStatusCode.OK, response);
            //    }
            //}
            //else
            //{
            //    response.error = true;
            //    response.message = AppMessage.invalidParameters;
            //    return Request.CreateResponse(HttpStatusCode.OK, response);
            //}
        }

        [HttpPost]
        [Route("check-application-user")]
        public async Task<HttpResponseMessage> CheckApplicationUser(string model)
        {
            return Request.CreateResponse(HttpStatusCode.OK);
        }

        [HttpPost]
        [Route("transfer-wallet-balance")]
        public async Task<HttpResponseMessage> TransferWalletBalance(string model)
        {
            return Request.CreateResponse(HttpStatusCode.OK);
        }


        //Recharge using Card | Apple Pay | Google Pay

        //Make Payment

        #endregion

        #region Credit/Debit Card

        [HttpGet]   //To Add credit card on client side
        [Route("get-setup-intent-client-secret")]
        public async Task<HttpResponseMessage> GetStripeSetupIntentClientSecret(string pID, string email, string customerID)
        {
            return Request.CreateResponse(HttpStatusCode.OK);
            //try
            //{
            //    response.data = new Dictionary<dynamic, dynamic>() {
            //            { "clientSecret", StripeIntegration.GetSetupIntentClientSecret(customerID, email, pID) }
            //        };
            //    response.error = false;
            //    response.message = AppMessage.msgSuccess;
            //    return Request.CreateResponse(HttpStatusCode.OK, response);
            //}
            //catch (Exception ex)
            //{
            //    response.error = true;
            //    Logger.WriteLog(ex);
            //    response.message = AppMessage.serverError;
            //    return Request.CreateResponse(HttpStatusCode.OK, response);
            //}
        }

        [HttpPost]
        [Route("update-default-credit-card")]
        public async Task<HttpResponseMessage> UpdateDefaultCreditCard([FromBody] string model)
        {
            return Request.CreateResponse(HttpStatusCode.OK);
            //if (!string.IsNullOrEmpty(model.cardToken) && !string.IsNullOrEmpty(model.customerID))
            //{
            //    var cust = StripeIntegration.UpdateDefaultPaymentMethod(model.cardToken, model.customerID);

            //    if (!string.IsNullOrEmpty(cust.Id))
            //    {
            //        StripeCustomer customer = new StripeCustomer
            //        {
            //            customerId = cust.Id,
            //            defaultSourceId = cust.InvoiceSettings.DefaultPaymentMethodId,
            //            cardsList = StripeIntegration.GetCardsList(cust.Id)
            //        };

            //        dic = new Dictionary<dynamic, dynamic>
            //                {
            //                    { "creditCardDetails", customer }
            //                };

            //        response.error = false;
            //        response.message = AppMessage.msgSuccess;
            //        response.data = dic;
            //        return Request.CreateResponse(HttpStatusCode.OK, response);
            //    }
            //    else
            //    {
            //        response.error = true;
            //        response.message = AppMessage.paymentGetwayError;
            //        return Request.CreateResponse(HttpStatusCode.OK, response);
            //    }
            //}
            //else
            //{
            //    response.error = true;
            //    response.message = AppMessage.invalidParameters;
            //    return Request.CreateResponse(HttpStatusCode.OK, response);
            //}
        }

        [HttpPost]
        [Route("delete-credit-card")]
        public async Task<HttpResponseMessage> DeleteCreditCard([FromBody] string model)
        {
            return Request.CreateResponse(HttpStatusCode.OK);
            //if (!string.IsNullOrEmpty(model.cardToken) && !string.IsNullOrEmpty(model.customerID))
            //{
            //    var card = StripeIntegration.DeleteCard(model.cardToken, model.customerID);

            //    if (!string.IsNullOrEmpty(card.Id))
            //    {
            //        var cust = StripeIntegration.GetCustomer(model.customerID);
            //        if (!string.IsNullOrEmpty(cust.Id))
            //        {
            //            StripeCustomer customer = new StripeCustomer
            //            {
            //                customerId = cust.Id,
            //                defaultSourceId = cust.InvoiceSettings.DefaultPaymentMethodId,
            //                cardsList = StripeIntegration.GetCardsList(cust.Id)
            //            };

            //            dic = new Dictionary<dynamic, dynamic>
            //                    {
            //                        { "creditCardDetails", customer }
            //                    };

            //            response.error = false;
            //            response.message = AppMessage.msgSuccess;
            //            response.data = dic;

            //            return Request.CreateResponse(HttpStatusCode.OK, response);
            //        }
            //        else
            //        {
            //            response.error = true;
            //            response.message = AppMessage.paymentGetwayError;
            //            return Request.CreateResponse(HttpStatusCode.OK, response);
            //        }
            //    }
            //    else
            //    {
            //        response.error = true;
            //        response.message = AppMessage.paymentGetwayError;
            //        return Request.CreateResponse(HttpStatusCode.OK, response);
            //    }
            //}
            //else
            //{
            //    response.error = true;
            //    response.message = AppMessage.invalidParameters;
            //    return Request.CreateResponse(HttpStatusCode.OK, response);
            //}
        }

        [HttpPost]  //Save payment details after successful completion on client side
        [Route("credit-card-payment")]
        public async Task<HttpResponseMessage> CreditCardPayment([FromBody] string model)
        {
            return Request.CreateResponse(HttpStatusCode.OK);
            //if (!string.IsNullOrEmpty(model.currency) && !string.IsNullOrEmpty(model.customerID) && !string.IsNullOrEmpty(model.isPaidClientSide) &&
            //    (Convert.ToDouble(model.paymentTip) >= 0) && !string.IsNullOrEmpty(model.passengerID) &&
            //    !string.IsNullOrEmpty(model.tripID) && !string.IsNullOrEmpty(model.isOverride) && !string.IsNullOrEmpty(model.fleetID) &&
            //    !string.IsNullOrEmpty(model.promoDiscountAmount) && !string.IsNullOrEmpty(model.walletUsedAmount.ToString())
            //    )
            //{

            //    bool isAlreadyPaid = CheckIfAlreadyPaid(model.tripID);

            //    //model.amount = TripFare + Tip
            //    double payment = Convert.ToDouble(model.amount);
            //    PaymentIntent paymentDetails = new PaymentIntent();

            //    if (!isAlreadyPaid)
            //    {
            //        StripeIntegration sc = new StripeIntegration();
            //        //1 euro is equivalent to 100 cents https://stripe.com/docs/currencies#zero-decimal

            //        if (payment > 0)
            //        {
            //            if (model.isPaidClientSide.ToLower().Equals("true"))
            //            {
            //                paymentDetails.Id = model.paymentID;
            //                paymentDetails.Status = "succeeded";
            //            }
            //            else
            //                paymentDetails = StripeIntegration.CreatePaymentIntent(model.tripID, model.customerID, (long)(float.Parse(model.amount) * 100));
            //        }
            //    }

            //    if (isAlreadyPaid || payment == 0 || (payment > 0 && paymentDetails.Status.Equals("succeeded")))//!string.IsNullOrEmpty(paymentDetails.Id)))
            //    {
            //        using (CanTaxiResellerEntities context = new CanTaxiResellerEntities())
            //        {
            //            var stripeTransactionId = payment > 0 ? "Trip CreditCard payment received. Stripe transactionId = " + paymentDetails.Id : "Trip creditcard payment. Zero payment.";

            //            //If already paid, trip will not update the trip data but returns required info.

            //            var trip = context.spAfterMobilePayment(false,//Convert.ToBoolean(model.isOverride), 
            //                model.tripID,
            //                stripeTransactionId,
            //                (int)App_Start.TripStatus.Completed,
            //                model.passengerID,
            //                this.ApplicationID,
            //                (Convert.ToDouble(model.amount) - Convert.ToDouble(model.paymentTip)).ToString(),
            //                "0.00",
            //                model.promoDiscountAmount,
            //                model.walletUsedAmount,
            //                model.paymentTip.ToString(),
            //                Common.getUtcDateTime(),
            //                (int)App_Start.PaymentMode.CreditCard,
            //                (int)App_Start.ResellerPaymentStatus.Paid,
            //                model.fleetID).FirstOrDefault();

            //            dic = new Dictionary<dynamic, dynamic>
            //            {
            //                { "tripID", model.tripID },
            //                { "tip", model.paymentTip },
            //                { "amount", string.Format("{0:0.00}", Convert.ToDouble(model.amount) - Convert.ToDouble(model.paymentTip) + Convert.ToDouble(model.walletUsedAmount) + Convert.ToDouble(model.promoDiscountAmount)) }
            //            };

            //            FireBaseController fc = new FireBaseController();
            //            var task = Task.Run(async () =>
            //            {
            //                if (!isAlreadyPaid)
            //                    await fc.sentSingleFCM(trip.DeviceToken, dic, "cap_paymentSuccess");

            //                await fc.delTripNode(model.tripID);
            //            });

            //            fc.updateDriverStatus(trip.CaptainID.ToString(), "false", model.tripID);
            //            //to avoid login on another device during trip
            //            fc.addDeleteNode(true, "", "CustomerTrips/" + model.passengerID);

            //            if (!isAlreadyPaid)
            //                SendInvoice(new InvoiceModel
            //                {
            //                    CustomerEmail = trip.CustomerEmail,// context.AspNetUsers.Where(u => u.Id.Equals(model.passengerID)).FirstOrDefault().Email,
            //                    TotalAmount = (Convert.ToDouble(model.amount) + Convert.ToDouble(model.walletUsedAmount) + Convert.ToDouble(model.promoDiscountAmount)).ToString(),
            //                    WalletUsedAmount = model.walletUsedAmount,
            //                    PromoDiscountAmount = model.promoDiscountAmount,
            //                    CashAmount = "0",
            //                    CaptainName = trip.CaptainName,
            //                    CustomerName = trip.CustomerName,
            //                    TripDate = trip.TripDate,
            //                    InvoiceNumber = trip.InvoiceNumber,
            //                    FleetName = trip.FleetName,
            //                    ATUNumber = trip.FleetATUNumber,
            //                    Street = trip.FleetAddress,
            //                    BuildingNumber = trip.FleetBuildingNumber,
            //                    PostCode = trip.FleetPostalCode,
            //                    City = trip.FleetCity,
            //                    PickUpAddress = trip.PickUpLocation,
            //                    DropOffAddress = trip.DropOffLocation,
            //                    CaptainUserName = trip.CaptainUserName,
            //                    Distance = trip.DistanceInKM.ToString("0.00"),
            //                    VehicleNumber = trip.PlateNumber,
            //                    FleetEmail = trip.FleetEmail
            //                });

            //            response.error = false;
            //            response.message = AppMessage.msgSuccess;
            //            return Request.CreateResponse(HttpStatusCode.OK, response);
            //        }
            //    }
            //    else
            //    {
            //        response.error = true;
            //        response.message = AppMessage.paymentGetwayError;
            //        var dic = new Dictionary<dynamic, dynamic>
            //        {
            //            { "Status", paymentDetails.Status },
            //            { "ClientSecret", paymentDetails.ClientSecret },
            //            { "FailureMessage", paymentDetails.Description }
            //        };
            //        response.data = dic;
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
        public async Task<HttpResponseMessage> GetNotificationsList([FromUri] GetNotificationListModel model)
        {
            var lstNotifications = await NotificationServices.GetNotifications(model.ReceiverId);
            return Request.CreateResponse(HttpStatusCode.OK, new ResponseWrapper
            {
                Error = false,
                Message = ResponseKeys.msgSuccess,
                Data = lstNotifications
            });
        }

        [HttpPost]
        [Route("read-notification")]
        public async Task<HttpResponseMessage> ReadNotification(string model)
        {
            return Request.CreateResponse(HttpStatusCode.OK);
        }

        #endregion  

        #region Invite Code

        [HttpPost]
        [Route("apply-invite-code")]
        public async Task<HttpResponseMessage> ApplyInviteCode([FromBody] ApplyInviteCode model)
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
        public async Task<HttpResponseMessage> GetRecentLocations([FromUri] GetRecentLocationList model)
        {
            if (model.PassengerId != string.Empty)
            {
                var lstLocation = await PassengerPlacesService.GetRecentTripsLocations(model.PassengerId);
                return Request.CreateResponse(HttpStatusCode.OK, new ResponseWrapper
                {
                    Error = false,
                    Message = ResponseKeys.msgSuccess,
                    Data = lstLocation
                });
            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResponseWrapper { Message = ResponseKeys.invalidParameters });
            }

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
            return Request.CreateResponse(HttpStatusCode.OK, new ResponseWrapper
            {
                Error = false,
                Data = await TripsManagerService.BookNewTrip(model),
                Message = ResponseKeys.msgSuccess,
            });
        }

        [HttpPost]  //Cancel normal booking which is not accepted yet
        [Route("time-out")]
        public async Task<HttpResponseMessage> TimeOut([FromBody] TripTimeOutRequest model)
        {
            var responseKey = await TripsManagerService.TimeOutTrip(model.TripId, model.PassengerId);
            if (responseKey.Equals(ResponseKeys.notFound))
            {
                return Request.CreateResponse(HttpStatusCode.NotFound, new ResponseWrapper { Message = ResponseKeys.notFound });
            }
            else if (responseKey.Equals(ResponseKeys.tripAlreadyBooked))
            {
                return Request.CreateResponse(HttpStatusCode.Forbidden, new ResponseWrapper { Message = ResponseKeys.tripAlreadyBooked });
            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.OK, new ResponseWrapper { Message = ResponseKeys.msgSuccess, Error = false });
            }
        }

        [HttpPost]
        [Route("cancel-trip")]
        public async Task<HttpResponseMessage> CancelTrip(CancelTripRequest model)
        {
            return Request.CreateResponse(HttpStatusCode.OK, await TripsManagerService.CancelTripByPassenger(model.TripId, model.PassengerId, model.DistanceTravelled, model.CancelID, model.IsLaterBooking));
        }

        [HttpPost]
        [Route("submit-feedback")]
        public async Task<HttpResponseMessage> SubmitFeedback([FromBody] UpdateTripUserFeedback model)
        {
            var result = await TripsManagerService.UserSubmitFeedback(model);

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
                });
            }
        }

        [HttpGet]
        [Route("current-utc-datetime")]
        public HttpResponseMessage getCurrentUTCDateTime()
        {
            return Request.CreateResponse(HttpStatusCode.OK, new ResponseWrapper
            {
                Error = false,
                Message = ResponseKeys.msgSuccess,
                Data = new Dictionary<dynamic, dynamic>
                            {
                                {"currentDateTime", DateTime.UtcNow.ToString(Formats.DateFormat) }
                            }
            });
        }

        #endregion
    }
}