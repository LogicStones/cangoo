﻿using DTOs.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTOs.API
{
	public class DriverStatus
	{
		public string ongoingRide { get; set; } = "";
		public string isBusy { get; set; } = "";
	}

	public class DriverEarnedPoints
	{
		public string earningPoints { get; set; } = "";
	}

	public class UpcomingLaterBooking
	{
		public string tripID { get; set; } = "";
		public string pickUpDateTime { get; set; } = "";
		public int seatingCapacity { get; set; }
		public string pickUplatitude { get; set; } = "";
		public string pickUplongitude { get; set; } = "";
		public string pickUpLocation { get; set; } = "";
		public string dropOfflatitude { get; set; } = "";
		public string dropOfflongitude { get; set; } = "";
		public string dropOffLocation { get; set; } = "";
		public string passengerName { get; set; } = "";
		public bool isSend30MinutSendFCM { get; set; } = false;
		public bool isSend20MinutSendFCM { get; set; } = false;
		public string isWeb { get; set; } = "";
	}

	public class FirebaseDriver
	{
		public string driverID { get; set; }
		public string isBusy { get; set; }
		public string driverFacilities { get; set; }
		public bool isPriorityHoursActive { get; set; }
		public Location location { get; set; }
		public string seatingCapacity { get; set; }
		public string vehicleFacilities { get; set; }
		public string ongoingRide { get; set; }
		public string companyID { get; set; }
		public string userName { get; set; }
		public string driverName { get; set; }
		public string phoneNumber { get; set; }
		public string priorityHourEndTime { get; set; }
		public string earningPoints { get; set; }
		public string priorityHourRemainingTime { get; set; }
		public string onlineSince { get; set; }
		public string lastUpdated { get; set; }
		public string deviceToken { get; set; }
		public int makeID { get; set; }
		public string make { get; set; }
		public int categoryID { get; set; }
		public string category { get; set; }
		public int modelID { get; set; }
		public string model { get; set; }
		public string vehicleID { get; set; }
		public string plateNumber { get; set; }
		public string registrationYear { get; set; }
		public string color { get; set; }
		//public double lat { get; set; }
		//public double lon { get; set; }
		//public string tripID { get; set; }
		//public double dropOfflat { get; set; }
		//public double dropOfflong { get; set; }
		//public long bearing { get; set; }
	}

	public class Location
	{
		public string g { get; set; }
		public List<double> l { get; set; }
	}

	public class PendingLaterBooking
	{
		public string userID { get; set; } = "";
		public string pickupDateTime { get; set; } = "";
		public string numberOfPerson { get; set; } = "";
	}


	public class AcceptRideDriverModel
	{
		public string pickupLocationLatitude { get; set; }
		public string pickupLocationLongitude { get; set; }
		public string midwayStop1LocationLatitude { get; set; }
		public string midwayStop1LocationLongitude { get; set; }
		public string dropOffLocationLatitude { get; set; }
		public string dropOffLocationLongitude { get; set; }
		public string passengerID { get; set; }
		public string passengerName { get; set; }
		public int? minDistance { get; set; }
		public double? requestTime { get; set; }
		public string phone { get; set; }
		public bool isWeb { get; set; }
		public bool isLaterBooking { get; set; }
		public List<CanclReasonsDTO> lstCancel = new List<CanclReasonsDTO>();
		public string tripID { get; set; }
		public string laterBookingPickUpDateTime { get; set; }
		public string isDispatchedRide { get; set; }
		public string distanceTraveled { get; set; }
		public string isReRouteRequest { get; set; }
		public string numberOfPerson { get; set; }
		public string description { get; set; }
		public string voucherCode { get; set; }
		public string voucherAmount { get; set; }
		public string isFareChangePermissionGranted { get; set; }
		public string bookingMode { get; set; }
	}

	public class ArrivedDriverRideModel
	{
		public string passengerName { get; set; }
		public double? passengerRating { get; set; }
		public string dropOffLatitude { get; set; }
		public string dropOffLongitude { get; set; }
		public string passenger_Pic { get; set; }
		public string bookingMode { get; set; }
		public string arrivalTime { get; set; }
	}

	public class startDriverRideModel
	{
		public string dropOffLatitude { get; set; }
		public string dropOffLongitude { get; set; }
		public string bookingMode { get; set; }
	}

	public class EndDriverRideModel
	{
		public string waitingCharges { get; set; }
		public string baseCharges { get; set; }
		public string bookingCharges { get; set; }
		public string travelCharges { get; set; }
		public string paymentMethod { get; set; }
		public string distance { get; set; }
		public double? duration { get; set; }
		public bool isPaymentRequested { get; set; }
		public bool isFavUser { get; set; }

		public string estimatedPrice { get; set; }
		public string discountType { get; set; }
		public string discountAmount { get; set; }
		public string availableWalletBalance { get; set; }
		public string isWalletPreferred { get; set; }
		public string isVoucherApplied { get; set; }
		public string voucherCode { get; set; }
		public string voucherAmount { get; set; }
		public bool isUserProfileUpdated { get; set; }
		public bool isFareChangePermissionGranted { get; set; }

		public string InBoundDistanceInMeters { get; set; }
		public string InBoundTimeInSeconds { get; set; }
		public string OutBoundDistanceInMeters { get; set; }
		public string OutBoundTimeInSeconds { get; set; }
		public string OutBoundTimeFare { get; set; }
		public string OutBoundDistanceFare { get; set; }
		public string InBoundTimeFare { get; set; }
		public string InBoundDistanceFare { get; set; }
		public string InBoundSurchargeAmount { get; set; }
		public string OutBoundSurchargeAmount { get; set; }
		public string bookingMode { get; set; }
	}

	public class PaymentPendingPassenger
	{
		public bool isPaymentRequested { get; set; }
		public string PaymentMode { get; set; }
		public bool isFareChangePermissionGranted { get; set; }
	}

	//public class RequestResponse
	//{
	//    public string PickUpLatitude { get; set; }
	//    public string PickUpLongitude { get; set; }
	//    public string PickUpLocation { get; set; }
	//    public string DropOffLatitude { get; set; }
	//    public string DropOffLongitude { get; set; }
	//    public string DropOffLocation { get; set; }
	//    public bool IsLaterBooking { get; set; }
	//    public int NumberOfPerson { get; set; }
	//    public string PickUpDateTime { get; set; }
	//    public string TripId { get; set; }
	//    public string PaymentMethod { get; set; }
	//    public string PaymentMethodId { get; set; }
	//    public string IsDispatchedRide { get; set; }
	//    public bool IsFavorite { get; set; }
	//    public bool IsWeb { get; set; }
	//    public string Description { get; set; }
	//    public string RequiredFacilities { get; set; }
	//    public List<FacilitiyDTO> Facilities { get; set; }
	//    public string DiscountType { get; set; }
	//    public string DiscountAmount { get; set; }
	//    public bool IsReRouteRequest { get; set; }
	//    public string EstimatedPrice { get; set; }
	//    public string BookingMode { get; set; }
	//    public string BookingModeId { get; set; }
	//    public string DispatcherID { get; set; }
	//}

	public class WalkInTrip
	{
		public string newTripID { get; set; }
	}

	public class LocationUpdate
	{
		public string latitude { get; set; }
		public string longitude { get; set; }
		public long locationTime { get; set; }
	}
}