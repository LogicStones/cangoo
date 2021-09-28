using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTOs.API
{
	public class PassengerProfileRequest
	{
		public string FirstName { get; set; }
		public string LastName { get; set; }
		public string PhoneNumber { get; set; }
		public string Email { get; set; }
		public string Password { get; set; }
	}

	public class PassengerProfileResponse
	{
		public string PassengerId { get; set; } = "";
		public string FirstName { get; set; } = "";
		public string LastName { get; set; } = "";
		public string PhoneNumber { get; set; } = "";
		public string Email { get; set; } = "";
		public string ProfilePicture { get; set; } = "";
		public string IsUserProfileUpdated { get; set; } = "";
		public string Rating { get; set; } = "";
		public string NumberDriverFavourites { get; set; } = "";
		public string NoOfTrips { get; set; } = "";
		public string SelectedPaymentMethod { get; set; } = "";
		public string CountryCode { get; set; } = "";
		public string Spendings { get; set; } = "";
		public string AccessToken { get; set; } = "";
		public string IsBlocked { get; set; } = "";
		public string IsVerified { get; set; } = "";
		public string ResellerId { get; set; } = "";
		public string ApplicationId { get; set; } = "";
		public string ApplicationAuthorizeArea { get; set; } = "";
	}

	public class UpdateProfileImageResponse
	{
		public string ProfilePicture { get; set; } = "";
	}

	public class UpdatePassengerNameRequest
	{
		[Required]
		public string PassengerId { get; set; }
		[Required]
		public string FirstName { get; set; }
		[Required]
		public string LastName { get; set; }
	}

	public class UpdatePassengerNameResponse
	{
		public string FirstName { get; set; } = "";
		public string LastName { get; set; } = "";
	}

	public class UpdatePassengerPhoneNumberRequest
	{
		[Required]
		public string PassengerId { get; set; }
		[Required]
		public string PhoneNumber { get; set; }
		[Required]
		public string CountryCode { get; set; }
	}

	public class UpdatePassengerPhoneNumberResponse
	{
		public string PhoneNumber { get; set; } = "";
		public string CountryCode { get; set; } = "";
	}

	public class UpdatePassengerEmailRequest
	{
		[Required]
		public string PassengerId { get; set; }
		[Required]
		public string Email { get; set; }
	}

	public class UpdatePassengerEmailResponse
	{
		public string Email { get; set; } = "";
	}
}