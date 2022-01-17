using Constants;
using DatabaseModel;
using DTOs.API;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services
{
    public class PopupService
    {
        public static async Task<PopUpDetailsDTO> GetValidPopupDetails(int userTypeId)
        {
            using (CangooEntities dbcontext = new CangooEntities())
            {
                var applicationId = ConfigurationManager.AppSettings["ApplicationID"].ToString();

                var query = dbcontext.Database.SqlQuery<PopUpDetailsDTO>(
                    @"select CAST(PopupID as varchar(36)) PopupID, Title, Text Description, LinkButtonText, RidirectURL, Image ImagePath, Buttontext,
 dbo.fnGetFormattedDateTime(StartDate) StartDate, dbo.fnGetFormattedDateTime(ExpiryDate) ExpiryDate
from popups
where isActive = 1 and receiverid = @userTypeId AND ExpiryDate >= @expiryDate
order by CreatedAt desc",
                                                                        new SqlParameter("@userTypeId", userTypeId),
                                                                        new SqlParameter("@expiryDate", DateTime.UtcNow));
             
                return await query.FirstOrDefaultAsync();
            }
        }
    }
}