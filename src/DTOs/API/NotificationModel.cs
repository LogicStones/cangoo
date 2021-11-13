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
        public string PopupID { get; set; } = "";
        public string ReceiverID { get; set; } = "";
        public string Title { get; set; } = "";
        public string RidirectURL { get; set; } = "";
        public string Text { get; set; } = "";
        public string LinkButtonText { get; set; } = "";
        public string StartDate { get; set; } = "";
        public string Image { get; set; } = "";
        public string ExpiryDate { get; set; } = "";
        public string ButtonText { get; set; } = "";
    }

    public class GetNotificationListModel
    {
        [Required]
        public string ReceiverId { get; set; }
    }
}
