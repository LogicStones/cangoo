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
    
    public partial class Trip
    {
        public System.Guid TripID { get; set; }
        public string PickupLocationLatitude { get; set; }
        public string PickupLocationLongitude { get; set; }
        public string PickUpLocation { get; set; }
        public string DropOffLocationLatitude { get; set; }
        public string DropOffLocationLongitude { get; set; }
        public string DropOffLocation { get; set; }
        public Nullable<System.Guid> CaptainID { get; set; }
        public Nullable<System.Guid> UserID { get; set; }
        public Nullable<System.Guid> VehicleID { get; set; }
        public Nullable<int> NoOfPerson { get; set; }
        public Nullable<System.DateTime> BookingDateTime { get; set; }
        public Nullable<System.DateTime> ArrivalDateTime { get; set; }
        public Nullable<System.DateTime> TripStartDatetime { get; set; }
        public Nullable<System.DateTime> TripEndDatetime { get; set; }
        public Nullable<bool> isLaterBooking { get; set; }
        public Nullable<System.DateTime> PickUpBookingDateTime { get; set; }
        public Nullable<int> DriverRating { get; set; }
        public Nullable<int> VehicleRating { get; set; }
        public Nullable<int> UserRating { get; set; }
        public string FareManagerID { get; set; }
        public Nullable<decimal> BaseFare { get; set; }
        public Nullable<double> WaitingMinutes { get; set; }
        public Nullable<decimal> BookingFare { get; set; }
        public Nullable<decimal> WaitingFare { get; set; }
        public Nullable<double> DistanceTraveled { get; set; }
        public Nullable<bool> isOverRided { get; set; }
        public Nullable<decimal> PerKMFare { get; set; }
        public Nullable<decimal> Tip { get; set; }
        public Nullable<System.Guid> PromoCodeID { get; set; }
        public Nullable<decimal> PromoDiscount { get; set; }
        public Nullable<System.Guid> VoucherID { get; set; }
        public Nullable<decimal> WalletAmountUsed { get; set; }
        public string DriverSubmittedFeedback { get; set; }
        public string UserSubmittedFeedback { get; set; }
        public Nullable<double> DriverEarnedPoints { get; set; }
        public string TripPaymentMode { get; set; }
        public Nullable<int> CancelID { get; set; }
        public Nullable<int> TripStatusID { get; set; }
        public Nullable<int> BookingModeID { get; set; }
        public Nullable<bool> isHotelBooking { get; set; }
        public string Description { get; set; }
        public Nullable<int> BookingTypeID { get; set; }
        public bool isDispatched { get; set; }
        public bool isReRouted { get; set; }
        public Nullable<double> DistanceToPickUpLocation { get; set; }
        public Nullable<System.Guid> ApplicationID { get; set; }
        public Nullable<System.Guid> ResellerID { get; set; }
        public Nullable<System.Guid> CompanyID { get; set; }
        public string facilities { get; set; }
        public Nullable<decimal> AdminProfit { get; set; }
        public Nullable<decimal> ResellerProfit { get; set; }
        public Nullable<decimal> ApplicationProfit { get; set; }
        public Nullable<decimal> FleetProfit { get; set; }
        public int UTCTimeZoneOffset { get; set; }
        public string InvoiceNumber { get; set; }
        public Nullable<bool> isFareChangePermissionGranted { get; set; }
        public Nullable<int> InBoundDistanceInMeters { get; set; }
        public Nullable<int> InBoundTimeInSeconds { get; set; }
        public Nullable<int> OutBoundDistanceInMeters { get; set; }
        public Nullable<int> OutBoundTimeInSeconds { get; set; }
        public Nullable<decimal> OutBoundTimeFare { get; set; }
        public Nullable<decimal> OutBoundDistanceFare { get; set; }
        public Nullable<decimal> InBoundTimeFare { get; set; }
        public Nullable<decimal> InBoundDistanceFare { get; set; }
        public Nullable<decimal> InBoundSurchargeAmount { get; set; }
        public Nullable<decimal> OutBoundSurchargeAmount { get; set; }
        public Nullable<System.Guid> DropOffFareMangerID { get; set; }
        public Nullable<decimal> InBoundBaseFare { get; set; }
        public Nullable<decimal> OutBoundBaseFare { get; set; }
        public string PolyLine { get; set; }
        public int PaymentModeId { get; set; }
        public bool isWishCarRequested { get; set; }
        public Nullable<int> VehicleCategoryId { get; set; }
        public string DriverUserAgent { get; set; }
        public string DriverOS { get; set; }
        public string DriverAppVersion { get; set; }
        public string PassengerUserAgent { get; set; }
        public string PassengerOS { get; set; }
        public string PassengerAppVersion { get; set; }
        public string Complaint { get; set; }
        public string MidwayStop1Latitude { get; set; }
        public string MidwayStop1Longitude { get; set; }
        public string MidwayStop1Location { get; set; }
        public Nullable<System.DateTime> MidwayStop1ArrivalDateTime { get; set; }
        public Nullable<System.DateTime> MidwayStop1LeaveDateTime { get; set; }
        public bool isLogisticRequest { get; set; }
    }
}
