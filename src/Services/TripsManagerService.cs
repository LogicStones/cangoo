using Constants;
using DatabaseModel;
using DTOs.API;
using Integrations;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services
{
    public class TripsManagerService
    {
        public static async Task<List<TripOverView>> GetPassengerCompletedTrips(string passengerId, int offSet, int limit)
        {
            using (CangooEntities dbContext = new CangooEntities())
            {
                var query = dbContext.Database.SqlQuery<TripOverView>("exec spGetPassengerCompletedTrips @PassengerID, @OffSet, @Limit",
                                                                new SqlParameter("@PassengerID", passengerId),
                                                                new SqlParameter("@OffSet", offSet),
                                                                new SqlParameter("@Limit", limit));
                return await query.ToListAsync();
            }
        }
        
        public static async Task<List<TripOverView>> GetPassengerCancelledTrips(string passengerId, int offSet, int limit)
        {
            using (CangooEntities dbContext = new CangooEntities())
            {
                var query = dbContext.Database.SqlQuery<TripOverView>("exec spGetPassengerCancelledTrips @PassengerID, @OffSet, @Limit",
                                                                new SqlParameter("@PassengerID", passengerId),
                                                                new SqlParameter("@OffSet", offSet),
                                                                new SqlParameter("@Limit", limit));
                return await query.ToListAsync();
            }
        }
        
        public static async Task<List<TripOverView>> GetPassengerScheduledTrips(string passengerId, int offSet, int limit)
        {
            using (CangooEntities dbContext = new CangooEntities())
            {
                var query = dbContext.Database.SqlQuery<TripOverView>("exec spGetPassengerScheduledTrips @PassengerID, @OffSet, @Limit",
                                                                new SqlParameter("@PassengerID", passengerId),
                                                                new SqlParameter("@OffSet", offSet),
                                                                new SqlParameter("@Limit", limit));
                return await query.ToListAsync();
            }
        }

        public static async Task<TripDetails> GetTripDetails(string tripId)
        {
            using (CangooEntities dbContext = new CangooEntities())
            {
                var query = dbContext.Database.SqlQuery<TripDetails>("exec spGetTripDetails @TripId",
                                                                new SqlParameter("@TripId", tripId));
                var details = await query.FirstOrDefaultAsync();
                details.FacilitiesList = await FacilitiesManagerService.GetFacilitiesListAsync();
                return details;
            }
        }

        public static async Task<List<GetRecentLocationDetails>> GetRecentLocation(string passengerId)
        {
            using (CangooEntities dbContext = new CangooEntities())
            {
                var query = dbContext.Database.SqlQuery<GetRecentLocationDetails>("SELECT TOP(5) DropOffLocationLatitude,DropOffLocationLongitude,DropOffLocation FROM Trips WHERE UserID=@passengerId AND TripStatusID=1 ORDER BY ArrivalDateTime DESC",
                                                                                                                    new SqlParameter("@passengerId", passengerId));
                return await query.ToListAsync();
            }
        }
    }
}