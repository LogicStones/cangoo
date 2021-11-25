using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTOs.API
{
    public class NotificationDetails
    {
        public string FeedId { get; set; } = "";
        public string Title { get; set; } = "";
        public string ShortDescription { get; set; } = "";
        public string CreationDate { get; set; } = "";
        public string ExpiryDate { get; set; } = "";
    }

    public class GetReadNotificationResponse
    {
        public string FeedId { get; set; } = "";
        public string Title { get; set; } = "";
        public string Detail { get; set; } = "";
        public string ExpiryDate { get; set; } = "";
        public string CreationDate { get; set; } = "";
    }

    public class NotificationListRequest
    {
        [Required]
        public string ReceiverId { get; set; }
    }

    public class ReadNotificationRequest
    {
        [Required]
        public string FeedId { get; set; }
    }
}
