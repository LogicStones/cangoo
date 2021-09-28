using API.Controllers;
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
    public class ResponseWrapperInitializer : ActionFilterAttribute
    {
        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            var baseController = actionContext.ControllerContext.Controller as BaseController;

            //if (baseController == null)
            //{
            //    throw new InvalidOperationException("It is not YourController !!!");
            //}

            // It is YourController - validate here
            baseController.ResponseWrapper = new DTOs.API.ResponseWrapper();
            

            // UPDATED ***************************
            // or test whether controller has property

            //var property = filterContext.Controller.GetType().GetProperty("YourProperty");

            //if (property == null)
            //{
            //    throw new InvalidOperationException("There is no YourProperty !!!");
            //}
        }
    }
}