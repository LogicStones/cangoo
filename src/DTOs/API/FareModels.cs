using DTOs.Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTOs.API
{
    public class EstimateFareRequest
    {
        [Required]
        public string ApplicationAuthorizeArea { get; set; }

        [Required]
        public string PickUpLatitude { get; set; }
        [Required]
        public string PickUpLongitude { get; set; }
        [Required]
        public string PickUpPostalCode { get; set; }

        public string MidwayLatitude { get; set; }
        public string MidwayLongitude { get; set; }
        public string MidwayPostalCode { get; set; }

        [Required]
        public string DropOffLatitude { get; set; }
        [Required]
        public string DropOffLongitutde { get; set; }
        [Required]
        public string DropOffPostalCode { get; set; }

        [Required]
        public string PolyLine { get; set; }
    }

    public class EstimateFareResponse
    {
        //public string PickUpFareManagerID { get; set; } = "";
        //public string MidwayFareManagerID { get; set; } = "";
        //public string DropOffFareMangerID { get; set; } = "";
        //public string InBoundDistanceInKM { get; set; } = "";
        //public string InBoundTimeInMinutes { get; set; } = "";
        //public string OutBoundDistanceInKM { get; set; } = "";
        //public string OutBoundTimeInMinutes { get; set; } = "";
        public string PolyLine { get; set; } = "";
        public List<FacilitiyDTO> Facilities { get; set; }
        public List<VehicleCategoryFareEstimate> Categories { get; set; }
        public CourierFareEstimate Courier { get; set; }
    }

    public class VehicleCategoryFareEstimate 
    {
        public string CategoryID { get; set; } = "";
        public string Amount { get; set; } = "0.0";
        public string ETA { get; set; } = "0";
        //public string InBoundBaseFare { get; set; } = "0.0";
        //public string InBoundDistanceFare { get; set; } = "0.0";
        //public string InBoundTimeFare { get; set; } = "0.0";
        //public string InBoundSurchargeAmount { get; set; } = "0.0";
        //public string OutBoundBaseFare { get; set; } = "0.0";
        //public string OutBoundDistanceFare { get; set; } = "0.0";
        //public string OutBoundTimeFare { get; set; } = "0.0";
        //public string OutBoundSurchargeAmount { get; set; } = "0.0";
    }

    public class CourierFareEstimate
    {
        public string Amount { get; set; } = "0.0";
        public string ETA { get; set; } = "0";
        public string Zones { get; set; } = "";
    }

    public class TripStats
    {
        public decimal DistanceInKM { get; set; }
        public decimal TimeInMinutes { get; set; }
    }
}