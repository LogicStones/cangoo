using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Constants
{
    public static class TransactionStatus
    {
        public const string succeeded = "succeeded";
        public const string requiresCapture = "requires_capture";
        public const string canceled = "canceled";
    }
}
