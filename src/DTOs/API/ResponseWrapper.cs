using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTOs.API
{
    public class ResponseWrapper
    {
        public bool Error { get; set; } = true;
        public string Message { get; set; }
        public dynamic Data { get; set; }
    }
}
