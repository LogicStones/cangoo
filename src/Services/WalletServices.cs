using Constants;
using DatabaseModel;
using DTOs.API;
using DTOs.Shared;
using Integrations;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;

namespace Services
{
    public class WalletServices
    {
        public static async Task<RedeemCouponCodeResponse> AddCouponCode(RedeemCouponCodeRequest model)
        {
            using (CangooEntities dbcontext = new CangooEntities())
            {
                var CouponsData = dbcontext.CouponsManagers.Where(x => x.Code.ToString().Equals(model.CouponCode)).FirstOrDefault();
                var AppID = Guid.Parse(ConfigurationManager.AppSettings["ApplicationID"].ToString());
                var ResellerId = Guid.Parse(ConfigurationManager.AppSettings["ResellerID"].ToString());
                WalletTransfer wallet = new WalletTransfer
                {
                    Amount = CouponsData.Amount,
                    RechargeDate = DateTime.UtcNow,
                    WalletTransferID = Guid.NewGuid(),
                    Referrence = "Promo Code Applied - " + CouponsData.Code,
                    TransferredBy = AppID,
                    TransferredTo = Guid.Parse(model.PassengerId),
                    ApplicationID = AppID,
                    ResellerID = ResellerId,
                };

                var userProfile = dbcontext.UserProfiles.Where(x => x.UserID.ToString().Equals(model.PassengerId)).FirstOrDefault();

                if (userProfile != null)
                {
                    userProfile.LastRechargedAt = wallet.RechargeDate;
                    userProfile.WalletBalance = userProfile.WalletBalance == null ? CouponsData.Amount : userProfile.WalletBalance + CouponsData.Amount;
                }
                else
                {
                    return null;
                }

                CouponsData.isUsed = true;
                CouponsData.UsedOn = wallet.RechargeDate;
                CouponsData.UsedBy = Guid.Parse(model.PassengerId);

                dbcontext.WalletTransfers.Add(wallet);
                await dbcontext.SaveChangesAsync();
                return new RedeemCouponCodeResponse
                {
                    WalletBalance = userProfile.WalletBalance.ToString(),
                };
            }
        }

        public static async Task<CouponsManager> IsValidCouponCode(string CouponCode)
        {
            using (CangooEntities dbcontext = new CangooEntities())
            {
                return dbcontext.CouponsManagers.Where(x => x.Code.ToString().Equals(CouponCode)).FirstOrDefault();
            }
        }

        public static async Task<CheckAppUserResponse> IsAppUserExist(string TransferUserMobile)
        {
            using (CangooEntities dbcontext = new CangooEntities())
            {
                var result = (from us in dbcontext.AspNetUsers
                              join up in dbcontext.UserProfiles
                              on us.Id equals up.UserID
                              where us.PhoneNumber.ToLower().Equals(TransferUserMobile.ToLower())
                              select new
                              { up.FirstName, up.LastName, us.Id, us.PhoneNumber }).FirstOrDefault();

                if (result != null)
                {
                    return new CheckAppUserResponse
                    {
                        PassengerId = result.Id,
                        FirstName = result.FirstName,
                        LastName = result.LastName,
                        PhoneNumber = result.PhoneNumber
                    };
                }
                else
                {
                    return null;
                }
            }
        }

