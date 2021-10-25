using FireSharp.Config;
using FireSharp.Interfaces;
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
        public static void Test() {

            IFirebaseConfig config = new FirebaseConfig
            {
                AuthSecret = ConfigurationManager.AppSettings["FirebaseAuthSecret"].ToString(),
                BasePath = ConfigurationManager.AppSettings["FirebaseBasePath"].ToString()
            };
            IFirebaseClient client;
        }

        //Read
        //Write
        //Update
        //Delete

        //UniCast
        //BroadCast
    }
}
