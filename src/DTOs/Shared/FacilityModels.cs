using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTOs.Shared
{
    public class PassengerFacilityDTO
    {
        public string FacilityID { get; set; }
        public string FacilityName { get; set; }
        public string FacilityIcon { get; set; }
    }

    public class DriverFacilityDTO
    {
        public string facilityID { get; set; }
        public string facilityName { get; set; }
        public string facilityIcon { get; set; }
    }

}