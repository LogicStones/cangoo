using Constants;
using DTOs.Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace DTOs.API
{
    public class GetDriverEarnedPointsRequest
    {
        [Required]
        [DefaultValue("")]
        public string captainID { get; set; }
    }

    public class GetAgreementsRequest
    {
        [Required]
        [DefaultValue("")]
        public string agreementTypeId { get; set; }
    }

    public class GetDriverProfileRequest
    {
        [Required]
        [DefaultValue("")]
        public string captainID { get; set; }

        //[Required]
        [DefaultValue("")]
        public string vehicleID { get; set; }
    }

            //    if (model != null && !string.IsNullOrEmpty(model.isOverride) && model.driverID != string.Empty && model.estimatedFare != string.Empty &&
            //model.duration != string.Empty && model.distance != string.Empty && model.tripID != string.Empty && model.fleetID != string.Empty &&
            //model.paymentMode != string.Empty && model.vehicleID != string.Empty && !string.IsNullOrEmpty(model.walletUsedAmount) &&
            //!string.IsNullOrEmpty(model.walletTotalAmount) && !string.IsNullOrEmpty(model.voucherUsedAmount) &&
            //!string.IsNullOrEmpty(model.promoDiscountAmount) && !string.IsNullOrEmpty(model.totalFare))

    public class CreditCardPaymentRequest
    {
        [Required]
        [DefaultValue("e5cece85-c6b7-4c37-bf02-573b4b3607e5")]
        public string fleetID { get; set; }
      
        [Required]
        [DefaultValue("")]
        public string tripID { get; set; }
       
        [Required]
        [DefaultValue("02179f6-167e-4cd9-9ce8-701b92771f97")]
        public string driverID { get; set; }
              
        [Required]
        [DefaultValue("dcd1d468-e032-49d4-9711-02715b3c4c5e")]
        public string vehicleID { get; set; }

        //[Required]
        //[DefaultValue("false")]
        //public string isOverride { get; set; }

        //[Required]
        //[DefaultValue("120")]
        //public string walletTotalAmount { get; set; }

        //[Required]
        //[DefaultValue("0")]
        //public string voucherUsedAmount { get; set; }

        //[Required]
        //[DefaultValue("")]
        //public string duration { get; set; }

        //[Required]
        //[DefaultValue("")]
        //public string distance { get; set; }

        //[Required]
        //[DefaultValue("CreditCard")]
        //public string paymentMode { get; set; }
        //public string PaymentModeId { get; set; }

        [Required]
        [DefaultValue("0")]
        public string promoDiscountAmount { get; set; }
       
        [Required]
        [DefaultValue("0")]
        public string walletUsedAmount { get; set; }
       
        [Required]
        [DefaultValue("100")]
        public string totalFare { get; set; }

        [DefaultValue("20")]
        public string tipAmount { get; set; }
    }

    public class GetDriverSettingsRequest
    {
        [Required]
        [DefaultValue("")]
        public string captainID { get; set; }
    }


    public class GetDriverStatsRequest
    {
        [Required]
        [DefaultValue("")]
        public string captainID { get; set; }

        [Required]
        [DefaultValue("")]
        public string vehicleID { get; set; }
    }

    public class GetDriverPendingLaterBookingsRequest
    {
        [Required]
        [DefaultValue("0")]
        public int offset { get; set; }
        
        [Required]
        [DefaultValue("10")]
        public int limit { get; set; }

        [Required]
        [DefaultValue("")]
        public int vehicleSeatingCapacity { get; set; }
    }

    public class GetDriverBookingHistoryRequest
    {
        [Required]
        [DefaultValue("")]
        public string captainID { get; set; }

        [Required]
        [DefaultValue("")]
        public string pageNo { get; set; }

        [Required]
        [DefaultValue("")]
        public string pageSize { get; set; }

        [Required]
        [DefaultValue("")]
        public string dateTo { get; set; }

        [Required]
        [DefaultValue("")]
        public string dateFrom { get; set; }
    }
    
    public class DriverGetUpComingBookingsRequest
    {
        [Required]
        [DefaultValue("")]
        public string userID { get; set; }

        [Required]
        [DefaultValue("")]
        public int offset { get; set; }
        
        [Required]
        [DefaultValue("")]
        public int limit { get; set; }
    }
    
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

    public class LaterBookingConflictDTO
    {
        public string pickUpDateTime { get; set; }
        public bool isConflict { get; set; }
    }

    public class DriverAcceptLaterBookingRequest
    {
        [Required]
        [DefaultValue("")]
        public string tripID { get; set; }

        [Required]
        [DefaultValue("")]
        public string driverID { get; set; }

        [DefaultValue("2021-11-22 14:44:22")]
        public DateTime pickUpDateTime { get; set; }

        [DefaultValue("False")]
        public bool isCheckLaterBookingConflict { get; set; }
    }

    public class DriverAcceptTripRequest
    {
        [Required]
        [DefaultValue("")]
        public string tripID { get; set; }
        
        [Required]
        [DefaultValue("902179f6-167e-4cd9-9ce8-701b92771f97")]
        public string driverID { get; set; }
        
        [Required]
        [DefaultValue("dcd1d468-e032-49d4-9711-02715b3c4c5e")]
        public string vehicleID { get; set; }
        
        [Required]
        [DefaultValue(false)]
        public bool isAccept { get; set; }
        
        [DefaultValue(false)]
        public bool isReRouteRequest { get; set; }

        [DefaultValue(false)]
        public bool isLaterBooking { get; set; }
                
        [DefaultValue(false)]
        public bool isWeb { get; set; }

        [DefaultValue("False")]
        public string isDispatchedRide { get; set; }

        [DefaultValue("")]
        public string dispatcherID { get; set; }

        [DefaultValue("123")]
        public string distanceToPickUpLocation { get; set; }
    }

    public class DriverCancelTripRequest
    {
        [Required]
        [DefaultValue("")]
        public string tripID { get; set; }
        
        [Required]
        [DefaultValue("902179f6-167e-4cd9-9ce8-701b92771f97")]
        public string driverID { get; set; }
        
        [Required]
        [DefaultValue(4)]
        public int cancelID { get; set; }
        
        [Required]
        [DefaultValue("True")]
        public string isAtPickupLocation { get; set; }
        
        [Required]
        [DefaultValue("[{\"lat\":68.3908316470314,\"lng\":-169.25109883759296},{\"lat\":38.39278982958803,\"lng\":171.06140116240704},{\"lat\":-57.243772723990084,\"lng\":154.71374491240704},{\"lat\":-33.0092509544725,\"lng\":66.64733866240704},{\"lat\":-41.525554039286014,\"lng\":13.912963662407037},{\"lat\":-45.549637304437304,\"lng\":-18.423294077947503},{\"lat\":-52.710099753168834,\"lng\":-100.16906758759296},{\"lat\":56.546179852988224,\"lng\":-176.950561665387},{\"lat\":82.84011158544916,\"lng\":-53.90546558717142},{\"lat\":82.53196871665712,\"lng\":5.8348478588152375},{\"lat\":82.76804348272478,\"lng\":88.83966180216396}]")]
        public string resellerArea { get; set; }
        
        [DefaultValue(false)]
        public bool isWeb { get; set; }

        [DefaultValue(false)]
        public bool isLaterBooking { get; set; }


        //Following params are set when cron job cancels and reroutes upcoming later booking


        [DefaultValue("73BABE98-3CA1-49E0-BE0A-1638B154762D")]
        public string resellerID { get; set; }

        [DefaultValue("18000")]
        public string timeZoneOffset { get; set; }

        [DefaultValue("False")]
        public string isDispatchedRide { get; set; }

        [DefaultValue(false)]
        public bool isReRouteRequest { get; set; }
    }
    
    public class DriverArrivedRequest
    {
        [Required]
        [DefaultValue("")]
        public string tripID { get; set; }
        [Required]
        [DefaultValue("")]
        public string driverID { get; set; }
        [DefaultValue("False")]
        public bool isWeb { get; set; }
        [DefaultValue("123")]
        public string distanceToPickUpLocation { get; set; }
    }

    public class DriverStartTripRequest
    {
        [Required]
        [DefaultValue("")]
        public string tripID { get; set; }
        [Required]
        [DefaultValue("")]
        public string driverID { get; set; }
        [DefaultValue("False")]
        public bool isWeb { get; set; }
    }

    public class DriverEndTripRequest
    {
        [Required]
        [DefaultValue("9565D981-E772-4D13-B7FD-7A04E460B406")]
        public string tripID { get; set; }

        [Required]
        [DefaultValue("902179f6-167e-4cd9-9ce8-701b92771f97")]
        public string driverID { get; set; }

        [Required]
        [DefaultValue("73BABE98-3CA1-49E0-BE0A-1638B154762D")]
        public string resellerID { get; set; }

        [Required]
        [DefaultValue("[{\"lat\":68.3908316470314,\"lng\":-169.25109883759296},{\"lat\":38.39278982958803,\"lng\":171.06140116240704},{\"lat\":-57.243772723990084,\"lng\":154.71374491240704},{\"lat\":-33.0092509544725,\"lng\":66.64733866240704},{\"lat\":-41.525554039286014,\"lng\":13.912963662407037},{\"lat\":-45.549637304437304,\"lng\":-18.423294077947503},{\"lat\":-52.710099753168834,\"lng\":-100.16906758759296},{\"lat\":56.546179852988224,\"lng\":-176.950561665387},{\"lat\":82.84011158544916,\"lng\":-53.90546558717142},{\"lat\":82.53196871665712,\"lng\":5.8348478588152375},{\"lat\":82.76804348272478,\"lng\":88.83966180216396}]")]
        public string resellerArea { get; set; }

        [Required]
        [DefaultValue("9673")]
        public string distance { get; set; }

        [Required]
        [DefaultValue("True")]
        public string isAtDropOffLocation { get; set; }

        [Required]
        [DefaultValue("BLA BLA BLA")]
        public string dropOffLocation { get; set; }

        [DefaultValue(32.1374413236167)]
        public double lat { get; set; }
        
        [DefaultValue(74.2070284762054)]
        public double lon { get; set; }

        [DefaultValue(false)]
        public bool isWeb { get; set; }

        [DefaultValue(false)]
        public bool isLaterBooking { get; set; }
    }

    public class DriverEndTripResponse
    {
        public string discountType { get; set; } = Enum.GetName(typeof(DiscountTypes), (int)DiscountTypes.Normal).ToLower();
        public string discountAmount { get; set; } = "0.00";
        public string pickUpFareManagerID { get; set; } = "0.00";
        public string dropOffFareMangerID { get; set; } = "0.00";
        public string inBoundDistanceInKM { get; set; } = "0.00";
        public string inBoundDistanceFare { get; set; } = "0.00";
        public string inBoundTimeInMinutes { get; set; } = "0.00";
        public string inBoundTimeFare { get; set; } = "0.00";
        public string outBoundDistanceInKM { get; set; } = "0.00";
        public string outBoundDistanceFare { get; set; } = "0.00";
        public string outBoundTimeInMinutes { get; set; } = "0.00";
        public string outBoundTimeFare { get; set; } = "0.00";
        public string inBoundSurchargeAmount { get; set; } = "0.00";
        public string outBoundSurchargeAmount { get; set; } = "0.00";
        public string inBoundBaseFare { get; set; } = "0.00";
        public string outBoundBaseFare { get; set; } = "0.00";
        public string polyLine { get; set; } = "";
        public string estimatedPrice { get; set; } = "0.00";
        public string passengerName { get; set; } = "";

        public string travelCharges { get; set; } = "0.00";
        public string waitingCharges { get; set; } = "0.00";
        public string bookingCharges { get; set; } = "0.00";
        public string baseCharges { get; set; } = "0.00";

        public string paymentMethod { get; set; } =  "";
        public string distance { get; set; } =  "0.00";
        
        public double? duration { get; set; } =  0;
        public bool isFavUser { get; set; } =  false;
        
        public string isVoucherApplied { get; set; } =  "false";
        public string voucherAmount { get; set; } = "0.00";
        public string voucherCode { get; set; } = "";

        public bool isUserProfileUpdated { get; set; } = true;
        public bool isFareChangePermissionGranted { get; set; } =  false;
        public string bookingMode { get; set; } =  "";
        public bool isWeb { get; set; } =  false;

        //public bool isPaymentRequested { get; set; }
        public string availableWalletBalance { get; set; } = "0.00";
        public bool? isWalletPreferred { get; set; } = false;

        //public string InBoundDistanceInMeters { get; set; }
        //public string InBoundTimeInSeconds { get; set; }
        //public string OutBoundDistanceInMeters { get; set; }
        //public string OutBoundTimeInSeconds { get; set; }
        //public string OutBoundTimeFare { get; set; }
        //public string OutBoundDistanceFare { get; set; }
        //public string InBoundTimeFare { get; set; }
        //public string InBoundDistanceFare { get; set; }
        //public string InBoundSurchargeAmount { get; set; }
        //public string OutBoundSurchargeAmount { get; set; }

    }

    public class CollectPaymentRequest
    {
        [Required]
        [DefaultValue("")]
        public string tripID { get; set; }

        [Required]
        [DefaultValue("902179f6-167e-4cd9-9ce8-701b92771f97")]
        public string driverID { get; set; }

        [Required]
        [DefaultValue("e5cece85-c6b7-4c37-bf02-573b4b3607e5")]
        public string fleetID { get; set; }

        //[Required]
        [DefaultValue("edf49e84-06fb-4a6d-9448-6011fc1bc611")]
        public string userID { get; set; }

        [Required]
        [DefaultValue("False")]
        public string isOverride { get; set; }

        [Required]
        [DefaultValue("22.12")]
        public string collectedAmount { get; set; }

        [Required]
        [DefaultValue("0.00")]
        public string promoDiscountAmount { get; set; }

        [Required]
        [DefaultValue("0.00")]
        public string walletUsedAmount { get; set; }

        [Required]
        [DefaultValue("0.00")]
        public string voucherUsedAmount { get; set; }

        [Required]
        [DefaultValue("45.65")]
        public string totalFare { get; set; }

        [Required]
        [DefaultValue("4.00")]
        public string tipAmount { get; set; }
    }
    
    public class PassengerFavUnFavRequest
    {
        [Required]
        [DefaultValue("")]
        public string tripID { get; set; }

        [Required]
        [DefaultValue("902179f6-167e-4cd9-9ce8-701b92771f97")]
        public string driverID { get; set; }
    }

    public class PassengerRatingRequest
    {
        [Required]
        [DefaultValue("")]
        public string tripID { get; set; }
        
        [Required]
        [DefaultValue("902179f6-167e-4cd9-9ce8-701b92771f97")]
        public string driverID { get; set; }
        
        [Required]
        [DefaultValue(5.0)]
        public double customerRating { get; set; }
        
        [DefaultValue("Farig bnda tha, fzool !! Lekin ! Dil ka acha tha BC :D")]
        public string description { get; set; }
    }
    
    public class DriverTripsDTO
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
        public List<DriverFacilityDTO> facilities { get; set; }
    }

    public class CaptainProfileResponse
    {
        public string name { get; set; }
        public string email { get; set; }
        public string phone { get; set; }
        public string shareCode { get; set; }
        public List<DriverFacilityDTO> captainFacilitiesList { get; set; }
        public string make { get; set; }
        public string model { get; set; }
        public string number { get; set; }
        public string seatingCapacity { get; set; }
        public List<DriverFacilityDTO> vehicleFacilitiesList { get; set; }
    }

    public class CaptainStatsResponse
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

    public class SaveCaptainSettingsRequest : CaptainSettingsDTO
    {
    }

    public class CaptainSettingsDTO
    {
        //[Required]
        [DefaultValue("")]
        public string requestRadius { get; set; }

        //[Required]
        [DefaultValue("")]
        public string showOtherVehicles { get; set; }

        //[Required]
        [DefaultValue("")]
        public string normalBookingNotificationTone { get; set; }

        //[Required]
        [DefaultValue("")]
        public string laterBookingNotificationTone { get; set; }

        //[Required]
        [DefaultValue("")]
        public string captainID { get; set; }
    }

    public class ActivatePriorityHourRequest
    {
        [Required]
        [DefaultValue("")]
        public string captainID { get; set; }

        [Required]
        [DefaultValue("")]
        public int duration { get; set; }
    }

    public class DriverTripsHistoryResponse
    {
        public string avgDriverRating { get; set; }
        public string avgVehicleRating { get; set; }
        public string totalTrips { get; set; }
        public string totalFare { get; set; }
        public string totalMobilePayEarning { get; set; }
        public string totalCashEarning { get; set; }
        public string totalTip { get; set; }
        public string totalEarnedPoints { get; set; }
        public List<DriverTripsDTO> trips { get; set; }
    }
    
    public class ScheduleBookingResponse
    {
        public string tripID { get; set; }
        public string pickUpDateTime { get; set; }
        public string pickUplatitude { get; set; }
        public string pickUplongitude { get; set; }
        public string pickUpLocation { get; set; }
        public string dropOfflatitude { get; set; }
        public string dropOfflongitude { get; set; }
        public string dropOffLocation { get; set; }
        public int seatingCapacity { get; set; }
        public string passengerName { get; set; }
        public string rating { get; set; }
        public string passengerID { get; set; }
        public bool isLaterBooking { get; set; }
        public string tripPaymentMode { get; set; }
        public bool isFav { get; set; }
        public string estimatedDistance { get; set; }
        public string isWeb { get; set; }
        public List<DriverFacilityDTO> facilities { get; set; }
        public string discountType { get; set; }
        public string discountAmount { get; set; }
        public double remainingTime { get; set; }
    }
    
    public class AgreementTypeResponse
    {
        public int TypeId { get; set; }
        public string Name { get; set; }
    }

    public class AgreementResponse
    {
        public int AgreementId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
    }

    public class FAQResponse
    {
        public int FaqId { get; set; }
        public string Question { get; set; }
        public string Answer { get; set; }
    }

    public class NewsFeedResponse
    {
        public Guid FeedId { get; set; }
        public string ShortDescrption { get; set; }
        public string Detail { get; set; }
    }

    //public class LaterBooking
    //{
    //    public string tripID { get; set; }
    //    public string pickUpDateTime { get; set; }
    //    public int seatingCapacity { get; set; }
    //    public string pickUplatitude { get; set; }
    //    public string pickUplongitude { get; set; }
    //    public string pickUpLocation { get; set; }
    //    public string dropOfflatitude { get; set; }
    //    public string dropOfflongitude { get; set; }
    //    public string dropOffLocation { get; set; }
    //    public string passengerName { get; set; }
    //    public bool isSend30MinutSendFCM { get; set; }
    //    public bool isSend20MinutSendFCM { get; set; }
    //    public string userID { get; set; }
    //    public string isWeb { get; set; }
    //}
    //public class PassengerRequest
    //{
    //    public string pID { get; set; }
    //    public string resellerID { get; set; }
    //    public string resellerArea { get; set; }
    //    public string fleetID { get; set; }
    //    public string firstName { get; set; }
    //    public string lastName { get; set; }
    //    public string email { get; set; }
    //    public string countryCode { get; set; }
    //    public string phoneNumber { get; set; }
    //    public string password { get; set; }
    //    public string deviceToken { get; set; }
    //    public string verificationCode { get; set; }
    //    public string oldPassword { get; set; }
    //    public string pickUplatitude { get; set; }
    //    public string pickUplongitude { get; set; }
    //    public string pickUpLocation { get; set; }
    //    public string dropOfflatitude { get; set; }
    //    public string dropOfflongitude { get; set; }
    //    public string dropOffLocation { get; set; }
    //    public string routePolyLine { get; set; }
    //    public string inBoundDistanceInKM { get; set; }
    //    public string inBoundDistanceFare { get; set; }
    //    public string inBoundTimeInMinutes { get; set; }
    //    public string inBoundTimeFare { get; set; }
    //    public string outBoundDistanceInKM { get; set; }
    //    public string outBoundDistanceFare { get; set; }
    //    public string outBoundTimeInMinutes { get; set; }
    //    public string outBoundTimeFare { get; set; }
    //    public string inBoundSurchargeAmount { get; set; }
    //    public string outBoundSurchargeAmount { get; set; }
    //    public string inBoundBaseFare { get; set; }
    //    public string outBoundBaseFare { get; set; }
    //    public string totalFare { get; set; }
    //    public string seatingCapacity { get; set; }
    //    public string selectedPaymentMethod { get; set; }
    //    public string estimatedDistance { get; set; }
    //    public bool isWallet { get; set; }
    //    public string tripID { get; set; }
    //    public string bookingModeId { get; set; }
    //    public string karhooTripID { get; set; }
    //    public string newTripID { get; set; }
    //    public string paypalTransactionID { get; set; }
    //    public bool isFav { get; set; }
    //    public string driverRating { get; set; }
    //    public string vehicleRating { get; set; }
    //    public int additionalFeedbackID { get; set; }
    //    public bool isLaterBooking { get; set; }
    //    public string laterBookingDate { get; set; }
    //    public bool isReRouteRequest { get; set; }
    //    public int cancelID { get; set; }
    //    public string description { get; set; }
    //    public string driverID { get; set; }
    //    public string vehicleID { get; set; }
    //    public string timeZoneOffset { get; set; }
    //    public string isOverride { get; set; }
    //    public string currency { get; set; }
    //    public string customerID { get; set; }
    //    public string paymentAmount { get; set; }
    //    public string paymentTip { get; set; }
    //    public string promoDiscountAmount { get; set; }
    //    public string promoCodeID { get; set; }
    //    public string walletUsedAmount { get; set; }
    //    public string voucherUsedAmount { get; set; }
    //    public string voucherAmount { get; set; }
    //    public string voucherCode { get; set; }
    //    public string bookingTypeID { get; set; }
    //    public string isWeb { get; set; }
    //    public string isDispatchedRide { get; set; }
    //    public string dispatcherID { get; set; }
    //    public string distance { get; set; }
    //    public string requiredFacilities { get; set; }
    //    public string discountType { get; set; }
    //    public string fixedFare { get; set; }
    //    public int tripStatusID { get; set; }
    //    public bool isBrainTree { get; set; }
    //    public bool isFareChangePermissionGranted { get; set; }
    //}

    //public class OnlineCaptainVehicleDetails
    //{
    //    public Guid VehicleID { get; set; }
    //    public string PlateNumber { get; set; }
    //    public Nullable<bool> isActive { get; set; }
    //    public bool isOccupied { get; set; }
    //    public string Model { get; set; }
    //    public string Make { get; set; }
    //    public Nullable<int> SeatingCapacity { get; set; }
    //    public Nullable<int> OccupiedBy { get; set; }
    //}

}