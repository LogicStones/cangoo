using Constants;
using DatabaseModel;
using DTOs.API;
using Services.Automapper;
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
    public class PassengerPlacesService
    {
        public static async Task<List<GetRecentLocationDetails>> GetRecentTripsLocations(string passengerId)
        {
            using (CangooEntities dbContext = new CangooEntities())
            {
                var query = dbContext.Database.SqlQuery<GetRecentLocationDetails>("SELECT TOP(5) DropOffLocationLatitude,DropOffLocationLongitude,DropOffLocation,DropOffLocationPostalCode," +
                    "PickupLocationLatitude,PickupLocationLongitude,PickUpLocation,PickupLocationPostalCode, " +
                    "MidwayStop1Latitude,MidwayStop1Longitude, MidwayStop1Location, MidwayStop1PostalCode " +
                    "FROM Trips WHERE UserID=@passengerId AND TripStatusID = @tripStatus ORDER BY ArrivalDateTime DESC",
                                                                                                                    new SqlParameter("@passengerId", passengerId),
                                                                                                                    new SqlParameter("@tripStatus", TripStatuses.Completed));
                return await query.ToListAsync();
            }
        }

        public static async Task<List<GetPassengerPlaces>> GetPassengerPlaces(string passengerId)
        {
            using (CangooEntities dbContext = new CangooEntities())
            {
                var query = dbContext.Database.SqlQuery<GetPassengerPlaces>("SELECT CAST(ID as VARCHAR(36)) ID,CAST(PlaceTypeId as VARCHAR(36)) PlaceTypeId,Name," +
                                                                            "Address,Latitude,Longitutde,PostalCode FROM PassengerPlaces WHERE PassengerId = @passengerId", 
                                                                                                                    new SqlParameter("@passengerId", passengerId));
                return await query.ToListAsync();
            }
        }

        public static async Task<int> UpdatePassengerPlaces(UpdatePassengerPlaceRequest model)
        {
            using (CangooEntities dbContext = new CangooEntities())
            {
              return  await dbContext.Database.ExecuteSqlCommandAsync("UPDATE PassengerPlaces SET PlaceTypeId = @typeId, Name =@name, Address = @address, Latitude = @latitude, Longitutde = @longitude, PostalCode = @postalCode WHERE ID = @Id",
                                                                                    new SqlParameter("@address", model.Address),
                                                                                    new SqlParameter("@typeId", model.PlaceTypeId),
                                                                                    new SqlParameter("@longitude", model.Longitutde),
                                                                                    new SqlParameter("@latitude", model.Latitude),
                                                                                    new SqlParameter("@name", model.Name),
                                                                                    new SqlParameter("@Id", model.ID),
                                                                                    new SqlParameter("@postalCode",model.PostalCode));
            }
        }

        public static async Task<int> AddPlace(AddPassengerPlaceRequest model)
        {
            using (CangooEntities dbContext = new CangooEntities())
            {

                var result = AutoMapperConfig._mapper.Map<AddPassengerPlaceRequest, PassengerPlace>(model);
                result.ApplicationID = Guid.Parse(ConfigurationManager.AppSettings["ApplicationID"].ToString());
                result.ResellerID = Guid.Parse(ConfigurationManager.AppSettings["ResellerID"].ToString());

                dbContext.PassengerPlaces.Add(result);
                await dbContext.SaveChangesAsync();
                return result.ID;
            }
        }
    }
}
