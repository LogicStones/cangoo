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
                var result = await query.ToListAsync();
                var lstNotificationDetails = new List<NotificationDetails>();
                if (result != null)
                {
                    foreach (var item in result)
                    {
                        if (DateTime.Compare(DateTime.Parse((Convert.ToDateTime(item.ExpiryDate).ToString())), DateTime.Parse(getUtcDateTime().ToString())) > 0)
                        {
                            lstNotificationDetails.Add(new NotificationDetails
                            {
                                PopupID = item.PopupID,
                                ReceiverID = item.ReceiverID,
                                Title = item.Title,
                                RidirectURL = item.RidirectURL,
                                StartDate = item.StartDate,
                                ExpiryDate = item.ExpiryDate,
                                Text = item.Text,
                                LinkButtonText = item.LinkButtonText,
                                Image = item.Image,
                                ButtonText = item.ButtonText
                            });
                        }
                    }
                }
                return lstNotificationDetails;
            }
        }

        public static DateTime getUtcDateTime()
        {
            return DateTime.UtcNow;
        }
    }
}
