using DTOs.Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        public List<PassengerFacilityDTO> FacilitiesList { get; set; }
    }

    public class BookTripRequest : FareBreakDownDTO
    {
        [Required]
        [DefaultValue("edf49e84-06fb-4a6d-9448-6011fc1bc611")]
        public string PassengerId { get; set; }

        [Required]
        [DefaultValue("32.1348011158146")]
        public string PickUpLatitude { get; set; }
        
        [Required]
        [DefaultValue("74.2097900505854")]
        public string PickUpLongitude { get; set; }
        
        [Required]
        [DefaultValue("52250")]
        public string PickUpPostalCode { get; set; }
        
        [Required]
        [DefaultValue("Madni Masjid, Madni Rd, Sector Y Peoples Colony, Gujranwala, Punjab, Pakistan")]
        public string PickUpLocation { get; set; }
        
        [DefaultValue("32.1418279")]
        public string MidwayStop1Latitude { get; set; }
        
        [DefaultValue("74.2103029")]
        public string MidwayStop1Longitude { get; set; }
        
        [DefaultValue("52250")]
        public string MidwayStop1PostalCode { get; set; }
        
        [DefaultValue("Madina Park, Sector Z Sector X Peoples Colony, Gujranwala, Pakistan")]
        public string MidwayStop1Location { get; set; }
        
        [Required]
        [DefaultValue("32.1374413236167")]
        public string DropOffLatitude { get; set; }
        
        [Required]
        [DefaultValue("74.2070284762054")]
        public string DropOffLongitude { get; set; }
        
        [Required]
        [DefaultValue("52250")]
        public string DropOffPostalCode { get; set; }
        
        [Required]
        [DefaultValue("Street 21, Sector Y Peoples Colony, Gujranwala, Punjab, Pakistan")]
        public string DropOffLocation { get; set; }
        
        [Required]
        [DefaultValue("345")]
        public string InBoundTimeInSeconds { get; set; }
        
        [Required]
        [DefaultValue("245")]
        public string InBoundDistanceInMeters { get; set; }
        
        [Required]
        [DefaultValue("56")]
        public string OutBoundTimeInSeconds { get; set; }
        
        [Required]
        [DefaultValue("12")]
        public string OutBoundDistanceInMeters { get; set; }
        
        [Required]
        [DefaultValue("4")]
        public string SeatingCapacity { get; set; }
        
        //[Required]
        //[DefaultValue("Cash")]
        //public string SelectedPaymentMethod { get; set; }
        
        [Required]
        [DefaultValue("1")]
        //public string SelectedPaymentMethodId { get; set; }
        public string PaymentModeId { get; set; }

        [Required]
        [DefaultValue("1")]
        public string CategoryId { get; set; }
        
        [Required]
        [DefaultValue("False")]
        public string IsWishCarRequest { get; set; }
        
        [Required]
        [DefaultValue("False")]
        public string IsCourierRequest { get; set; }
        
        [Required]
        [DefaultValue("3")]
        public string BookingModeId { get; set; }
        
        [Required]
        [DefaultValue("False")]
        public string IsReRouteRequest { get; set; }
        
        [Required]
        [DefaultValue("18000")]
        public string TimeZoneOffset { get; set; }
        
        [Required]
        [DefaultValue("False")]
        public string IsLaterBooking { get; set; }
        
        [Required]
        [DefaultValue("normal")]
        public string DiscountType { get; set; }
        
        [DefaultValue("0.0")]
        public string DiscountAmount { get; set; }

        [DefaultValue("")]
        public string PromoCodeId { get; set; }

        [DefaultValue("")]
        public string LaterBookingDate { get; set; }
        
        [DefaultValue("")]
        public string TripId { get; set; }
        
        [DefaultValue("")]
        public string KarhooTripId { get; set; }
        
        [DefaultValue("")]
        public string Description { get; set; }
        
        [DefaultValue("")]
        public string DriverId { get; set; }
        

        [DefaultValue("")]
        public string RequiredFacilities { get; set; }
        
        [DefaultValue("")]
        public string DeviceToken { get; set; }
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

    public class DispatchedRideLogDTO
    {
        public Guid DispatchLogID { get; set; }
        public Guid TripID { get; set; }
        public Guid CaptainID { get; set; }
        public Guid DispatchedBy { get; set; }
        public DateTime LogTime { get; set; }
        public Guid ApplicationID { get; set; }
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

    public class ApplyPromoCodeRequest
    {
        [Required]
        public string TripId { get; set; }
        [Required]
        public string PassengerId { get; set; }
        [Required]
        public string PromoCodeId { get; set; }
    }

    public class UpdateTripPaymentMethodRequest
    {
        [Required]
        public string PaymentModeId { get; set; } = "";
        [Required]
        public string TripId { get; set; } = "";
        [Required]
        public string PassengerId { get; set; } = "";
    }

    public class UpdateTripTipAmountRequest
    {
        [Required]
        public string TripId { get; set; } = "";
        [Required]
        public string TipAmount { get; set; } = "";
    }

    public class UpdateTripUserFeedback
    {
        [Required]
        public string TripId { get; set; }
        [Required]
        public string UserFeedBack { get; set; }
    }
}
