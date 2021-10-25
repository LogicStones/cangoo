using DatabaseModel;
using DTOs.API;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services
{
    public class RewardPointService
    {
        public static async Task<List<RewardDetails>> GetRewards()
        {
            using (CangooEntities dbContext = new CangooEntities())
            {
                var query = dbContext.Database.SqlQuery<RewardDetails>("SELECT RewardID,Deduction,RedeemAmount,Description,StartDate,ExpiryDate,IsActive FROM RewardPointsManager WHERE IsActive= @active",
                                                                                                                                        new SqlParameter("@active", true));
                return await query.ToListAsync();
            }
        }

        public static async Task<int> ReedemPassengerPoints(PassengerReedemRewardRequsest model)
        {
            using (CangooEntities dbContext = new CangooEntities())
            {
                var user = GetUser(model.PassengerId);
                if (user != null)
                {
                    var Points = GetRewardPoints(model.RewardId);
                    var ApplicationId = Guid.Parse(ConfigurationManager.AppSettings["ApplicationID"].ToString());
                    var ResellerId = Guid.Parse(ConfigurationManager.AppSettings["ResellerID"].ToString());
                    dbContext.WalletTransfers.Add(new WalletTransfer
                    {
                        Amount = (decimal)Points.RedeemAmount,
                        RechargeDate = DateTime.UtcNow,
                        WalletTransferID = Guid.NewGuid(),
                        Referrence = Points.Deduction + " Reward points redeemed",
                        TransferredBy = ApplicationId,
                        TransferredTo = Guid.Parse(model.PassengerId),
                        ApplicationID = ApplicationId,
                        ResellerID = ResellerId
                    });
                    user.RewardPoints -= Points.Deduction;
                    user.WalletBalance += Points.RedeemAmount;
                    user.LastRechargedAt = DateTime.UtcNow;
                }
                return await dbContext.SaveChangesAsync();
            }
        }

        //public static async Task<int> GetPassengerPoints(string passengerId)
        //{
        //    using (CangooEntities dbContext = new CangooEntities())
        //    {
        //        var user = GetUser(passengerId);
        //    }
        //}

        public static UserProfile GetUser(string passengerId)
        {
            using (CangooEntities dbContext=new CangooEntities())
            {
                return dbContext.UserProfiles.Where(u => u.UserID == passengerId).FirstOrDefault();
            }
        }

        public static RewardPointsManager GetRewardPoints(int rewardId)
        {
            using (CangooEntities dbContext = new CangooEntities())
            {
                return dbContext.RewardPointsManagers.Where(u => u.RewardID == rewardId).FirstOrDefault();
            }
        }
    }
}
