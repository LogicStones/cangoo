using DatabaseModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity;
using System.Configuration;

namespace Services
{
    public static class AuthenticationService
    {
        public static async Task<bool> IsAppliactionBlockedAsync(string ApplicationID)
        {
            using (var context = new CangooEntities())
            {
                return (await context.Applications.Where(a => a.ApplicationID.ToString().Equals(ApplicationID)).FirstOrDefaultAsync()).isBlocked;
            }
        }

        public static bool IsValidApplicationUser(string ApplicationID)
        {
            return ApplicationID.ToUpper().Equals(ConfigurationManager.AppSettings["ApplicationID"].ToString().ToUpper());
        }

        public static string GenerateOTP()
        {
            Random random = new Random();
            int length = 4;
            const string chars = "0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public static string GetRandomPassword()
        {
            Random random = new Random();
            int length = 6;
            //const string chars = "ABCDEFGHIJKLMNOPQRSTabcdefghigklmnopqrstuvwxyz0123456789!@#$%&";
            const string chars = "0123456789";

            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
