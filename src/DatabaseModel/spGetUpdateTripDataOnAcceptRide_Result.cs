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
    
    public partial class spGetUpdateTripDataOnAcceptRide_Result
    {
        public Nullable<int> ArrivedTime { get; set; }
        public Nullable<double> RequestWaitingTime { get; set; }
        public Nullable<System.Guid> UserID { get; set; }
        public Nullable<int> VehicleCategoryId { get; set; }
        public string VehicleCategory { get; set; }
        public Nullable<decimal> TotalFare { get; set; }
        public string PickupLocationLatitude { get; set; }
        public string PickupLocationLongitude { get; set; }
        public string PickUpLocation { get; set; }
        public string PickupLocationPostalCode { get; set; }
        public string MidwayStop1Latitude { get; set; }
        public string MidwayStop1Longitude { get; set; }
        public string MidwayStop1Location { get; set; }
        public string MidwayStop1PostalCode { get; set; }
        public string dropoffLocationLatitude { get; set; }
        public string dropofflocationLongitude { get; set; }
        public string DropOffLocation { get; set; }
        public string DropOffLocationPostalCode { get; set; }
        public Nullable<System.DateTime> PickUpBookingDateTime { get; set; }
        public Nullable<System.DateTime> BookingDateTime { get; set; }
        public bool isReRouted { get; set; }
        public string description { get; set; }
        public string VoucherCode { get; set; }
        public Nullable<decimal> VoucherAmount { get; set; }
        public string PhoneNumber { get; set; }
        public string PassengerName { get; set; }
    }
}
