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
    public class ExceptionHandling : ExceptionFilterAttribute
    {
        public override void OnException(HttpActionExecutedContext context)
        {

            using (var stream = new MemoryStream())
            {
                var temp = (HttpContextBase)context.Request.Properties["MS_HttpContext"];
                temp.Request.InputStream.Seek(0, SeekOrigin.Begin);
                temp.Request.InputStream.CopyTo(stream);
                string PayLoad = Encoding.UTF8.GetString(stream.ToArray());

                Log.Information("Endpoint : " + context.Request.RequestUri);
                Log.Information("PayLoad : " + PayLoad);
                Log.Information("Exception details : " + context.Exception);

                //context.Response = context.Request.CreateResponse(HttpStatusCode.InternalServerError, new Utilities.API.ResponseWrapper
                //{
                //    Error = true,
                //    Message = Utilities.API.ResponseKeys.serverError,
                //    Data = context.Exception
                //});
            }

            //response.Headers.Add("X-Error", "An unexpected error occured");
            //context.Result = new ResponseMessageResult(response);

            //if (context.Exception is NotImplementedException)
            //{
            //    context.Response = new HttpResponseMessage(HttpStatusCode.NotImplemented);
            //}
        }
    }
}