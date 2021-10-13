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
                var query = dbContext.Database.SqlQuery<PromoCodeDetails>("", new SqlParameter("@passengerId", passengerId));
                return await query.ToListAsync();
            }
        }
    }
}
