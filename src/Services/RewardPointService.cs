using DatabaseModel;
using DTOs.API;
using DTOs.Shared;
using Services.Automapper;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace Services
{
    public class RewardPointService
    {
        public static async Task<List<RewardDetail>> GetRewardPointsList()
        {
            using (CangooEntities dbContext = new CangooEntities())
            {
                var query = dbContext.Database.SqlQuery<RewardDetail>(@"
SELECT CAST(RewardID as VARCHAR(36)) RewardId, CAST(Deduction as VARCHAR(36)) Deduction, Description, IsActive,
CAST(RedeemAmount as VARCHAR(36)) RedeemAmount, 
CONVERT(VARCHAR, StartDate, 120) StartDate,
CONVERT(VARCHAR, ExpiryDate, 120) ExpiryDate 
FROM RewardPointsManager WHERE IsActive = 1");

                return await query.ToListAsync();
            }
        }

        public static async Task<PassengerEarnedRewardRespose> GetPassengerRewardPoint(string passengerId)
        {
            using (CangooEntities dbcontext = new CangooEntities())
            {
                var result = await dbcontext.UserProfiles.Where(x => x.UserID == passengerId).FirstOrDefaultAsync();
                return new PassengerEarnedRewardRespose
                {
                    RewardPoint = result.RewardPoints.ToString()
                };
            }
        }

        public static async Task<PassengerReedemRewardResponse> ReedemRewardPoints(ReedemPassengerCangoosRequsest model)
        {
            using (CangooEntities dbContext = new CangooEntities())
            {
                var applicationId = ConfigurationManager.AppSettings["ApplicationID"].ToString();
                var resellerId = ConfigurationManager.AppSettings["ResellerID"].ToString();

                var userProfile = await UserService.GetProfileByIdAsync(model.PassengerId, applicationId, resellerId);
                if (userProfile != null)
                {
                    dbContext.WalletTransfers.Add(new WalletTransfer
                    {
                        Amount = int.Parse(model.RedeemAmount),
                        RechargeDate = DateTime.UtcNow,
                        WalletTransferID = Guid.NewGuid(),
                        Referrence = model.Deduction + " Reward points redeemed",
                        TransferredBy = Guid.Parse(applicationId),
                        TransferredTo = Guid.Parse(model.PassengerId),
                        ApplicationID = Guid.Parse(applicationId),
                        ResellerID = Guid.Parse(resellerId)
                    });

                    userProfile.RewardPoints -= int.Parse(model.Deduction);
                    userProfile.WalletBalance += decimal.Parse(model.RedeemAmount);
                    userProfile.AvailableWalletBalance += decimal.Parse(model.RedeemAmount);
                    userProfile.LastRechargedAt = DateTime.UtcNow;

                    dbContext.Entry(AutoMapperConfig._mapper.Map<PassengerProfileDTO, UserProfile>(userProfile)).State = EntityState.Modified;

                    await dbContext.SaveChangesAsync();
                }
                return new PassengerReedemRewardResponse
                {
                    RewardPoint = userProfile.RewardPoints.ToString(),
                    WalletBalance = userProfile.WalletBalance.ToString(),
                    AvailableWalletBalance = userProfile.AvailableWalletBalance.ToString()
                };
            }
        }
    }
}
