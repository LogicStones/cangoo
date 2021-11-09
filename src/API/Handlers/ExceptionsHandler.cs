using DTOs.API;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http.ExceptionHandling;
using System.Web.Http.Results;

namespace API.Handlers
{
    /// <summary>
    ///Application Level Exception Handling
    /// </summary>
    public class ExceptionsHandler : ExceptionHandler
    {
        public async override Task HandleAsync(ExceptionHandlerContext context, CancellationToken cancellationToken)
        {
            var response = context.Request.CreateResponse(HttpStatusCode.InternalServerError, new ResponseWrapper
            {
                Error = true,
                Message = Constants.ResponseKeys.serverError,
                Data = context.Exception
            });

            response.Headers.Add("X-Error", "An unexpected error occured");
            context.Result = new ResponseMessageResult(response);
        }
    }
}