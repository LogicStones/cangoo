using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTOs.API
{
    public class RedeemCouponCodeRequest
    {
        [Required]
        public string PassengerId { get; set; }
        [Required]
        public string CouponCode { get; set; }
    }

    public class RedeemCouponCodeResponse
    {
        public string WalletBalance { get; set; }
    }

    public class CheckAppUserRequest
    {
        [Required]
        public string ReciverMobileNo { get; set; }
    }

    public class CheckAppUserResponse
    {
        public string PassengerId { get; set; }
        public string PhoneNumber { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }

    public class ShareWalletBalanceRequest
    {
        [Required]
        public string SenderId { get; set; }
        [Required]
        public string ReceiverId { get; set; }
        [Required]
        public string TotalWalletBalance { get; set; }
        [Required]
        public string ShareAmount { get; set; }
    }

    public class ShareWalletBalanceResponse
    {
        public string TransferedAmount { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }


    public class MobilePaymentWalletRechargeRequest
    {
        [Required]
        public string TransactionId { get; set; }
        [Required]
        public string Method { get; set; }
        [Required]
        public string Amount { get; set; }
        [Required]
        public string PassengerId { get; set; }
    }

    public class MobilePaymentWalletRechargeResponse
    {
        public string Amount { get; set; }
    }

    public class WalletDetailsRequest
    {
        [Required]
        public string PassengerId { get; set; }
    }

    public class WalletDetailsResponse
    {
        public string PassengerId { get; set; }
        public string TotalWalletBalance { get; set; } = "0.00";
        public string AvailableWalletBalance { get; set; } = "0.00";
        public List<StripeCard> CardsList { get; set; } = new List<StripeCard>();
    }

    public class StripeCard
    {
        public string CardId { get; set; }
        public string Brand { get; set; }
        public string ExpiryMonth { get; set; }
        public string ExpiryYear { get; set; }
        public string Last4Digits { get; set; }
        public string CardHolderName { get; set; }
        public string CardDescription { get; set; } //addressline1 on stripe response
    }
}
