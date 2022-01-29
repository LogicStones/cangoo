using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTOs.API
{
    public class NotificationDetail
    {
        public string FeedId { get; set; } = "";
        public string Title { get; set; } = "";
        public string ShortDescription { get; set; } = "";
        public string Detail { get; set; } = "";
        public string Image { get; set; } = "";
        public string IsRead { get; set; } = "";
        public string CreationDate { get; set; } = "";
        public string ReadDate { get; set; } = "";
        public string ExpiryDate { get; set; } = "";
    }

    public class GetNotificationsListRequest
    {
        [Required]
        [DefaultValue("edf49e84-06fb-4a6d-9448-6011fc1bc611")]
        public string PassengerId { get; set; }
    }

    public class ReadNotificationRequest
    {
        [Required]
        [DefaultValue("edf49e84-06fb-4a6d-9448-6011fc1bc611")]
        public string PassengerId { get; set; }

        [Required]
        [DefaultValue("")]
        public string FeedId { get; set; }
    }
}
