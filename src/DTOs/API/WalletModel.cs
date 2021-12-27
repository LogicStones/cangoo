using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTOs.API
{
    public class RedeemCouponCodeRequest
    {
        [Required]
        [DefaultValue("edf49e84-06fb-4a6d-9448-6011fc1bc611")]
        public string PassengerId { get; set; }

        [Required]
        [DefaultValue("")]
        public string CouponCode { get; set; }
    }

    public class RedeemCouponCodeResponse
    {
        public string RechargedAmount { get; set; }
        public string WalletBalance { get; set; }
    }

    public class CheckAppUserRequest
    {
        [Required]
        [DefaultValue("+923117803648")]
        public string ReceiverMobileNo { get; set; }
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
        [DefaultValue("edf49e84-06fb-4a6d-9448-6011fc1bc611")]
        public string SenderId { get; set; }

        [Required]
        [DefaultValue("edf49e84-06fb-4a6d-9448-6011fc1bc611")]
        public string ReceiverId { get; set; }
        
        [Required]
        [DefaultValue("10.23")]
        public string ShareAmount { get; set; }
    }

    public class ShareWalletBalanceResponse
    {
        public string WalletBalance { get; set; }
        public string AvailableWalletBalance { get; set; }
        public string TransferedAmount { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }

    public class MobilePaymentWalletRechargeRequest
    {
        [Required]
        [DefaultValue("Test-Stripe-Payment-IntentId")]
        public string TransactionId { get; set; }

        [Required]
        [DefaultValue("GooglePay, ApplePay, CreditCard, PayPal")]
        public string Method { get; set; }
        
        [Required]
        [DefaultValue("10.20")]
        public string Amount { get; set; }
        
        [Required]
        [DefaultValue("edf49e84-06fb-4a6d-9448-6011fc1bc611")]
        public string PassengerId { get; set; }
    }

    public class MobilePaymentWalletRechargeResponse
    {
        public string Amount { get; set; }
    }

    public class WalletDetailsRequest
    {
        [Required]
        [DefaultValue("edf49e84-06fb-4a6d-9448-6011fc1bc611")]
        public string PassengerId { get; set; }
    }

    public class WalletDetailsResponse
    {
        public string PassengerId { get; set; }
        public string TotalWalletBalance { get; set; } = "0.00";
        public string AvailableWalletBalance { get; set; } = "0.00";
        public string CustomerId { get; set; } = "";
        public string DefaultCardId { get; set; } = "";
        public List<StripeCard> CardsList { get; set; } = new List<StripeCard>();
    }

    //string pID, string email, string customerID

    public class StripeClientSecretRequest
    {
        [Required]
        [DefaultValue("edf49e84-06fb-4a6d-9448-6011fc1bc611")]
        public string PassengerId { get; set; }

        [Required]
        [DefaultValue("test.logicstones@gmail.com")]
        public string Email { get; set; }

        [DefaultValue("")]
        public string CustomerId { get; set; }
    }

    public class StripeClientSecretResponse
    {
        public string ClientSecret { get; set; } = "";
    }

    public class DeleteCreditCardRequest
    {
        [Required]
        [DefaultValue("")]
        public string CardToken { get; set; }

        [Required]
        [DefaultValue("")]
        public string CustomerId { get; set; }
    }

    public class DeleteCreditCardResponse
    {
        public string CustomerId { get; set; }
        public string DefaultSourceId { get; set; }
        public List<StripeCard> CardsList { get; set; } = new List<StripeCard>();
    }

    public class CreditCardPaymentInent
    {
        public string PaymentIntentId { get; set; }
        public string Status { get; set; }
        public string ClientSecret { get; set; }
        public string Description { get; set; }
        //public string Brand { get; set; }
        //public string Last4Digits { get; set; }
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

    //public class UpdateDefaultCreditCardRequest
    //{
    //    [Required]
    //    public string CardToken { get; set; }

    //    [Required]
    //    public string CustomerId { get; set; }
    //}

    public class UpdateDefaultCreditCardResponse
    {
        public string CustomerId { get; set; }
        public string DefaultCardId { get; set; }
        public List<StripeCard> CardsList { get; set; } = new List<StripeCard>();
    }

    //public class AuthorizeCreditCardPaymentRequest
    //{
    //    [Required]
    //    [DefaultValue("EUR")]
    //    public string Currency { get; set; }

    //    [Required]
    //    [DefaultValue("cus_Ki0HplO99Pjr5L")]
    //    public string CustomerId { get; set; }

    //    [Required]
    //    [DefaultValue("card_1K2g2vJeFP4nLZjMXR3F3fDr")]
    //    public string CardId { get; set; }

    //    [Required]
    //    [DefaultValue("16.23")]
    //    public string FareAmount { get; set; }

    //    [Required]
    //    [DefaultValue("edf49e84-06fb-4a6d-9448-6011fc1bc611")]
    //    public string PassengerID { get; set; }

    //    //[Required]
    //    [DefaultValue("False")]
    //    public string IsPaidClientSide { get; set; }

    //    public string PaymentId { get; set; }
    //}

    //public class CancelAuthorizedCreditCardPaymentRequest
    //{
    //    [Required]
    //    [DefaultValue("pi_3K2y80JeFP4nLZjM1OzIRdrY")]
    //    public string PaymentIntentId { get; set; }
    //}

    //public class AdjustCreditCardPaymentRequest
    //{
    //    [Required]
    //    [DefaultValue("pi_3K2y80JeFP4nLZjM1OzIRdrY")]
    //    public string PaymentIntentId { get; set; }

    //    [Required]
    //    [DefaultValue("16.23")]
    //    public string FareAmount { get; set; }
    //}

    //public class UpdateCreditCardPaymentRequest
    //{
    //    [Required]
    //    [DefaultValue("pi_3K2y80JeFP4nLZjM1OzIRdrY")]

    //    public string PaymentIntentId { get; set; }

    //    [Required]
    //    [DefaultValue("16.23")]
    //    public string FareAmount { get; set; }
    //}

    //public class CaptureCreditCardPaymentRequest
    //{
    //    [Required]
    //    [DefaultValue("pi_3K2y80JeFP4nLZjM1OzIRdrY")]
    //    public string PaymentIntentId { get; set; }
    //}

    //public class CreditCardPaymentRequest
    //{
    //    [Required]
    //    public string Currency { get; set; }
    //    [Required]
    //    public string CustomerId { get; set; }
    //    [Required]
    //    public string IsPaidClientSide { get; set; }
    //    [Required]
    //    public string TipAmount { get; set; }
    //    [Required]
    //    public string FareAmount { get; set; }
    //    [Required]
    //    public string PassengerID { get; set; }
    //    [Required]
    //    public string TripId { get; set; }
    //    [Required]
    //    public string IsOverride { get; set; }
    //    [Required]
    //    public string FleetID { get; set; }
    //    [Required]
    //    public string PromoDiscountAmount { get; set; }
    //    [Required]
    //    public string WalletUsedAmount { get; set; }
    //    [Required]
    //    public string PaymentId { get; set; }
    //}

}
