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
    public class NotificationServices
    {
        public static async Task<List<NotificationDetails>> GetNotifications(string ReceiverId)
        {
            using (CangooEntities dbcontext=new CangooEntities())
            {
                var query = dbcontext.Database.SqlQuery<NotificationDetails>("SELECT CAST(PopupID as varchar(36))PopupID,CAST(ReceiverID as varchar(36))ReceiverID," +
                                                                        "Title,RidirectURL,Text,LinkButtonText,CAST(StartDate as varchar(36))StartDate,Image," +
                                                                        "CAST(ExpiryDate as varchar(36))ExpiryDate,ButtonText from Notifications " +
                                                                        "WHERE ReceiverID = @receiverId and IsActive= @active",
                                                                        new SqlParameter("@receiverId",ReceiverId),
                                                                        new SqlParameter("@active",true));
                return await query.ToListAsync();
            }
        }
    }
}
