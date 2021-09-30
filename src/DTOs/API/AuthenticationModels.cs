using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTOs.API
{
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
	public class PassengerLogOutResponse
	{
		[Required]
		public string PhoneNumber { get; set; }
		[Required]
		public string CurrentPassword { get; set; }
		[Required]
		public string NewPassword { get; set; }
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
}
