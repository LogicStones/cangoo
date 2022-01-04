using Constants;
using DatabaseModel;
using DTOs.API;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services
{
    public class InvitationService
    {
        public static async Task<ResponseWrapper> ApplyShareCode(string shareCode, string passengerId)
        {
            using (var dbContext = new CangooEntities())
            {
                //IsUserEligible
                if (dbContext.Trips.Where(x => x.UserID.ToString().Equals(passengerId)).Any())
                {
                    return new ResponseWrapper
                    {
                        Error = true,
                        Message = ResponseKeys.inviteCodeNotApplicable,
                    };
                }

                //IsUserAlreadyInvited
                if (dbContext.UserInvites.Where(x => x.UserID.ToString().Equals(passengerId)).Any())
                {
                    return new ResponseWrapper
                    {
                        Error = true,
                        Message = ResponseKeys.inviteCodeAlreadyApplied,
                    };
                }


                //Validate and Apply
                var applicationId = ConfigurationManager.AppSettings["ApplicationID"].ToString();
                var resellerId = ConfigurationManager.AppSettings["ResellerID"].ToString();

                var result = await DriverService.GetDriverByInviteCode(shareCode);
                if (result != null)
                {
                    UserInvite userInvite = new UserInvite
                    {
                        UserInvitesID = Guid.NewGuid(),
                        UserID = Guid.Parse(passengerId),
                        ReferralID = result.CaptainID,
                        DateTime = DateTime.UtcNow,
                        ApplicationID = Guid.Parse(applicationId),
                        IsReferredByDriver = true
                    };

                    result.EarningPoints = result.EarningPoints == null ? 50 : result.EarningPoints + 50 <= 300 ? result.EarningPoints + 50 : 300;

                    WalletTransfer wallet = new WalletTransfer
                    {
                        Amount = 10,
                        RechargeDate = DateTime.UtcNow,
                        WalletTransferID = Guid.NewGuid(),
                        Referrence = "Reward: Captain invite code applied.",
                        TransferredBy = Guid.Parse(applicationId),
                        TransferredTo = Guid.Parse(passengerId),
                        ApplicationID = Guid.Parse(applicationId),
                        ResellerID = Guid.Parse(resellerId),
                    };

                    var userProfile = dbContext.UserProfiles.Where(x => x.UserID.ToString().Equals(passengerId)).FirstOrDefault();
                    if (userProfile != null)
                    {
                        userProfile.LastRechargedAt = DateTime.UtcNow;
                        userProfile.WalletBalance += 10;
                        userProfile.AvailableWalletBalance += 10;
                    }

                    dbContext.UserInvites.Add(userInvite);
                    dbContext.WalletTransfers.Add(wallet);

                    await dbContext.SaveChangesAsync();
                }
                else
                {
                    var user = await UserService.GetProfileByShareCodeAsync(shareCode, applicationId, resellerId);
                    if (result != null)
                    {
                        //TBD : What to reward
                    }
                    else
                    {
                        return new ResponseWrapper
                        {
                            Error = true,
                            Message = ResponseKeys.invalidInviteCode,
                        };
                    }
                }

                return new ResponseWrapper
                {
                    Error = false,
                    Message = ResponseKeys.msgSuccess,
                };
            }
        }
    }
}