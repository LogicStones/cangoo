using DatabaseModel;
using DTOs.API;
using DTOs.Shared;
using Services.Automapper;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services
{
    public class FacilitiesService
    {
        public static async Task<List<PassengerFacilityDTO>> GetPassengerFacilitiesList()
        {
            using (var context = new CangooEntities())
            {
                var resellerId = ConfigurationManager.AppSettings["ResellerID"].ToString();
                var applicationId = ConfigurationManager.AppSettings["ApplicationID"].ToString();

                var facilities = await context.Facilities.Where(f => f.ResellerID.ToString().Equals(resellerId) 
                && f.ApplicationID.ToString().Equals(applicationId) 
                && f.isActive == true).ToListAsync();
                return AutoMapperConfig._mapper.Map<List<Facility>, List<PassengerFacilityDTO>>(facilities);
            }
        }

        public static async Task<List<PassengerFacilityDTO>> GetPassengerFacilitiesDetailByIds(string subscribedFacilities)
        {

            using (var context = new CangooEntities())
            {
                var resellerId = ConfigurationManager.AppSettings["ResellerID"].ToString();
                var applicationId = ConfigurationManager.AppSettings["ApplicationID"].ToString();

                var facilities = await context.Facilities
                    .Where(f => f.ResellerID.ToString().Equals(resellerId)
                    && f.ApplicationID.ToString().Equals(applicationId)
                    && f.FacilityID.ToString().Contains(subscribedFacilities)
                    && f.isActive == true).ToListAsync();

                return AutoMapperConfig._mapper.Map<List<Facility>, List<PassengerFacilityDTO>>(facilities);
            }
        }

        public static async Task<List<DriverFacilityDTO>> GetDriverFacilitiesList()
        {
            using (var context = new CangooEntities())
            {
                var resellerId = ConfigurationManager.AppSettings["ResellerID"].ToString();
                var applicationId = ConfigurationManager.AppSettings["ApplicationID"].ToString();

                var facilities = await context.Facilities.Where(f => f.ResellerID.ToString().Equals(resellerId)
                && f.ApplicationID.ToString().Equals(applicationId)
                && f.isActive == true).ToListAsync();
                return AutoMapperConfig._mapper.Map<List<Facility>, List<DriverFacilityDTO>>(facilities);
            }
        }

        public static async Task<List<DriverFacilityDTO>> GetDriverFacilitiesDetailByIds(string subscribedFacilities)
        {

            using (var context = new CangooEntities())
            {
                var resellerId = ConfigurationManager.AppSettings["ResellerID"].ToString();
                var applicationId = ConfigurationManager.AppSettings["ApplicationID"].ToString();

                var facilities = await context.Facilities
                    .Where(f => f.ResellerID.ToString().Equals(resellerId)
                    && f.ApplicationID.ToString().Equals(applicationId)
                    && f.FacilityID.ToString().Contains(subscribedFacilities)
                    && f.isActive == true).ToListAsync();

                return AutoMapperConfig._mapper.Map<List<Facility>, List<DriverFacilityDTO>>(facilities);
            }
        }
    }
}
