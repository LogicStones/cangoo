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

    public class GetRecentLocationResponse
    {
        public List<GetRecentLocationDetails> Locations { get; set; } = new List<GetRecentLocationDetails>();
    }

    public class GetRecentLocationDetails
    {
        public string DropOffLatitude { get; set; } = "";
        public string DropOffLongitude { get; set; } = "";
        public string DropOffLocation { get; set; } = "";
    }
}
