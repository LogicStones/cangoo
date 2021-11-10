using DatabaseModel;
using DTOs.API;
using System;
using System.Collections.Generic;
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
                                                                                                        new SqlParameter("@active",true));
                return await query.ToListAsync();
            }
        }

        public static async Task AddUserPromoCode()
        {
            using(CangooEntities dbcontext=new CangooEntities())
            {
                //if (!string.IsNullOrEmpty(model.passengerID) && !string.IsNullOrEmpty(model.promoCode))
                //{
                //    using (CanTaxiResellerEntities context = new CanTaxiResellerEntities())
                //    {
                //        if (model.addPromo.ToString().ToLower().Equals("true"))
                //        {
                //            var promo = context.PromoManagers.Where(p => p.PromoCode.ToLower().Equals(model.promoCode.ToLower())
                //            && p.ApplicationID.ToString().ToLower().Equals(this.ApplicationID.ToLower())
                //            && p.ResellerID.ToString().ToLower().Equals(this.ResellerID.ToLower())
                //            ).FirstOrDefault();

                //            if (promo != null)
                //            {
                //                if (DateTime.Compare(DateTime.Parse(((DateTime)promo.ExpiryDate).ToString(Common.dateFormat)), DateTime.Parse(Common.getUtcDateTime().ToString(Common.dateFormat))) <= 0)
                //                {
                //                    response.error = false;
                //                    response.message = AppMessage.promoExpired;
                //                    return Request.CreateResponse(HttpStatusCode.OK, response);
                //                }
                //                else
                //                {

                //                    dic = new Dictionary<dynamic, dynamic>()
                //                    {
                //                        { "promoCodeAmount",string.Format("{0:0.00}", promo.Amount)},
                //                        { "promoCodeType",(bool)promo.isFixed ? "Fixed" : "Percentage"},
                //                        { "promoCode",model.promoCode},
                //                        { "expiryDateTime", ((DateTime)promo.ExpiryDate).ToString(Common.dateFormat)},
                //                    };

                //                    var userPromos = context.UserPromos.Where(up => up.UserID.ToLower().Equals(model.passengerID.ToLower())).ToList();

                //                    //Ideally this case should never happen, whenever a promo code is removed it is marked as isActive false.
                //                    var activePromo = userPromos.Where(up => up.isActive == true).FirstOrDefault();
                //                    if (activePromo != null)
                //                        activePromo.isActive = false;

                //                    //In case promo was applied then removed, and now applying again before expiry
                //                    var alreadyApplied = userPromos.Where(up => up.PromoID == promo.PromoID).FirstOrDefault();

                //                    if (alreadyApplied != null)
                //                    {
                //                        if (alreadyApplied.NoOfUsage < promo.Repetition)
                //                        {
                //                            dic.Add("promoCodeAllowedRepititions", promo.Repetition);
                //                            dic.Add("promoCodeNoOfUsage", alreadyApplied.NoOfUsage);
                //                            alreadyApplied.isActive = true;
                //                        }
                //                        else
                //                        {
                //                            response.error = true;
                //                            response.message = AppMessage.promoLimitExceeded;
                //                            return Request.CreateResponse(HttpStatusCode.OK, response);
                //                        }
                //                    }
                //                    else
                //                    {
                //                        var newPromo = new UserPromo
                //                        {
                //                            ID = Guid.NewGuid(),
                //                            isActive = true,
                //                            UserID = model.passengerID,
                //                            PromoID = promo.PromoID,
                //                            ApplicationID = Guid.Parse(this.ApplicationID),
                //                            NoOfUsage = 0
                //                        };

                //                        context.UserPromos.Add(newPromo);

                //                        dic.Add("promoCodeAllowedRepititions", promo.Repetition);
                //                        dic.Add("promoCodeNoOfUsage", 0);
                //                    }

                //                    context.SaveChanges();

                //                    response.data = dic;
                //                    response.error = false;
                //                    response.message = AppMessage.promoCodeApplied;
                //                    return Request.CreateResponse(HttpStatusCode.OK, response);
                //                }
                //            }
                //            else
                //            {
                //                response.error = true;
                //                response.message = AppMessage.invalidPromo;
                //                return Request.CreateResponse(HttpStatusCode.OK, response);
                //            }
                //        }
                //        else
                //        {
                //            context.UserPromos.Where(up => up.UserID.ToLower().Equals(model.passengerID.ToLower())
                //            && up.ApplicationID.ToString().ToLower().Equals(this.ApplicationID.ToLower())
                //            && up.isActive == true).FirstOrDefault().isActive = false;

                //            context.SaveChanges();

                //            response.error = false;
                //            response.message = AppMessage.promoCodeRemoved;
                //            return Request.CreateResponse(HttpStatusCode.OK, response);
                //        }
                //    }
                //}
                //else
                //{
                //    response.error = true;
                //    response.message = AppMessage.invalidParameters;
                //    return Request.CreateResponse(HttpStatusCode.OK, response);
                //}
            }
        }
    }
}
