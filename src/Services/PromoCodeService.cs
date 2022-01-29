using Constants;
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
    public class PromoCodeService
    {
        public static async Task<List<PromoCodeDetail>> GetPromoCodes(string passengerId)
        {
            using (CangooEntities dbContext = new CangooEntities())
            {
                var query = dbContext.Database.SqlQuery<PromoCodeDetail>("exec spGetUserPromos @passengerId,@active",
                                                                                                        new SqlParameter("@passengerId", passengerId),
                                                                                                        new SqlParameter("@active", true));

                return await query.ToListAsync();
            }
        }

        public static async Task<ResponseWrapper> AddUserPromoCode(AddPromoCodeRequest model)
        {
            using (CangooEntities dbcontext = new CangooEntities())
            {
                var promo = dbcontext.PromoManagers.Where(x => x.PromoCode.Equals(model.PromoCode) && x.IsActive == true).FirstOrDefault();
                if (promo == null)
                {
                    return new ResponseWrapper
                    {
                        Message = ResponseKeys.invalidPromo,
                    };
                }

                if (DateTime.Compare(DateTime.Parse(Convert.ToDateTime(promo.ExpiryDate).ToString()), DateTime.Parse(DateTime.UtcNow.ToString())) < 0)
                {
                    return new ResponseWrapper
                    {
                        Message = ResponseKeys.promoExpired,
                    };
                }

                var AppID = Guid.Parse(ConfigurationManager.AppSettings["ApplicationID"].ToString());
                var userpromos = dbcontext.UserPromos.Where(x => x.UserID.ToLower().Equals(model.PassengerId.ToLower())).ToList();
                
                if (userpromos.Where(up => up.PromoID == promo.PromoID).Any())
                {
                    return new ResponseWrapper
                    {
                        Message = ResponseKeys.promoAlreadyApplied,
                    };
                }

                var promodata = new UserPromo
                {
                    ID = Guid.NewGuid(),
                    isActive = true,
                    UserID = model.PassengerId,
                    PromoID = promo.PromoID,
                    ApplicationID = AppID,
                    NoOfUsage = 0
                };

                dbcontext.UserPromos.Add(promodata);
                await dbcontext.SaveChangesAsync();

                return new ResponseWrapper
                {
                    Error = false,
                    Message = ResponseKeys.msgSuccess,
                    Data = new AddUserPromoResponse
                    {
                        Amount = string.Format("{0:0.00}", promo.Amount),
                        PromoType = (bool)promo.isFixed ? "Fixed" : "Percentage",
                        PromoCode = model.PromoCode,
                        ExpiryDate = ((DateTime)promo.ExpiryDate).ToString(),
                        AllowedRepition = promo.Repetition.ToString(),
                        NoOfUsage = promodata.NoOfUsage.ToString()
                    }
                };
            }
        }

        public static async Task ApplyTripPromoCode(string userPromoCodeId)
        {
            using (CangooEntities dbcontext = new CangooEntities())
            {
                await dbcontext.Database.ExecuteSqlCommandAsync("UPDATE UserPromos SET NoOfUsage = NoOfUsage + 1 Where ID = @userPromoCodeId;",
                                                                                      new SqlParameter("@userPromoCodeId", userPromoCodeId));
            }
        }

        public static async Task<ResponseWrapper> UpdateTripPromo(string currentUserPromoCodeId, string newUserPromoCodeId, string promoCodeId, string tripId, string passengerId)
        {
            using (CangooEntities dbcontext = new CangooEntities())
            {
                var result = await dbcontext.Database.ExecuteSqlCommandAsync("UPDATE Trips SET PromoCodeID = @promoCodeId WHERE TripID = @tripId AND UserID = @passengerId;" +
                    "UPDATE UserPromos SET NoOfUsage = NoOfUsage + 1 Where ID = @newUserPromoCodeId;" +
                    "UPDATE UserPromos SET NoOfUsage = NoOfUsage - 1 Where ID = @currentUserPromoCodeId;",
                                                                                      new SqlParameter("@promoCodeId", promoCodeId),
                                                                                      new SqlParameter("@currentUserPromoCodeId", currentUserPromoCodeId),
                                                                                      new SqlParameter("@newUserPromoCodeId", newUserPromoCodeId),
                                                                                      new SqlParameter("@tripId", tripId),
                                                                                      new SqlParameter("@passengerId", passengerId));

                if (result == 0)
                    return new ResponseWrapper
                    {
                        Message = ResponseKeys.failedToUpdate
                    };
                else
                    return new ResponseWrapper
                    {
                        Error = false,
                        Message = ResponseKeys.msgSuccess
                    };
            }
        }

        //public static DiscountTypeDTO GetUserPromoDiscountAmount(string applicationID, string userID, string promoCodeId)
        //{
        //    using (var dbContext = new CangooEntities())
        //    {
        //        var disccountDetails = new DiscountTypeDTO
        //        {
        //            PromoCodeId = promoCodeId
        //        };

        //        DateTime dt = DateTime.UtcNow;

        //        var availablePromoCodes = dbContext.PromoManagers
        //            .Where(p => p.ApplicationID.ToString() == applicationID && p.IsActive == true && p.isSpecialPromo == false && p.StartDate <= dt && p.ExpiryDate >= dt)
        //            .OrderBy(p => p.StartDate).ToList();


        //        if (availablePromoCodes.Any())
        //        {
        //            var appliedPromoCode = dbContext.UserPromos.Where(up => up.UserID == userID && up.PromoID.ToString().Equals(promoCodeId) && up.isActive == true).FirstOrDefault();
        //            if (appliedPromoCode != null)
        //            {
        //                if (availablePromoCodes.Exists(p => p.PromoID == appliedPromoCode.PromoID))
        //                {
        //                    var promo = availablePromoCodes.Find(p => p.PromoID == appliedPromoCode.PromoID);

        //                    if (promo.isFixed == true)
        //                    {
        //                        disccountDetails.DiscountType = Enum.GetName(typeof(DiscountTypes), (int)DiscountTypes.Fixed).ToLower();
        //                        disccountDetails.DiscountAmount = string.Format("{0:0.00}", (decimal)promo.Amount);
        //                    }
        //                    else
        //                    {
        //                        disccountDetails.DiscountType = Enum.GetName(typeof(DiscountTypes), (int)DiscountTypes.Percentage).ToLower();
        //                        disccountDetails.DiscountAmount = string.Format("{0:0.00}", (decimal)promo.Amount);
        //                    }
        //                }
        //            }
        //        }

        //        return disccountDetails;
        //    }
        //}
    }
}
