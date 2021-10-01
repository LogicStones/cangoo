using DatabaseModel;
using DTOs.API;
using DTOs.Shared;
using Services.Automapper;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services
{
    public class FavoritesService
    {
        public static async Task<List<SearchDriversResponse>> GetDriversSerachResultListAsync(string driverUserName)
        {
            using (var context = new CangooEntities())
            {
                var query = context.Database.SqlQuery<SearchDriversResponse>(
string.Format(@"Select CAST(c.CaptainID as VARCHAR(36)) DriverId, c.Name, CAST(c.Rating as VARCHAR) Rating, c.Picture ProfilePicture 
From Captain c inner join AspNetUsers anu on c.CaptainID = anu.Id
Where anu.UserName like '%{0}%'", driverUserName));

                return await query.ToListAsync();
            }
        }

        public static async Task<int> AddFavoriteDriverAsync(string driverId, string passengerId, string applicationId)
        {
            using (var context = new CangooEntities())
            {
                return await context.Database.ExecuteSqlCommandAsync(
@"IF EXISTS(Select * from UserFavoriteCaptain WHERE CaptainID = {2} and UserID = {1})
BEGIN 
    UPDATE UserFavoriteCaptain SET IsFavByPassenger = {3} WHERE CaptainID = {2} and UserID = {1}
END ELSE BEGIN
    INSERT INTO UserFavoriteCaptain (ID, UserID, CaptainID, IsFavByPassenger, IsFavByCaptain, ApplicationID) 
VALUES ({0},{1},{2},{3},{4},{5}) END",
Guid.NewGuid(), passengerId, driverId, true, false, applicationId);
            }
        }

        public static async Task<int> DeleteFavoriteDriverAsync(string driverId, string passengerId)
        {
            using (var context = new CangooEntities())
            {
                return await context.Database.ExecuteSqlCommandAsync(
@"IF EXISTS(Select * from UserFavoriteCaptain WHERE CaptainID = {1} and UserID = {0} and IsFavByCaptain = {2})
BEGIN 
    UPDATE UserFavoriteCaptain SET IsFavByPassenger = {3} WHERE CaptainID = {1} and UserID = {0}
END ELSE BEGIN
    DELETE FROM UserFavoriteCaptain WHERE CaptainID = {1} and UserID = {0}
END",
passengerId, driverId, true, false);
            }
        }

        public static async Task<List<FavoriteDriversListResponse>> GetFavoriteDriversListAsync(string passengerId)
        {
            using (var context = new CangooEntities())
            {
                var query = context.Database.SqlQuery<FavoriteDriversListResponse>(
@"Select CAST(c.CaptainID as VARCHAR(36)) DriverId, c.Name, CAST(c.Rating as VARCHAR) Rating, c.Picture ProfilePicture 
From Captain c inner join UserFavoriteCaptain ufc on c.CaptainID = ufc.CaptainID
Where ufc.UserID = {0}", passengerId);

                return await query.ToListAsync();
            }
        }
    }
}
