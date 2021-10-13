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
    public class TrustedContactManagerService
    {
        public static async Task<int> UpdateTrustedContact(UpdateTrustedContact model)
        {
            using (CangooEntities dbContext = new CangooEntities())
            {
                return await dbContext.Database.ExecuteSqlCommandAsync("",
                                                                                      new SqlParameter("@", model.Name),
                                                                                      new SqlParameter("@", model.CountryCode),
                                                                                      new SqlParameter("@", model.PhoneNumber),
                                                                                      new SqlParameter("@", model.Email));
            }
        }

        public static async Task<List<TrustedContactDetails>> GetTrustedContact(string passengerId)
        {
            using (CangooEntities dbContext = new CangooEntities())
            {
                var query = dbContext.Database.SqlQuery<TrustedContactDetails>("", new SqlParameter("@", passengerId));
                return await query.ToListAsync();
            }
        }
    }
}
