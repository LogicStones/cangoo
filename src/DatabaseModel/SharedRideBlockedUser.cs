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
    
    public partial class SharedRideBlockedUser
    {
        public System.Guid ID { get; set; }
        public Nullable<System.Guid> BlockedBy { get; set; }
        public Nullable<System.Guid> BlockedUserID { get; set; }
        public System.DateTime BlockDate { get; set; }
        public System.Guid ApplicationID { get; set; }
    }
}
