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
    
    public partial class spGetFleetMonthlyInvoice_Result
    {
        public string FleetName { get; set; }
        public string ReportNumber { get; set; }
        public Nullable<System.Guid> CaptainID { get; set; }
        public string CaptainCode { get; set; }
        public Nullable<int> TotalTrips { get; set; }
        public decimal TotalTip { get; set; }
        public decimal TotalCashEarning { get; set; }
        public decimal TotalMobilePayEarning { get; set; }
        public decimal ApplicationProfit { get; set; }
        public Nullable<decimal> PayableToCaptain { get; set; }
    }
}
