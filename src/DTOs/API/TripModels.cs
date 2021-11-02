using DTOs.Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTOs.API
{
    public class PassengerTripsListRequest
    {
        [Required]
        public string PassengerId { get; set; }
        [Required]
        public string OffSet { get; set; }
        [Required]
        public string Limit { get; set; }
    }

    public class PassengerTripsListResponse
    {
        public string TotalRecords { get; set; } = "0";
        public List<TripOverView> Trips { get; set; } = new List<TripOverView>();
    }

    public class TripOverView
    {
        public string TripID { get; set; } = "";
        public string TotalRecord { get; set; } = "";
        public string BookingDateTime { get; set; } = "";
        public string TripStatusID { get; set; } = "";
        public string Fare { get; set; } = "";
        public string PickUpLocation { get; set; } = "";
        public string MidwayStop1Location { get; set; } = "";
        public string DropOffLocation { get; set; } = "";
    }

    public class PassengerTripDetailRequest
    {
        [Required]
        public string TripId { get; set; }
    }

    public class TripDetails
    {
        public string PickUpLocationLatitude { get; set; } = "";
        public string PickupLocationLongitude { get; set; } = "";
        public string PickupLocation { get; set; } = "";
        public string MidWayStop1Latitude { get; set; } = "";
        public string MidWayStop1Longitude { get; set; } = "";
        public string MidwayStop1Location { get; set; } = "";
        public string DropOffLocationLatitude { get; set; } = "";
        public string DropOffLocationLongitude { get; set; } = "";
        public string DropOffLocation { get; set; } = "";
        public string BookingDateTime { get; set; } = "";
        public string AcceptanceDateTime { get; set; } = "";
        public string ArrivalDateTime { get; set; } = "";
        public string StartDateTime { get; set; } = "";
        public string MidwayStop1ArrivalDateTime { get; set; } = "";
        public string TripEndDatetime { get; set; } = "";
        public string CaptainName { get; set; } = "";
        public string CaptainPicture { get; set; } = "";
        public string VehicleCategory { get; set; } = "";
        public string PolyLine { get; set; } = "";
        public string Distance { get; set; } = "";
        public string Fare { get; set; } = "";
        public string Tip { get; set; } = "";
        public string PromoID { get; set; } = "";
        public string PromoDiscount { get; set; } = "";
        public string PromoCode { get; set; } = "";
        public string StatusId { get; set; } = "";
        public string CancelReason { get; set; } = "";
        public string PaymentModeId { get; set; } = "";
        public string Description { get; set; } = "";
        public string Complaint { get; set; } = "";
        public string FacilityIds { get; set; } = "";
        public string Make { get; set; } = "";
        public string Model { get; set; } = "";
        public string PlateNumber { get; set; } = "";
        public List<FacilitiyDTO> FacilitiesList { get; set; }
    }

    public class BookTripRequest
    {
        [Required]
        public string PassengerId { get; set; }
        public string ApplicationAuthorizeArea { get; set; }
        public string DeviceToken { get; set; }
        [Required]
        public string PickUpLatitude { get; set; }
        [Required]
        public string PickUpLongitude { get; set; }
        [Required]
        public string PickUpLocation { get; set; }
        public string MidwayLatitude { get; set; }
        public string MidwayLongitude { get; set; }
        public string MidwayLocation { get; set; }
        [Required]
        public string DropOffLatitude { get; set; }
        [Required]
        public string DropOffLongitutde { get; set; }
        [Required]
        public string DropOffLocation { get; set; }
        public string InBoundDistanceInKM { get; set; }
        public string InBoundDistanceFare { get; set; }
        public string InBoundTimeInMinutes { get; set; }
        public string InBoundTimeFare { get; set; }
        public string OutBoundDistanceInKM { get; set; }
        public string OutBoundDistanceFare { get; set; }
        public string OutBoundTimeInMinutes { get; set; }
        public string OutBoundTimeFare { get; set; }
        public string InBoundSurchargeAmount { get; set; }
        public string OutBoundSurchargeAmount { get; set; }
        public string InBoundBaseFare { get; set; }
        public string OutBoundBaseFare { get; set; }
        public string SeatingCapacity { get; set; }
        [Required]
        public string SelectedPaymentMethod { get; set; }
        [Required]
        public string SelectedPaymentMethodId { get; set; }
        public string TripID { get; set; }
        public string BookingModeId { get; set; }
        public string KarhooTripID { get; set; }
        [Required]
        public string IsLaterBooking { get; set; }
        public string LaterBookingDate { get; set; }
        public string IsReRouteRequest { get; set; }
        public string Description { get; set; }
        public string DriverID { get; set; }
        [Required]
        public string TimeZoneOffset { get; set; }
        public string PromoDiscountAmount { get; set; }
        public string PromoCodeID { get; set; }
        public string Distance { get; set; }
        public string RequiredFacilities { get; set; }
        public string DiscountType { get; set; }
    }

    public class BookTripResponse : DiscountTypeDTO
    {
        public string RequestTime { get; set; } = "";
        public string TripId { get; set; } = "";
        public string IsLaterBooking { get; set; } = "";
    }

    public class TripRequestLogDTO
    {
        public Guid RequestLogID { get; set; }
        public Guid TripID { get; set; }
        public Guid CaptainID { get; set; }
        public DateTime TimeStamp { get; set; }
        public string CaptainLocationLatitude { get; set; }
        public string CaptainLocationLongitude { get; set; }
        public double DistanceToPickUpLocation { get; set; }
        public bool isReRouteRequest { get; set; }
    }

    public class TripTimeOutRequest
    {
        [Required]
        public string PassengerId { get; set; }
        [Required]
        public string TripId { get; set; }
    }

    public class CancelTripRequest
    {
        [Required]
        public string PassengerId { get; set; }
        [Required]
        public string TripId { get; set; }
        [Required]
        public string DistanceTravelled { get; set; }
        [Required]
        public string CancelID { get; set; }
        [Required]
        public string IsLaterBooking { get; set; }
    }

    public class CancelTripResponse
    {
        public string TripId { get; set; } = "";
        public string IsLaterBooking { get; set; } = "";
    }
}
