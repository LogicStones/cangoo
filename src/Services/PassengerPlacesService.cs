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
        public static async Task<List<PlaceDetails>> GetPassengerPlaces(string passengerId)
        {
            using (CangooEntities dbContext = new CangooEntities())
            {
                var query = dbContext.Database.SqlQuery<PlaceDetails>("SELECT [PlacesTypesID],[Name],[Address],[Latitude],[Longitutde],[PostalCode] FROM [PassengerPlaces] WHERE [PassengerId] = @passengerId", new SqlParameter("@passengerId", passengerId));
                return await query.ToListAsync();
            }
        }

        public static async Task<int> UpdatePassengerPlaces(UpdatePassengerPlaceRequest model)
        {
            using (CangooEntities dbContext = new CangooEntities())
            {
              return  await dbContext.Database.ExecuteSqlCommandAsync("UPDATE PassengerPlaces SET PlacesTypesID = @typeId, Name =@name, Address = @address, Latitude = @latitude, Longitutde = @longitude, PostalCode = @postalCode WHERE ID = @Id",
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
