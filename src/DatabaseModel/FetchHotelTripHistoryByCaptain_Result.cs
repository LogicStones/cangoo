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
    
    public partial class FetchHotelTripHistoryByCaptain_Result
    {
        public Nullable<long> RowNo { get; set; }
        public Nullable<int> Total { get; set; }
        public string BookingDateTime { get; set; }
        public string Vehicle { get; set; }
        public Nullable<System.Guid> VehicleID { get; set; }
        public string Status { get; set; }
        public System.Guid tripId { get; set; }
        public string Type { get; set; }
        public Nullable<decimal> Fare { get; set; }
    }
}