        public static async Task<ShareWalletBalanceResponse> TransferUsingMobile(ShareWalletBalanceRequest model)
        {
            using (CangooEntities dbcontext = new CangooEntities())
            {
                var applicationid = Guid.Parse(ConfigurationManager.AppSettings["ApplicationID"].ToString());
                var resellerid = Guid.Parse(ConfigurationManager.AppSettings["ResellerID"].ToString());
                var senderProfile = await UserService.GetProfileAsync(model.SenderId, applicationid.ToString(), resellerid.ToString());
                if (senderProfile != null)
                {
                    var receiverProfile = await UserService.GetProfileAsync(model.ReceiverId, applicationid.ToString(), resellerid.ToString());
                    if (receiverProfile != null)
                    {
                        WalletTransfer wallet = new WalletTransfer
                        {
                            Amount = decimal.Parse(model.ShareAmount),
                            RechargeDate = DateTime.UtcNow,
                            WalletTransferID = Guid.NewGuid(),
                            Referrence = "In App Wallet Transfer Using Mobile No",
                            TransferredBy = Guid.Parse(model.SenderId),
                            TransferredTo = Guid.Parse(model.ReceiverId),
                            ApplicationID = applicationid,
                            ResellerID = resellerid,
                        };

                        var sendingProfile = dbcontext.UserProfiles.Where(x => x.UserID.ToString().Equals(model.SenderId)).FirstOrDefault();
                        if (sendingProfile != null)
                        {
                            if (sendingProfile.WalletBalance > 0)
                            {
                                sendingProfile.WalletBalance -= decimal.Parse(model.ShareAmount);
                            }
                            else
                            {
                                return null;
                            }
                        }
                        else
                        {
                            return null;
                        }

                        var transferProfile = dbcontext.UserProfiles.Where(x => x.UserID.ToString().Equals(model.ReceiverId)).FirstOrDefault();
                        if (transferProfile != null)
                        {
                            if (transferProfile.WalletBalance > 0)
                            {
                                transferProfile.LastRechargedAt = wallet.RechargeDate;
                                transferProfile.WalletBalance += decimal.Parse(model.ShareAmount);
                            }

                        }
                        else
                        {
                            return null;
                        }
                        dbcontext.WalletTransfers.Add(wallet);
                        await dbcontext.SaveChangesAsync();

                        return new ShareWalletBalanceResponse
                        {
                            FirstName = transferProfile.FirstName,
                            LastName = transferProfile.LastName,
                            TransferedAmount = wallet.Amount.ToString(),
                        };
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }
        }

        public static async Task<MobilePaymentWalletRechargeResponse> CardsWalletRecharge(MobilePaymentWalletRechargeRequest model)
        {
            using (CangooEntities dbcontext = new CangooEntities())
            {
                var ApplicationID = Guid.Parse(ConfigurationManager.AppSettings["ApplicationID"].ToString());
                var ResellerId = Guid.Parse(ConfigurationManager.AppSettings["ResellerID"].ToString());
                var userProfile = await UserService.GetProfileAsync(model.PassengerId, ApplicationID.ToString(), ResellerId.ToString());
                if (userProfile != null)
                {
                    WalletTransfer wallet = new WalletTransfer
                    {
                        Amount = decimal.Parse(model.Amount),
                        RechargeDate = DateTime.UtcNow,
                        WalletTransferID = Guid.NewGuid(),
                        Referrence = "Wallet Recharge Using Method - " + model.Method + " And Transaction Id - " + model.TransactionId,
                        TransferredBy = ApplicationID,
                        TransferredTo = Guid.Parse(model.PassengerId),
                        ApplicationID = ApplicationID,
                        ResellerID = ResellerId,
                    };

                    await UserService.UpdateUserWalletBalance(wallet.RechargeDate.ToString(), wallet.Amount.ToString(), model.PassengerId);
                    dbcontext.WalletTransfers.Add(wallet);
                    await dbcontext.SaveChangesAsync();

                    return new MobilePaymentWalletRechargeResponse
                    {
                        Amount = wallet.Amount.ToString()
                    };
                }
                else
                {
                    return null;
                }

            }

        }

        public static async Task<ResponseWrapper> GetUserWalletDetails(string passengerId, string applicationId, string resellerId)
        {
            using (CangooEntities dbcontext = new CangooEntities())
            {
                var profile = await UserService.GetProfileAsync(passengerId, applicationId, resellerId);

                if (!string.IsNullOrEmpty(profile.CreditCardCustomerID))
                {
                    var customer = StripeIntegration.GetCustomer(profile.CreditCardCustomerID);
                    if (!string.IsNullOrEmpty(customer.Id))
                    {
                        return new ResponseWrapper
                        {
                            Error = false,
                            Message = ResponseKeys.msgSuccess,
                            Data = new WalletDetailsResponse
                            {
                                PassengerId = passengerId,
                                TotalWalletBalance = profile.WalletBalance.ToString(),
                                AvailableWalletBalance = "0.00",
                                CustomerId = profile.CreditCardCustomerID,
                                DefaultCardId = customer.InvoiceSettings.DefaultPaymentMethodId,
                                CardsList = StripeIntegration.GetCardsList(customer.Id)
                            }
                        };
                    }
                    else
                    {
                        return new ResponseWrapper
                        {
                            Message = ResponseKeys.paymentGetwayError,
                            Data = new WalletDetailsResponse()
                            {
                                PassengerId = passengerId,
                                AvailableWalletBalance = "0.00",
                                CustomerId = profile.CreditCardCustomerID,
                                DefaultCardId = customer.InvoiceSettings.DefaultPaymentMethodId,
                                TotalWalletBalance = profile.WalletBalance.ToString()
                            }
                        };
                    }
                }
                else
                {
                    return new ResponseWrapper
                    {
                        Error = false,
                        Message = ResponseKeys.msgSuccess,
                        Data = new WalletDetailsResponse()
                        {
                            PassengerId = passengerId,
                            AvailableWalletBalance = "0.00",
                            CustomerId = profile.CreditCardCustomerID,
                            TotalWalletBalance = profile.WalletBalance.ToString(),
                        }
                    };
                }
            }
        }

        public static async Task<string> GetSetupIntentSecret(string customerId, string email, string passengerId)
        {
            if (string.IsNullOrEmpty(customerId))
            {
                customerId = StripeIntegration.CreateCustomer(passengerId, email);
                await UserService.UpdateStripeCustomerId(customerId, passengerId);
            }

            return StripeIntegration.GetSetupIntentClientSecret(customerId);
        }

        public static async Task<ResponseWrapper> UpdateDefaultCreditCard(string cardToken, string customerId)
        {
            var cust = StripeIntegration.UpdateDefaultPaymentMethod(cardToken, customerId);

            if (!string.IsNullOrEmpty(cust.Id))
            {
                return new ResponseWrapper
                {
                    Error = false,
                    Data = new UpdateDefaultCreditCardResponse
                    {
                        CustomerId = cust.Id,
                        DefaultCardId = cust.InvoiceSettings.DefaultPaymentMethodId,
                        CardsList = StripeIntegration.GetCardsList(cust.Id)
                    },
                    Message = ResponseKeys.msgSuccess
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

        //public static async Task<ResponseWrapper> HoldCreditCardPayment(string isPaidClientSide, string paymentId, string customerId, string fareAmount)
        //{
        //    double payment = Convert.ToDouble(fareAmount);
        //    CreditCardPaymentInent paymentDetails = new CreditCardPaymentInent();

        //        //1 euro is equivalent to 100 cents https://stripe.com/docs/currencies#zero-decimal

        //        if (payment > 0)
        //        {
        //            if (isPaidClientSide.ToLower().Equals("true"))
        //            {
        //                paymentDetails.Id = paymentId;
        //                paymentDetails.Status = "succeeded";
        //            }
        //            else
        //                paymentDetails = await AuthoizeCreditCardPayment(customerId, fareAmount);
        //        }

        //    return new ResponseWrapper
        //    {
        //        Error = false,
        //        Message = ResponseKeys.msgSuccess,
        //        Data = paymentDetails
        //    };

        //    if (payment == 0 || (payment > 0 && paymentDetails.Status.Equals("succeeded")))//!string.IsNullOrEmpty(paymentDetails.Id)))
        //    {
        //        using (CangooEntities dbContext = new CangooEntities())
        //        {
        //            var stripeTransactionId = payment > 0 ? "Trip CreditCard payment received. Stripe transactionId = " + paymentDetails.Id : "Trip creditcard payment. Zero payment.";

        //            return new ResponseWrapper { Error = false, Message = ResponseKeys.msgSuccess };
        //        }
        //    }
        //    else
        //    {
        //        return new ResponseWrapper
        //        {
        //            Message = ResponseKeys.paymentGetwayError,
        //            Data = new Dictionary<dynamic, dynamic>
        //            {
        //                { "Status", paymentDetails.Status },
        //                { "ClientSecret", paymentDetails.ClientSecret },
        //                { "FailureMessage", paymentDetails.Description }
        //            }
        //        };
        //    }
        //}

        public static async Task<CreditCardPaymentInent> AuthoizeCreditCardPayment(string customerId, string cardId, string fareAmount)
        {
            var paymentIntent = StripeIntegration.AuthorizePayment(customerId, cardId, (long)(float.Parse(fareAmount) * 100));
            return new CreditCardPaymentInent
            {
                PaymentIntentId = paymentIntent.Id,
                Status = paymentIntent.Status,
                ClientSecret = paymentIntent.ClientSecret,
                Description = "",
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

        //public static async Task<CreditCardPaymentInent> UpdateAuthorizedPayment(string paymentIntentId, string fareAmount)
        //{
        //    var paymentIntent = StripeIntegration.UpdateAuthorizedPayment(paymentIntentId, (long)(float.Parse(fareAmount) * 100));
        //    return new CreditCardPaymentInent
        //    {
        //        PaymentIntentId = paymentIntent.Id,
        //        Status = paymentIntent.Status
        //    };
        //}

        public static async Task<CreditCardPaymentInent> CaptureAuthorizedPaymentPartially(string paymentIntentId, string fareAmount)
        {
            var paymentIntent = StripeIntegration.CaptureAuthorizedPaymentPartially(paymentIntentId, (long)(float.Parse(fareAmount) * 100));
            return new CreditCardPaymentInent
            {
                PaymentIntentId = paymentIntent.Id,
                Status = paymentIntent.Status
            };
        }

        //public static async Task<ResponseWrapper> CaptureCreditCardPayment(string isPaidClientSide, string paymentId,
        //    string tripId, string customerId, string fareAmount, string tipAmount, string fleetId,
        //    string passengerId, string applicationId, string promoDiscountAmount, string walletUsedAmount)
        //{
        //    bool isAlreadyPaid = false;// CheckIfAlreadyPaid(model.TripId);

        //    //model.amount = TripFare + Tip
        //    double payment = Convert.ToDouble(fareAmount);
        //    CreditCardPaymentInent paymentDetails = new CreditCardPaymentInent();

        //    if (!isAlreadyPaid)
        //    {
        //        //1 euro is equivalent to 100 cents https://stripe.com/docs/currencies#zero-decimal

        //        if (payment > 0)
        //        {
        //            if (isPaidClientSide.ToLower().Equals("true"))
        //            {
        //                paymentDetails.PaymentIntentId = paymentId;
        //                paymentDetails.Status = "succeeded";
        //            }
        //            else
        //                paymentDetails = await CaptureAuthorizedPayment(tripId, customerId, fareAmount, tipAmount);
        //        }
        //    }

        //    if (isAlreadyPaid || payment == 0 || (payment > 0 && paymentDetails.Status.Equals("succeeded")))//!string.IsNullOrEmpty(paymentDetails.Id)))
        //    {
        //        using (CangooEntities dbContext = new CangooEntities())
        //        {
        //            var stripeTransactionId = payment > 0 ? "Trip CreditCard payment received. Stripe transactionId = " + paymentDetails.PaymentIntentId : "Trip creditcard payment. Zero payment.";

        //            //If already paid, trip will not update the trip data but returns required info.

        //            var trip = dbContext.spAfterMobilePayment(false,//Convert.ToBoolean(model.isOverride), 
        //                tripId,
        //                stripeTransactionId,
        //                (int)TripStatuses.Completed,
        //                passengerId,
        //                applicationId,
        //                (Convert.ToDouble(fareAmount)).ToString(),
        //                "0.00",
        //                promoDiscountAmount,
        //                walletUsedAmount,
        //                tipAmount,
        //                DateTime.UtcNow,
        //                (int)PaymentModes.CreditCard,
        //                (int)PaymentStatuses.Paid,
        //                fleetId).FirstOrDefault();

        //            var notificationPayload = new CreditCardPaymentNotification
        //            {
        //                tripID = tripId,
        //                tip = tipAmount,
        //                amount = string.Format("{0:0.00}", Convert.ToDouble(fareAmount) + Convert.ToDouble(walletUsedAmount) + Convert.ToDouble(promoDiscountAmount))
        //            };

        //            if (!isAlreadyPaid)
        //                await PushyService.UniCast(trip.DeviceToken, notificationPayload, NotificationKeys.cap_paymentSuccess);

        //            await FirebaseService.DeleteTrip(tripId);

        //            await FirebaseService.SetDriverFree(trip.CaptainID.ToString(), tripId);

        //            //to avoid login on another device during trip
        //            await FirebaseService.FreePassengerFromCurrentTrip(passengerId, tripId);

        //            //if (!isAlreadyPaid)
        //            //    SendInvoice(new InvoiceModel
        //            //    {
        //            //        CustomerEmail = trip.CustomerEmail,// context.AspNetUsers.Where(u => u.Id.Equals(model.passengerID)).FirstOrDefault().Email,
        //            //        TotalAmount = (Convert.ToDouble(tipAmount) + Convert.ToDouble(fareAmount) + Convert.ToDouble(walletUsedAmount) + Convert.ToDouble(promoDiscountAmount)).ToString(),
        //            //        WalletUsedAmount = walletUsedAmount,
        //            //        PromoDiscountAmount = promoDiscountAmount,
        //            //        CashAmount = "0",
        //            //        CaptainName = trip.CaptainName,
        //            //        CustomerName = trip.CustomerName,
        //            //        TripDate = trip.TripDate,
        //            //        InvoiceNumber = trip.InvoiceNumber,
        //            //        FleetName = trip.FleetName,
        //            //        ATUNumber = trip.FleetATUNumber,
        //            //        Street = trip.FleetAddress,
        //            //        BuildingNumber = trip.FleetBuildingNumber,
        //            //        PostCode = trip.FleetPostalCode,
        //            //        City = trip.FleetCity,
        //            //        PickUpAddress = trip.PickUpLocation,
        //            //        DropOffAddress = trip.DropOffLocation,
        //            //        CaptainUserName = trip.CaptainUserName,
        //            //        Distance = trip.DistanceInKM.ToString("0.00"),
        //            //        VehicleNumber = trip.PlateNumber,
        //            //        FleetEmail = trip.FleetEmail
        //            //    });

        //            return new ResponseWrapper { Error = false, Message = ResponseKeys.msgSuccess };
        //        }
        //    }
        //    else
        //    {
        //        return new ResponseWrapper
        //        {
        //            Message = ResponseKeys.paymentGetwayError,
        //            Data = new Dictionary<dynamic, dynamic>
        //            {
        //                { "Status", paymentDetails.Status },
        //                { "ClientSecret", paymentDetails.ClientSecret },
        //                { "FailureMessage", paymentDetails.Description }
        //            }
        //        };
        //    }
        //}

        public static async Task<CreditCardPaymentInent> CaptureAuthorizedPayment(string paymentIntentId)
        {
            var paymentIntent = StripeIntegration.CaptureAuthorizedPayment(paymentIntentId);
            return new CreditCardPaymentInent
            {
                PaymentIntentId = paymentIntent.Id,
                Status = paymentIntent.Status
            };
        }
        private void SendInvoice(InvoiceModel model)
        {
            //var headerLink = this.Url.Link("Default", new { Controller = "Invoice", Action = "Header" });
            //var footerLink = this.Url.Link("Default", new { Controller = "Invoice", Action = "Footer" });

            //System.Web.Routing.RouteData route = new System.Web.Routing.RouteData();
            //route.Values.Add("action", "SendInvoice");
            //route.Values.Add("controller", "Invoice");

            //InvoiceController controllerObj = new InvoiceController();
            //System.Web.Mvc.ControllerContext newContext = new System.Web.Mvc.ControllerContext(new HttpContextWrapper(System.Web.HttpContext.Current), route, controllerObj);
            //controllerObj.ControllerContext = newContext;

            //controllerObj.SendInvoice(model, headerLink, footerLink);
        }
    }
}