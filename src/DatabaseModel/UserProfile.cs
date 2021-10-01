//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace DatabaseModel
{
    using System;
    using System.Collections.Generic;
    
    public partial class UserProfile
    {
        public string UserID { get; set; }
        public Nullable<System.Guid> ResellerID { get; set; }
        public Nullable<System.Guid> ApplicationID { get; set; }
        public string ProfilePicture { get; set; }
        public string OriginalPicture { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public Nullable<decimal> WalletBalance { get; set; }
        public Nullable<double> Rating { get; set; }
        public string PhoneVerificationCode { get; set; }
        public Nullable<int> NumberDriverFavourites { get; set; }
        public string CreditCardCustomerID { get; set; }
        public string PreferredPaymentMethod { get; set; }
        public Nullable<bool> isWalletPreferred { get; set; }
        public Nullable<bool> isCoWorker { get; set; }
        public string CountryCode { get; set; }
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
