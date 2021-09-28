using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using API.Filters;
using DTOs.API;

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

        //public Dictionary<dynamic, dynamic> dic;
        //ResponseEntity response = new ResponseEntity();

        // GET: api/Base
        //public IEnumerable<string> Get()
        //{
        //    return new string[] { "value1", "value2" };
        //}

        //// GET: api/Base/5
        //public string Get(int id)
        //{
        //    return "value";
        //}

        //// POST: api/Base
        //public void Post([FromBody]string value)
        //{
        //}

        //// PUT: api/Base/5
        //public void Put(int id, [FromBody]string value)
        //{
        //}

        //// DELETE: api/Base/5
        //public void Delete(int id)
        //{
        //}
    }
}
