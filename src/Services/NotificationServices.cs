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
    public class NotificationServices
    {
        public static async Task<int> GetValidNotificationsCount()
        {
            using (CangooEntities dbcontext = new CangooEntities())
            {
                var applicationId = ConfigurationManager.AppSettings["ApplicationID"].ToString();

                return await dbcontext.Notifications.Where(n =>
                n.ApplicationID.ToString().Equals(applicationId) &&
                n.ExpiryDate >= DateTime.Today).CountAsync();
            }
        }

        public static async Task<List<NotificationDetail>> GetValidNotifications(string userTypeId, string passengerId)
        {
            using (CangooEntities dbcontext = new CangooEntities())
            {
                var query = dbcontext.Database.SqlQuery<NotificationDetail>(
                    @"SELECT CAST(n.FeedID as varchar(36)) FeedID, Title, ShortDescription, 
CASE WHEN urn.ID IS NULL THEN 'False' ELSE 'True' END IsRead, 
ISNULL(CONVERT(VARCHAR, urn.ReadDateTime, 120), '') ReadDate, 
CONVERT(VARCHAR, CreationDate, 120) CreationDate, 
CONVERT(VARCHAR, ExpiryDate, 120) ExpiryDate 

FROM Notifications n
left join UserReadNotifications urn on n.FeedID = urn.FeedId AND urn.UserID = @passengerId
WHERE ApplicationUserTypeID = @userTypeId AND ExpiryDate >= @expiryDate",
                                                                        new SqlParameter("@userTypeId", userTypeId),
                                                                        new SqlParameter("@passengerId", passengerId),
                                                                        new SqlParameter("@expiryDate", DateTime.Today.ToString()));
                return await query.ToListAsync();
            }
        }

        public static async Task<NotificationDetail> GetNotificationdetails(string feedId, string passengerId)
        {
            using (CangooEntities dbcontext = new CangooEntities())
            {
                var query = dbcontext.Database.SqlQuery<NotificationDetail>(@"
IF NOT EXISTS(SELECT ID FROM UserReadNotifications WHERE FeedID = @feedId AND UserId = @passengerId) 
    BEGIN 
        INSERT INTO UserReadNotifications VALUES (NEWID(), @feedId, @passengerId, @readDateTime) 
    END 

SELECT CAST(FeedID as varchar(36))FeedID, Title, ShortDescription, Detail, Image, 
CONVERT(VARCHAR, CreationDate, 120) CreationDate, 
CONVERT(VARCHAR, ExpiryDate, 120) ExpiryDate 
FROM Notifications 
WHERE FeedID = @feedId",
                                                                        new SqlParameter("@passengerId", passengerId),
                                                                        new SqlParameter("@readDateTime", DateTime.UtcNow),
                                                                        new SqlParameter("@feedId", feedId));
                return await query.FirstOrDefaultAsync();
            }
        }
    }
}