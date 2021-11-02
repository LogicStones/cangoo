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

    public class DriverBookingRequestNotification : DiscountTypeDTO
    {
        public string PickUpLatitude { get; set; }
        public string PickUpLongitude { get; set; }
        public string PickUpLocation { get; set; }
        public string DropOffLatitude { get; set; }
        public string DropOffLongitude { get; set; }
        public string DropOffLocation { get; set; }
        public string IsLaterBooking { get; set; }
        public string NumberOfPerson { get; set; }
        public string PickUpDateTime { get; set; }
        public string TripId { get; set; }
        public string PaymentMethod { get; set; }
        public string PaymentMethodId { get; set; }
        public string IsDispatchedRide { get; set; }
        public string IsFavorite { get; set; }
        public string IsWeb { get; set; }
        public string Description { get; set; }
        public string RequiredFacilities { get; set; }
        public string IsReRouteRequest { get; set; }
        public string EstimatedPrice { get; set; }
        public string BookingMode { get; set; }
        public string BookingModeId { get; set; }
        public string DispatcherID { get; set; }
        public string RequestTimeOut { get; set; }
        public string SeatingCapacity { get; set; }
        public string VoucherAmount { get; set; }
        public string VoucherCode { get; set; }
        public string DeviceToken { get; set; }
        public string ReRouteRequestTime { get; set; }
        public string PreviousCaptainId { get; set; }
        public List<CanclReasonsDTO> CancelReasons { get; set; }
        public List<FacilitiyDTO> Facilities { get; set; }
    }
}

