using Constants;
using DatabaseModel;
using DTOs.API;
using DTOs.Shared;
using Integrations;
using Services.Automapper;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services
{
    public class TripsManagerService
    {
        public static async Task<List<TripOverView>> GetPassengerCompletedTrips(string passengerId, int offSet, int limit)
        {
            using (CangooEntities dbContext = new CangooEntities())
            {
                var query = dbContext.Database.SqlQuery<TripOverView>("exec spGetPassengerCompletedTrips @PassengerID, @OffSet, @Limit",
                                                                new SqlParameter("@PassengerID", passengerId),
                                                                new SqlParameter("@OffSet", offSet),
                                                                new SqlParameter("@Limit", limit));
                return await query.ToListAsync();
            }
        }

        public static async Task<List<TripOverView>> GetPassengerCancelledTrips(string passengerId, int offSet, int limit)
        {
            using (CangooEntities dbContext = new CangooEntities())
            {
                var query = dbContext.Database.SqlQuery<TripOverView>("exec spGetPassengerCancelledTrips @PassengerID, @OffSet, @Limit",
                                                                new SqlParameter("@PassengerID", passengerId),
                                                                new SqlParameter("@OffSet", offSet),
                                                                new SqlParameter("@Limit", limit));
                return await query.ToListAsync();
            }
        }

        public static async Task<List<TripOverView>> GetPassengerScheduledTrips(string passengerId, int offSet, int limit)
        {
            using (CangooEntities dbContext = new CangooEntities())
            {
                var query = dbContext.Database.SqlQuery<TripOverView>("exec spGetPassengerScheduledTrips @PassengerID, @OffSet, @Limit",
                                                                new SqlParameter("@PassengerID", passengerId),
                                                                new SqlParameter("@OffSet", offSet),
                                                                new SqlParameter("@Limit", limit));
                return await query.ToListAsync();
            }
        }

        public static async Task<TripDetails> GetFullTripById(string tripId)
        {
            using (CangooEntities dbContext = new CangooEntities())
            {
                var query = dbContext.Database.SqlQuery<TripDetails>("exec spGetTripDetails @TripId",
                                                                new SqlParameter("@TripId", tripId));
                var details = await query.FirstOrDefaultAsync();
                details.FacilitiesList = await FacilitiesService.GetFacilitiesListAsync();
                return details;
            }
        }

        private static async Task<Trip> GetTripById(string tripId)
        {
            using (var dbContext = new CangooEntities())
            {
                return dbContext.Trips.Where(t => t.TripID.ToString().Equals(tripId)).FirstOrDefault();
            }
        }
        private static async Task<Trip> GetPassengerTripById(string tripId, string passengerId)
        {
            using (var dbContext = new CangooEntities())
            {
                return dbContext.Trips.Where(t => t.TripID.ToString().Equals(tripId) && t.UserID.ToString().Equals(passengerId)).FirstOrDefault();
            }
        }

        //public static async Task<TripDTO> GetTripDTOById(string tripId)
        //{
        //    using (CangooEntities dbContext = new CangooEntities())
        //    {
        //        var trip = dbContext.Trips.Where(t => t.TripID.ToString().Equals(tripId)).FirstOrDefault();
        //        return AutoMapperConfig._mapper.Map<Trip, TripDTO>(trip); ;
        //    }
        //}

        public static async Task<List<GetRecentLocationDetails>> GetRecentLocation(string passengerId)
        {
            using (CangooEntities dbContext = new CangooEntities())
            {
                var query = dbContext.Database.SqlQuery<GetRecentLocationDetails>("SELECT TOP(5) DropOffLocationLatitude,DropOffLocationLongitude,DropOffLocation,PickupLocationPostalCode, DropOffLocationPostalCode, MidwayStop1PostalCode FROM Trips WHERE UserID=@passengerId AND TripStatusID = @tripStatus ORDER BY ArrivalDateTime DESC",
                                                                                                                    new SqlParameter("@passengerId", passengerId),
                                                                                                                    new SqlParameter("@tripStatus", TripStatuses.Completed));
                return await query.ToListAsync();
            }
        }

        public static async Task<BookTripResponse> BookNewTrip(BookTripRequest model)
        {
            using (CangooEntities dbContext = new CangooEntities())
            {
                var resellerId = ConfigurationManager.AppSettings["ResellerID"].ToString();
                var applicationId = ConfigurationManager.AppSettings["ApplicationID"].ToString();

                //Object to add / update trip in database
                //Trip tp = new Trip();

                //if later booking then write in pending later bookings to send FCM 2 min before pickup time if no one accepts booking
                //FireBaseController fc = new FireBaseController();

                //var lstcr = await CancelReasonsService.GetCancelReasons(tp.BookingTypeID == (int)BookingTypes.Normal ? true : false, tp.BookingTypeID == (int)BookingTypes.Later ? true : false, false);

                List<FacilitiyDTO> facilities = new List<FacilitiyDTO>();

                if (!string.IsNullOrEmpty(model.RequiredFacilities))
                {
                    facilities = await FacilitiesService.GetFacilitiesDetailByIds(model.RequiredFacilities);
                }
                else
                    model.RequiredFacilities = "";

                int timeOut = await ApplicationSettingService.GetRequestTimeOut(applicationId);

                //Object to be used to populate captains FCM object
                DriverBookingRequestNotification bookingRN = new DriverBookingRequestNotification
                {
                    RequestTimeOut = timeOut.ToString(),
                    PickUpLatitude = model.PickUpLatitude,
                    PickUpLongitude = model.PickUpLongitude,
                    DropOffLatitude = model.DropOffLatitude,
                    DropOffLongitude = model.DropOffLongitutde,
                    IsLaterBooking = model.IsLaterBooking,
                    SeatingCapacity = model.SeatingCapacity,
                    RequiredFacilities = model.RequiredFacilities,
                    Description = string.IsNullOrEmpty(model.Description) ? "" : model.Description,
                    IsReRouteRequest = model.IsReRouteRequest,
                    IsDispatchedRide = false.ToString(),
                    Facilities = facilities,
                    IsFavorite = false.ToString(),
                    //CancelReasons = lstcr,
                    //IsWeb = false,
                    //REFACTOR - Remove this flag
                    //isLaterBookingStarted = false
                };

                Trip tp = new Trip();
                if (bool.Parse(model.IsReRouteRequest))
                {
                    tp = await GetTripById(model.TripID);
                    tp.isReRouted = true;

                    //If trip was On The Way / Arrived, then during rerouting status is set to cancel.
                    if (bool.Parse(model.IsLaterBooking))
                    {
                        //UPDATE: If in process later booking was canceled by driver, even then deal it as later booking
                        ////ensure later booking was not started, otherwise deal as normal booking
                        //pr.isLaterBookingStarted = tp.TripStatusID != (int)TripStatus.Waiting ? true : false;

                        tp.TripStatusID = (int)TripStatuses.RequestSent;
                        bookingRN.PickUpDateTime = tp.PickUpBookingDateTime.ToString();
                    }

                    await LogReRoutedTrip(model, applicationId, tp);

                    if (tp.VoucherID != null)
                    {
                        var voucher = await GetVoucherDetails((Guid)tp.VoucherID);
                        bookingRN.VoucherAmount = voucher.Amount.ToString();
                        bookingRN.VoucherCode = voucher.VoucherCode;
                    }

                    bookingRN.Description = tp.Description;
                    bookingRN.PreviousCaptainId = model.DriverID;
                    bookingRN.DeviceToken = model.DeviceToken;
                }
                else
                {
                    /*TBD: Replace isHotelBooking and isLaterBooking logic with BookingModes = 4
                    and BookingTypes = 2 respectively*/
                    //if (string.IsNullOrEmpty(model.karhooTripID))

                    if (int.Parse(model.BookingModeId) == (int)BookingModes.Karhoo)
                    {
                        tp.TripID = Guid.Parse(model.KarhooTripID);
                        tp.BookingModeID = (int)BookingModes.Karhoo;
                        bookingRN.IsWeb = true.ToString();
                        bookingRN.BookingMode = Enum.GetName(typeof(BookingModes), (int)BookingModes.Karhoo).ToLower();
                    }
                    else if (int.Parse(model.BookingModeId) == (int)BookingModes.Dispatcher)
                    {
                        tp.TripID = Guid.NewGuid();
                        tp.BookingModeID = (int)BookingModes.Dispatcher;
                        bookingRN.IsWeb = true.ToString();
                        bookingRN.IsDispatchedRide = true.ToString();
                        bookingRN.BookingMode = Enum.GetName(typeof(BookingModes), (int)BookingModes.Dispatcher).ToLower();
                    }
                    else//if (string.IsNullOrEmpty(model.BookingModeId))
                    {
                        tp.TripID = Guid.NewGuid();
                        tp.BookingModeID = (int)BookingModes.UserApplication;
                        bookingRN.IsWeb = false.ToString();
                        bookingRN.BookingMode = "";
                    }

                    tp.ApplicationID = Guid.Parse(applicationId);
                    tp.ResellerID = Guid.Parse(resellerId);
                    tp.PickupLocationLatitude = model.PickUpLatitude;
                    tp.PickupLocationLongitude = model.PickUpLongitude;
                    tp.PickUpLocation = model.PickUpLocation;
                    tp.DropOffLocationLatitude = string.IsNullOrEmpty(model.DropOffLatitude) ? "0.00" : model.DropOffLatitude;
                    tp.DropOffLocationLongitude = string.IsNullOrEmpty(model.DropOffLongitutde) ? "0.00" : model.DropOffLongitutde;
                    tp.DropOffLocation = string.IsNullOrEmpty(model.DropOffLocation) ? "" : model.DropOffLocation;
                    tp.DistanceTraveled = string.IsNullOrEmpty(model.DropOffLocation) ? 0.00 : double.Parse(model.Distance);
                    tp.UserID = new Guid(model.PassengerId);
                    tp.TripStatusID = (int)TripStatuses.RequestSent;
                    tp.BookingDateTime = DateTime.UtcNow;
                    tp.TripPaymentMode = model.SelectedPaymentMethod;
                    tp.isLaterBooking = bool.Parse(model.IsLaterBooking);
                    tp.NoOfPerson = Convert.ToInt32(model.SeatingCapacity);
                    tp.BookingTypeID = (int)BookingTypes.Normal;
                    tp.isHotelBooking = false;
                    tp.Description = string.IsNullOrEmpty(model.Description) ? "" : model.Description;
                    tp.facilities = model.RequiredFacilities;
                    tp.isReRouted = false;
                    tp.UTCTimeZoneOffset = int.Parse(model.TimeZoneOffset);
                    tp.InBoundDistanceInMeters = string.IsNullOrEmpty(model.InBoundDistanceInKM) ? 0 : (int)(double.Parse(model.InBoundDistanceInKM) * 1000);
                    tp.InBoundDistanceFare = string.IsNullOrEmpty(model.InBoundDistanceFare) ? 0 : decimal.Parse(model.InBoundDistanceFare);
                    tp.InBoundTimeInSeconds = string.IsNullOrEmpty(model.InBoundTimeInMinutes) ? 0 : (int)(double.Parse(model.InBoundTimeInMinutes) * 60);
                    tp.InBoundTimeFare = string.IsNullOrEmpty(model.InBoundTimeFare) ? 0 : decimal.Parse(model.InBoundTimeFare);
                    tp.OutBoundDistanceInMeters = string.IsNullOrEmpty(model.OutBoundDistanceInKM) ? 0 : (int)(double.Parse(model.OutBoundDistanceInKM) * 1000);
                    tp.OutBoundDistanceFare = string.IsNullOrEmpty(model.OutBoundDistanceFare) ? 0 : decimal.Parse(model.OutBoundDistanceFare);
                    tp.OutBoundTimeInSeconds = string.IsNullOrEmpty(model.OutBoundTimeInMinutes) ? 0 : (int)(double.Parse(model.OutBoundTimeInMinutes) * 60);
                    tp.OutBoundTimeFare = string.IsNullOrEmpty(model.OutBoundTimeFare) ? 0 : decimal.Parse(model.OutBoundTimeFare);
                    tp.InBoundSurchargeAmount = string.IsNullOrEmpty(model.InBoundSurchargeAmount) ? 0 : decimal.Parse(model.InBoundSurchargeAmount);
                    tp.OutBoundSurchargeAmount = string.IsNullOrEmpty(model.OutBoundSurchargeAmount) ? 0 : decimal.Parse(model.OutBoundSurchargeAmount);


                    //TBD : Accomodate formatted adjustment value


                    tp.InBoundBaseFare = string.IsNullOrEmpty(model.InBoundBaseFare) ? 0 : decimal.Parse(model.InBoundBaseFare);
                    tp.OutBoundBaseFare = string.IsNullOrEmpty(model.OutBoundBaseFare) ? 0 : decimal.Parse(model.OutBoundBaseFare);
                    tp.isFareChangePermissionGranted = false;
                    tp.isOverRided = false;

                    if (bool.Parse(model.IsLaterBooking))
                    {
                        tp.BookingTypeID = (int)BookingTypes.Later;

                        if (tp.BookingModeID == (int)BookingModes.UserApplication || tp.BookingModeID == (int)BookingModes.Karhoo)
                            tp.PickUpBookingDateTime = Convert.ToDateTime(model.LaterBookingDate).AddSeconds(-Convert.ToDouble(model.TimeZoneOffset));
                        else //Portal ride request laterBookingDate is already converted to UTC by server.
                            tp.PickUpBookingDateTime = Convert.ToDateTime(model.LaterBookingDate);

                        //pr.pickUpDateTime = Convert.ToDateTime(model.laterBookingDate).AddSeconds(-Convert.ToDouble(model.timeZoneOffset));
                        bookingRN.PickUpDateTime = ((DateTime)tp.PickUpBookingDateTime).ToString(Formats.DateFormat);

                        await FirebaseService.AddPendingLaterBookings(tp.UserID.ToString(), tp.TripID.ToString(), ((DateTime)tp.PickUpBookingDateTime).ToString(Formats.DateFormat), bookingRN.SeatingCapacity);

                        await FirebaseService.SetPassengerLaterBookingCancelReasons();
                    }

                    dbContext.Trips.Add(tp);
                    dbContext.SaveChanges();
                }

                /* REFACTOR
                 * This check seems to be unnecessary, because in case of later booking we have already checked and returned discount details 
                 *in estimated fare api response and same are forwarded here in this request.
                 BUT FOR LATER BOOKIN REQUEST FROM WEB PORTAL THIS CHECK MAY BE REQUIRED*/

                var result = new BookTripResponse();

                if (bool.Parse(model.IsLaterBooking))
                {
                    //result.DiscountType = "normal";
                    //result.DiscountAmount = "0.00";

                   var specialPromoDetails = await FareManagerService.IsSpecialPromotionApplicable(model.PickUpLatitude, model.PickUpLongitude, model.DropOffLatitude, model.DropOffLongitutde, applicationId, true, DateTime.Parse(bookingRN.PickUpDateTime));

                    bookingRN.DiscountType = specialPromoDetails.DiscountType;
                    bookingRN.DiscountAmount = specialPromoDetails.DiscountAmount;
                }
                else
                {
                    // in case of normal booking discount is applied only if dropoff location is provided (which hits estimated fare api)
                    bookingRN.DiscountType = string.IsNullOrEmpty(model.DiscountType) ? "normal" : model.DiscountType;
                    bookingRN.DiscountAmount = string.IsNullOrEmpty(model.PromoDiscountAmount) ? "0.00" : model.PromoDiscountAmount;
                }

                bookingRN.TripId = tp.TripID.ToString();
                bookingRN.PickUpLatitude = tp.PickupLocationLatitude;
                bookingRN.PickUpLongitude = tp.PickupLocationLongitude;
                bookingRN.DropOffLatitude = tp.DropOffLocationLatitude;
                bookingRN.DropOffLongitude = tp.DropOffLocationLongitude;
                bookingRN.PickUpLocation = tp.PickUpLocation;
                bookingRN.DropOffLocation = tp.DropOffLocation;
                bookingRN.NumberOfPerson = Convert.ToInt32(tp.NoOfPerson).ToString();
                bookingRN.PaymentMethod = tp.TripPaymentMode;
                bookingRN.EstimatedPrice = FareManagerService.FormatFareValue(
                                        decimal.Parse(
                                            string.Format("{0:0.00}", tp.InBoundBaseFare + tp.InBoundDistanceFare + tp.InBoundTimeFare + tp.InBoundSurchargeAmount +
                                            tp.OutBoundBaseFare + tp.OutBoundDistanceFare + tp.OutBoundTimeFare + tp.OutBoundSurchargeAmount)
                                        )
                                    ).ToString("0.00");

                //Send FCM of new / updated trip to online drivers
                //string tripId, string passengerId, int reqSeatingCapacity, DriverBookingRequestNotification bookingRN, dynamic hotelSetting
                await FirebaseService.SendRideRequestToOnlineDrivers(bookingRN.TripId, model.PassengerId, Convert.ToInt32(model.SeatingCapacity), bookingRN, null);

                result.RequestTime = timeOut.ToString();
                result.TripId = tp.TripID.ToString();
                result.IsLaterBooking = model.IsLaterBooking;

                //response.error = false;
                //response.message = AppMessage.msgSuccess;
                //response.data = dic;

                ////If rideRequest is generated using code
                //if (Request == null)
                //    return request.CreateResponse(HttpStatusCode.OK, response);
                //else
                //    return Request.CreateResponse(HttpStatusCode.OK, response);

                return result;
            }
        }

        public static async Task<string> TimeOutTrip(string tripId, string passengerId)
        {
            var tp = await GetPassengerTripById(tripId, passengerId);

            if (tp == null)
            {
                return ResponseKeys.notFound;
            }

            if (tp.TripStatusID != (int)TripStatuses.RequestSent) //Ride already cancelled or accepted.
            {
                return ResponseKeys.tripAlreadyBooked;
            }

            await FirebaseService.DeleteTrip(tripId);
            await FirebaseService.DeletePassengerTrip(passengerId);       //New implementation

            using (var dbContext = new CangooEntities())
            {
                tp.TripStatusID = (int)TripStatuses.TimeOut;
                tp.TripEndDatetime = DateTime.UtcNow;
                await dbContext.SaveChangesAsync();
            }

            return ResponseKeys.msgSuccess;
        }

        public static async Task<ResponseWrapper> CancelTripByPassenger(string tripId, string passengerId, string distanceTravelled, string cancelId, string isLaterBooking)
        {
            using (var dbContext = new CangooEntities())
            {
                var result = new CancelTripResponse
                {
                    TripId = tripId,
                    IsLaterBooking = isLaterBooking
                };
                
                double estimatedDistance = 0;
                var trip = await GetPassengerTripById(tripId, passengerId);

                if (trip != null)
                {
                    estimatedDistance = await FirebaseService.GetTripEstimatedDistanceOnArrival(trip.CaptainID.ToString());
                }

                var tp = dbContext.spPassengerCancelRide(tripId, int.Parse(cancelId), (int)TripStatuses.Cancel,
                        (double.Parse(distanceTravelled) <= estimatedDistance ? double.Parse(distanceTravelled) : estimatedDistance) / 100).FirstOrDefault();

                //In case later booking was not accepted by any captain then that is not an error.
                if (tp == null)
                {
                    if (bool.Parse(isLaterBooking))
                    {
                        await FirebaseService.DeletePendingLaterBooking(tripId);
                    }

                    return new ResponseWrapper
                    {
                        Message = ResponseKeys.msgSuccess,
                        Error = false,
                        Data = result
                    };
                }

                await FirebaseService.UpdateDriverEarnedPoints(tp.CaptainID.ToString(), tp.EarningPoints.ToString());

                //TBD: Check online driver in case of later booking.

                if (bool.Parse(isLaterBooking))
                {
                    await FirebaseService.DeleteUpcomingLaterBooking(tp.CaptainID.ToString());
                    await FirebaseService.DeletePendingLaterBooking(tripId);
                    
                    var upcomingBooking = await DriverService.GetUpcomingLaterBookings(tp.CaptainID.ToString());
                    await FirebaseService.DeleteUpcomingLaterBooking(tp.CaptainID.ToString());
                    await FirebaseService.AddUpcomingLaterBooking(tp.CaptainID.ToString(), upcomingBooking);
                }
                else
                {
                    await FirebaseService.DeleteTrip(tripId);
                    //driver set as busy
                    await FirebaseService.SetDriverBusy(tp.CaptainID.ToString(), tripId);
                    //to avoid login on another device during trip
                    await FirebaseService.DeletePassengerTrip(passengerId);
                }

                await PushyService.UniCast(tp.deviceToken, result, NotificationKeys.cap_rideCancel);

                return new ResponseWrapper
                {
                    Message = ResponseKeys.msgSuccess,
                    Error = false,
                    Data = result
                };
            }
        }

        private static async Task<CompanyVoucher> GetVoucherDetails(Guid voucherId)
        {
            using (var dbContext = new CangooEntities())
            {
                return dbContext.CompanyVouchers.Where(cv => cv.VoucherID == voucherId && cv.isUsed == false).FirstOrDefault();
            }
        }

        private static async Task LogReRoutedTrip(BookTripRequest model, string applicationId, Trip tp)
        {
            using (var dbContext = new CangooEntities())
            {
                //TBD: Add captain priority points
                dbContext.ReroutedRidesLogs.Add(new ReroutedRidesLog()
                {
                    CaptainID = Guid.Parse(model.DriverID),
                    ReroutedLogID = Guid.NewGuid(),
                    LogTime = DateTime.UtcNow,
                    TripID = tp.TripID,
                    CancelID = tp.CancelID ?? null,
                    RideAcceptanceTime = tp.PickUpBookingDateTime,
                    isLaterBooking = bool.Parse(model.IsLaterBooking),//(bool)tp.isLaterBooking,
                    ApplicationID = Guid.Parse(applicationId)
                });

                await dbContext.SaveChangesAsync();
            }
        }

        public static async Task LogBookRequestRecipientDrivers(List<TripRequestLogDTO> drivers)
        {
            using (var dbContext = new CangooEntities())
            {
                var recipents = AutoMapperConfig._mapper.Map<List<TripRequestLogDTO>, List<TripRequestLog>>(drivers);

                dbContext.TripRequestLogs.AddRange(recipents);
                await dbContext.SaveChangesAsync();
            }
        }
    }
}