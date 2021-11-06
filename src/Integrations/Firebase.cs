using FireSharp.Config;
using FireSharp.Interfaces;
using FireSharp.Response;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integrations
{
    public class FirebaseIntegration
    {
        private static IFirebaseClient client;
        private static IFirebaseConfig config = new FirebaseConfig
        {
            AuthSecret = ConfigurationManager.AppSettings["FirebaseAuthSecret"].ToString(),
            BasePath = ConfigurationManager.AppSettings["FirebaseBasePath"].ToString()
        };

        public static async Task Write(string path, dynamic data)
        {
            SetClient();
            await client.SetAsync(path, data);
        }

        public static async Task<dynamic> Read(string path)
        {
            SetClient();
            return await client.GetAsync(path);
        }

        public static async Task Update(string path, dynamic data)
        {
            SetClient();
            await client.UpdateAsync(path, data);
        }

        public static async Task Delete(string path)
        {
            SetClient();
            await client.DeleteAsync(path);
        }

        private static void SetClient()
        {
            client = new FireSharp.FirebaseClient(config);
        }
    }
}
