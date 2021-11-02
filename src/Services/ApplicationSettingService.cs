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
    public class ApplicationSettingService
    {
        public static async Task<int> GetRequestTimeOut(string applicationId)
        {
            var settings = await GetApplicationSettings(applicationId);
            return settings != null ? (settings.RequestResponseTime != null ? ((int)settings.RequestResponseTime + 15) : 60) : 60;
        }

        public static async Task<ApplicationSetting> GetApplicationSettings(string applicationId)
        {
            using (var dbContext = new CangooEntities())
            {
                return await dbContext.ApplicationSettings.Where(a => a.ApplicationID.ToString().ToLower().Equals(applicationId.ToLower())).FirstOrDefaultAsync();
            }
        }
    }
}
