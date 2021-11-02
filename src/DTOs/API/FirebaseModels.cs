using DTOs.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTOs.API
{
	public class DriverStatus
	{
		public string OngoingRide { get; set; } = "";
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
		public string DriverID { get; set; }
		public string IsBusy { get; set; }
		public string DriverFacilities { get; set; }
		public bool IsPriorityHoursActive { get; set; }
		public Location Location { get; set; }
		public string SeatingCapacity { get; set; }
		public string VehicleFacilities { get; set; }
		public string OngoingRide { get; set; }

		public string companyID { get; set; }
		public string tripID { get; set; }
		public string userName { get; set; }
		public string driverName { get; set; }
		public string phoneNumber { get; set; }
		public double lat { get; set; }
		public double lon { get; set; }
		public double dropOfflat { get; set; }
		public double dropOfflong { get; set; }
		public string priorityHourEndTime { get; set; }
		public string earningPoints { get; set; }
		public string priorityHourRemainingTime { get; set; }
		public string onlineSince { get; set; }
		public string lastUpdated { get; set; }
		public long bearing { get; set; }
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
