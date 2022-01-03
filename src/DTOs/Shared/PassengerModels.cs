using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTOs.Shared
{
    public class PassengerIdentityDTO
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public bool EmailConfirmed { get; set; }
        public string PhoneNumber { get; set; }
        public bool PhoneNumberConfirmed { get; set; }
        public string UserName { get; set; }
    }
    public class PassengerProfileDTO
    {
        public string UserID { get; set; }
        public Nullable<System.Guid> ResellerID { get; set; }
        public Nullable<System.Guid> ApplicationID { get; set; }
        public string ProfilePicture { get; set; }
        public string OriginalPicture { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string CountryCode { get; set; }
        public Nullable<decimal> WalletBalance { get; set; }
        public Nullable<decimal> AvailableWalletBalance { get; set; }
        public Nullable<double> Rating { get; set; }
        public string PhoneVerificationCode { get; set; }
        public Nullable<int> NumberDriverFavourites { get; set; }
        public string CreditCardCustomerID { get; set; }
        public string PreferredPaymentMethod { get; set; }
        public Nullable<bool> isWalletPreferred { get; set; }
        public Nullable<bool> isCoWorker { get; set; }
        public Nullable<System.DateTime> LastRechargedAt { get; set; }
        public Nullable<decimal> Spendings { get; set; }
        public Nullable<int> NoOfTrips { get; set; }
        public string DeviceToken { get; set; }
        public Nullable<bool> IsActive { get; set; }
        public Nullable<bool> isSharedBookingEnabled { get; set; }
        public System.DateTime MemberSince { get; set; }
        public int RewardPoints { get; set; }
        public Nullable<int> LanguageID { get; set; }
    }
}
