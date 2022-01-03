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
    public class LanguageService
    {
        public static async Task<List<LanguagesDetail>> GetAllLanguages()
        {
            using (CangooEntities dbContext = new CangooEntities())
            {
                var ApplicationID = Guid.Parse(ConfigurationManager.AppSettings["ApplicationID"].ToString());
                var ResellerID = Guid.Parse(ConfigurationManager.AppSettings["ResellerID"].ToString());

                var query = dbContext.Database.SqlQuery<LanguagesDetail>("SELECT CAST(Id as VARCHAR(36)) Id,Language,ShortName " +
                                                                         "FROM Languages WHERE ApplicationId=@applicationId AND ResellerId = @resellerId",
                                                                                                                    new SqlParameter("@applicationId", ApplicationID),
                                                                                                                    new SqlParameter("@resellerId", ResellerID));
                return await query.ToListAsync();
            }
        }

        public static async Task<LanguagesDetail> GetLanguageById(string id)
        {
            using (CangooEntities dbContext = new CangooEntities())
            {
                var query = dbContext.Database.SqlQuery<LanguagesDetail>("SELECT CAST(Id as VARCHAR(36)) Id, Language, ShortName " +
                                                                         "FROM Languages WHERE Id = @id",
                                                                         new SqlParameter("@id", id));
                return await query.FirstOrDefaultAsync();
            }
        }


        public static async Task<ResponseWrapper> UpdatePassengerLanguage(UpdateLanguageRequest model)
        {
            using (CangooEntities dbContext = new CangooEntities())
            {
                var result = await dbContext.Database.ExecuteSqlCommandAsync("UPDATE UserProfile SET LanguageID = @Id WHERE UserID = @passengerId",
                                                                                      new SqlParameter("@Id", model.LanguageId),
                                                                                      new SqlParameter("@passengerId", model.PassengerId));
                if (result == 0)
                {
                    return new ResponseWrapper
                    {
                        Message = ResponseKeys.failedToUpdate
                    };
                }
                else
                {
                    return new ResponseWrapper
                    {
                        Error = false,
                        Message = ResponseKeys.msgSuccess,
                        Data = await GetLanguageById(model.LanguageId)
                    };
                }
            }
        }
    }
}
