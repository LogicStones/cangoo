using Constants;
using DatabaseModel;
using DTOs.API;
using Integrations;
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
    public class VehiclesService
    {
        public static async Task<string> GetVehicleETA(string categoryId)
        {
            using (var dbContext = new CangooEntities())
            {
                return "";
            }
        }
    }
}
