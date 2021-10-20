using System;
using System.Collections.Generic;
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
		public string PhoneNumber { get; set; }
		[Required]
		public string Password { get; set; }
		[Required]
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
		public string password { get; set; }
	}


	#endregion
}
