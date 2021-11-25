﻿using Constants;
using DatabaseModel;
using DTOs.API;
using DTOs.Shared;
using Integrations;
using System;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;

namespace Services
{
    public class WalletRechargeServices
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

                    await UserService.UpdateUserWalletBalance(wallet.RechargeDate.ToString(), wallet.Amount.ToString());
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
                            Data = new WalletDetailsResponse()
                            {
                                PassengerId = customer.Id,
                                TotalWalletBalance = profile.WalletBalance.ToString(),
                                AvailableWalletBalance = "0.00",
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
                                PassengerId = customer.Id,
                                AvailableWalletBalance = "0.00",
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
                            TotalWalletBalance = profile.WalletBalance.ToString(),
                        }
                    };
                }
            }
        }
    }
}