using DTOs.API;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace API.Filters
{
    public class ModelStateValidation : ActionFilterAttribute
    {
        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            if (!actionContext.ModelState.IsValid)
            {
                actionContext.Response = actionContext.Request.CreateResponse(HttpStatusCode.BadRequest, new ResponseWrapper
                {
                    Error = true,
                    Message = Constants.ResponseKeys.invalidParameters,
                    Data = actionContext.ModelState.Select(x => x.Value.Errors)
                           .Where(y => y.Count > 0)
                           .ToList()
                });
            }
            else
            {
                using (var stream = new MemoryStream())
                {
                    var temp = (HttpContextBase)actionContext.Request.Properties["MS_HttpContext"];
                    temp.Request.InputStream.Seek(0, SeekOrigin.Begin);
                    temp.Request.InputStream.CopyTo(stream);
                    string PayLoad = Encoding.UTF8.GetString(stream.ToArray());

                    Log.Information(string.Format("Endpoint : {0}", actionContext.Request.RequestUri.AbsolutePath));
                    Log.Information(string.Format("PayLoad: {0}", PayLoad));
                }



            }
        }
    }
}