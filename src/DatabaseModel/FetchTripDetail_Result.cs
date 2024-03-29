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
    
    public partial class FetchTripDetail_Result
    {
        public string CaptainName { get; set; }
        public Nullable<System.Guid> CaptainID { get; set; }
        public string Customer { get; set; }
        public Nullable<System.Guid> CustomerID { get; set; }
        public string ResellerName { get; set; }
        public System.Guid ResellerID { get; set; }
        public string ApplicationName { get; set; }
        public System.Guid ApplicationID { get; set; }
        public Nullable<System.Guid> FleetID { get; set; }
        public string FleetName { get; set; }
        public System.Guid TripID { get; set; }
        public string PickupLocationLatitude { get; set; }
        public string PickupLocationLongitude { get; set; }
        public string DropOffLocationLatitude { get; set; }
        public string DropOffLocationLongitude { get; set; }
        public string Vehicle { get; set; }
        public string BookingType { get; set; }
        public Nullable<System.DateTime> BookingDateTime { get; set; }
        public Nullable<System.DateTime> ArrivalDateTime { get; set; }
        public Nullable<int> DriverRating { get; set; }
        public Nullable<int> VehicleRating { get; set; }
        public Nullable<int> UserRating { get; set; }
        public Nullable<double> DistanceTraveled { get; set; }
        public Nullable<double> WaitingMinutes { get; set; }
        public string TripPaymentMode { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public Nullable<decimal> WaitingFare { get; set; }
        public Nullable<decimal> BookingFare { get; set; }
        public Nullable<decimal> BaseFare { get; set; }
        public Nullable<decimal> PerKMFare { get; set; }
        public Nullable<decimal> WalletAmountUsed { get; set; }
        public Nullable<decimal> PromoDiscount { get; set; }
        public Nullable<decimal> Tip { get; set; }
        public Nullable<decimal> VoucherAmount { get; set; }
        public string DriverSubmittedFeedback { get; set; }
        public string UserSubmittedFeedback { get; set; }
        public string PickUpLocation { get; set; }
        public string DropOffLocation { get; set; }
        public Nullable<decimal> Fare { get; set; }
    }
}
