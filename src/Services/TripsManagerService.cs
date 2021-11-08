﻿using Constants;
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

        public static async Task<Trip> GetTripById(string tripId)
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

        public static async Task<BookTripResponse> BookNewTrip(BookTripRequest model)
        {

            //TBD : Save Passenger and Driver Device info etc from filter

            using (CangooEntities dbContext = new CangooEntities())
            {
                var resellerId = ConfigurationManager.AppSettings["ResellerID"].ToString();
                var applicationId = ConfigurationManager.AppSettings["ApplicationID"].ToString();

                //Object to add / update trip in database
                //Trip tp = new Trip();

                //if later booking then write in pending later bookings to send FCM 2 min before pickup time if no one accepts booking
                //FireBaseController fc = new FireBaseController();

                //var lstcr = await CancelReasonsService.GetCancelReasons(tp.BookingTypeID == (int)BookingTypes.Normal ? true : false, tp.BookingTypeID == (int)BookingTypes.Later ? true : false, false);

                //List<FacilitiyDTO> facilities = new List<FacilitiyDTO>();

                //if (!string.IsNullOrEmpty(model.RequiredFacilities))
                //{
                //    facilities = await FacilitiesService.GetFacilitiesDetailByIds(model.RequiredFacilities);
                //}
                //else
                //    model.RequiredFacilities = "";

                int timeOut = await ApplicationSettingService.GetRequestTimeOut(applicationId);

                //Object to be used to populate captains FCM object
                DriverBookingRequestNotification bookingRN = new DriverBookingRequestNotification
                {
                    PickUpLatitude = model.PickUpLatitude,
                    PickUpLongitude = model.PickUpLongitude,
                    DropOffLatitude = model.DropOffLatitude,
                    DropOffLongitude = model.DropOffLongitude,
                    IsLaterBooking = model.IsLaterBooking,
                    SeatingCapacity = model.SeatingCapacity,
                    RequiredFacilities = model.RequiredFacilities,
                    IsReRouteRequest = model.IsReRouteRequest,
                    Description = string.IsNullOrEmpty(model.Description) ? "" : model.Description,
                    RequestTimeOut = timeOut.ToString(),
                    IsDispatchedRide = false.ToString(),
                    IsFavorite = false.ToString(),
                    Facilities = await FacilitiesService.GetFacilitiesDetailByIds(model.RequiredFacilities),
                    CancelReasons = await CancelReasonsService.GetCancelReasons(true, false, false),

                    //IsWeb = false,
                    //REFACTOR - Remove this flag
                    //isLaterBookingStarted = false
                };

                Trip tp = new Trip();
                if (bool.Parse(model.IsReRouteRequest))
                {
                    tp = await GetTripById(model.TripId);
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
                    bookingRN.PreviousCaptainId = model.DriverId;
                    //bookingRN.DeviceToken = model.DeviceToken;
                }
                else
                {
                    /*TBD: Replace isHotelBooking and isLaterBooking logic with BookingModes = 4
                    and BookingTypes = 2 respectively*/
                    //if (string.IsNullOrEmpty(model.karhooTripID))

                    if (int.Parse(model.BookingModeId) == (int)BookingModes.Karhoo)
                    {
                        tp.TripID = Guid.Parse(model.KarhooTripId);
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
                        bookingRN.BookingMode = Enum.GetName(typeof(BookingModes), (int)BookingModes.UserApplication).ToLower();
                    }

                    tp.ApplicationID = Guid.Parse(applicationId);
                    tp.ResellerID = Guid.Parse(resellerId);
                    tp.PickupLocationLatitude = model.PickUpLatitude;
                    tp.PickupLocationLongitude = model.PickUpLongitude;
                    tp.PickupLocationPostalCode = model.PickUpPostalCode;
                    tp.PickUpLocation = model.PickUpLocation;

                    tp.MidwayStop1Latitude = model.MidwayStop1Latitude;
                    tp.MidwayStop1Longitude = model.MidwayStop1Longitude;
                    tp.MidwayStop1PostalCode = model.MidwayStop1PostalCode;
                    tp.MidwayStop1Location = model.MidwayStop1Location;

                    tp.DropOffLocationLatitude = model.DropOffLatitude;
                    tp.DropOffLocationLongitude = model.DropOffLongitude;
                    tp.DropOffLocationPostalCode = model.DropOffPostalCode;
                    tp.DropOffLocation = model.DropOffLocation;

                    //tp.MidwayStop1Latitude = string.IsNullOrEmpty(model.MidwayStop1Latitude) ? "0.00" : model.MidwayStop1Latitude;
                    //tp.MidwayStop1Longitude = string.IsNullOrEmpty(model.MidwayStop1Longitude) ? "0.00" : model.MidwayStop1Longitude;
                    //tp.MidwayStop1PostalCode = model.MidwayStop1PostalCode;
                    //tp.MidwayStop1Location = string.IsNullOrEmpty(model.MidwayStop1Location) ? "" : model.MidwayStop1Location;
                    //tp.DropOffLocationLatitude = string.IsNullOrEmpty(model.DropOffLatitude) ? "0.00" : model.DropOffLatitude;
                    //tp.DropOffLocationLongitude = string.IsNullOrEmpty(model.DropOffLongitutde) ? "0.00" : model.DropOffLongitutde;
                    //tp.DropOffLocationPostalCode = model.DropOffPostalCode;
                    //tp.DropOffLocation = string.IsNullOrEmpty(model.DropOffLocation) ? "" : model.DropOffLocation;

                    tp.UserID = Guid.Parse(model.PassengerId);
                    tp.TripStatusID = (int)TripStatuses.RequestSent;
                    tp.isFareChangePermissionGranted = false;
                    tp.isOverRided = false;
                    tp.BookingDateTime = DateTime.UtcNow;
                    tp.TripPaymentMode = model.SelectedPaymentMethod;
                    tp.PaymentModeId = int.Parse(model.SelectedPaymentMethodId);
                    tp.isLaterBooking = bool.Parse(model.IsLaterBooking);
                    tp.NoOfPerson = int.Parse(model.SeatingCapacity);
                    tp.BookingTypeID = (int)BookingTypes.Normal;
                    tp.isHotelBooking = false;
                    tp.Description = model.Description;
                    tp.facilities = model.RequiredFacilities;
                    tp.isReRouted = false;
                    tp.UTCTimeZoneOffset = int.Parse(model.TimeZoneOffset);

                    tp.InBoundDistanceInMeters = int.Parse(model.InBoundDistanceInMeters);
                    tp.OutBoundDistanceInMeters = int.Parse(model.OutBoundDistanceInMeters);
                    tp.DistanceTraveled = tp.InBoundDistanceInMeters + tp.OutBoundDistanceInMeters;

                    tp.InBoundTimeInSeconds = int.Parse(model.InBoundTimeInSeconds);
                    tp.OutBoundTimeInSeconds = int.Parse(model.OutBoundTimeInSeconds);

                    tp.InBoundDistanceFare = decimal.Parse(model.InBoundDistanceFare);
                    tp.OutBoundDistanceFare = decimal.Parse(model.OutBoundDistanceFare);

                    tp.InBoundTimeFare = decimal.Parse(model.InBoundTimeFare);
                    tp.OutBoundTimeFare = decimal.Parse(model.OutBoundTimeFare);

                    tp.BaseFare = decimal.Parse(model.BaseFare) + decimal.Parse(model.FormattingAdjustment);
                    tp.BookingFare = decimal.Parse(model.BookingFare);
                    tp.WaitingFare = decimal.Parse(model.WaitingFare);
                    tp.InBoundSurchargeAmount = decimal.Parse(model.SurchargeAmount);   //Quick Fix : Without adding new column

                    tp.OutBoundSurchargeAmount = 0;
                    tp.InBoundBaseFare = 0;
                    tp.OutBoundBaseFare = 0;

                    //tp.InBoundDistanceInMeters = string.IsNullOrEmpty(model.InBoundDistanceInMeters) ? 0 : int.Parse(model.InBoundDistanceInMeters);
                    //tp.OutBoundDistanceInMeters = string.IsNullOrEmpty(model.OutBoundDistanceInMeters) ? 0 : int.Parse(model.OutBoundDistanceInMeters);
                    //tp.DistanceTraveled = tp.InBoundDistanceInMeters + tp.OutBoundDistanceInMeters;

                    //tp.InBoundTimeInSeconds = string.IsNullOrEmpty(model.InBoundTimeInSeconds) ? 0 : int.Parse(model.InBoundTimeInSeconds);
                    //tp.OutBoundTimeInSeconds = string.IsNullOrEmpty(model.OutBoundTimeInSeconds) ? 0 : int.Parse(model.OutBoundTimeInSeconds);

                    //tp.InBoundDistanceFare = string.IsNullOrEmpty(model.InBoundDistanceFare) ? 0 : decimal.Parse(model.InBoundDistanceFare);
                    //tp.OutBoundDistanceFare = string.IsNullOrEmpty(model.OutBoundDistanceFare) ? 0 : decimal.Parse(model.OutBoundDistanceFare);

                    //tp.InBoundTimeFare = string.IsNullOrEmpty(model.InBoundTimeFare) ? 0 : decimal.Parse(model.InBoundTimeFare);
                    //tp.OutBoundTimeFare = string.IsNullOrEmpty(model.OutBoundTimeFare) ? 0 : decimal.Parse(model.OutBoundTimeFare);

                    //tp.BaseFare = string.IsNullOrEmpty(model.BaseFare) ? 0 : (decimal.Parse(model.BaseFare) + decimal.Parse(model.FormattingAdjustment));
                    //tp.BookingFare = string.IsNullOrEmpty(model.BookingFare) ? 0 : decimal.Parse(model.BookingFare);
                    //tp.WaitingFare = string.IsNullOrEmpty(model.WaitingFare) ? 0 : decimal.Parse(model.WaitingFare);

                    //tp.InBoundSurchargeAmount = string.IsNullOrEmpty(model.SurchargeAmount) ? 0 : decimal.Parse(model.SurchargeAmount);

                    if (!string.IsNullOrEmpty(model.PromoCodeId))
                        tp.PromoCodeID = Guid.Parse(model.PromoCodeId);

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
                    await dbContext.SaveChangesAsync();
                }

                /* REFACTOR
                 * This check seems to be unnecessary, because in case of later booking we have already checked and returned discount details 
                 *in estimated fare api response and same are forwarded here in this request.
                 BUT FOR LATER BOOKIN REQUEST FROM WEB PORTAL THIS CHECK MAY BE REQUIRED*/


                //REFACTOR
                //Following check seems to be un-necessary, set the value in bookingRN initialization and check for
                //special promotions in estimate fare API.

                if (bool.Parse(model.IsLaterBooking))
                {
                    //result.DiscountType = "normal";
                    //result.DiscountAmount = "0.00";

                    var specialPromoDetails = await FareManagerService.IsSpecialPromotionApplicable(model.PickUpLatitude, model.PickUpLongitude, model.DropOffLatitude, model.DropOffLongitude, applicationId, true, DateTime.Parse(bookingRN.PickUpDateTime));

                    bookingRN.DiscountType = specialPromoDetails.DiscountType;
                    bookingRN.DiscountAmount = specialPromoDetails.DiscountAmount;
                }
                else
                {
                    // in case of normal booking discount is applied only if dropoff location is provided (which hits estimated fare api)
                    bookingRN.DiscountType = model.DiscountType;
                    bookingRN.DiscountAmount = model.PromoDiscountAmount;
                    //bookingRN.DiscountType = string.IsNullOrEmpty(model.DiscountType) ? "normal" : model.DiscountType;
                    //bookingRN.DiscountAmount = string.IsNullOrEmpty(model.PromoDiscountAmount) ? "0.00" : model.PromoDiscountAmount;
                }

                bookingRN.TripId = tp.TripID.ToString();
                bookingRN.PickUpLatitude = tp.PickupLocationLatitude;
                bookingRN.PickUpLongitude = tp.PickupLocationLongitude;
                bookingRN.PickUpLocation = tp.PickUpLocation;

                bookingRN.MidwayStop1Latitude = tp.MidwayStop1Latitude;
                bookingRN.MidwayStop1Longitude = tp.MidwayStop1Longitude;
                bookingRN.MidwayStop1Location = tp.MidwayStop1Location;

                bookingRN.DropOffLatitude = tp.DropOffLocationLatitude;
                bookingRN.DropOffLongitude = tp.DropOffLocationLongitude;
                bookingRN.DropOffLocation = tp.DropOffLocation;

                bookingRN.PaymentMethod = tp.TripPaymentMode;
                bookingRN.PaymentModeId = tp.PaymentModeId.ToString();
                bookingRN.EstimatedPrice = ((decimal)tp.InBoundDistanceFare + (decimal)tp.OutBoundDistanceFare + (decimal)tp.InBoundTimeFare + (decimal)tp.OutBoundTimeFare +
                                            (decimal)tp.BaseFare + (decimal)tp.BookingFare + (decimal)tp.WaitingFare + (decimal)tp.InBoundSurchargeAmount +
                                            (decimal)tp.OutBoundSurchargeAmount + (decimal)tp.InBoundBaseFare + (decimal)tp.OutBoundBaseFare).ToString("0.00");

                //Send FCM of new / updated trip to online drivers
                //string tripId, string passengerId, int reqSeatingCapacity, DriverBookingRequestNotification bookingRN, dynamic hotelSetting
                await FirebaseService.SendRideRequestToOnlineDrivers(bookingRN.TripId, model.PassengerId, Convert.ToInt32(model.SeatingCapacity), bookingRN, null);

                return new BookTripResponse
                {
                    RequestTime = timeOut.ToString(),
                    TripId = tp.TripID.ToString(),
                    IsLaterBooking = model.IsLaterBooking
                };

                //response.error = false;
                //response.message = AppMessage.msgSuccess;
                //response.data = dic;

                ////If rideRequest is generated using code
                //if (Request == null)
                //    return request.CreateResponse(HttpStatusCode.OK, response);
                //else
                //    return Request.CreateResponse(HttpStatusCode.OK, response);

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
                        await FirebaseService.DeleteTrip(tripId);

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
                    await FirebaseService.DeleteTrip(tripId);
                    
                    var upcomingBooking = await DriverService.GetUpcomingLaterBooking(tp.CaptainID.ToString());
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


        public static async Task<spGetRideDetail_Result> GetRideDetail(string tripID, bool isWeb)
        {
            using (var dbContext = new CangooEntities())
            {
                return dbContext.spGetRideDetail(tripID, (int)TripStatuses.PaymentPending, isWeb).FirstOrDefault();
            }
        }

        private static async Task LogReRoutedTrip(BookTripRequest model, string applicationId, Trip tp)
        {
            using (var dbContext = new CangooEntities())
            {
                //TBD: Add captain priority points
                dbContext.ReroutedRidesLogs.Add(new ReroutedRidesLog()
                {
                    CaptainID = Guid.Parse(model.DriverId),
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
        public static async Task LogDispatchedTrips(DispatchedRideLogDTO dispatchedTrip)
        {
            using (var dbContext = new CangooEntities())
            {
                var trip = AutoMapperConfig._mapper.Map<DispatchedRideLogDTO, DispatchedRidesLog>(dispatchedTrip);

                dbContext.DispatchedRidesLogs.Add(trip);
                await dbContext.SaveChangesAsync();
            }
        }

    }
}