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
                        if (DateTime.Compare(DateTime.Parse((Convert.ToDateTime(item.ExpiryDate).ToString())), DateTime.Parse(getUtcDateTime().ToString())) > 0)
                        {
                            lstPromoCodeDetails.Add(new PromoCodeDetails
                            {
                                ID = item.ID,
                                PromoCode = item.PromoCode,
                                NoOfUsage = item.NoOfUsage,
                                PromoID = item.PromoID,
                                StartDate = item.StartDate,
                                ExpiryDate = item.ExpiryDate,
                                Amount = item.Amount,
                                PaymentType = item.PaymentType,
                                Repetition = item.Repetition
                            });
                        }
                    }
                }
                return lstPromoCodeDetails;
            }
        }

        public static async Task<AddUserPromoResponse> AddUserPromoCode(AddPromoCode model)
        {
            using (CangooEntities dbcontext = new CangooEntities())
            {
                var promo = dbcontext.PromoManagers.Where(x => x.PromoCode == model.PromoCode && x.IsActive == true).FirstOrDefault();
                if (promo != null)
                {
                    if (DateTime.Compare(DateTime.Parse((Convert.ToDateTime(promo.ExpiryDate).ToString())), DateTime.Parse(getUtcDateTime().ToString())) > 0)
                    {
                        var AppID = Guid.Parse(ConfigurationManager.AppSettings["ApplicationID"].ToString());
                        var userpromos = dbcontext.UserPromos.Where(x => x.UserID.ToLower().Equals(model.PassengerId.ToLower())).ToList();
                        var alreadyApplied = userpromos.Where(up => up.PromoID == promo.PromoID).FirstOrDefault();
                        if (alreadyApplied != null)
                        {
                            if (alreadyApplied.NoOfUsage < promo.Repetition)
                            {
                                return new AddUserPromoResponse
                                {
                                    Amount = string.Format("{0:0.00}", promo.Amount),
                                    PromoType = (bool)promo.isFixed ? "Fixed" : "Percentage",
                                    PromoCode = model.PromoCode,
                                    ExpiryDate = ((DateTime)promo.ExpiryDate).ToString(),
                                    AllowedRepition = promo.Repetition.ToString(),
                                    NoOfUsage = alreadyApplied.NoOfUsage.ToString(),
                                };
                            }
                        }
                        else
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
                            
                            return new AddUserPromoResponse
                            {
                                Amount = string.Format("{0:0.00}", promo.Amount),
                                PromoType = (bool)promo.isFixed ? "Fixed" : "Percentage",
                                PromoCode = model.PromoCode,
                                ExpiryDate = ((DateTime)promo.ExpiryDate).ToString(),
                                AllowedRepition = promo.Repetition.ToString(),
                                NoOfUsage = promodata.NoOfUsage.ToString()
                            };
                        }
                    }
                }

                return null;
            }
        }
        

        public static DateTime getUtcDateTime()
        {
            return DateTime.UtcNow;
        }
    }
}
