using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTOs.API
{
    public class PromoCodeDetails
    {
        public string ID { get; set; } = "";
        public string PromoID { get; set; } = "";
        public string NoOfUsage { get; set; } = "";
        public string PromoCode { get; set; } = "";
        public string StartDate { get; set; } = "";
        public string ExpiryDate { get; set; } = "";
        public string Amount { get; set; } = "";
        public string PaymentType { get; set; } = "";
        public string Repetition { get; set; } = "";

    }

    public class GetPassengerPromoRequest
    {
        [Required]
        public string PassengerId { get; set; }
    }

    public class AddPromoCodeRequest
    {
        [Required]
        public string PromoCode { get; set; }
        [Required]
        public string PassengerId { get; set; }
    }

    public class AddUserPromoResponse
    {
        [Required]
        public string Amount { get; set; }
        [Required]
        public string PromoType { get; set; }
        [Required]
        public string PromoCode { get; set; }
        [Required]
        public string ExpiryDate { get; set; }
        [Required]
        public string AllowedRepition { get; set; }
        [Required]
        public string NoOfUsage { get; set; }
    }
}
