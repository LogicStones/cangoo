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
    
    public partial class FetchFacilities_Result
    {
        public Nullable<long> RowNo { get; set; }
        public Nullable<int> Total { get; set; }
        public int FacilityID { get; set; }
        public string FacilityIcon { get; set; }
        public System.Guid ApplicationID { get; set; }
        public string FacilityName { get; set; }
        public System.Guid ResellerID { get; set; }
        public bool isActive { get; set; }
    }
}
