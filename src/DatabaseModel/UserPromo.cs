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
    
    public partial class UserPromo
    {
        public System.Guid ID { get; set; }
        public string UserID { get; set; }
        public System.Guid PromoID { get; set; }
        public bool isActive { get; set; }
        public System.Guid ApplicationID { get; set; }
        public int NoOfUsage { get; set; }
    }
}
