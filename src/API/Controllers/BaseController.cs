using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using API.Filters;
using DTOs.API;
using System.Threading.Tasks;

namespace API.Controllers
{
    [ResponseWrapperInitializer]
    public class BaseController : ApiController
    {
        public string ResellerID
        {
            get
            {
                return Request.Headers.GetValues("ResellerID").First();
            }
        }

        public string ApplicationID
        {
            get
            {
                return Request.Headers.GetValues("ApplicationID").First();
            }
        }

        public ResponseWrapper ResponseWrapper { get; set; }

        [HttpGet]
        [Route("auth-token-validation")]
        public async Task<HttpResponseMessage> TokenValidation()
        {
            return Request.CreateResponse(HttpStatusCode.OK);
        }
    }
}
