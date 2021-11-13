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
                var query = dbcontext.Database.SqlQuery<NotificationDetails>("SELECT CAST(FeedID as varchar(36))FeedID,Title,ShortDescription,CAST(CreationDate as varchar(36))CreationDate," +
                                                                                "CAST(ExpiryDate as varchar(36))ExpiryDate FROM Notifications WHERE ApplicationUserTypeID = @usertype",
                                                                        new SqlParameter("@usertype",int.Parse(ReceiverId)));
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
                                FeedId = item.FeedId,
                                Title = item.Title,
                                ShortDescription = item.ShortDescription,
                                CreationDate = item.CreationDate,
                                ExpiryDate = item.ExpiryDate,
                            });
                        }
                    }
                }
                return lstNotificationDetails;
            }
        }


        public static async Task<List<GetReadNotificationResponse>> GetFullReadNotification(string FeedId)
        {
            using(CangooEntities dbcontext=new CangooEntities())
            {
                var query = dbcontext.Database.SqlQuery<GetReadNotificationResponse>("SELECT CAST(FeedID as varchar(36))FeedID,Title,Detail,CAST(CreationDate as varchar(36))CreationDate," +
                                                                            "CAST(ExpiryDate as varchar(36))ExpiryDate FROM Notifications WHERE FeedID = @feedid",
                                                                        new SqlParameter("@feedid", FeedId));
                return await query.ToListAsync();
            }
        }

        public static DateTime getUtcDateTime()
        {
            return DateTime.UtcNow;
        }
    }
}
