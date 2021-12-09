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
using System.Data.Entity;
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
                details.FacilitiesList = await FacilitiesService.GetPassengerFacilitiesList();
                return details;
            }
        }

        public static async Task<int> UpdateTripPaymentMode(UpdateTripPaymentMethodRequest model)
        {
            using (CangooEntities dbcontext=new CangooEntities())
            {
                return (await dbcontext.Database.ExecuteSqlCommandAsync("UPDATE Trips SET PaymentModeId = @paymentmodeid WHERE TripID = @tripId AND UserID = @passengerId",
                                                                                      new SqlParameter("@paymentmodeid", model.PaymentModeId),
                                                                                      new SqlParameter("@tripId", model.TripId),
                                                                                      new SqlParameter("@passengerId", model.PassengerId)));
            }
        }

        public static async Task<int> UserSubmitFeedback(UpdateTripUserFeedback model)
        {
            using (CangooEntities dbcontext=new CangooEntities())
            {
                return await dbcontext.Database.ExecuteSqlCommandAsync("UPDATE Trips SET UserSubmittedFeedback = @userfeedback WHERE TripID = @tripId",
                                                                                      new SqlParameter("@userfeedback", model.UserFeedBack),
                                                                                      new SqlParameter("@tripId", model.TripId));
            }
        }

        public static async Task<Trip> GetTripById(string tripId)
        {
            using (var dbContext = new CangooEntities())
            {
                return await dbContext.Trips.Where(t => t.TripID.ToString().Equals(tripId)).FirstOrDefaultAsync();
            }
        }

        private static async Task<Trip> GetPassengerTripById(string tripId, string passengerId)
        {
            using (var dbContext = new CangooEntities())
            {
                return await dbContext.Trips.Where(t => t.TripID.ToString().Equals(tripId) && t.UserID.ToString().Equals(passengerId)).FirstOrDefaultAsync();
            }
        }

        public static async Task<ResponseWrapper> BookNewTrip(BookTripRequest model)
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
                    lat = model.PickUpLatitude,
                    lan = model.PickUpLongitude,
                    dropOfflatitude = model.DropOffLatitude,
                    dropOfflongitude = model.DropOffLongitude,
                    isLaterBooking = bool.Parse(model.IsLaterBooking),
                    numberOfPerson = int.Parse(model.SeatingCapacity),
                    requiredFacilities = model.RequiredFacilities,
                    isReRouteRequest = bool.Parse(model.IsReRouteRequest),
                    description = string.IsNullOrEmpty(model.Description) ? "" : model.Description,
                    requestTimeOut = timeOut,// double.Parse(timeOut.ToString()),
                    isDispatchedRide = false.ToString(),
                    fav = false,
                    facilities = await FacilitiesService.GetDriverFacilitiesDetailByIds(model.RequiredFacilities),
                    lstCancel = await CancelReasonsService.GetDriverCancelReasons(true, false, false),

                    BookingModeId = model.BookingModeId,
                    bookingMode = Enum.GetName(typeof(BookingModes), int.Parse(model.BookingModeId)).ToLower(),

                    discountType = Enum.GetName(typeof(DiscountTypes), (int)DiscountTypes.Normal).ToLower(),
                    discountAmount = "0.0",

                    dispatcherID = "",
                    estimatedPrice = model.TotalFare,

                    PaymentModeId = model.PaymentModeId,
                    CustomerId = model.CustomerId,
                    CardId = model.CardId,
                    Brand = model.Brand,
                    Last4Digits = model.Last4Digits,
                    WalletBalance = model.WalletBalance

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
                        bookingRN.pickUpDateTime = (DateTime)tp.PickUpBookingDateTime;
                    }

                    await LogReRoutedTrip(model, applicationId, tp);

                    if (tp.VoucherID != null)
                    {
                        var voucher = await GetVoucherDetails((Guid)tp.VoucherID);
                        bookingRN.voucherAmount = voucher.Amount.ToString();
                        bookingRN.voucherCode = voucher.VoucherCode;
                    }

                    bookingRN.description = tp.Description;
                    bookingRN.previousCaptainId = model.DriverId;
                    //bookingRN.DeviceToken = model.DeviceToken;

                    await dbContext.SaveChangesAsync();
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

                        bookingRN.isWeb = true;
                        bookingRN.isDispatchedRide = false.ToString();
                        bookingRN.BookingModeId = ((int)BookingModes.Karhoo).ToString();
                        bookingRN.bookingMode = Enum.GetName(typeof(BookingModes), (int)BookingModes.Karhoo).ToLower();
                    }
                    else if (int.Parse(model.BookingModeId) == (int)BookingModes.Dispatcher)
                    {
                        tp.TripID = Guid.NewGuid();
                        tp.BookingModeID = (int)BookingModes.Dispatcher;

                        bookingRN.isWeb = true;
                        bookingRN.isDispatchedRide = true.ToString();
                        bookingRN.BookingModeId = ((int)BookingModes.Dispatcher).ToString();
                        bookingRN.bookingMode = Enum.GetName(typeof(BookingModes), (int)BookingModes.Dispatcher).ToLower();
                    }
                    else//if (string.IsNullOrEmpty(model.BookingModeId))
                    {
                        tp.TripID = Guid.NewGuid();
                        tp.BookingModeID = (int)BookingModes.UserApplication;

                        bookingRN.isWeb = false;
                        bookingRN.isDispatchedRide = false.ToString();
                        bookingRN.BookingModeId = ((int)BookingModes.UserApplication).ToString();
                        bookingRN.bookingMode = Enum.GetName(typeof(BookingModes), (int)BookingModes.UserApplication).ToLower();


                        if (model.PaymentModeId.Equals(((int)PaymentModes.CreditCard).ToString()))
                        {
                            if (string.IsNullOrEmpty(model.CustomerId) || string.IsNullOrEmpty(model.CardId) || string.IsNullOrEmpty(model.TotalFare))
                            {
                                return new ResponseWrapper
                                {
                                    Error = true,
                                    Message = ResponseKeys.invalidParameters
                                };
                            }

                            var details = await WalletServices.AuthoizeCreditCardPayment(model.CustomerId, model.CardId, model.TotalFare);

                            if (!details.Status.Equals("requires_capture"))
                            {
                                return new ResponseWrapper
                                {
                                    Message = ResponseKeys.paymentGetwayError,
                                    Data = new BookTripResponse
                                    {
                                        CreditCardPaymentDetils = details
                                    }
                                };
                            }
                            tp.CreditCardPaymentIntent = details.PaymentIntentId;
                        }
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

                    tp.UserID = Guid.Parse(model.PassengerId);
                    tp.TripStatusID = (int)TripStatuses.RequestSent;
                    tp.isFareChangePermissionGranted = false;
                    tp.isOverRided = false;
                    tp.BookingDateTime = DateTime.UtcNow;
                    tp.TripPaymentMode = Enum.GetName(typeof(PaymentModes), int.Parse(model.PaymentModeId));// model.SelectedPaymentMethod;
                    tp.PaymentModeId = int.Parse(model.PaymentModeId);
                    tp.isLaterBooking = bool.Parse(model.IsLaterBooking);
                    tp.NoOfPerson = int.Parse(model.SeatingCapacity);
                    tp.BookingTypeID = (int)BookingTypes.Normal;
                    tp.isHotelBooking = false;
                    tp.Description = model.Description;
                    tp.facilities = model.RequiredFacilities;
                    tp.isReRouted = false;
                    tp.UTCTimeZoneOffset = int.Parse(model.TimeZoneOffset);
                    tp.VehicleCategoryId = int.Parse(model.CategoryId);

                    tp.InBoundDistanceInMeters = int.Parse(model.InBoundDistanceInMeters);
                    tp.OutBoundDistanceInMeters = int.Parse(model.OutBoundDistanceInMeters);
                    tp.DistanceTraveled = tp.InBoundDistanceInMeters + tp.OutBoundDistanceInMeters;

                    tp.InBoundTimeInSeconds = int.Parse(model.InBoundTimeInSeconds);
                    tp.OutBoundTimeInSeconds = int.Parse(model.OutBoundTimeInSeconds);

                    tp.InBoundDistanceFare = decimal.Parse(model.InBoundDistanceFare);
                    tp.OutBoundDistanceFare = decimal.Parse(model.OutBoundDistanceFare);

                    tp.InBoundTimeFare = decimal.Parse(model.InBoundTimeFare);
                    tp.OutBoundTimeFare = decimal.Parse(model.OutBoundTimeFare);

                    tp.InBoundSurchargeAmount = decimal.Parse(model.SurchargeAmount);   //Quick Fix : Without adding new column
                    tp.OutBoundSurchargeAmount = 0;

                    //BaseFare and BookingFare will be added from InBound only - So we'll use BaseFare and BookingFare fields in database
                    tp.InBoundBaseFare = 0;
                    tp.OutBoundBaseFare = 0;

                    tp.BaseFare = decimal.Parse(model.BaseFare) + decimal.Parse(model.FormattingAdjustment);
                    tp.BookingFare = decimal.Parse(model.BookingFare);
                    tp.WaitingFare = 0;// decimal.Parse(model.WaitingFare);
                    tp.PerKMFare = 0;
                    tp.PromoDiscount = 0;

                    tp.FareManagerID = model.InBoundRSFMId;
                    tp.DropOffFareMangerID = Guid.Parse(model.OutBoundRSFMId);

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
                        bookingRN.pickUpDateTime = ((DateTime)tp.PickUpBookingDateTime);//.ToString(Formats.DateFormat);

                        await FirebaseService.AddPendingLaterBookings(tp.UserID.ToString(), tp.TripID.ToString(), ((DateTime)tp.PickUpBookingDateTime).ToString(Formats.DateFormat), bookingRN.numberOfPerson.ToString());
                        await FirebaseService.SetPassengerLaterBookingCancelReasons();
                    }

                    tp.isSpecialPromotionApplied = model.DiscountType.Equals(Enum.GetName(typeof(DiscountTypes), (int)DiscountTypes.Special).ToLower());

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

                //if (bool.Parse(model.IsLaterBooking))
                //{
                //    //result.DiscountType = "normal";
                //    //result.DiscountAmount = "0.00";

                //    var specialPromoDetails = await FareManagerService.IsSpecialPromotionApplicable(model.PickUpLatitude, model.PickUpLongitude, model.DropOffLatitude, model.DropOffLongitude, applicationId, true, bookingRN.pickUpDateTime);

                //    bookingRN.discountType = specialPromoDetails.DiscountType;
                //    bookingRN.discountAmount = specialPromoDetails.DiscountAmount;
                //    //bookingRN.PromoCodeId = specialPromoDetails.PromoCodeId;
                //}
                //else
                //{
                //    // in case of normal booking discount is applied only if dropoff location is provided (which hits estimated fare api)
                //    bookingRN.discountType = model.DiscountType;
                //    bookingRN.discountAmount = model.DiscountAmount;
                //    //bookingRN.PromoCodeId = model.PromoCodeId;
                //    //bookingRN.DiscountType = string.IsNullOrEmpty(model.DiscountType) ? "normal" : model.DiscountType;
                //    //bookingRN.DiscountAmount = string.IsNullOrEmpty(model.PromoDiscountAmount) ? "0.00" : model.PromoDiscountAmount;
                //}

                bookingRN.discountType = model.DiscountType;
                bookingRN.discountAmount = model.DiscountAmount;

                bookingRN.tripID = tp.TripID.ToString();
                bookingRN.lat = tp.PickupLocationLatitude;
                bookingRN.lan = tp.PickupLocationLongitude;
                bookingRN.pickUpLocation = tp.PickUpLocation;

                bookingRN.MidwayStop1Latitude = tp.MidwayStop1Latitude;
                bookingRN.MidwayStop1Longitude = tp.MidwayStop1Longitude;
                bookingRN.MidwayStop1Location = tp.MidwayStop1Location;

                bookingRN.dropOfflatitude = tp.DropOffLocationLatitude;
                bookingRN.dropOfflongitude = tp.DropOffLocationLongitude;
                bookingRN.dropOffLocation = tp.DropOffLocation;

                bookingRN.paymentMethod = tp.TripPaymentMode;
                bookingRN.PaymentModeId = tp.PaymentModeId.ToString();

                //Send FCM of new / updated trip to online drivers
                //string tripId, string passengerId, int reqSeatingCapacity, DriverBookingRequestNotification bookingRN, dynamic hotelSetting

                //Explicitly create new thread to return API response.
                var task = Task.Run(async () =>
                {
                    await FirebaseService.SendRideRequestToOnlineDrivers(bookingRN.tripID, model.PassengerId, model.CategoryId.ToString(), int.Parse(model.SeatingCapacity), bookingRN);
                });

                return new ResponseWrapper
                {
                    Error = false,
                    Message = ResponseKeys.msgSuccess,
                    Data = new BookTripResponse
                    {
                        RequestTime = timeOut.ToString(),
                        TripId = tp.TripID.ToString(),
                        IsLaterBooking = model.IsLaterBooking
                    }
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
                var response = new CancelTripResponse
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
                        Data = response
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

                await PushyService.UniCast(tp.deviceToken, 
                    new DriverCancelRequestNotification
                    {
                        tripID = tripId,
                        isLaterBooking = bool.Parse(isLaterBooking)
                    }, 
                    NotificationKeys.cap_rideCancel);

                return new ResponseWrapper
                {
                    Message = ResponseKeys.msgSuccess,
                    Error = false,
                    Data = response
                };
            }
        }

        private static async Task<CompanyVoucher> GetVoucherDetails(Guid voucherId)
        {
            using (var dbContext = new CangooEntities())
            {
                return await dbContext.CompanyVouchers.Where(cv => cv.VoucherID == voucherId && cv.isUsed == false).FirstOrDefaultAsync();
            }
        }

        public static async Task<spGetDriverUserDeviceTokens_Result> GetDriverAndPassengerDeviceToken(string tripId)
        {
            using (var dbContext = new CangooEntities())
            {
                return dbContext.spGetDriverUserDeviceTokens(tripId).FirstOrDefault();
            }
        }

        public static async Task<spUpcomingLaterBookingDetailsForCancel_Result> GetUpcomingLaterBookingDetailsForCancel(string tripId)
        {
            using (var dbContext = new CangooEntities())
            {
                return dbContext.spUpcomingLaterBookingDetailsForCancel(tripId).FirstOrDefault();
            }
        }

        public static async Task<List<spGetUpcomingLaterBooking_Result>> GetUpcomingLaterBookings()
        {
            using (var dbContext = new CangooEntities())
            {
                return dbContext.spGetUpcomingLaterBooking(DateTime.UtcNow.ToString(), (int)TripStatuses.LaterBookingAccepted).ToList();
            }
        }

        public static async Task<spGetRideDetail_Result> GetRideDetail(string tripID, bool isWeb)
        {
            using (var dbContext = new CangooEntities())
            {
                return dbContext.spGetRideDetail(tripID, (int)TripStatuses.Picked, isWeb).FirstOrDefault();
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

        public static async Task<BookTripRequest> GetCancelledTripRequestObject(DriverCancelTripRequest req, spCaptainCancelRide_Result tp)
        {
            var discountDetails = await FirebaseService.GetTripDiscountDetails(tp.TripID.ToString());

            return new BookTripRequest
            {
                PickUpLatitude = tp.PickupLocationLatitude,
                PickUpLongitude = tp.PickupLocationLongitude,
                PickUpPostalCode = "",
                PickUpLocation = tp.PickUpLocation,

                MidwayStop1Latitude = "",
                MidwayStop1Longitude = "",
                MidwayStop1PostalCode = "",
                MidwayStop1Location = "",

                DropOffLatitude = tp.DropOffLocationLatitude,
                DropOffLongitude = tp.DropOffLocationLongitude,
                DropOffPostalCode = "",
                DropOffLocation = tp.DropOffLocation,

                PassengerId = tp.UserID.ToString(),
                PaymentModeId = tp.PaymentModeId.ToString(),//Enum.GetName(typeof(ResellerPaymentModes), tp.TripPaymentMode),
                LaterBookingDate = tp.PickUpBookingDateTime.ToString(),
                TripId = tp.TripID.ToString(),
                SeatingCapacity = tp.NoOfPerson.ToString(),
                RequiredFacilities = tp.facilities,
                IsLaterBooking = req.isLaterBooking.ToString(),
                DriverId = req.driverID,
                TimeZoneOffset = "0", //Will not be considered, in case of later booking reroute, PickUpBookingDateTime is being fetched from db
                IsReRouteRequest = true.ToString(),
                CategoryId = tp.VehicleCategoryId.ToString(),
                BookingModeId = tp.BookingModeID.ToString(),
                DiscountAmount = discountDetails.DiscountAmount,
                DiscountType = discountDetails.DiscountType,
                TotalFare = (await FareManagerService.GetTripCalculatedFare(tp.TripID.ToString())).ToString("0.00")
                //DeviceToken = tp.DeviceToken,
                //resellerArea = req.resellerArea,
            };

            //NEW IMPLEMENTATION : Adjusted in Firebase Service (SendRideRequestToOnlineDrivers)

            //string path = "Trips/" + req.tripID.ToString() + "/discount";

            //client = new FireSharp.FirebaseClient(config);
            //FirebaseResponse resp = client.Get(path);
            //if (!string.IsNullOrEmpty(resp.Body) && !resp.Body.Equals("null"))
            //{
            //    Dictionary<string, dynamic> discountDetails = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(resp.Body);
            //    pr.discountType = discountDetails["type"].ToString();
            //    pr.promoDiscountAmount = discountDetails["amount"].ToString();
            //}
        }
    }
}