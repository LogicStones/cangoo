using DatabaseModel;
using DTOs.API;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Services.Automapper;
using System.Configuration;

namespace Services
{
    public class TrustedContactManagerService
    {
        public static async Task<int> UpdateTrustedContact(UpdateTrustedContactRequest model)
        {
            using (CangooEntities dbContext = new CangooEntities())
            {
                if (IsContactExist(model.PassengerId) == true)
                {
                    return  (await dbContext.Database.ExecuteSqlCommandAsync("UPDATE TrustedContacts SET FirstName = @firstName ,CountryCode = @countryCode,MobileNo = @mobileNo,Email = @email WHERE PassengerId = @passengerId",
                                                                                      new SqlParameter("@firstName", model.FirstName),
                                                                                      new SqlParameter("@countryCode", model.CountryCode),
                                                                                      new SqlParameter("@mobileNo", model.MobileNo),
                                                                                      new SqlParameter("@email", model.Email),
                                                                                      new SqlParameter("@passengerId", model.PassengerId)));
                }
                else
                {
                    var result = AutoMapperConfig._mapper.Map<UpdateTrustedContactRequest, TrustedContact>(model);
                    result.ApplicationId = Guid.Parse(ConfigurationManager.AppSettings["ApplicationID"].ToString());
                    result.ResellerId = Guid.Parse(ConfigurationManager.AppSettings["ResellerID"].ToString());

                    dbContext.TrustedContacts.Add(result);
                    return (await dbContext.SaveChangesAsync());
                }
            }
        }

        public static async Task<List<GetTrustedContact>> GetTrustedContact(string passengerId)
        {
            using (CangooEntities dbContext = new CangooEntities())
            {
                var query = dbContext.Database.SqlQuery<GetTrustedContact>("SELECT CAST(Id as VARCHAR(36))Id,FirstName,CountryCode,MobileNo,Email FROM TrustedContacts WHERE PassengerId = @passengerId", 
                                                                                                                                                        new SqlParameter("@passengerId", passengerId));
                return await query.ToListAsync();
            }
        }

        public static bool IsContactExist(string passengerId)
        {
            using (CangooEntities dbContext = new CangooEntities())
            {
                Guid userId = Guid.Parse(passengerId);
                return dbContext.TrustedContacts.Where(x => x.PassengerId == userId).Any();
            }
        }
    }
}
