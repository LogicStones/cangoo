using DatabaseModel;
using DTOs.API;
using DTOs.Shared;
using Services.Automapper;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services
{
    public class FavoritiesService
    {
        public static async Task<List<FacilitiyDTO>> AddFavoriteDriverAsync()
        {
            using (var context = new CangooEntities())
            {
                //DestinationType obj = Mapper.Map<SourceType, DestinationType>(sourceValueObject);
                //List<DestinationType> listObj = Mapper.Map<List<SourceType>, List<DestinationType>>(enumarableSourceValueObject);
                
                var facilities = await context.Facilities.Where(f => f.isActive == true).ToListAsync();
                return AutoMapperConfig._mapper.Map<List<Facility>, List<FacilitiyDTO>>(facilities);

            }
        }

        public static async Task<List<FacilitiyDTO>> DeleteFavoriteDriverAsync()
        {
            using (var context = new CangooEntities())
            {
                //DestinationType obj = Mapper.Map<SourceType, DestinationType>(sourceValueObject);
                //List<DestinationType> listObj = Mapper.Map<List<SourceType>, List<DestinationType>>(enumarableSourceValueObject);

                var facilities = await context.Facilities.Where(f => f.isActive == true).ToListAsync();
                return AutoMapperConfig._mapper.Map<List<Facility>, List<FacilitiyDTO>>(facilities);

            }
        }

        public static async Task<List<FacilitiyDTO>> GetFavoriteDriversListAsync()
        {
            using (var context = new CangooEntities())
            {
                //DestinationType obj = Mapper.Map<SourceType, DestinationType>(sourceValueObject);
                //List<DestinationType> listObj = Mapper.Map<List<SourceType>, List<DestinationType>>(enumarableSourceValueObject);

                var facilities = await context.Facilities.Where(f => f.isActive == true).ToListAsync();
                return AutoMapperConfig._mapper.Map<List<Facility>, List<FacilitiyDTO>>(facilities);

            }
        }

        //public static async Task<List<FacilitiyDTO>> AddFavoritePassengerAsync()
        //{
        //    using (var context = new CangooEntities())
        //    {
        //        //DestinationType obj = Mapper.Map<SourceType, DestinationType>(sourceValueObject);
        //        //List<DestinationType> listObj = Mapper.Map<List<SourceType>, List<DestinationType>>(enumarableSourceValueObject);

        //        var facilities = await context.Facilities.Where(f => f.isActive == true).ToListAsync();
        //        return AutoMapperConfig._mapper.Map<List<Facility>, List<FacilitiyDTO>>(facilities);

        //    }
        //}

        //public static async Task<List<FacilitiyDTO>> DeleteFavoritePassengerAsync()
        //{
        //    using (var context = new CangooEntities())
        //    {
        //        //DestinationType obj = Mapper.Map<SourceType, DestinationType>(sourceValueObject);
        //        //List<DestinationType> listObj = Mapper.Map<List<SourceType>, List<DestinationType>>(enumarableSourceValueObject);

        //        var facilities = await context.Facilities.Where(f => f.isActive == true).ToListAsync();
        //        return AutoMapperConfig._mapper.Map<List<Facility>, List<FacilitiyDTO>>(facilities);

        //    }
        //}

        //public static async Task<List<FacilitiyDTO>> GetFavoritePassengersListAsync()
        //{
        //    using (var context = new CangooEntities())
        //    {
        //        //DestinationType obj = Mapper.Map<SourceType, DestinationType>(sourceValueObject);
        //        //List<DestinationType> listObj = Mapper.Map<List<SourceType>, List<DestinationType>>(enumarableSourceValueObject);

        //        var facilities = await context.Facilities.Where(f => f.isActive == true).ToListAsync();
        //        return AutoMapperConfig._mapper.Map<List<Facility>, List<FacilitiyDTO>>(facilities);

        //    }
        //}
    }
}
