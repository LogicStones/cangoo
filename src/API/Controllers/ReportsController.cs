//using CanTaxi_Api.App_Start;
//using CanTaxi_Api.Models;
//using LS_CanTaxiApi.DAL;
//using Rotativa;
//using Rotativa.Options;
//using System;
//using System.Collections.Generic;
//using System.Globalization;
//using System.Linq;
//using System.Web;
using System.Web.Mvc;

namespace API.Controllers
{
	public class ReportsController : Controller
	{
//		public ActionResult TestReport()
//		{
//			var ApplicationId = System.Configuration.ConfigurationManager.AppSettings["ApplicationID"];

//			//Cron job will start at start of next month.

//			//var firstDayOfMonth = new DateTime(DateTime.Today.Month == 1 ? DateTime.Today.AddYears(-1).Year : DateTime.Today.Year,
//			//									DateTime.Today.AddMonths(-1).Month,
//			//									1);

//			//var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

//			var fromDate = DateTime.Today.AddDays(-8);
//			var toDate = DateTime.Today.AddDays(-2);

//			//var fromDate = DateTime.Today.AddDays(-7);
//			//var toDate = DateTime.Today.AddDays(-1);

//			var reportDate = DateTime.Today;

//			var reportDateRange = fromDate.ToString("d", CultureInfo.CreateSpecificCulture("de-DE")) + " - " + toDate.ToString("d", CultureInfo.CreateSpecificCulture("de-DE"));

//			using (CanTaxiResellerEntities context = new CanTaxiResellerEntities())
//			{
//				var fleets = context.spGetApplicationAllFleets(ApplicationId, fromDate, toDate).ToList();

//				foreach (var fleet in fleets)
//				{
//                    if (!fleet.CompanyId.ToString().ToUpper().Equals("E5CECE85-C6B7-4C37-BF02-573B4B3607E5"))
//                        //if (!fleet.CompanyId.ToString().ToUpper().Equals("47F1077E-B354-49C6-A206-BB16245B8825"))
//                        continue;

//					var reportDetails = context.spGetCurrentReportNumber(fleet.CompanyId.ToString(), reportDateRange, "Started", DateTime.Now).FirstOrDefault();

//					var TourOverviewReportModel = new TourOverviewReportModel
//					{
//						FleetName = fleet.FleetName,
//						ATUNumber = fleet.FleetATUNumber,
//						City = fleet.FleetCity,
//						Street = fleet.FleetAddress,
//						BuildingNumber = fleet.FleetBuildingNumber,
//						PostCode = fleet.FleetPostalCode,
//						ReportDate = reportDate.ToString("d", CultureInfo.CreateSpecificCulture("de-DE")),
//						ReportEndDate = toDate.ToString("d", CultureInfo.CreateSpecificCulture("de-DE")),
//						ReportStartDate = fromDate.ToString("d", CultureInfo.CreateSpecificCulture("de-DE")),
//						ReportNumber = reportDetails.InvoiceReportNumber,
//						BonusPerRide = 10.0M,
//						Summary = new List<ReportSummary>(),
//						CaptainsList = new List<CaptainsReport>()
//					};

//					var fleetMonthlySummary = context.spGetFleetMonthlyInvoice(fleet.CompanyId.ToString(), fromDate, toDate).ToList();

//					var InvoiceSummaryReportModel = new InvoiceSummaryReportModel
//					{
//						FleetName = fleet.FleetName,
//						ATUNumber = fleet.FleetATUNumber,
//						City = fleet.FleetCity,
//						Street = fleet.FleetAddress,
//						BuildingNumber = fleet.FleetBuildingNumber,
//						PostCode = fleet.FleetPostalCode,
//						FleetBankAccountDetails = fleet.FleetIBAN,
						
//						BonusPerRide = TourOverviewReportModel.BonusPerRide,
//						ReportDate = TourOverviewReportModel.ReportDate,
//						ReportEndDate = TourOverviewReportModel.ReportEndDate,
//						ReportStartDate = TourOverviewReportModel.ReportStartDate,
//						ReportNumber = TourOverviewReportModel.ReportNumber,

//						ApplicationTotalCommission = fleetMonthlySummary.Sum(c => c.ApplicationProfit) ?? 0,
//						TotalTrips = fleetMonthlySummary.Sum(c => c.TotalTrips) ?? 0,
//						CashTripsTotalFare = fleetMonthlySummary.Sum(c => c.TotalCashEarning) ?? 0,
//						MobileTripsTotalFare = fleetMonthlySummary.Sum(c => c.TotalMobilePayEarning) ?? 0,
//						TripsTotalTip = fleetMonthlySummary.Sum(c => c.TotalTip) ?? 0
//					};

//					InvoiceSummaryReportModel.TripsTotalBonus = InvoiceSummaryReportModel.TotalTrips * TourOverviewReportModel.BonusPerRide;
//					InvoiceSummaryReportModel.TripsTotalFare = InvoiceSummaryReportModel.CashTripsTotalFare + (fleetMonthlySummary.Sum(c => c.TotalMobilePayEarning) ?? 0);
//					InvoiceSummaryReportModel.TotalServicesCharges = (InvoiceSummaryReportModel.TripsTotalFare - InvoiceSummaryReportModel.CashTripsTotalFare) * 0.035M;

//					var MonthlyInvoiceReportModel = new MonthlyInvoiceReportModel
//					{
//						FleetName = fleet.FleetName,
//						ATUNumber = fleet.FleetATUNumber,
//						City = fleet.FleetCity,
//						Street = fleet.FleetAddress,
//						BuildingNumber = fleet.FleetBuildingNumber,
//						PostCode = fleet.FleetPostalCode,

//						ReportDate = TourOverviewReportModel.ReportDate,
//						ReportEndDate = TourOverviewReportModel.ReportEndDate,
//						ReportStartDate = TourOverviewReportModel.ReportStartDate,
//						ReportNumber = TourOverviewReportModel.ReportNumber,

//						TotalTrips = InvoiceSummaryReportModel.TotalTrips,
//						TotalTripsAppCommission = InvoiceSummaryReportModel.ApplicationTotalCommission
//						//HospitalityRepaymentAmount = 0,
//						//KickBacksAmount = 0,
//						//TotalHospitalityRepayment = 0,
//						//TotalKickBacks = 0,
//						//CaptainsInvitesList = new List<CaptainInvitesSummary>()
//					};

//					foreach (var captain in fleetMonthlySummary)
//					{
//						int captainTotalTrips = captain.TotalTrips ?? 0;
//						TourOverviewReportModel.Summary.Add(new ReportSummary
//						{
//							DriverCode = captain.CaptainCode,
//							TotalTrips = captainTotalTrips,
//							AppSale = (decimal)captain.TotalMobilePayEarning,
//							CashSale = (decimal)captain.TotalCashEarning,
//							Bonus = captainTotalTrips * TourOverviewReportModel.BonusPerRide,
//							TotalSale = (decimal)(captain.TotalMobilePayEarning + captain.TotalCashEarning),
//							ApplicationCommission = (decimal)captain.ApplicationProfit + ((decimal)captain.ApplicationProfit * 0.2M),
//							ServiceCharges = ((decimal)captain.TotalMobilePayEarning) * 0.035M,	//3.5% mobile payment processing charges
//							PayableToCaptain = 
//								(decimal)captain.TotalMobilePayEarning 
//							//+	(captainTotalTrips * TourOverviewReportModel.BonusPerRide) 
//							-	(decimal)captain.TotalMobilePayEarning * 0.035M 
//							-	(decimal)captain.ApplicationProfit 
//							-	((decimal)captain.ApplicationProfit * 0.2M)
//						});

//						var captainTripsDetails = context.spGetCaptainMonthlyInvoice(captain.CaptainID.ToString(), fromDate, toDate).ToList();

//						var cptainMonthlySummary = new CaptainsReport
//						{
//							DriverCode = captain.CaptainCode,
//							ReportNumber = fleet.ReportsId.ToString(),
//							ReportDate = reportDate.ToShortDateString(),
//							ReportEndDate = toDate.ToShortDateString(),
//							ReportStartDate = fromDate.ToShortDateString(),
//							TripsList = new List<CaptainTripsDetailedReport>()
//						};

//						foreach (var trip in captainTripsDetails)
//						{
//							var tripItem = new CaptainTripsDetailedReport
//							{
//								ApplicationCommission = (decimal)trip.ApplicationProfit,
//								PaymentMode = trip.TripPaymentMode,
//								TripDate = ((DateTime)trip.TripDateTime).ToString("g", CultureInfo.CreateSpecificCulture("de-DE")),
//								TripFare = (decimal)trip.TripAmount,
//								Bonus = TourOverviewReportModel.BonusPerRide,
//								ServicesCharges = trip.TripPaymentMode.Equals("Bar") ? 0.00M : ((decimal)trip.TripAmount) * 0.035M
//							};

//							if (trip.TripPaymentMode.Equals("Bar"))
//								tripItem.PayableToCaptain = TourOverviewReportModel.BonusPerRide - (decimal)trip.ApplicationProfit - ((decimal)trip.ApplicationProfit * .2M);
//							else
//								tripItem.PayableToCaptain = (decimal)trip.TripAmount + TourOverviewReportModel.BonusPerRide - (decimal)trip.ApplicationProfit - ((decimal)trip.ApplicationProfit * .2M) - ((decimal)trip.TripAmount * 0.035M);

//							cptainMonthlySummary.TripsList.Add(tripItem);
//						}

//						TourOverviewReportModel.CaptainsList.Add(cptainMonthlySummary);
//					}

//					context.spUpdateReportStatus(reportDetails.ReportId, "Completed", DateTime.Now);

//					string customSwitches = string.Format("--print-media-type --allow {0} --footer-html {0} --header-html {1}",
//					Url.Action("Footer", "Reports", new { area = "" }, "http"), Url.Action("Header", "Reports", new { area = "" }, "http"));


//					var reports = new List<ReportAttachments>
//						{
//							new ReportAttachments
//							{
//								Name = "Fahrtenuebersicht.pdf",
//								FleetName = TourOverviewReportModel.FleetName,
//								ReportNumber = TourOverviewReportModel.ReportNumber,
//								ReportMonth = reportDateRange,
//								Content = GenerateReport(TourOverviewReportModel, "TourOverview")
//							},

//							new ReportAttachments
//							{
//								Name = "Monatsrechnung.pdf",
//								FleetName = TourOverviewReportModel.FleetName,
//								ReportNumber = TourOverviewReportModel.ReportNumber,
//								ReportMonth = reportDateRange,
//								Content = GenerateReport(MonthlyInvoiceReportModel, "MonthyInvoice")
//							},

//							new ReportAttachments
//							{
//								Name = "Leistungsuebersicht.pdf",
//								FleetName = TourOverviewReportModel.FleetName,
//								ReportNumber = TourOverviewReportModel.ReportNumber,
//								ReportMonth = reportDateRange,
//								Content = GenerateReport(InvoiceSummaryReportModel, "InvoiceSummary")
//							}
//						};


//					EmailManager.SendReports("test.logicstones@gmail.com",
//								string.Format(@"<pre>Lieber Partner, 

//in der Beilage übermitteln wir deine wöchentliche Abrechnung für {0}. 
//Wir bedanken uns für die Zusammenarbeit! 


//Mit besten Grüßen 

 
//HEAD OFFICE 
//E: rechnung@cangoo.at 
//T: + 43 699 101 01 101 
//A: Laxenburgerstr. 216, 1230 Wien</pre>", reportDateRange),
//								"Ihre Leistungsübersicht",
//								"rechnung@cangoo.at",
//								"cangoo",
//								reports);

//					//return new ViewAsPdf(TourOverviewReportModel)
//					//{
//					//    ViewName = "TourOverview",
//					//    PageMargins = { Left = 10, Bottom = 30, Right = 10, Top = 38 },
//					//    PageOrientation = Orientation.Portrait,
//					//    PageSize = Size.A4,
//					//    CustomSwitches = customSwitches
//					//};

//					//return new ViewAsPdf(MonthlyInvoiceReportModel)
//					//{
//					//    ViewName = "MonthyInvoice",
//					//    PageMargins = { Left = 10, Bottom = 30, Right = 10, Top = 38 },
//					//    PageOrientation = Orientation.Portrait,
//					//    PageSize = Size.A4,
//					//    CustomSwitches = customSwitches
//					//};

//					//return new ViewAsPdf(InvoiceSummaryReportModel)
//					//{
//					//    ViewName = "InvoiceSummary",
//					//    PageMargins = { Left = 10, Bottom = 30, Right = 10, Top = 38 },
//					//    PageOrientation = Orientation.Portrait,
//					//    PageSize = Size.A4,
//					//    CustomSwitches = customSwitches
//					//};

//				}
//				return View();
//			}
//		}

//		public ActionResult Index()
//		{
//			try
//			{
//				var ApplicationId = System.Configuration.ConfigurationManager.AppSettings["ApplicationID"];

//				//var fromDate = DateTime.Today.AddDays(-9);
//				//var toDate = DateTime.Today.AddDays(-7);
//				var fromDate = new DateTime(2021, 7, 1);
//				var toDate = new DateTime(2021, 7, 31);
//				var reportDate = DateTime.Today;

//				var reportDateRange = fromDate.ToString("d", CultureInfo.CreateSpecificCulture("de-DE")) + " - " + toDate.ToString("d", CultureInfo.CreateSpecificCulture("de-DE"));

//				using (CanTaxiResellerEntities context = new CanTaxiResellerEntities())
//				{
//					var fleets = context.spGetApplicationAllFleets(ApplicationId, fromDate, toDate).ToList();

//					foreach (var fleet in fleets)
//					{
//						//if (!fleet.CompanyId.ToString().ToUpper().Equals("47F1077E-B354-49C6-A206-BB16245B8825"))
//						//    continue;

//						if (fleet.MonthlyTrips == 0 || fleet.MonthlyTrips == null)
//							continue;

//                        var reportDetails = context.spGetCurrentReportNumber(fleet.CompanyId.ToString(), reportDateRange, "Started", DateTime.Now).FirstOrDefault();

//						var TourOverviewReportModel = new TourOverviewReportModel
//						{
//							FleetName = fleet.FleetName,
//							ATUNumber = fleet.FleetATUNumber,
//							City = fleet.FleetCity,
//							Street = fleet.FleetAddress,
//							BuildingNumber = fleet.FleetBuildingNumber,
//							PostCode = fleet.FleetPostalCode,
//							ReportDate = reportDate.ToString("d", CultureInfo.CreateSpecificCulture("de-DE")),
//							ReportEndDate = toDate.ToString("d", CultureInfo.CreateSpecificCulture("de-DE")),
//							ReportStartDate = fromDate.ToString("d", CultureInfo.CreateSpecificCulture("de-DE")),
//							ReportNumber = reportDetails.InvoiceReportNumber,
//							BonusPerRide = 0.00M,
//							Summary = new List<ReportSummary>(),
//							CaptainsList = new List<CaptainsReport>()
//						};

//						var fleetMonthlySummary = context.spGetFleetMonthlyInvoice(fleet.CompanyId.ToString(), fromDate, toDate).ToList();

//						var InvoiceSummaryReportModel = new InvoiceSummaryReportModel
//						{
//							FleetName = fleet.FleetName,
//							ATUNumber = fleet.FleetATUNumber,
//							City = fleet.FleetCity,
//							Street = fleet.FleetAddress,
//							BuildingNumber = fleet.FleetBuildingNumber,
//							PostCode = fleet.FleetPostalCode,
//							FleetBankAccountDetails = fleet.FleetIBAN,

//							BonusPerRide = TourOverviewReportModel.BonusPerRide,
//							ReportDate = TourOverviewReportModel.ReportDate,
//							ReportEndDate = TourOverviewReportModel.ReportEndDate,
//							ReportStartDate = TourOverviewReportModel.ReportStartDate,
//							ReportNumber = TourOverviewReportModel.ReportNumber,

//							ApplicationTotalCommission = fleetMonthlySummary.Sum(c => c.ApplicationProfit) ?? 0,
//							TotalTrips = fleetMonthlySummary.Sum(c => c.TotalTrips) ?? 0,
//							CashTripsTotalFare = fleetMonthlySummary.Sum(c => c.TotalCashEarning) ?? 0,
//							MobileTripsTotalFare = fleetMonthlySummary.Sum(c => c.TotalMobilePayEarning) ?? 0,
//							TripsTotalTip = fleetMonthlySummary.Sum(c => c.TotalTip) ?? 0
//						};

//						InvoiceSummaryReportModel.TripsTotalBonus = InvoiceSummaryReportModel.TotalTrips * TourOverviewReportModel.BonusPerRide;
//						InvoiceSummaryReportModel.TripsTotalFare = InvoiceSummaryReportModel.CashTripsTotalFare + (fleetMonthlySummary.Sum(c => c.TotalMobilePayEarning) ?? 0);
//						InvoiceSummaryReportModel.TotalServicesCharges = (InvoiceSummaryReportModel.TripsTotalFare - InvoiceSummaryReportModel.CashTripsTotalFare) * 0.035M;     //3.5% mobile payment processing charges

//						var MonthlyInvoiceReportModel = new MonthlyInvoiceReportModel
//						{
//							FleetName = fleet.FleetName,
//							ATUNumber = fleet.FleetATUNumber,
//							City = fleet.FleetCity,
//							Street = fleet.FleetAddress,
//							BuildingNumber = fleet.FleetBuildingNumber,
//							PostCode = fleet.FleetPostalCode,

//							ReportDate = TourOverviewReportModel.ReportDate,
//							ReportEndDate = TourOverviewReportModel.ReportEndDate,
//							ReportStartDate = TourOverviewReportModel.ReportStartDate,
//							ReportNumber = TourOverviewReportModel.ReportNumber,

//							TotalTrips = InvoiceSummaryReportModel.TotalTrips,
//							TotalTripsAppCommission = InvoiceSummaryReportModel.ApplicationTotalCommission
//							//HospitalityRepaymentAmount = 0,
//							//KickBacksAmount = 0,
//							//TotalHospitalityRepayment = 0,
//							//TotalKickBacks = 0,
//							//CaptainsInvitesList = new List<CaptainInvitesSummary>()
//						};

//						foreach (var captain in fleetMonthlySummary)
//						{
//							int captainTotalTrips = captain.TotalTrips ?? 0;
//							TourOverviewReportModel.Summary.Add(new ReportSummary
//							{
//								DriverCode = captain.CaptainCode,
//								TotalTrips = captainTotalTrips,
//								AppSale = (decimal)captain.TotalMobilePayEarning,
//								CashSale = (decimal)captain.TotalCashEarning,
//								Bonus = captainTotalTrips * TourOverviewReportModel.BonusPerRide,
//								TotalSale = (decimal)(captain.TotalMobilePayEarning + captain.TotalCashEarning),
//								ApplicationCommission = (decimal)captain.ApplicationProfit, //+ ((decimal)captain.ApplicationProfit * 0.2M),
//								ServiceCharges = ((decimal)captain.TotalMobilePayEarning) * 0.035M, //3.5% mobile payment processing charges
//								PayableToCaptain =
//									(decimal)captain.TotalMobilePayEarning
//									+ (captainTotalTrips * TourOverviewReportModel.BonusPerRide) 
//									- (decimal)captain.TotalMobilePayEarning * 0.035M
//									- (decimal)captain.ApplicationProfit
//									- ((decimal)captain.ApplicationProfit * 0.2M)
//							});

//							var captainTripsDetails = context.spGetCaptainMonthlyInvoice(captain.CaptainID.ToString(), fromDate, toDate).ToList();

//							var cptainMonthlySummary = new CaptainsReport
//							{
//								DriverCode = captain.CaptainCode,
//								ReportNumber = fleet.ReportsId.ToString(),
//								ReportDate = reportDate.ToShortDateString(),
//								ReportEndDate = toDate.ToShortDateString(),
//								ReportStartDate = fromDate.ToShortDateString(),
//								TripsList = new List<CaptainTripsDetailedReport>()
//							};

//							foreach (var trip in captainTripsDetails)
//							{
//								var tripItem = new CaptainTripsDetailedReport
//								{
//									ApplicationCommission = (decimal)trip.ApplicationProfit,
//									PaymentMode = trip.TripPaymentMode,
//									TripDate = ((DateTime)trip.TripDateTime).ToString("g", CultureInfo.CreateSpecificCulture("de-DE")),
//									TripFare = (decimal)trip.TripAmount,
//									Bonus = TourOverviewReportModel.BonusPerRide,
//									ServicesCharges = trip.TripPaymentMode.Equals("Bar") ? 0.00M : ((decimal)trip.TripAmount) * 0.035M
//								};

//								if (trip.TripPaymentMode.Equals("Bar"))
//									tripItem.PayableToCaptain = TourOverviewReportModel.BonusPerRide - (decimal)trip.ApplicationProfit - ((decimal)trip.ApplicationProfit * .2M);
//								else
//									tripItem.PayableToCaptain = (decimal)trip.TripAmount + TourOverviewReportModel.BonusPerRide - (decimal)trip.ApplicationProfit - ((decimal)trip.ApplicationProfit * .2M) - ((decimal)trip.TripAmount * 0.035M);

//								cptainMonthlySummary.TripsList.Add(tripItem);
//							}

//							TourOverviewReportModel.CaptainsList.Add(cptainMonthlySummary);
//						}

//						context.spUpdateReportStatus(reportDetails.ReportId, "Completed", DateTime.Now);

//						string customSwitches = string.Format("--print-media-type --allow {0} --footer-html {0} --header-html {1}",
//						Url.Action("Footer", "Reports", new { area = "" }, "http"), Url.Action("Header", "Reports", new { area = "" }, "http"));


//						var reports = new List<ReportAttachments>
//						{
//							new ReportAttachments
//							{
//								Name = "Fahrtenuebersicht.pdf",
//								FleetName = TourOverviewReportModel.FleetName,
//								ReportNumber = TourOverviewReportModel.ReportNumber,
//								ReportMonth = reportDateRange,
//								Content = GenerateReport(TourOverviewReportModel, "TourOverview")
//							},

//							new ReportAttachments
//							{
//								Name = "Monatsrechnung.pdf",
//								FleetName = TourOverviewReportModel.FleetName,
//								ReportNumber = TourOverviewReportModel.ReportNumber,
//								ReportMonth = reportDateRange,
//								Content = GenerateReport(MonthlyInvoiceReportModel, "MonthyInvoice")
//							},

//							new ReportAttachments
//							{
//								Name = "Leistungsuebersicht.pdf",
//								FleetName = TourOverviewReportModel.FleetName,
//								ReportNumber = TourOverviewReportModel.ReportNumber,
//								ReportMonth = reportDateRange,
//								Content = GenerateReport(InvoiceSummaryReportModel, "InvoiceSummary")
//							}
//						};


//						EmailManager.SendReports(fleet.Email,//"developer.cantaxi@gmail.com"
//									string.Format(@"<pre>Lieber Partner, 

//in der Beilage übermitteln wir deine wöchentliche Abrechnung für {0}. 
//Wir bedanken uns für die Zusammenarbeit! 


//Mit besten Grüßen 

 
//HEAD OFFICE 
//E: rechnung@cangoo.at 
//T: + 43 699 101 01 101 
//A: Laxenburgerstr. 216, 1230 Wien</pre>", reportDateRange),
//									"Ihre Leistungsübersicht",
//									"rechnung@cangoo.at",
//									"cangoo",
//									reports);
//					}
//					return View();
//				}
//			}
//			catch (Exception ex)
//			{
//				Logger.WriteLog(ex);
//			}
//			return View();
//		}


//		#region  Without per ride incentive

//		//		public ActionResult Index()
//		//		{
//		//			try
//		//			{
//		//				var ApplicationId = System.Configuration.ConfigurationManager.AppSettings["ApplicationID"];

//		//				//Cron job will start at start of next month.

//		//				var firstDayOfMonth = new DateTime(DateTime.Today.Month == 1 ? DateTime.Today.AddYears(-1).Year : DateTime.Today.Year,
//		//												DateTime.Today.AddMonths(-1).Month,
//		//												1);

//		//				var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);
//		//				var reportDate = DateTime.Today;

//		//				using (CanTaxiResellerEntities context = new CanTaxiResellerEntities())
//		//				{
//		//					var fleets = context.spGetApplicationAllFleets(ApplicationId).ToList();

//		//					foreach (var fleet in fleets)
//		//					{
//		//						var reportDetails = context.spGetCurrentReportNumber(fleet.CompanyId.ToString(), firstDayOfMonth.ToString("MMMM"), "Started", DateTime.Now).FirstOrDefault();

//		//                        var TourOverviewReportModel = new TourOverviewReportModel
//		//						{
//		//							FleetName = fleet.FleetName,
//		//							ATUNumber = fleet.FleetATUNumber,
//		//							City = fleet.FleetCity,
//		//							Street = fleet.FleetAddress,
//		//							BuildingNumber = fleet.FleetBuildingNumber,
//		//							PostCode = fleet.FleetPostalCode,
//		//							ReportDate = reportDate.ToString("d", CultureInfo.CreateSpecificCulture("de-DE")),
//		//							ReportEndDate = lastDayOfMonth.ToString("d", CultureInfo.CreateSpecificCulture("de-DE")),
//		//							ReportStartDate = firstDayOfMonth.ToString("d", CultureInfo.CreateSpecificCulture("de-DE")),
//		//							ReportNumber = reportDetails.InvoiceReportNumber,
//		//							Summary = new List<ReportSummary>(),
//		//							CaptainsList = new List<CaptainsReport>()
//		//						};

//		//						var fleetMonthlySummary = context.spGetFleetMonthlyInvoice(fleet.CompanyId.ToString(), firstDayOfMonth, lastDayOfMonth).ToList();

//		//						var InvoiceSummaryReportModel = new InvoiceSummaryReportModel
//		//						{
//		//							FleetName = fleet.FleetName,
//		//							ATUNumber = fleet.FleetATUNumber,
//		//							City = fleet.FleetCity,
//		//							Street = fleet.FleetAddress,
//		//							BuildingNumber = fleet.FleetBuildingNumber,
//		//							PostCode = fleet.FleetPostalCode,

//		//							ReportDate = TourOverviewReportModel.ReportDate,
//		//							ReportEndDate = TourOverviewReportModel.ReportEndDate,
//		//							ReportStartDate = TourOverviewReportModel.ReportStartDate,
//		//							ReportNumber = TourOverviewReportModel.ReportNumber,

//		//							ApplicationTotalCommission = fleetMonthlySummary.Sum(c => c.ApplicationProfit) ?? 0,
//		//							TotalTrips = fleetMonthlySummary.Sum(c => c.TotalTrips) ?? 0,
//		//							CashTripsTotalFare = fleetMonthlySummary.Sum(c => c.TotalCashEarning) ?? 0,
//		//							TripsTotalTip = fleetMonthlySummary.Sum(c => c.TotalTip) ?? 0
//		//						};

//		//						InvoiceSummaryReportModel.TripsTotalFare = InvoiceSummaryReportModel.CashTripsTotalFare + (fleetMonthlySummary.Sum(c => c.TotalMobilePayEarning) ?? 0);
//		//						InvoiceSummaryReportModel.TotalServicesCharges = (InvoiceSummaryReportModel.TripsTotalFare - InvoiceSummaryReportModel.CashTripsTotalFare) * 0.035M;

//		//						var MonthlyInvoiceReportModel = new MonthlyInvoiceReportModel
//		//						{
//		//							FleetName = fleet.FleetName,
//		//							ATUNumber = fleet.FleetATUNumber,
//		//							City = fleet.FleetCity,
//		//							Street = fleet.FleetAddress,
//		//							BuildingNumber = fleet.FleetBuildingNumber,
//		//							PostCode = fleet.FleetPostalCode,

//		//							ReportDate = TourOverviewReportModel.ReportDate,
//		//							ReportEndDate = TourOverviewReportModel.ReportEndDate,
//		//							ReportStartDate = TourOverviewReportModel.ReportStartDate,
//		//							ReportNumber = TourOverviewReportModel.ReportNumber,

//		//							TotalTrips = InvoiceSummaryReportModel.TotalTrips,
//		//							TotalTripsAppCommission = InvoiceSummaryReportModel.ApplicationTotalCommission
//		//							//HospitalityRepaymentAmount = 0,
//		//							//KickBacksAmount = 0,
//		//							//TotalHospitalityRepayment = 0,
//		//							//TotalKickBacks = 0,
//		//							//CaptainsInvitesList = new List<CaptainInvitesSummary>()
//		//						};

//		//						foreach (var captain in fleetMonthlySummary)
//		//						{
//		//							TourOverviewReportModel.Summary.Add(new ReportSummary
//		//							{
//		//								ApplicationCommission = (decimal)captain.ApplicationProfit,
//		//								AppSale = (decimal)captain.TotalMobilePayEarning,
//		//								ServiceCharges = ((decimal)captain.TotalMobilePayEarning) * 0.035M, //3.5% mobile payment processing charges
//		//								CashSale = (decimal)captain.TotalCashEarning,
//		//								DriverCode = captain.CaptainCode,
//		//								PayableToCaptain = (decimal)captain.PayableToCaptain - ((decimal)captain.TotalMobilePayEarning * 0.035M),
//		//								TotalSale = (decimal)(captain.TotalMobilePayEarning + captain.TotalCashEarning),
//		//								TotalTrips = captain.TotalTrips ?? 0
//		//							});

//		//							var captainTripsDetails = context.spGetCaptainMonthlyInvoice(captain.CaptainID.ToString(), firstDayOfMonth, lastDayOfMonth).ToList();

//		//							var cptainMonthlySummary = new CaptainsReport
//		//							{
//		//								DriverCode = captain.CaptainCode,
//		//								ReportNumber = fleet.ReportsId.ToString(),
//		//								ReportDate = reportDate.ToShortDateString(),
//		//								ReportEndDate = lastDayOfMonth.ToShortDateString(),
//		//								ReportStartDate = firstDayOfMonth.ToShortDateString(),
//		//								TripsList = new List<CaptainTripsDetailedReport>()
//		//							};

//		//							foreach (var trip in captainTripsDetails)
//		//							{
//		//								cptainMonthlySummary.TripsList.Add(new CaptainTripsDetailedReport
//		//								{
//		//									ApplicationCommission = (decimal)trip.ApplicationProfit,
//		//									PayableToCaptain = (decimal)trip.PayableToCaptain - (trip.TripPaymentMode.Equals("Bar") ? 0.00M : ((decimal)trip.TripAmount) * 0.035M),
//		//									PaymentMode = trip.TripPaymentMode,
//		//									TripDate = ((DateTime)trip.TripDateTime).ToString("g", CultureInfo.CreateSpecificCulture("de-DE")),
//		//									TripFare = (decimal)trip.TripAmount,
//		//									ServicesCharges = trip.TripPaymentMode.Equals("Bar") ? 0.00M : ((decimal)trip.TripAmount) * 0.035M
//		//								});
//		//							}

//		//							TourOverviewReportModel.CaptainsList.Add(cptainMonthlySummary);
//		//						}

//		//						var reports = new List<ReportAttachments>
//		//                        {
//		//                            new ReportAttachments
//		//                            {
//		//                                Name = "Fahrtenuebersicht.pdf",
//		//								FleetName = TourOverviewReportModel.FleetName,
//		//								ReportNumber = TourOverviewReportModel.ReportNumber,
//		//								ReportMonth = firstDayOfMonth.ToString("MMMM") + ", " + firstDayOfMonth.ToString("yyyy"),
//		//								Content = GenerateReport(TourOverviewReportModel, "TourOverview")
//		//                            },

//		//                            new ReportAttachments
//		//                            {
//		//                                Name = "Monatsrechnung.pdf",
//		//								FleetName = TourOverviewReportModel.FleetName,
//		//								ReportNumber = TourOverviewReportModel.ReportNumber,
//		//								ReportMonth = firstDayOfMonth.ToString("MMMM") + ", " + firstDayOfMonth.ToString("yyyy"),
//		//								Content = GenerateReport(MonthlyInvoiceReportModel, "MonthyInvoice")
//		//                            },

//		//                            new ReportAttachments
//		//                            {
//		//                                Name = "Leistungsuebersicht.pdf",
//		//								FleetName = TourOverviewReportModel.FleetName,
//		//								ReportNumber = TourOverviewReportModel.ReportNumber,
//		//								ReportMonth = firstDayOfMonth.ToString("MMMM") + ", " + firstDayOfMonth.ToString("yyyy"),
//		//								Content = GenerateReport(InvoiceSummaryReportModel, "InvoiceSummary")
//		//                            }
//		//                        };


//		//						EmailManager.SendReports(fleet.Email,
//		//									string.Format(@"<pre>Lieber Partner, 

//		//in der Beilage übermitteln wir deine monatliche Abrechnung für {0} {1}. 
//		//Wir bedanken uns für die Zusammenarbeit! 


//		//Mit besten Grüßen 


//		//HEAD OFFICE 
//		//E: rechnung@cangoo.at 
//		//T: + 43 699 101 01 101 
//		//A: Laxenburgerstr. 216, 1230 Wien</pre>", lastDayOfMonth.ToString("MMMM"), lastDayOfMonth.Year),
//		//									"Ihre Leistungsübersicht",
//		//									"rechnung@cangoo.at",
//		//									"cangoo",
//		//									reports);

//		//						//TBD: Log ResellerID, ApplicationID, ReportNumber, ApplicationFee, TaxAmount, ServiceCharges, CashPayment, MobilePayment, PromoDiscount, 
//		//						//WalletUsedAmount, Vouchers Used Amount, LaterBookings, NormalBookings, MobilePayment Trips, CashPayment Trips, 
//		//						//Cancelled Trips, Completed Trips, Other Trips, InvoiceReportNumber

//		//						context.spUpdateReportStatus(reportDetails.ReportId, "Completed", DateTime.Now);
//		//                    }
//		//				}
//		//			}
//		//			catch (Exception ex)
//		//			{
//		//				Logger.WriteLog(ex);
//		//			}
//		//			return View();
//		//		}

//		#endregion
//		public ActionResult Header()
//		{
//			return View();
//		}
//		public ActionResult Footer()
//		{
//			return View();
//		}
//		public Byte[] GenerateReport(dynamic model, string ReportViewName)
//		{
//			string customSwitches = string.Format("--print-media-type --allow {0} --footer-html {0} --header-html {1}",
//					Url.Action("Footer", "Reports", new { area = "" }, "http"), Url.Action("Header", "Reports", new { area = "" }, "http"));

//			var report = new ViewAsPdf(model)
//			{
//				ViewName = ReportViewName,
//				PageMargins = { Left = 10, Bottom = 30, Right = 10, Top = 38 },
//				PageOrientation = Orientation.Portrait,
//				PageSize = Size.A4,
//				CustomSwitches = customSwitches
//			};

//			byte[] pdfData = report.BuildFile(this.ControllerContext);
//			return pdfData;
//		}
	}
}