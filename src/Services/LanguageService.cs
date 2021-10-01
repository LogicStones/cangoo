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
                var query = dbContext.Database.SqlQuery<LanguagesDetail>("SELECT [Id],[Language],[ShortName] FROM [dbo].[Languages] WHERE ApplicationId=@applicationId AND ResellerId = @resellerId",
                                                                                                                    new SqlParameter("@applicationId", ApplicationID),
                                                                                                                    new SqlParameter("@resellerId", ResellerID));
                return await query.ToListAsync();
            }
        }

        public static async Task<int> UpdatePassengerLanguage(UpdateLanguageRequest model)
        {
            using (CangooEntities dbContext = new CangooEntities())
            {
                return await dbContext.Database.ExecuteSqlCommandAsync("UPDATE [dbo].[UserProfile] SET [LanguageID] = @Id WHERE [UserID] = @passengerId",
                                                                                      new SqlParameter("@Id", model.Id),
                                                                                      new SqlParameter("@passengerId", model.PassengerId));
            }
        }
    }
}
