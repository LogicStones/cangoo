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
    public class FacilitiesManagerService
    {
        public static async Task<List<FacilitiyDTO>> GetFacilitiesListAsync()
        {
            using (var context = new CangooEntities())
            {
                //DestinationType obj = Mapper.Map<SourceType, DestinationType>(sourceValueObject);
                //List<DestinationType> listObj = Mapper.Map<List<SourceType>, List<DestinationType>>(enumarableSourceValueObject);
                
                var facilities = await context.Facilities.Where(f => f.isActive == true).ToListAsync();
                return AutoMapperConfig._mapper.Map<List<Facility>, List<FacilitiyDTO>>(facilities);

            }
        }
    }
}
