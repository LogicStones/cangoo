using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTOs.API
{
    public class ApplyCouponCode
    {
        [Required]
        public string PassengerId { get; set; }
        [Required]
        public string CouponCode { get; set; }
    }

    public class GetApplicationUser
    {
        [Required]
        public string TransferUserMobile { get; set; }
    }

    public class GetApplicationUserResponse
    {
        public string PassengerId { get; set; }
        public string PhoneNumber { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }

    public class AmountTransferByMobileNo
    {
        [Required]
        public string SenderId { get; set; }
        [Required]
        public string Amount { get; set; }
        [Required]
        public string ReceiverId { get; set; }
        [Required]
        public string WalletBalance { get; set; }
    }

    public class AmountTransferByMobileResponse
    {
        public string TransferedAmount { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }

    public class AddCouponCodeResponse
    {
        public string WalletBalance { get; set; }
    }

    public class GetCardsWalletRecharge
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

    public class CardsWalletRechargeResponse
    {
        public string Amount { get; set; }
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

    public class GetWalletDetailsModel
    {
        [Required]
        public string PassengerId { get; set; }
    }

    public class StripeCustomer
    {
        public string WalletBalance { get; set; }
        public string PassengerId { get; set; }
        public List<StripeCard> CardsList { get; set; }
    }
}
