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
    
    public partial class Application
    {
        public System.Guid ApplicationID { get; set; }
        public System.Guid ResellerID { get; set; }
        public string OwnerName { get; set; }
        public string CompanyName { get; set; }
        public string Logo { get; set; }
        public string OriginalLogo { get; set; }
        public string ContractFile { get; set; }
        public string OriginalContractFile { get; set; }
        public string AuthorizedArea { get; set; }
        public Nullable<double> PercentagePayable { get; set; }
        public double PercentageReceiveable { get; set; }
        public int SubscriptionPlanID { get; set; }
        public System.DateTime SubscriptionDate { get; set; }
        public Nullable<System.DateTime> PaymentDueDate { get; set; }
        public int SubscriptionTypeID { get; set; }
        public int PaymentModeID { get; set; }
        public int PaymentTypeID { get; set; }
        public int PaymentStatusID { get; set; }
        public string Address { get; set; }
        public string TaxNumber { get; set; }
        public string SubscribedModules { get; set; }
        public bool isBlocked { get; set; }
    }
}
