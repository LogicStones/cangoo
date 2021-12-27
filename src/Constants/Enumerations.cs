using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Constants
{
	public enum SystemRoles
	{
		SuperAdmin = 1,
		Reseller,
		BusinessAdmin,
		Captain,
		User,
		DispatchUsers,
		ApplicationAdmin
	}

	public enum Languages
	{
		German = 1,
		Turkish,
		English
	}

	public enum TripStatuses
	{
		Completed = 1,  //Payment Received
		OnTheWay = 2,   //Start Ride | On The Way
		Arrived = 3,
		RequestSent = 4,    //Request Sent
		Picked = 5,
		Cancel = 6,     //Trip Canceled
		PaymentPending = 7,     //End Ride   //"Payment Pending"
		TimeOut = 8,
		LaterBookingAccepted = 9,
		PaymentRequested = 10,   //Payment Requested
		ReRouting = 11   //ReRouting
	}

	public enum PassengerPlacesTypes
	{
		Home = 1,
		Work = 2,
		Other = 3
	}

	public enum VehicleCategories
	{
		Standard = 1,
		Comfort,
		Premium,
		Grossraum,//XL
		GreenTaxi,
		Logistic
	}

	public enum FareManagerShifts
	{
		Morning = 1,
		Evening,
		Weekend
	}

	public enum BookingTypes
	{
		Normal = 1,
		Later,
		Shared,
		WalkIn
	}

	public enum BookingModes
	{
		Voucher = 1,
		UserWebPortal,
		UserApplication,
		Hotel,
		Karhoo,
		Dispatcher
	}

	public enum PaymentModes
	{
		Cash = 1,
		Paypal,
		CreditCard,
		Gift,
		Wallet
	}

	public enum PaymentStatuses
	{
		Receivable = 1,
		Payable,
		Paid,
		Received
	}

	public enum DiscountTypes
	{
		Normal = 0,
		Fixed,
		Percentage,
		Special
	}
}