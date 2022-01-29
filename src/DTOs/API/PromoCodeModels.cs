using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTOs.API
{
    public class PromoCodeDetail
    {
        public string UserPromoCodeId { get; set; } = "";
        public string PromoCodeId { get; set; } = "";
        public string PromoCode { get; set; } = "";
        public string AllowedRepetitions { get; set; } = "";
        public string NoOfUsage { get; set; } = "";
        public string StartDate { get; set; } = "";
        public string ExpiryDate { get; set; } = "";
        public string Amount { get; set; } = "";
        public string DiscountTypeId { get; set; } = "";
    }

    public class GetPassengerPromoRequest
    {
        [Required]
        [DefaultValue("95c08150-c0c6-431a-9f2c-2f223cef8911")]
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
