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
    
    public partial class FetchVehicleTripsHistory_Result
    {
        public Nullable<long> RowNo { get; set; }
        public Nullable<int> Total { get; set; }
        public string TripDate { get; set; }
        public string Type { get; set; }
        public string Status { get; set; }
        public System.Guid tripId { get; set; }
        public Nullable<int> totalTrips { get; set; }
        public Nullable<decimal> avgDriverRating { get; set; }
        public Nullable<decimal> totalFare { get; set; }
        public Nullable<decimal> totalCashEarning { get; set; }
        public Nullable<decimal> totalMobilePayEarning { get; set; }
        public Nullable<decimal> totalTip { get; set; }
        public Nullable<decimal> totalEarnedPoints { get; set; }
        public string UserName { get; set; }
        public string CaptainName { get; set; }
        public Nullable<decimal> Fare { get; set; }
    }
}
