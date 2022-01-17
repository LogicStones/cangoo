using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTOs.API
{
    public class DashboardDataResponse
    {
        public string TotalNotifications { get; set; } = "";
        public string RewardPoint { get; set; } = "";
        public PopUpDetailsDTO PopUp { get; set; } = new PopUpDetailsDTO();
    }

    public class PopUpDetailsDTO
    {
        public string PopupID { get; set; } = "";
        public string ReceiverID { get; set; } = "";
        public string Title { get; set; } = "";
        public string Text { get; set; } = "";
        public string LinkButtonText { get; set; } = "";
        public string RidirectURL { get; set; } = "";
        public string ImagePath { get; set; } = "";
        public string ExpiryDate { get; set; } = "";
        public string StartDate { get; set; } = "";
        public string ButtonText { get; set; } = "";
    }
}