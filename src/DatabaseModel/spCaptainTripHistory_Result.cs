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
    
    public partial class spCaptainTripHistory_Result
    {
        public System.Guid tripID { get; set; }
        public Nullable<int> totalRecord { get; set; }
        public Nullable<long> rn { get; set; }
        public Nullable<int> totalTrips { get; set; }
        public Nullable<decimal> avgDriverRating { get; set; }
        public Nullable<decimal> avgVehicleRating { get; set; }
        public Nullable<decimal> totalEarnedPoints { get; set; }
        public Nullable<decimal> totalFare { get; set; }
        public Nullable<decimal> totalCashEarning { get; set; }
        public Nullable<decimal> totalMobilePayEarning { get; set; }
        public Nullable<decimal> totalTip { get; set; }
        public Nullable<decimal> Fare { get; set; }
        public Nullable<decimal> Tip { get; set; }
        public Nullable<decimal> TripCashPayment { get; set; }
        public Nullable<decimal> TripMobilePayment { get; set; }
        public string PickupLocationLatitude { get; set; }
        public string PickupLocationLongitude { get; set; }
        public string PickUpLocation { get; set; }
        public string DropOffLocationLatitude { get; set; }
        public string DropOffLocationLongitude { get; set; }
        public string DropOffLocation { get; set; }
        public Nullable<System.DateTime> BookingDateTime { get; set; }
        public Nullable<System.DateTime> PickUpBookingDateTime { get; set; }
        public Nullable<System.DateTime> ArrivalDateTime { get; set; }
        public Nullable<System.DateTime> TripStartDatetime { get; set; }
        public Nullable<System.DateTime> TripEndDatetime { get; set; }
        public Nullable<double> DistanceTraveled { get; set; }
        public string facilities { get; set; }
        public double DriverEarnedPoints { get; set; }
        public int DriverRating { get; set; }
        public int VehicleRating { get; set; }
        public string PassengerName { get; set; }
        public string Status { get; set; }
        public string BookingType { get; set; }
        public string BookingMode { get; set; }
        public string PlateNumber { get; set; }
        public string Model { get; set; }
        public string make { get; set; }
        public string PaymentMode { get; set; }
    }
}
