using Constants;
using DatabaseModel;
using DTOs.API;
using DTOs.Shared;
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
        public static async Task<List<DriverCancelReasonsDTO>> GetDriverCancelReasons(bool isNormalBookingReleated, bool isLaterBookingReleated, bool isCaptainReleated)
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

                return AutoMapperConfig._mapper.Map<List<CancelReason>, List<DriverCancelReasonsDTO>>(lstCancl);
            }
        }

        public static async Task<List<PassengerCancelReasonsDTO>> GetPassengerCancelReasons(bool isNormalBookingReleated, bool isLaterBookingReleated, bool isCaptainReleated)
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

                return AutoMapperConfig._mapper.Map<List<CancelReason>, List<PassengerCancelReasonsDTO>>(lstCancl);
            }
        }
    }
}
