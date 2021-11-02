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
        public static async Task<List<FacilitiyDTO>> GetFacilitiesListAsync()
        {
            using (var context = new CangooEntities())
            {
                var resellerId = ConfigurationManager.AppSettings["ResellerID"].ToString();
                var applicationId = ConfigurationManager.AppSettings["ApplicationID"].ToString();

                var facilities = await context.Facilities.Where(f => f.ResellerID.ToString().Equals(resellerId) 
                && f.ApplicationID.ToString().Equals(applicationId) 
                && f.isActive == true).ToListAsync();
                return AutoMapperConfig._mapper.Map<List<Facility>, List<FacilitiyDTO>>(facilities);
            }
        }

        public static async Task<List<FacilitiyDTO>> GetFacilitiesDetailByIds(string subscribedFacilities)
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

                return AutoMapperConfig._mapper.Map<List<Facility>, List<FacilitiyDTO>>(facilities);
            }

            //var facilities = await GetFacilitiesListAsync();

            //var capFac = subscribedFacilities.Split(',');
            //var lstFacilities = new List<FacilitiyDTO>();
            //foreach (var temp in capFac)
            //{
            //    foreach (var fac in facilities)
            //    {
            //        if (temp.Equals(fac.FacilityID.ToString()))
            //        {
            //            lstFacilities.Add(new FacilitiyDTO()
            //            {
            //                FacilityID = fac.FacilityID.ToString(),
            //                FacilityName = fac.FacilityName,
            //                FacilityIcon = fac.FacilityIcon
            //            });
            //            break;
            //        }
            //    }
            //}

            //Filter facilities
            //lstFacilities.Select(f => f.FacilityID.())

            //return facilities;
        }
    }
}
