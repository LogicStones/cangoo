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
    
    public partial class NewsFeed
    {
        public System.Guid FeedID { get; set; }
        public string Title { get; set; }
        public string ShortDescription { get; set; }
        public string Detail { get; set; }
        public System.DateTime CreationDate { get; set; }
        public System.DateTime ExpiryDate { get; set; }
        public int ApplicationUserTypeID { get; set; }
        public System.Guid ApplicationID { get; set; }
    }
}
