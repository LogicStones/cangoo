using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTOs.API
{
	public class ReportsBase
	{
		public string ReportNumber { get; set; }
		public string FleetName { get; set; }
		public string PostCode { get; set; }
		public string City { get; set; }
		public string Street { get; set; }
		public string BuildingNumber { get; set; }
		public string ATUNumber { get; set; }
		public string ReportDate { get; set; }
		public string ReportStartDate { get; set; }
		public string ReportEndDate { get; set; }
		public decimal BonusPerRide { get; set; }
	}

	public class TourOverviewReportModel : ReportsBase
	{
		public List<ReportSummary> Summary { get; set; }
		public List<CaptainsReport> CaptainsList { get; set; }
	}

	public class ReportSummary
	{
		public string DriverCode { get; set; }
		public decimal TotalSale { get; set; }
		public decimal CashSale { get; set; }
		public decimal AppSale { get; set; }
		public decimal Bonus { get; set; }
		public decimal ServiceCharges { get; set; }
		public int TotalTrips { get; set; }
		public decimal ApplicationCommission { get; set; }
		public decimal PayableToCaptain { get; set; }
	}

	public class CaptainsReport
	{
		public string ReportNumber { get; set; }
		public string ReportDate { get; set; }
		public string ReportStartDate { get; set; }
		public string ReportEndDate { get; set; }
		public string DriverCode { get; set; }
		public List<CaptainTripsDetailedReport> TripsList { get; set; }
	}

	public class CaptainTripsDetailedReport
	{
		public string TripDate { get; set; }
		public string PaymentMode { get; set; }
		public decimal TripFare { get; set; }
		public decimal Bonus { get; set; }
		public decimal ServicesCharges { get; set; }
		public decimal ApplicationCommission { get; set; }
		public decimal PayableToCaptain { get; set; }
	}

	public class InvoiceSummaryReportModel : ReportsBase
	{
		public int TotalTrips { get; set; }
		public decimal TripsTotalFare { get; set; }
		public decimal CashTripsTotalFare { get; set; }
		public decimal MobileTripsTotalFare { get; set; }
		public decimal TripsTotalTip { get; set; }
		public decimal TotalServicesCharges { get; set; }
		public decimal TripsTotalBonus { get; set; }
		public decimal ApplicationTotalCommission { get; set; }
		public string FleetBankAccountDetails { get; set; }
	}

	public class MonthlyInvoiceReportModel : ReportsBase
	{
		public int TotalTrips { get; set; }
		public decimal TotalTripsAppCommission { get; set; }
		public int TotalHospitalityRepayment { get; set; }
		public decimal HospitalityRepaymentAmount { get; set; }
		public int TotalKickBacks { get; set; }
		public decimal KickBacksAmount { get; set; }
		public List<CaptainInvitesSummary> CaptainsInvitesList { get; set; }
	}

	public class CaptainInvitesSummary
	{
		public string DriverCode { get; set; }
		public int TotalInvites { get; set; }
		public decimal InvitesTotalCommission { get; set; }
	}

	public class ReportAttachments
	{
		public string Name { get; set; }
		public string ReportMonth { get; set; }
		public string ReportNumber { get; set; }
		public string FleetName { get; set; }
		public byte[] Content { get; set; }
	}
}
