using DTOs.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTOs.API
{
	public class NewDeviceLogInNotification
	{
		public string PassengerId { get; set; }
		public string DeviceToken { get; set; }
	}

	public class PassengerRequestAcceptedNotification
	{
		public string TripId { get; set; }
		public string PickUpLatitude { get; set; }
		public string PickUpLongitude { get; set; }
		public string MidwayStop1Latitude { get; set; }
		public string MidwayStop1Longitude { get; set; }
		public string DropOffLatitude { get; set; }
		public string DropOffLongitude { get; set; }
		public string DriverId { get; set; }
		public string DriverName { get; set; }
		public string DriverPicture { get; set; }
		public string DriverRating { get; set; }
		public string DriverContactNumber { get; set; }
		public string VehicleNumber { get; set; }
		public string Model { get; set; }
		public string Make { get; set; }
		public string VehicleRating { get; set; }
		public string IsWeb { get; set; }
		public string IsLaterBooking { get; set; }
		public string IsDispatchedRide { get; set; }
		public string IsReRouteRequest { get; set; }
		public string SeatingCapacity { get; set; }
		public string LaterBookingPickUpDateTime { get; set; }
		public string Description { get; set; }
		public string VoucherCode { get; set; }
		public string VoucherAmount { get; set; }
		public List<CanclReasonsDTO> lstCancel = new List<CanclReasonsDTO>();

	}

	public class EndRideFCM
	{
		public string tripID { get; set; }
		public string tripRewardPoints { get; set; }
		public string totalRewardPoints { get; set; }
		public string driverName { get; set; }
		public string driverImage { get; set; }
		public bool isFav { get; set; }
		public decimal estimateFare { get; set; }
		public DateTime? bookingDateTime { get; set; }
		public DateTime? endRideDateTime { get; set; }
		public string pickLat { get; set; }
		public string pickLong { get; set; }
		public string dropLat { get; set; }
		public string dropLong { get; set; }
		public string distance { get; set; }
		public string date { get; set; }
		public string time { get; set; }
		public string paymentMode { get; set; }
	}

	public class DriverBookingRequestNotification //: DiscountTypeDTO
	{
		public string tripID { get; set; }
        public string lat { get; set; } // To be replaced with pickUplatitude while revamping driver app
        public string lan { get; set; } // To be replaced with pickUplongitude while revamping driver app
        public string paymentMethod { get; set; }
		public bool fav { get; set; }
		public bool isWeb { get; set; }
		public string dropOfflatitude { get; set; }
		public string dropOfflongitude { get; set; }
		public bool isLaterBooking { get; set; }
		public int numberOfPerson { get; set; }
		public DateTime pickUpDateTime { get; set; }
		public List<FacilitiyDTO> facilities { get; set; }
		public string discountAmount { get; set; }
		public string discountType { get; set; }
		public string isDispatchedRide { get; set; }
		public string dispatcherID { get; set; }
		public bool isReRouteRequest { get; set; }
		public string estimatedPrice { get; set; }
		public string bookingMode { get; set; }

		//Only Aforementioned properties are being consumed from notification in driver app

		public double? requestTimeOut { get; set; }
		public List<CanclReasonsDTO> lstCancel = new List<CanclReasonsDTO>();
		public string description { get; set; }
		public string voucherAmount { get; set; }
		public string voucherCode { get; set; }
		public string requiredFacilities { get; set; }
		public string previousCaptainId { get; set; }
		public string reRouteRequestTime { get; set; }
		public bool isLaterBookingStarted { get; set; }
		public string deviceToken { get; set; }
		public string pickUpLocation { get; set; }
		public string dropOffLocation { get; set; }


        //public string PickUpLatitude { get; set; }
        //public string PickUpLongitude { get; set; }
        //public string PickUpLocation { get; set; }
        public string MidwayStop1Latitude { get; set; }
        public string MidwayStop1Longitude { get; set; }
        public string MidwayStop1Location { get; set; }
        //public string DropOffLatitude { get; set; }
        //public string DropOffLongitude { get; set; }
        //public string DropOffLocation { get; set; }
        //public string IsLaterBooking { get; set; }
        //public string SeatingCapacity { get; set; }
        //public string PickUpDateTime { get; set; }
        //public string TripId { get; set; }
        //public string PaymentMethod { get; set; }
        public string PaymentModeId { get; set; }
        //public string IsDispatchedRide { get; set; }
        //public string IsFavorite { get; set; }
        //public string IsWeb { get; set; }
        //public string Description { get; set; }
        //public string RequiredFacilities { get; set; }
        //public string IsReRouteRequest { get; set; }
        //public string EstimatedPrice { get; set; }
        //public string BookingMode { get; set; }
        public string BookingModeId { get; set; }
        //public string RequestTimeOut { get; set; }
        //public string VoucherAmount { get; set; }
        //public string VoucherCode { get; set; }
        //public string DeviceToken { get; set; }
        //public string ReRouteRequestTime { get; set; }
        //public string PreviousCaptainId { get; set; }
        //public List<CanclReasonsDTO> CancelReasons { get; set; }
        //public List<FacilitiyDTO> Facilities { get; set; }
    }
}