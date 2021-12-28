using DatabaseModel;
using DTOs.API;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace Services
{
    public class RewardPointService
    {
        public static async Task<List<RewardDetails>> GetRewards()
        {
            using (CangooEntities dbContext = new CangooEntities())
            {
                var query = dbContext.Database.SqlQuery<RewardDetails>("SELECT CAST(RewardID as VARCHAR(36)) RewardId,CAST(Deduction as VARCHAR(36))Deduction," +
                                                                        "CAST(RedeemAmount as VARCHAR(36))RedeemAmount,Description,CAST(StartDate as VARCHAR(36))StartDate," +
                                                                        "CAST(ExpiryDate as VARCHAR(36))ExpiryDate,IsActive FROM RewardPointsManager WHERE IsActive= @active",
                                                                                                  new SqlParameter("@active", 1));
                return await query.ToListAsync();
            }
        }

        public static async Task<PassengerEarnedRewardRespose> GetPassengerRewardPoint(string passengerId)
        {
            using (CangooEntities dbcontext = new CangooEntities())
            {
                var result = dbcontext.UserProfiles.Where(x => x.UserID == passengerId).FirstOrDefault();
                return new PassengerEarnedRewardRespose
                {
                    RewardPoint = result.RewardPoints.ToString()
                };
            }
        }

        public static async Task<PassengerReedemRewardResponse> ReedemPassengerPoints(ReedemPassengerCangoosRequsest model)
        {
            using (CangooEntities dbContext = new CangooEntities())
            {
                var user = GetUser(model.PassengerId);
                if (user != null)
                {
                    var ApplicationId = Guid.Parse(ConfigurationManager.AppSettings["ApplicationID"].ToString());
                    var ResellerId = Guid.Parse(ConfigurationManager.AppSettings["ResellerID"].ToString());
                    dbContext.WalletTransfers.Add(new WalletTransfer
                    {
                        Amount = int.Parse(model.RedeemAmount),
                        RechargeDate = DateTime.UtcNow,
                        WalletTransferID = Guid.NewGuid(),
                        Referrence = model.Deduction + " Reward points redeemed",
                        TransferredBy = ApplicationId,
                        TransferredTo = Guid.Parse(model.PassengerId),
                        ApplicationID = ApplicationId,
                        ResellerID = ResellerId
                    });
                    user.RewardPoints -= int.Parse(model.Deduction);
                    user.WalletBalance += decimal.Parse(model.RedeemAmount);
                    user.AvailableWalletBalance += decimal.Parse(model.RedeemAmount);
                    user.LastRechargedAt = DateTime.UtcNow;
                    await dbContext.SaveChangesAsync();
                }
                return new PassengerReedemRewardResponse
                {
                    RewardPoint = user.RewardPoints.ToString(),
                    WalletBalance = user.WalletBalance.ToString(),
                    AvailableWalletBalance = user.AvailableWalletBalance.ToString()
                };
            }
        }

        public static UserProfile GetUser(string passengerId)
        {
            using (CangooEntities dbContext = new CangooEntities())
            {
                return dbContext.UserProfiles.Where(u => u.UserID == passengerId).FirstOrDefault();
            }
        }

    }
}
