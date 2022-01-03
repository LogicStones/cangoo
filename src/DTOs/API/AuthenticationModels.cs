using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTOs.API
{
	#region Passenger

	public class GenerateOTPRequest
	{
		[Required]
		public string PhoneNumber { get; set; }
	}

	public class GenerateOTPResponse
	{
		public string OTP { get; set; } = "";
		public string IsUserProfileUpdated { get; set; } = "";
	}

	public class PassengerRegisterRequest
	{
		[Required]
		public string PhoneNumber { get; set; }
		[Required]
		public string Password { get; set; }
		[Required]
		public string CountryCode { get; set; }
		[Required]
		public string DeviceToken { get; set; }
	}

	public class PassengerLoginRequest
	{
		[Required]
		[DefaultValue("+923117803648")]
		public string PhoneNumber { get; set; }

		[Required]
		[DefaultValue("12345")]
		public string Password { get; set; }

		[Required]
		[DefaultValue("e40a93742285b9c1739c00")]
		public string DeviceToken { get; set; }
	}

	public class PassengerAuthenticationResponse
	{
		public string PassengerId { get; set; } = "";
		public string FirstName { get; set; } = "";
		public string LastName { get; set; } = "";
		public string PhoneNumber { get; set; } = "";
		public string Email { get; set; } = "";
		public string OriginalPicture { get; set; } = "";
		public string Rating { get; set; } = "";
		public string NumberDriverFavourites { get; set; } = "";
		public string NoOfTrips { get; set; } = "";
		public string SelectedPaymentMethod { get; set; } = "";
		public string CountryCode { get; set; } = "";
		public string Spendings { get; set; } = "";
		public string AccessToken { get; set; } = "";
		public string DefaultLanguageId { get; set; } = "";
		public string DefaultLanguageName { get; set; } = "";
		public string TrustedContactName { get; set; } = "";
		public string IsBlocked { get; set; } = "";
		public string IsUserProfileUpdated { get; set; } = "";
		public string IsVerified { get; set; } = "";
		public string ResellerId { get; set; } = "";
		public string ApplicationId { get; set; } = "";
		public string ApplicationAuthorizeArea { get; set; } = "";
	}

	public class PassengerForgetPasswordRequest
	{
		[Required]
		public string PhoneNumber { get; set; }
	}

	public class PassengerChangePasswordRequest
	{
		[Required]
		public string PhoneNumber { get; set; }
		[Required]
		public string CurrentPassword { get; set; }
		[Required]
		public string NewPassword { get; set; }
	}

	public class PassengerLogOutRequest
	{
		[Required]
		public string PassengerId { get; set; }
	}

	public class PassengerVerifyDeviceTokenRequest
	{
		[Required]
		public string PassengerId { get; set; }
		[Required]
		public string DeviceToken { get; set; }
	}

	public class PassengerVerifyDeviceTokenResponse
	{
		public string IsTokenVerified { get; set; } = "";
	}

	#endregion

	#region Driver

	public class DriverVerifyDeviceTokenRequest
	{
		[Required]
		public string driverID { get; set; }
		[Required]
		public string DeviceToken { get; set; }
	}

	public class DriverVerifyDeviceTokenResponse
	{
		public bool isTokenVerified { get; set; }
	}

	public class DriverVerifyPhoneNumberRequest
	{
		[Required]
		public string UserName { get; set; }
	}

	public class DriverVerifyPhoneNumberResponse
	{
		public string code { get; set; } = "";
		public string user_id { get; set; } = "";
		public bool is_user_register { get; set; } 
	}


	public class DriverSetNewPasswordRequest
	{
		[Required]
		public string userID { get; set; }
		[Required]
		public string password { get; set; }
	}

	public class DriverLoginRequest
	{
		[Required]
		public string UserName { get; set; }
		[Required]
		public string password { get; set; }
		[Required]
		public string DeviceToken { get; set; }
	}

	public class DriverLoginResponse
	{
		public string userID { get; set; }
		public string resellerID { get; set; }
		public string applicationID { get; set; }
		public string fleetID { get; set; }
		public string phone { get; set; }
		public string verificationCode { get; set; }
		public string password { get; set; }
		public string oldPassword { get; set; }
		public string Name { get; set; }
		public string UserName { get; set; }
		public string EarningPoints { get; set; }
		public string LastLogin { get; set; }
		public Nullable<bool> IsPriorityHoursActive { get; set; }
		public string Email { get; set; }
		public string TotalOnlineHours { get; set; }
		public string MemberSince { get; set; }
		public string ShareCode { get; set; }
		public string NumberOfFavoriteUser { get; set; }
		public string access_Token { get; set; }
		public string Picture { get; set; }
		public string DrivingLicense { get; set; }
		public string DeviceToken { get; set; }
		public double? rating { get; set; }
		public decimal? totalEarnings { get; set; }
		public int? totalRides { get; set; }
		public int offset { get; set; }
		public int limit { get; set; }
		public string priorityHourEndTime { get; set; }
		public string dateFrom { get; set; }
		public string dateTo { get; set; }
		public string vehicleID { get; set; }
		public string normalBookingNotificationTone { get; set; }
		public string laterBookingNotificationTone { get; set; }
		public string showOtherVehicles { get; set; }
		public bool isAlreadyInTrip { get; set; }
		public dynamic vehicleDetails { get; set; }
	}

	public class DriverLogOutRequest
	{
		public Boolean isBooked { get; set; }
		public string vehicleID { get; set; }
		public string driverID { get; set; }
		public double driverRating { get; set; }
		public string driverName { get; set; }
		public Nullable<bool> isPriorityHoursActive { get; set; }
		public string priorityHourEndTime { get; set; }
		public string earningPoints { get; set; }
		public string make { get; set; }
		public string model { get; set; }
		public string number { get; set; }
		public string seatingCapacity { get; set; }
		public string DeviceToken { get; set; }
	}

	public class DriverResetPasswordRequest
	{
		public string userID { get; set; }
		public string resellerID { get; set; }
		public string applicationID { get; set; }
		public string fleetID { get; set; }
		public string phone { get; set; }
		public string verificationCode { get; set; }
		public string password { get; set; }
		public string oldPassword { get; set; }
		public string Name { get; set; }
		public string UserName { get; set; }
		public string EarningPoints { get; set; }
		public string LastLogin { get; set; }
		public Nullable<bool> IsPriorityHoursActive { get; set; }
		public string Email { get; set; }
		public string TotalOnlineHours { get; set; }
		public string MemberSince { get; set; }
		public string ShareCode { get; set; }
		public string NumberOfFavoriteUser { get; set; }
		public string access_Token { get; set; }
		public string Picture { get; set; }
		public string DrivingLicense { get; set; }
		public string DeviceToken { get; set; }
		public double? rating { get; set; }
		public decimal? totalEarnings { get; set; }
		public int? totalRides { get; set; }
		public int offset { get; set; }
		public int limit { get; set; }
		public string priorityHourEndTime { get; set; }
		public string dateFrom { get; set; }
		public string dateTo { get; set; }
		public string vehicleID { get; set; }
		public string normalBookingNotificationTone { get; set; }
		public string laterBookingNotificationTone { get; set; }
		public string showOtherVehicles { get; set; }
		public bool isAlreadyInTrip { get; set; }
		public dynamic vehicleDetails { get; set; }
	}

	public class DriverChangePasswordRequest
	{
		public string userID { get; set; }
		public string resellerID { get; set; }
		public string applicationID { get; set; }
		public string fleetID { get; set; }
		public string phone { get; set; }
		public string verificationCode { get; set; }
		public string password { get; set; }
		public string oldPassword { get; set; }
		public string Name { get; set; }
		public string UserName { get; set; }
		public string EarningPoints { get; set; }
		public string LastLogin { get; set; }
		public Nullable<bool> IsPriorityHoursActive { get; set; }
		public string Email { get; set; }
		public string TotalOnlineHours { get; set; }
		public string MemberSince { get; set; }
		public string ShareCode { get; set; }
		public string NumberOfFavoriteUser { get; set; }
		public string access_Token { get; set; }
		public string Picture { get; set; }
		public string DrivingLicense { get; set; }
		public string DeviceToken { get; set; }
		public double? rating { get; set; }
		public decimal? totalEarnings { get; set; }
		public int? totalRides { get; set; }
		public int offset { get; set; }
		public int limit { get; set; }
		public string priorityHourEndTime { get; set; }
		public string dateFrom { get; set; }
		public string dateTo { get; set; }
		public string vehicleID { get; set; }
		public string normalBookingNotificationTone { get; set; }
		public string laterBookingNotificationTone { get; set; }
		public string showOtherVehicles { get; set; }
		public bool isAlreadyInTrip { get; set; }
		public dynamic vehicleDetails { get; set; }
	}

	#endregion
}
