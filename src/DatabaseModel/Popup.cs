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
    
    public partial class Popup
    {
        public int PopupID { get; set; }
        public int ReceiverID { get; set; }
        public string Title { get; set; }
        public string RidirectURL { get; set; }
        public string Text { get; set; }
        public string LinkButtonText { get; set; }
        public Nullable<System.DateTime> StartDate { get; set; }
        public string Image { get; set; }
        public Nullable<System.DateTime> ExpiryDate { get; set; }
        public string ButtonText { get; set; }
        public Nullable<System.Guid> ApplicationID { get; set; }
        public Nullable<System.Guid> ResellerId { get; set; }
        public Nullable<System.DateTime> CreatedAt { get; set; }
        public Nullable<System.Guid> UserID { get; set; }
        public bool IsActive { get; set; }
    }
}
