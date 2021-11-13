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
        public static async Task<int> ApplyInvitation(ApplyInviteCode model)
        {
            using (CangooEntities dbcontext = new CangooEntities())
            {
                var AppID = Guid.Parse(ConfigurationManager.AppSettings["ApplicationID"].ToString());
                var ResellerId= Guid.Parse(ConfigurationManager.AppSettings["ResellerID"].ToString());
                var result = await DriverService.GetDriverByInviteCode(model.InviteCode);
                if (result != null)
                {
                    UserInvite userInvite = new UserInvite
                    {
                        UserInvitesID = Guid.NewGuid(),
                        UserID = Guid.Parse(model.PassengerId),
                        CaptainID = result.CaptainID,
                        DateTime = DateTime.UtcNow,
                        ApplicationID = AppID,
                    };

                    result.EarningPoints = result.EarningPoints == null ? 50 : result.EarningPoints + 50 <= 300 ? result.EarningPoints + 50 : 300;

                    WalletTransfer wallet = new WalletTransfer
                    {
                        Amount = 10,
                        RechargeDate = DateTime.UtcNow,
                        WalletTransferID = Guid.NewGuid(),
                        Referrence = "Reward: Captain invite code applied.",
                        TransferredBy = AppID,
                        TransferredTo = Guid.Parse(model.PassengerId),
                        ApplicationID = AppID,
                        ResellerID = ResellerId,
                    };

                    var userProfile = dbcontext.UserProfiles.Where(x => x.UserID.ToString().Equals(model.PassengerId)).FirstOrDefault();
                    if (userProfile != null)
                    {
                        userProfile.LastRechargedAt = DateTime.UtcNow;
                        userProfile.WalletBalance += 10;
                    }

                    dbcontext.UserInvites.Add(userInvite);
                    dbcontext.WalletTransfers.Add(wallet);

                    await dbcontext.SaveChangesAsync();
                    return (int)userProfile.WalletBalance;
                }
                else
                {
                    return 0;
                }
            }
        }

        public static bool IsUserInviteCodeApplicable(string passengerId)
        {
            using (CangooEntities dbcontext=new CangooEntities())
            {
                return dbcontext.Trips.Where(x => x.UserID.ToString().Equals(passengerId)).Any();
            }
        }

        public static bool IsUserInviteCodeAlreadyApplied(string passengerId)
        {
            using (CangooEntities dbcontext=new CangooEntities())
            {
                return dbcontext.UserInvites.Where(x => x.UserID.ToString().Equals(passengerId)).Any();
            }
        }
    }
}
