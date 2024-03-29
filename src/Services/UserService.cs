﻿using AutoMapper;
using Constants;
using DatabaseModel;
using DTOs.API;
using DTOs.API.Notificatons;
using DTOs.Shared;
using Services.Automapper;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Services
{
    public class UserService
    {
        public static async Task<PassengerIdentityDTO> GetByUserNameAsync(string userName)
        {
            using (var context = new CangooEntities())
            {
                //DestinationType obj = Mapper.Map<SourceType, DestinationType>(sourceValueObject);
                //List<DestinationType> listObj = Mapper.Map<List<SourceType>, List<DestinationType>>(enumarableSourceValueObject);
                var user = await context.AspNetUsers.Where(u => u.UserName.Equals(userName)).FirstOrDefaultAsync();
                return AutoMapperConfig._mapper.Map<AspNetUser, PassengerIdentityDTO>(user);
            }
        }

        public static async Task CreateUserProfileAsync(string userId, string verificationCode, string countryCode, string deviceToken, string applicationId, string resellerId)
        {
            using (var dbContext = new CangooEntities())
            {
                var psng = new UserProfile
                {
                    UserID = userId,
                    PhoneVerificationCode = verificationCode,
                    FirstName = "",
                    LastName = "",
                    ProfilePicture = "~/Images/User/userAvatar.png",
                    OriginalPicture = "",
                    Rating = 5,
                    Spendings = 0,
                    WalletBalance = 0,
                    PreferredPaymentMethod = "Cash",
                    CountryCode = countryCode,
                    NumberDriverFavourites = 0,
                    NoOfTrips = 0,
                    CreditCardCustomerID = "",
                    isWalletPreferred = false,
                    isCoWorker = false,
                    DeviceToken = deviceToken,
                    IsActive = true,
                    isSharedBookingEnabled = false,
                    LastRechargedAt = null,
                    ApplicationID = Guid.Parse(applicationId),
                    ResellerID = Guid.Parse(resellerId),
                    MemberSince = DateTime.UtcNow
                };

                dbContext.UserProfiles.Add(psng);
                await dbContext.SaveChangesAsync();
            }
        }

        public static async Task<PassengerProfileDTO> GetProfileAsync(string userId)
        {
            using (var dbContext = new CangooEntities())
            {
               var profile = await dbContext.UserProfiles.Where(up => up.UserID.Equals(userId)).FirstOrDefaultAsync();
                return AutoMapperConfig._mapper.Map<UserProfile, PassengerProfileDTO>(profile);
            }
        }
        
        public static async Task UpdateNameAsync(string firstName, string lastName, string userId)
        {
            using (var dbContext = new CangooEntities())
            {
                //int noOfRowUpdated = ctx.Database.ExecuteSqlCommand("Update student set studentname = 'changed student by command' where studentid = 1");
                //int noOfRowInserted = ctx.Database.ExecuteSqlCommand("insert into student(studentname) values('New Student')");
                //int noOfRowDeleted = ctx.Database.ExecuteSqlCommand("delete from student where studentid = 1");

                //var result = await dbContext.UserProfiles.Where(up => up.UserID.Equals(profile.UserID)).FirstOrDefaultAsync();
                await dbContext.Database.ExecuteSqlCommandAsync("Update Userprofile Set FirstName = {0}, LastName = {1} where UserID = {2}", firstName, lastName, userId);
            }
        }
        
        public static async Task UpdateEmailAsync(string email, string userId)
        {
            using (var dbContext = new CangooEntities())
            {
                await dbContext.Database.ExecuteSqlCommandAsync("Update AspNetUsers Set Email = {0} where Id = {1}", email, userId);
            }
        }

        public static async Task UpdateDeviceTokenAsync(string deviceToken, string userId)
        {
            using (var dbContext = new CangooEntities())
            {
                await dbContext.Database.ExecuteSqlCommandAsync("Update Userprofile Set DeviceToken = {0} where UserId = {1}", deviceToken, userId);
            }
        }

        public static async Task UpdatePhoneNumberAsync(string phoneNumber, string countryCode, string userId)
        {
            using (var dbContext = new CangooEntities())
            {
                await dbContext.Database.ExecuteSqlCommandAsync(@"
Update AspNetUsers Set UserName = {0}, PhoneNumber = {0} where Id = {2};
Update Userprofile Set CountryCode = {1} where UserID = {2};", phoneNumber, countryCode, userId);
            }
        }

        public static async Task UpdateImageAsync(string savedFilePath, string uploadedFileName, string userId)
        {
            using (var dbContext = new CangooEntities())
            {
                await dbContext.Database.ExecuteSqlCommandAsync("Update Userprofile Set ProfilePicture = {0}, OriginalPicture = {1} where UserID = {2}", savedFilePath, uploadedFileName, userId);
            }
        }

        public static async Task<string> GetAccesToken(string userName, string password)
        {
            using (var client = new HttpClient())
            {
                var requestParams = new List<KeyValuePair<string, string>>
                                            {
                                                new KeyValuePair<string, string>("grant_type", "password"),
                                                new KeyValuePair<string, string>("username", userName),
                                                new KeyValuePair<string, string>("password", password)
                                            };

                var requestParamsFormUrlEncoded = new FormUrlEncodedContent(requestParams);
                var request = System.Web.HttpContext.Current.Request;
                var tokenServiceUrl = request.Url.GetLeftPart(UriPartial.Authority) + request.ApplicationPath + "/Token";

                var tokenServiceResponse = await client.PostAsync(tokenServiceUrl, requestParamsFormUrlEncoded);
                var responseString = await tokenServiceResponse.Content.ReadAsStringAsync();
                var access_Token = "";

                if (responseString.Contains("access_token"))
                {
                    var subString = responseString.Split(':');
                    access_Token = subString[1].Split(',')[0].Replace("\"", "");

                    //public Object GetToken()
                    //{
                    //    var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(ConfigurationManager.AppSettings["JWTSecretKey"].ToString()));
                    //    var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

                    //    //Create a List of Claims, Keep claims name short    
                    //    //var permClaims = new List<Claim>
                    //    //{
                    //    //    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    //    //    new Claim("valid", "1"),
                    //    //    new Claim("userid", "1"),
                    //    //    new Claim("userid1", "1"),
                    //    //    new Claim("userid2", "1"),
                    //    //    new Claim("userid3", "1"),
                    //    //    new Claim("userid4", "1"),
                    //    //    new Claim("userid5", "1"),
                    //    //    new Claim("name", "bilal")
                    //    //};

                    //    //Create Security Token object by giving required parameters    
                    //    var token = new JwtSecurityToken(
                    //        issuer: ConfigurationManager.AppSettings["APIURL"].ToString(),
                    //        //audience: issuer,
                    //        //claims: permClaims,
                    //        expires: DateTime.Now.AddDays(1),
                    //        signingCredentials: credentials);

                    //    var jwt_token = new JwtSecurityTokenHandler().WriteToken(token);
                    //    return new { data = jwt_token };
                    //}

                }

                return access_Token;
            }
        }

        public static async Task<ResponseWrapper> GetAccessTokenAndPassengerProfileData(string userId, string userName, string password, string deviceToken, string email, string phoneNumber,
            string applicationId,
            string resellerId,
            bool isExistingUser,
            bool isUserProfileUpdated,
            bool isLogin)
        {
            using (var context = new CangooEntities())
            {
                var userProfile = await GetProfileAsync(userId);

                if (isExistingUser)
                {
                    //verify account is not blocked by application admin explicitly
                    if (userProfile.IsActive != true)
                    {
                        return new ResponseWrapper
                        {
                            Message = ResponseKeys.userBlocked,
                            Data = new PassengerAuthenticationResponse
                            {
                                PassengerId = userId,
                                IsBlocked = true.ToString()
                            }
                        };
                    }

                    //First time login check and if user was logged out intentionally.
                    if (!string.IsNullOrEmpty(userProfile.DeviceToken))
                    {
                        //Pushy device token never changes. If user is already logged in on some other device then force logout.
                        if (!userProfile.DeviceToken.ToLower().Equals(deviceToken.ToLower()))
                        {
                            //FireBaseController fc = new FireBaseController();
                            NewDeviceLogInNotification payload = new NewDeviceLogInNotification
                            {
                                PassengerId = userId,
                                DeviceToken = userProfile.DeviceToken
                            };

                            await NotificationService.UniCast(userProfile.DeviceToken, payload, NotificationKeys.pas_NewDeviceLoggedIn);

                        }
                    }

                    await UpdateDeviceTokenAsync(deviceToken, userProfile.UserID);
                }

                PassengerAuthenticationResponse passengerModel = new PassengerAuthenticationResponse
                {
                    PassengerId = userProfile.UserID.ToString(),
                    FirstName = userProfile.FirstName,
                    LastName = userProfile.LastName,
                    PhoneNumber = phoneNumber,
                    Email = email ?? "",
                    OriginalPicture = userProfile.ProfilePicture,
                    Rating = string.Format("{0:0.00}", userProfile.Rating.ToString()),
                    NumberDriverFavourites = ((int)userProfile.NumberDriverFavourites).ToString(),
                    NoOfTrips = ((int)userProfile.NoOfTrips).ToString(),
                    SelectedPaymentMethod = userProfile.PreferredPaymentMethod,
                    CountryCode = userProfile.CountryCode,
                    Spendings = string.Format("{0:0.00}", userProfile.Spendings.ToString()),
                    ResellerId = resellerId,
                    ApplicationId = applicationId,
                    AccessToken = ""
                };

                if (!isExistingUser || (isExistingUser && !isUserProfileUpdated) || isLogin)
                {
                    passengerModel.AccessToken = await GetAccesToken(userName, password);
                    if (string.IsNullOrEmpty(passengerModel.AccessToken))
                    {
                        return new ResponseWrapper
                        {
                            Message = ResponseKeys.authenticationFailed
                        };
                    }
                }

                var applicationData = context.spGetApplicationArea(applicationId).FirstOrDefault();

                passengerModel.IsUserProfileUpdated = isUserProfileUpdated.ToString();
                passengerModel.IsVerified = true.ToString();
                passengerModel.ApplicationId = applicationData.ApplicationID.ToString();
                passengerModel.ApplicationAuthorizeArea = applicationData.AuthorizedArea;

                return new ResponseWrapper
                {
                    Error = false,
                    Message = ResponseKeys.msgSuccess,
                    Data = passengerModel
                };
            }
        }
    }
}