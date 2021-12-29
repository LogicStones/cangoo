using Constants;
using DatabaseModel;
using DTOs.API;
using DTOs.Shared;
using Integrations;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace Services
{
    public class PaymentsServices
    {
        public static async Task<ResponseWrapper> GetUserWalletDetails(string passengerId, string applicationId, string resellerId)
        {
            using (CangooEntities dbcontext = new CangooEntities())
            {
                var profile = await UserService.GetProfileByIdAsync(passengerId, applicationId, resellerId);

                var response = new WalletDetailsResponse()
                {
                    PassengerId = passengerId,
                    WalletBalance = profile.WalletBalance.ToString(),
                    AvailableWalletBalance = profile.AvailableWalletBalance.ToString()
                };

                if (!string.IsNullOrEmpty(profile.CreditCardCustomerID))
                {
                    var customer = StripeIntegration.GetCustomer(profile.CreditCardCustomerID);
                    if (!string.IsNullOrEmpty(customer.Id))
                    {
                        response.CustomerId = profile.CreditCardCustomerID;
                        response.DefaultCardId = customer.InvoiceSettings.DefaultPaymentMethodId;
                        response.CardsList = StripeIntegration.GetCardsList(customer.Id);

                        return new ResponseWrapper
                        {
                            Error = false,
                            Message = ResponseKeys.msgSuccess,
                            Data = response
                        };
                    }
                    else
                    {
                        return new ResponseWrapper
                        {
                            Message = ResponseKeys.paymentGetwayError,
                            Data = response
                        };
                    }
                }
                else
                {
                    return new ResponseWrapper
                    {
                        Error = false,
                        Message = ResponseKeys.msgSuccess,
                        Data = response
                    };
                }
            }
        }

        #region Credit Card

        public static async Task<string> GetSetupIntentSecret(string customerId, string email, string passengerId)
        {
            if (string.IsNullOrEmpty(customerId))
            {
                customerId = StripeIntegration.CreateCustomer(passengerId, email);
                await UserService.UpdateStripeCustomerId(customerId, passengerId);
            }

            return StripeIntegration.GetSetupIntentClientSecret(customerId);
        }

        //public static async Task<ResponseWrapper> UpdateDefaultCreditCard(string cardToken, string customerId)
        public static async Task UpdateDefaultCreditCard(string cardToken, string customerId)
        {
            StripeIntegration.UpdateDefaultPaymentMethod(cardToken, customerId);
            //var cust = StripeIntegration.UpdateDefaultPaymentMethod(cardToken, customerId);

            //if (!string.IsNullOrEmpty(cust.Id))
            //{
            //    return new ResponseWrapper
            //    {
            //        Error = false,
            //        Data = new UpdateDefaultCreditCardResponse
            //        {
            //            CustomerId = cust.Id,
            //            DefaultCardId = cust.InvoiceSettings.DefaultPaymentMethodId,
            //            CardsList = StripeIntegration.GetCardsList(cust.Id)
            //        },
            //        Message = ResponseKeys.msgSuccess
            //    };
            //}
            //else
            //{
            //    return new ResponseWrapper
            //    {
            //        Message = ResponseKeys.paymentGetwayError
            //    };
            //}
        }

        public static async Task<ResponseWrapper> DeleteCreditCard(string cardToken, string customerId)
        {
            var card = StripeIntegration.DeleteCard(cardToken, customerId);

            if (!string.IsNullOrEmpty(card.Id))
            {
                var customer = StripeIntegration.GetCustomer(customerId);
                if (!string.IsNullOrEmpty(customer.Id))
                {
                    return new ResponseWrapper
                    {
                        Error = false,
                        Message = ResponseKeys.msgSuccess,
                        Data = new DeleteCreditCardResponse
                        {
                            CustomerId = customer.Id,
                            DefaultSourceId = customer.InvoiceSettings.DefaultPaymentMethodId,
                            CardsList = StripeIntegration.GetCardsList(customer.Id)
                        }
                    };
                }
                else
                {
                    return new ResponseWrapper
                    {
                        Message = ResponseKeys.paymentGetwayError
                    };
                }
            }
            else
            {
                return new ResponseWrapper
                {
                    Message = ResponseKeys.paymentGetwayError
                };
            }
        }

        public static async Task<ResponseWrapper> SetCreditCardPaymentMethod(string isPaidClientSide, string stripePaymentIntentId, string customerId, string cardId, string totalFare, string description)
        {
            if (string.IsNullOrEmpty(customerId) || string.IsNullOrEmpty(cardId) || string.IsNullOrEmpty(totalFare))
            {
                return new ResponseWrapper
                {
                    Message = ResponseKeys.invalidParameters
                };
            }

            if (bool.Parse(isPaidClientSide))
            {
                return new ResponseWrapper
                {
                    Error = false,
                    Message = ResponseKeys.msgSuccess,
                    Data = new CreditCardPaymentInent
                    {
                        PaymentIntentId = stripePaymentIntentId,
                        Status = TransactionStatus.requiresCapture
                    }
                };
            }

            var details = await AuthoizeCreditCardPayment(customerId, cardId, totalFare, description);

            if (!details.Status.Equals(TransactionStatus.requiresCapture))
            {
                return new ResponseWrapper
                {
                    Message = ResponseKeys.paymentGetwayError,
                    Data = details
                };
            }

            return new ResponseWrapper
            {
                Error = false,
                Message = ResponseKeys.msgSuccess,
                Data = details
            };
        }

        public static async Task<ResponseWrapper> SetWalletPaymentMethod(PassengerProfileDTO userProfile, string totalFare)
        {
            if (userProfile.AvailableWalletBalance < decimal.Parse(totalFare))
            {
                return new ResponseWrapper
                {
                    Message = ResponseKeys.insufficientWalletBalance
                };
            }
            else
            {
                userProfile.AvailableWalletBalance -= decimal.Parse(totalFare);
                return new ResponseWrapper
                {
                    Error = false,
                    Message = ResponseKeys.msgSuccess
                };
            }
        }

        public static async Task<CreditCardPaymentInent> AuthoizeCreditCardPayment(string customerId, string cardId, string fareAmount, string description)
        {
            //await UpdateDefaultCreditCard(cardId, customerId);

            var paymentIntent = StripeIntegration.AuthorizePayment(customerId, cardId, (long)(float.Parse(fareAmount) * 100), description);
            return new CreditCardPaymentInent
            {
                PaymentIntentId = paymentIntent.Id,
                Status = paymentIntent.Status,
                ClientSecret = paymentIntent.ClientSecret,
                Description = ""
                //Brand = paymentIntent.PaymentMethod.Card.Brand,
                //Last4Digits = paymentIntent.PaymentMethod.Card.Last4
            };
        }

        public static async Task<CreditCardPaymentInent> CancelAuthorizedPayment(string paymentIntentId)
        {
            var paymentIntent = StripeIntegration.CancelAuthorizedPayment(paymentIntentId);
            return new CreditCardPaymentInent
            {
                PaymentIntentId = paymentIntent.Id,
                Status = paymentIntent.Status
            };
        }

        public static async Task<CreditCardPaymentInent> CaptureAuthorizedPaymentPartially(string paymentIntentId, string fareAmount)
        {
            var paymentIntent = StripeIntegration.CaptureAuthorizedPaymentPartially(paymentIntentId, (long)(float.Parse(fareAmount) * 100));
            return new CreditCardPaymentInent
            {
                PaymentIntentId = paymentIntent.Id,
                Status = paymentIntent.Status
            };
        }

        public static async Task<CreditCardPaymentInent> CaptureTipFromTripCreditCard(string description, string customerId, long amount, string paymentIntentId)
        {
            var usedPaymentIntent = StripeIntegration.GetPaymentIntentDetails(paymentIntentId);

            var paymentIntent = StripeIntegration.CreatePaymentIntent(description, customerId, amount, usedPaymentIntent.PaymentMethodId);
            return new CreditCardPaymentInent
            {
                PaymentIntentId = paymentIntent.Id,
                Status = paymentIntent.Status
            };
        }

        #endregion

        #region Wallet Recharge

        public static async Task<ResponseWrapper> RedeemCouponCode(string passengerId, string couponCode)
        {
            using (CangooEntities dbcontext = new CangooEntities())
            {
                var couponDetails = await dbcontext.CouponsManagers.Where(x => x.Code.Equals(couponCode)).FirstOrDefaultAsync();

                if (couponDetails == null)
                {
                    return new ResponseWrapper
                    {
                        Message = ResponseKeys.invalidCouponCode,
                    };
                }
                else if (couponDetails.isUsed)
                {
                    return new ResponseWrapper
                    {
                        Message = ResponseKeys.couponCodeAlreadyApplied,
                    };
                }
                else
                {
                    var userProfile = dbcontext.UserProfiles.Where(x => x.UserID.ToString().Equals(passengerId)).FirstOrDefault();

                    if (userProfile == null)
                    {
                        return new ResponseWrapper
                        {
                            Message = ResponseKeys.userNotFound
                        };
                    }

                    UpdateWalletBalance(couponDetails.Amount, userProfile);

                    WalletTransfer wallet = new WalletTransfer
                    {
                        Amount = couponDetails.Amount,
                        RechargeDate = DateTime.UtcNow,
                        WalletTransferID = Guid.NewGuid(),
                        Referrence = "Coupon Code Redeemed - " + couponDetails.Code,
                        TransferredTo = Guid.Parse(passengerId),
                        TransferredBy = Guid.Parse(ConfigurationManager.AppSettings["ApplicationID"].ToString()),
                        ApplicationID = Guid.Parse(ConfigurationManager.AppSettings["ApplicationID"].ToString()),
                        ResellerID = Guid.Parse(ConfigurationManager.AppSettings["ResellerID"].ToString()),
                    };

                    dbcontext.WalletTransfers.Add(wallet);

                    couponDetails.isUsed = true;
                    couponDetails.UsedOn = wallet.RechargeDate;
                    couponDetails.UsedBy = Guid.Parse(passengerId);

                    await dbcontext.SaveChangesAsync();

                    return new ResponseWrapper
                    {
                        Error = false,
                        Message = ResponseKeys.msgSuccess,
                        Data = new RedeemCouponCodeResponse
                        {
                            RechargedAmount = couponDetails.Amount.ToString("0.00"),
                            WalletBalance = userProfile.WalletBalance.ToString(),
                            AvailableWalletBalance = userProfile.WalletBalance.ToString()
                        }
                    };
                }
            }
        }

        public static async Task<ResponseWrapper> MobilePaymentWalletRecharge(MobilePaymentWalletRechargeRequest model)
        {
            using (CangooEntities dbcontext = new CangooEntities())
            {
                var ApplicationId = Guid.Parse(ConfigurationManager.AppSettings["ApplicationID"].ToString());
                var ResellerId = Guid.Parse(ConfigurationManager.AppSettings["ResellerID"].ToString());

                //var userProfile = await UserService.GetProfileAsync(model.PassengerId, ApplicationId.ToString(), ResellerId.ToString());
                var userProfile = dbcontext.UserProfiles.Where(x => x.UserID.ToString().Equals(model.PassengerId)).FirstOrDefault();

                if (userProfile == null)
                {
                    return new ResponseWrapper
                    {
                        Message = ResponseKeys.userNotFound
                    };
                }

                WalletTransfer wallet = new WalletTransfer
                {
                    Amount = decimal.Parse(model.Amount),
                    RechargeDate = DateTime.UtcNow,
                    WalletTransferID = Guid.NewGuid(),
                    Referrence = "Wallet Recharge Using Method - " + model.Method + " And Transaction Id - " + model.TransactionId,
                    TransferredBy = ApplicationId,
                    TransferredTo = Guid.Parse(model.PassengerId),
                    ApplicationID = ApplicationId,
                    ResellerID = ResellerId,
                };

                //await UserService.UpdateUserWalletBalance(wallet.RechargeDate.ToString(), wallet.Amount.ToString(), model.PassengerId);
                dbcontext.WalletTransfers.Add(wallet);

                UpdateWalletBalance(decimal.Parse(model.Amount), userProfile);

                await dbcontext.SaveChangesAsync();

                return new ResponseWrapper
                {
                    Error = false,
                    Message = ResponseKeys.msgSuccess,
                    Data = new MobilePaymentWalletRechargeResponse
                    {
                        Amount = wallet.Amount.ToString()
                    }
                };
            }
        }

        #region In App Transfer

        public static async Task<ResponseWrapper> TransferUsingMobile(ShareWalletBalanceRequest model)
        {
            using (CangooEntities dbcontext = new CangooEntities())
            {
                var applicationId = Guid.Parse(ConfigurationManager.AppSettings["ApplicationID"].ToString());
                var resellerId = Guid.Parse(ConfigurationManager.AppSettings["ResellerID"].ToString());

                var senderProfile = dbcontext.UserProfiles.Where(x => x.UserID.ToString().Equals(model.SenderId)).FirstOrDefault();

                if(senderProfile.AvailableWalletBalance < decimal.Parse(model.ShareAmount))
                {
                    return new ResponseWrapper
                    {
                        Message = ResponseKeys.insufficientWalletBalance
                    };
                }

                senderProfile.WalletBalance -= decimal.Parse(model.ShareAmount);
                senderProfile.AvailableWalletBalance -= decimal.Parse(model.ShareAmount);

                var receiverProfile = dbcontext.UserProfiles.Where(x => x.UserID.ToString().Equals(model.ReceiverId)).FirstOrDefault();
                receiverProfile.WalletBalance = receiverProfile.WalletBalance == null ? decimal.Parse(model.ShareAmount) : receiverProfile.WalletBalance += decimal.Parse(model.ShareAmount);
                receiverProfile.AvailableWalletBalance += decimal.Parse(model.ShareAmount);
                receiverProfile.LastRechargedAt = DateTime.UtcNow;

                WalletTransfer wallet = new WalletTransfer
                {
                    Amount = decimal.Parse(model.ShareAmount),
                    RechargeDate = DateTime.UtcNow,
                    WalletTransferID = Guid.NewGuid(),
                    Referrence = "In App Wallet Transfer Using Mobile No",
                    TransferredBy = Guid.Parse(model.SenderId),
                    TransferredTo = Guid.Parse(model.ReceiverId),
                    ApplicationID = applicationId,
                    ResellerID = resellerId,
                };

                dbcontext.WalletTransfers.Add(wallet);
                await dbcontext.SaveChangesAsync();

                return new ResponseWrapper
                {
                    Error = false,
                    Message = ResponseKeys.msgSuccess,
                    Data = new ShareWalletBalanceResponse
                    {
                        FirstName = receiverProfile.FirstName,
                        LastName = receiverProfile.LastName,
                        AvailableWalletBalance = ((decimal)senderProfile.AvailableWalletBalance).ToString("0.00"),
                        WalletBalance = ((decimal)senderProfile.WalletBalance).ToString("0.00"),
                        TransferedAmount = model.ShareAmount
                    }
                };
            }
        }

        #endregion

        #endregion

        public static async Task ReleaseWalletScrewedAmount(string passengerId, decimal amount)
        {
            using (var dbContext = new CangooEntities())
            {
                var userProfile = await dbContext.UserProfiles.Where(up => up.UserID.Equals(passengerId)).FirstOrDefaultAsync();
                userProfile.AvailableWalletBalance += amount;
                dbContext.SaveChanges();
            }
        }

        private static void UpdateWalletBalance(decimal amount, UserProfile userProfile)
        {
            userProfile.LastRechargedAt = DateTime.UtcNow;
            userProfile.WalletBalance = userProfile.WalletBalance == null ? amount : userProfile.WalletBalance + amount;
            userProfile.AvailableWalletBalance += amount;
        }

    }
}