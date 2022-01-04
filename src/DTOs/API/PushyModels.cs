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

	public class PassengerRequestAcceptedNotification : TripPaymentMode
	{
		public string TripId { get; set; }
		public string PickUpLatitude { get; set; }
		public string PickUpLongitude { get; set; }
		public string PickUpLocation { get; set; }
		public string MidwayStop1Latitude { get; set; }
		public string MidwayStop1Longitude { get; set; }
		public string MidwayStop1Location { get; set; }
		public string DropOffLatitude { get; set; }
		public string DropOffLongitude { get; set; }
		public string DropOffLocation { get; set; }
		public string DriverId { get; set; }
		public string DriverName { get; set; }
		public string DriverPicture { get; set; }
		public string DriverRating { get; set; }
		public string DriverContactNumber { get; set; }
		public string VehicleNumber { get; set; }
		public string VehicleCategoryId { get; set; }
		public string Make { get; set; }
		public string Model { get; set; }
		public string Color { get; set; }
		public string SeatingCapacity { get; set; }
		public string VehicleRating { get; set; }
		public string VehicleCategory { get; set; }
		public string FleetAddress { get; set; }
		public string FleetName { get; set; }
		public string IsWeb { get; set; }
		public string IsLaterBooking { get; set; }
		public string IsDispatchedRide { get; set; }
		public string IsReRouteRequest { get; set; }
		public string PickUpDateTime { get; set; }
		public string Description { get; set; }
		public string VoucherCode { get; set; }
		public string VoucherAmount { get; set; }
		public string TotalFare { get; set; }

		public List<PassengerCancelReasonsDTO> CancelReasons = new List<PassengerCancelReasonsDTO>();
		public List<PassengerFacilityDTO> Facilities = new List<PassengerFacilityDTO>();
	}

	public class EndRideFCM
	{
		public string TripId { get; set; }
		public string TripRewardPoints { get; set; }
		public string TotalRewardPoints { get; set; }
		public string DriverName { get; set; }
		public string DriverImage { get; set; }
		public string IsFavorite { get; set; }
		public string TotalFare { get; set; }
		public string BookingDateTime { get; set; }
		public string EndTripDateTime { get; set; }
		public string PickUpLatitude { get; set; }
		public string PickUpLongitude { get; set; }
		public string MidwayStop1Latitude { get; set; }
		public string MidwayStop1Longitude { get; set; }
		public string DropOffLatitude { get; set; }
		public string DropOffLongitude { get; set; }
		public string Distance { get; set; }
		public string Date { get; set; }
		public string Time { get; set; }
		public string PaymentMode { get; set; }
		public string PaymentModeId { get; set; }
	}

	public class CashPaymentNotification
	{
		public string TripId { get; set; }
		public string SelectedTipAmount { get; set; }
		public string DriverId { get; set; }
		public string TotalFare { get; set; }
		public string IsDriverFavorite { get; set; }
		public string PaymentModeId { get; set; }
	}

	public class MobilePaymentNotification
	{
		public string TripId { get; set; }
		public string SelectedTipAmount { get; set; }
		public string PromoDiscountAmount { get; set; }
		public string PaymentModeId { get; set; }
		public string TotalFare { get; set; }
		public string DriverId { get; set; }
		public string IsDriverFavorite { get; set; }
		public string Brand { get; set; }
		public string Last4Digits { get; set; }
		public string WalletBalance { get; set; }
		public string AvailableWalletBalance { get; set; }
	}

	public class TipPaymentNotification
	{
		public string TipAmount { get; set; }
		public string PassengerName { get; set; }
	}

	public class DriverBookingRequestNotification : TripPaymentMode //: DiscountTypeDTO
	{
		public string tripID { get; set; }
		public string lat { get; set; } // To be replaced with pickUplatitude while revamping driver app
		public string lan { get; set; } // To be replaced with pickUplongitude while revamping driver app
		public bool fav { get; set; }
		public bool isWeb { get; set; }
		public string dropOfflatitude { get; set; }
		public string dropOfflongitude { get; set; }
		public bool isLaterBooking { get; set; }
		public int numberOfPerson { get; set; }
		public DateTime pickUpDateTime { get; set; }
		public List<DriverFacilityDTO> facilities { get; set; } = new List<DriverFacilityDTO>();
		public string discountAmount { get; set; }
		public string discountType { get; set; }
		public string isDispatchedRide { get; set; }
		public string dispatcherID { get; set; } = "";
		public bool isReRouteRequest { get; set; }
		public string estimatedPrice { get; set; }

		//Only Aforementioned properties are being consumed from notification in driver app

		public double? requestTimeOut { get; set; }
		public List<DriverCancelReasonsDTO> lstCancel { get; set; } = new List<DriverCancelReasonsDTO>();
		public string description { get; set; }
		public string voucherAmount { get; set; } = "";
		public string voucherCode { get; set; } = "";
		public string requiredFacilities { get; set; }
		public string previousCaptainId { get; set; } = "";
		public string reRouteRequestTime { get; set; } = "";
		public bool isLaterBookingStarted { get; set; }
		public string deviceToken { get; set; } = "";
		public string pickUpLocation { get; set; }
		public string dropOffLocation { get; set; }
		public string paymentMethod { get; set; }
		public string bookingMode { get; set; }
		public string BookingModeId { get; set; }
		public string MidwayStop1Latitude { get; set; }
		public string MidwayStop1Longitude { get; set; }
		public string MidwayStop1Location { get; set; }
	}

	public class DriverCancelRequestNotification
	{
		public string tripID { get; set; }
		public bool isLaterBooking { get; set; }
	}

	public class PassengerCancelRequestNotification
	{
		public string TripId { get; set; }
        public string IsLaterBooking { get; set; }
    }
}