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
        public static async Task<List<PromoCodeDetails>> GetPromoCodes(string passengerId)
        {
            using (CangooEntities dbContext = new CangooEntities())
            {
                var query = dbContext.Database.SqlQuery<PromoCodeDetails>("exec spGetUserPromos @passengerId,@active",
                                                                                                        new SqlParameter("@passengerId", passengerId),
                                                                                                        new SqlParameter("@active", true));
                var result = await query.ToListAsync();
                var lstPromoCodeDetails = new List<PromoCodeDetails>();
                if (result != null)
                {
                    foreach (var item in result)
                    {
                        if (DateTime.Compare(DateTime.Parse((Convert.ToDateTime(item.ExpiryDate).ToString())), DateTime.Parse(DateTime.UtcNow.ToString())) > 0)
                        {
                            lstPromoCodeDetails.Add(new PromoCodeDetails
                            {
                                UserPromoCodeId = item.UserPromoCodeId,
                                PromoCode = item.PromoCode,
                                NoOfUsage = item.NoOfUsage,
                                PromoCodeId = item.PromoCodeId,
                                StartDate = item.StartDate,
                                ExpiryDate = item.ExpiryDate,
                                Amount = item.Amount,
                                PaymentType = item.PaymentType,
                                AllowedRepetitions = item.AllowedRepetitions
                            });
                        }
                    }
                }
                return lstPromoCodeDetails;
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
                var alreadyApplied = userpromos.Where(up => up.PromoID == promo.PromoID).FirstOrDefault();

                if (alreadyApplied == null)
                {
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

                if (alreadyApplied.NoOfUsage >= promo.Repetition)
                {
                    return new ResponseWrapper
                    {
                        Message = ResponseKeys.promoLimitExceeded
                    };
                }
                else
                {
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
                            NoOfUsage = alreadyApplied.NoOfUsage.ToString(),
                        }
                    };
                }
            }
        }

        public static async Task<int> UpdateTripPromo(string promoCodeId, string tripId, string passengerId)
        {
            using (CangooEntities dbcontext = new CangooEntities())
            {
                return await dbcontext.Database.ExecuteSqlCommandAsync("UPDATE Trips SET PromoCodeID = @promocodeid WHERE TripID = @tripId AND UserID = @passengerId",
                                                                                      new SqlParameter("@promocodeid", promoCodeId),
                                                                                      new SqlParameter("@tripId", tripId),
                                                                                      new SqlParameter("@passengerId", passengerId));
            }
        }

        public static DiscountTypeDTO GetUserPromoDiscountAmount(string applicationID, string userID, string promoCodeId)
        {
            using (var dbContext = new CangooEntities())
            {
                var disccountDetails = new DiscountTypeDTO
                {
                    PromoCodeId = promoCodeId
                };

                DateTime dt = DateTime.UtcNow;

                var availablePromoCodes = dbContext.PromoManagers
                    .Where(p => p.ApplicationID.ToString() == applicationID && p.IsActive == true && p.isSpecialPromo == false && p.StartDate <= dt && p.ExpiryDate >= dt)
                    .OrderBy(p => p.StartDate).ToList();


                if (availablePromoCodes.Any())
                {
                    var appliedPromoCode = dbContext.UserPromos.Where(up => up.UserID == userID && up.PromoID.ToString().Equals(promoCodeId) && up.isActive == true).FirstOrDefault();
                    if (appliedPromoCode != null)
                    {
                        if (availablePromoCodes.Exists(p => p.PromoID == appliedPromoCode.PromoID))
                        {
                            var promo = availablePromoCodes.Find(p => p.PromoID == appliedPromoCode.PromoID);

                            if (promo.isFixed == true)
                            {
                                disccountDetails.DiscountType = Enum.GetName(typeof(DiscountTypes), (int)DiscountTypes.Fixed).ToLower();
                                disccountDetails.DiscountAmount = string.Format("{0:0.00}", (decimal)promo.Amount);
                            }
                            else
                            {
                                disccountDetails.DiscountType = Enum.GetName(typeof(DiscountTypes), (int)DiscountTypes.Percentage).ToLower();
                                disccountDetails.DiscountAmount = string.Format("{0:0.00}", (decimal)promo.Amount);
                            }
                        }
                    }
                }

                return disccountDetails;
            }
        }
    }
}
