using Constants;
using DatabaseModel;
using DTOs.API;
using Integrations;
using Services.Automapper;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Common;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services
{
    public class CancelReasonsService
    {
        public static async Task<List<CanclReasonsDTO>> GetCancelReasons(bool isNormalBookingReleated, bool isLaterBookingReleated, bool isCaptainReleated)
        {
            using (var dbContext = new CangooEntities())
            {
                List<CancelReason> lstCancl = new List<CancelReason>();
                
                if (isNormalBookingReleated)
                    lstCancl = await dbContext.CancelReasons.Where(c => c.isNormalBookingReleated == true &&
                    c.isCaptainReleated == isCaptainReleated).ToListAsync();
                else
                    lstCancl = await dbContext.CancelReasons.Where(c => c.isLaterBookingReleated == true &&
                    c.isCaptainReleated == isCaptainReleated).ToListAsync();

                return AutoMapperConfig._mapper.Map<List<CancelReason>, List<CanclReasonsDTO>>(lstCancl);

                //List<CanclReasonsDTO> lstcr = new List<CanclReasonsDTO>();
                //foreach (var item in lstCancl)
                //{
                //    CanclReasonsDTO cr = new CanclReasonsDTO
                //    {
                //        Id = item.ID,
                //        Reason = item.Reason
                //    };
                //    lstcr.Add(cr);
                //}

                //return lstcr;
            }
        }
    }
}
