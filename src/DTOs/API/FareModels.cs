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
        public string PolyLine { get; set; }
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
        public string InBoundTimeInSeconds { get; set; }
        [Required]
        public string InBoundDistanceInMeters { get; set; }
        [Required]
        public string OutBoundTimeInSeconds { get; set; }
        [Required]
        public string OutBoundDistanceInMeters { get; set; }
    }

    public class EstimateFareResponse : InBoundOutBoundStats
    {
        public string PolyLine { get; set; } = "";
        public List<FacilitiyDTO> Facilities { get; set; }
        public List<VehicleCategoryFareEstimate> Categories { get; set; }
        public CourierFareEstimate Courier { get; set; }
    }

    public class InBoundOutBoundStats
    {
        public string InBoundDistanceInKM { get; set; } = "";
        public string InBoundTimeInMinutes { get; set; } = "";
        public string OutBoundDistanceInKM { get; set; } = "";
        public string OutBoundTimeInMinutes { get; set; } = "";
    }

    public class DistanceAndTimeFareDTO {
        public decimal DistanceFare { get; set; } = 0;
        public decimal TimeFare { get; set; } = 0;
    }

    public class FareDetailsDTO
    {
        public decimal InBoundBaseFare { get; set; } = 0;
        public decimal InBoundBookingFare { get; set; } = 0;
        public decimal InBoundWaitingFare { get; set; } = 0;
        public decimal InBoundSurchargeAmount { get; set; } = 0;
        public DistanceAndTimeFareDTO InBound { get; set; } = new DistanceAndTimeFareDTO();
        public DistanceAndTimeFareDTO OutBound { get; set; } = new DistanceAndTimeFareDTO();
        public decimal SurchargeAmount { get; set; } = 0;
        public decimal TotalFare { get; set; } = 0;
        public decimal FormattingAdjustment { get; set; } = 0;
    }

    public class DiscountTypeDTO
    {
        public string DiscountType { get; set; } = "normal";
        public string DiscountAmount { get; set; } = "0.00";
    }

    public class SpecialPromotionDTO : DiscountTypeDTO
    {
        public string PromotionId { get; set; }
    }

    public class VehicleCategoryFareEstimate : InBoundOutBoundFareDetails
    {
        public string CategoryID { get; set; } = "";
        public string Amount { get; set; } = "0.0";
        public string FormattingAdjustment { get; set; } = "0.0";
        public string ETA { get; set; } = "0";
        public string InBoundRSFMID { get; set; } = "";
        public string OutBoundRSFMID { get; set; } = "";
    }

    public class InBoundOutBoundFareDetails
    {
        public string InBoundBaseFare { get; set; } = "0.0";
        public string InBoundBookingFare { get; set; } = "0.0";
        public string InBoundWaitingFare { get; set; } = "0.0";
        public string InBoundDistanceFare { get; set; } = "0.0";
        public string InBoundTimeFare { get; set; } = "0.0";
        public string InBoundSurchargeAmount { get; set; } = "0.0";
        public string OutBoundBaseFare { get; set; } = "0.0";
        public string OutBoundBookingFare { get; set; } = "0.0";
        public string OutBoundWaitingFare { get; set; } = "0.0";
        public string OutBoundDistanceFare { get; set; } = "0.0";
        public string OutBoundTimeFare { get; set; } = "0.0";
        public string OutBoundSurchargeAmount { get; set; } = "0.0";
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