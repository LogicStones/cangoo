using DTOs.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DTOs.API
{
    public class DatabaseOlineDriversDTO
    {
        public Guid CaptainID { get; set; }
        public string Name { get; set; }
        public bool? IsPriorityHoursActive { get; set; }
        public string DeviceToken { get; set; }
        public double? Rating { get; set; }
        public string LaterBookingNotificationTone { get; set; }
        public string NormalBookingNotificationTone { get; set; }
    }

    public class PassengerRequest
    {
        public string pID { get; set; }
        public string resellerID { get; set; }
        public string resellerArea { get; set; }
        public string fleetID { get; set; }
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string email { get; set; }
        public string countryCode { get; set; }
        public string phoneNumber { get; set; }
        public string password { get; set; }
        public string deviceToken { get; set; }
        public string verificationCode { get; set; }
        public string oldPassword { get; set; }
        public string pickUplatitude { get; set; }
        public string pickUplongitude { get; set; }
        public string pickUpLocation { get; set; }
        public string dropOfflatitude { get; set; }
        public string dropOfflongitude { get; set; }
        public string dropOffLocation { get; set; }
        public string routePolyLine { get; set; }
        public string inBoundDistanceInKM { get; set; }
        public string inBoundDistanceFare { get; set; }
        public string inBoundTimeInMinutes { get; set; }
        public string inBoundTimeFare { get; set; }
        public string outBoundDistanceInKM { get; set; }
        public string outBoundDistanceFare { get; set; }
        public string outBoundTimeInMinutes { get; set; }
        public string outBoundTimeFare { get; set; }
        public string inBoundSurchargeAmount { get; set; }
        public string outBoundSurchargeAmount { get; set; }
        public string inBoundBaseFare { get; set; }
        public string outBoundBaseFare { get; set; }
        public string totalFare { get; set; }
        public string seatingCapacity { get; set; }
        public string selectedPaymentMethod { get; set; }
        public string estimatedDistance { get; set; }
        public bool isWallet { get; set; }
        public string tripID { get; set; }
        public string bookingModeId { get; set; }
        public string karhooTripID { get; set; }
        public string newTripID { get; set; }
        public string paypalTransactionID { get; set; }
        public bool isFav { get; set; }
        public string driverRating { get; set; }
        public string vehicleRating { get; set; }
        public int additionalFeedbackID { get; set; }
        public bool isLaterBooking { get; set; }
        public string laterBookingDate { get; set; }
        public bool isReRouteRequest { get; set; }
        public int cancelID { get; set; }
        public string description { get; set; }
        public string driverID { get; set; }
        public string vehicleID { get; set; }
        public string timeZoneOffset { get; set; }
        public string isOverride { get; set; }
        public string currency { get; set; }
        public string customerID { get; set; }
        public string paymentAmount { get; set; }
        public string paymentTip { get; set; }
        public string promoDiscountAmount { get; set; }
        public string promoCodeID { get; set; }
        public string walletUsedAmount { get; set; }
        public string voucherUsedAmount { get; set; }
        public string voucherAmount { get; set; }
        public string voucherCode { get; set; }
        public string bookingTypeID { get; set; }
        public string isWeb { get; set; }
        public string isDispatchedRide { get; set; }
        public string dispatcherID { get; set; }
        public string distance { get; set; }
        public string requiredFacilities { get; set; }
        public string discountType { get; set; }
        public string fixedFare { get; set; }
        public int tripStatusID { get; set; }
        public bool isBrainTree { get; set; }
        public bool isFareChangePermissionGranted { get; set; }
    }
   
    public class RequestModel
    {
        public string resellerID { get; set; }
        public string fleetID { get; set; }
        public string tripID { get; set; }
        public double lat { get; set; }
        public double lon { get; set; }
        public bool isAccept { get; set; }
        public bool isReRouteRequest { get; set; }
        public string driverID { get; set; }
        public string userID { get; set; }
        public string walkInOldUserID { get; set; }
        public string reDateTime { get; set; }
        public double customerRating { get; set; }
        public string duration { get; set; }
        public string distance { get; set; }
        public string paymentMode { get; set; }
        public string resellerArea { get; set; }
        public string vehicleID { get; set; }
        public string phoneNumber { get; set; }
        public int cancelID { get; set; }
        public int bookingModeID { get; set; }
        public bool passengerFav { get; set; }
        public bool isWeb { get; set; }
        public string pickUplatitude { get; set; }
        public string pickUplongitude { get; set; }
        public string dropOfflatitude { get; set; }
        public string dropOfflongitude { get; set; }
        public string dropOffLocation { get; set; }
        public DateTime pickUpDateTime { get; set; }
        public bool isCheckLaterBookingConflict { get; set; }
        public bool isLaterBooking { get; set; }
        public string timeZoneOffset { get; set; }
        public string distanceToPickUpLocation { get; set; }
        public string isAtPickupLocation { get; set; }
        public string estimatedFare { get; set; }
        public string discountType { get; set; }
        public string isOverride { get; set; }
        public string isAtDropOffLocation { get; set; }
        public string collectedAmount { get; set; }
        public string promoDiscountAmount { get; set; }
        public string walletUsedAmount { get; set; }
        public string walletTotalAmount { get; set; }
        public string voucherUsedAmount { get; set; }
        public string totalFare { get; set; }
        public string tipAmount { get; set; }
        public string isDispatchedRide { get; set; }
        public string dispatcherID { get; set; }
        public string description { get; set; }
        public string passengerName { get; set; }
        public bool isWalkIn { get; set; }
    }

    public class VehicleDetail
    {
        public Boolean isBooked { get; set; }
        public string vehicleID { get; set; }
        public string driverID { get; set; }
        public double driverRating { get; set; }
        public string driverName { get; set; }
        public Nullable<bool> isPriorityHoursActive { get; set; }
        public string priorityHourEndTime { get; set; }
        public string earningPoints { get; set; }
        public string make { get; set; }
        public string model { get; set; }
        public string number { get; set; }
        public string seatingCapacity { get; set; }
        public string DeviceToken { get; set; }
    }

    public class InvoiceModel
    {
        public string FleetEmail { get; set; }
        public string CustomerEmail { get; set; }
        public string TripDate { get; set; }
        public string CustomerName { get; set; }
        public string InvoiceNumber { get; set; }
        public string FleetName { get; set; }
        public string ATUNumber { get; set; }
        public string PostCode { get; set; }
        public string City { get; set; }
        public string Street { get; set; }
        public string BuildingNumber { get; set; }
        public string CaptainName { get; set; }
        public string CaptainUserName { get; set; }
        public string VehicleNumber { get; set; }
        public string PickUpAddress { get; set; }
        public string DropOffAddress { get; set; }
        public string Distance { get; set; }
        public string TotalAmount { get; set; }
        public string PromoDiscountAmount { get; set; }
        public string WalletUsedAmount { get; set; }
        public string CashAmount { get; set; }
    }

    public class DriverModel
	{
		public string userID { get; set; }
		public string resellerID { get; set; }
		public string applicationID { get; set; }
		public string fleetID { get; set; }
		public string phone { get; set; }
        public string verificationCode { get; set; }
        public string password { get; set; }
        public string oldPassword { get; set; }
		public string Name { get; set; }
		public string UserName { get; set; }
		public string EarningPoints { get; set; }
        public string LastLogin { get; set; }
        public Nullable<bool> IsPriorityHoursActive { get; set; }
        public string Email { get; set; }
        public string TotalOnlineHours { get; set; }
        public string MemberSince { get; set; }
        public string ShareCode { get; set; }
        public string NumberOfFavoriteUser { get; set; }
        public string access_Token { get; set; }
        public string Picture { get; set; }
        public string DrivingLicense { get; set; }
        public string DeviceToken { get; set; }
        public double? rating { get; set; }
        public decimal? totalEarnings { get; set; }
        public int? totalRides { get; set; }
        public int offset { get; set; }
        public int limit { get; set; }
        public string priorityHourEndTime { get; set; }
        public string dateFrom { get; set; }
        public string dateTo { get; set; }
        public string vehicleID { get; set; }
        public string normalBookingNotificationTone { get; set; }
		public string laterBookingNotificationTone { get; set; }
        public string showOtherVehicles { get; set; }
        public bool isAlreadyInTrip { get; set; }
        public dynamic vehicleDetails { get; set; }
    }

    public class OnlineCaptainVehicleDetails
    {
        public Guid VehicleID { get; set; }
        public string PlateNumber { get; set; }
        public Nullable<bool> isActive { get; set; }
        public bool isOccupied { get; set; }
        public string Model { get; set; }
        public string Make { get; set; }
        public Nullable<int> SeatingCapacity { get; set; }
        public Nullable<int> OccupiedBy { get; set; }
    }

    public class CaptainProfile
    {
        public string name { get; set; }
        public string email { get; set; }
        public string phone { get; set; }
        public string shareCode { get; set; }
        public List<FacilitiyDTO> captainFacilitiesList { get; set; }
        public string make { get; set; }
        public string model { get; set; }
        public string number { get; set; }
        public string seatingCapacity { get; set; }
        public List<FacilitiyDTO> vehicleFacilitiesList { get; set; }
    }

    public class CaptainStats
    {
        public string cashRides { get; set; }
        public string mobilePayRides { get; set; }
        public string captainRating { get; set; }
        public string vehicleRating { get; set; }
        public string cashEarning { get; set; }
        public string mobilePayEarning { get; set; }
        public string favPassengers { get; set; }
        public string memberSince { get; set; }
        public string avgMobilePayEarning { get; set; }
        public string avgCashEarning { get; set; }
        public string currentMonthOnlineHours { get; set; }
        public string currentMonthAcceptanceRate { get; set; }
    }
    
    public class CaptainSettings
    {
        public string requestRadius { get; set; }
        public string showOtherVehicles { get; set; }
        public string normalBookingNotificationTone { get; set; }
        public string laterBookingNotificationTone { get; set; }
        public string captainID { get; set; }
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
    
    public class LaterBookingConflict
    {
        public string pickUpDateTime { get; set; }
        public bool isConflict { get; set; }
    }
   
    public class PriorityHour
    {
        public string captainID { get; set; }
        public int duration { get; set; }
    }
    
    public class DriverTripsHistory
    {
        public string avgDriverRating { get; set; }
        public string avgVehicleRating { get; set; }
        public string totalTrips { get; set; }
        public string totalFare { get; set; }
        public string totalMobilePayEarning { get; set; }
        public string totalCashEarning { get; set; }
        public string totalTip { get; set; }
        public string totalEarnedPoints { get; set; }
        public List<DriverTrips> trips { get; set; }
    }
    
    public class DriverTrips
    {
        public string tripID { get; set; }
        public string pickupLocationLatitude { get; set; }
        public string pickupLocationLongitude { get; set; }
        public string pickupLocation { get; set; }
        public string dropOffLocationLatitude { get; set; }
        public string dropOffLocationLongitude { get; set; }
        public string dropOffLocation { get; set; }
        public string cashPayment { get; set; }
        public string mobilePayment { get; set; }
        public string fare { get; set; }
        public string tip { get; set; }
        public string bookingDateTime { get; set; }
        public string pickUpBookingDateTime { get; set; }
        public string tripArrivalDatetime { get; set; }
        public string tripStartDatetime { get; set; }
        public string tripEndDatetime { get; set; }
        public string distanceTraveled { get; set; }
        public string driverEarnedPoints { get; set; }
        public string driverRating { get; set; }
        public string vehicleRating { get; set; }
        public string passengerName { get; set; }
        public string tripStatus { get; set; }
        public string bookingType { get; set; }
        public string bookingMode { get; set; }
        public string plateNumber { get; set; }
        public string model { get; set; }
        public string make { get; set; }
		public string paymentMode { get; set; }
		public List<FacilitiyDTO> facilities { get; set; }
	}
}