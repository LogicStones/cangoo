using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTOs.Shared
{
    public class PassengerCancelReasonsDTO
    {
        public int Id { get; set; }
        public string Reason { get; set; }
    }

    public class DriverCancelReasonsDTO
    {
        public int id { get; set; }
        public string reason { get; set; }
    }
}
