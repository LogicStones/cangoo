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
    
    public partial class CaptainPaymentsHistory
    {
        public System.Guid CaptainPaymentsHistoryId { get; set; }
        public System.Guid CaptainId { get; set; }
        public System.Guid TransferredBy { get; set; }
        public decimal Amount { get; set; }
        public string Reference { get; set; }
        public System.DateTime TransferDate { get; set; }
        public System.Guid TransactionId { get; set; }
        public System.Guid ApplicationID { get; set; }
        public System.Guid ResellerID { get; set; }
    }
}
