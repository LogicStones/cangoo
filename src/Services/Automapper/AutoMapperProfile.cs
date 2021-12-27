using AutoMapper;
using DatabaseModel;
using DTOs.API;
using DTOs.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Automapper
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            MapLists();
            MapSingle();
        }

        private void MapLists()
        {
            //CreateMap<List<KeywordDTO>, List<KeywordJson>>();
            //CreateMap<List<NotificationTypeDTO>, List<NotificationTypeJson>>();
            //CreateMap<List<SubscriptionPlanDTO>, List<SubscriptionPlanJson>>();
        }

        private void MapSingle()
        {
            CreateMap<CancelReason, PassengerCancelReasonsDTO>().ReverseMap();
            CreateMap<CancelReason, DriverCancelReasonsDTO>().ReverseMap();
            CreateMap<Facility, PassengerFacilityDTO>().ReverseMap();
            CreateMap<Facility, DriverFacilityDTO>().ReverseMap();
            CreateMap<AspNetUser, PassengerIdentityDTO>().ReverseMap();
            CreateMap<UserProfile, PassengerProfileDTO>().ReverseMap();
            CreateMap<PassengerPlace, AddPassengerPlaceRequest>().ReverseMap();
            CreateMap<TrustedContact, UpdateTrustedContactRequest>().ReverseMap();
            CreateMap<spGetOnlineDriver_Result, DatabaseOlineDriversDTO>().ReverseMap();
            CreateMap<TripRequestLogDTO, TripRequestLog>().ReverseMap();
            CreateMap<DispatchedRideLogDTO, DispatchedRidesLog>().ReverseMap();

            //Following 3 mappings are quick fix for ride FirebaseService.SendRideRequestToOnlineDrivers
            CreateMap<DriverBookingRequestNotification, FirebasePassenger>()
                .ForMember(dest => dest.BookingMode, opt => opt.MapFrom(src => src.bookingMode))
                .ForMember(dest => dest.BookingModeId, opt => opt.MapFrom(src => src.BookingModeId))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.description))
                .ForMember(dest => dest.DiscountAmount, opt => opt.MapFrom(src => src.discountAmount))
                .ForMember(dest => dest.DiscountType, opt => opt.MapFrom(src => src.discountType))
                .ForMember(dest => dest.DropOffLatitude, opt => opt.MapFrom(src => src.dropOfflatitude))
                .ForMember(dest => dest.DropOffLongitude, opt => opt.MapFrom(src => src.dropOfflongitude))
                .ForMember(dest => dest.DropOffLocation, opt => opt.MapFrom(src => src.dropOffLocation))
                .ForMember(dest => dest.IsDispatchedRide, opt => opt.MapFrom(src => src.isDispatchedRide.ToString()))
                .ForMember(dest => dest.IsFavorite, opt => opt.MapFrom(src => src.fav.ToString()))
                .ForMember(dest => dest.IsLaterBooking, opt => opt.MapFrom(src => src.isLaterBooking.ToString()))
                .ForMember(dest => dest.IsReRouteRequest, opt => opt.MapFrom(src => src.isReRouteRequest.ToString()))
                .ForMember(dest => dest.IsWeb, opt => opt.MapFrom(src => src.isWeb.ToString()))
                .ForMember(dest => dest.PickUpDateTime, opt => opt.MapFrom(src => src.pickUpDateTime))
                .ForMember(dest => dest.MidwayStop1Latitude, opt => opt.MapFrom(src => src.MidwayStop1Latitude))
                .ForMember(dest => dest.MidwayStop1Longitude, opt => opt.MapFrom(src => src.MidwayStop1Longitude))
                .ForMember(dest => dest.MidwayStop1Location, opt => opt.MapFrom(src => src.MidwayStop1Location))
                .ForMember(dest => dest.PaymentMethod, opt => opt.MapFrom(src => src.paymentMethod))
                .ForMember(dest => dest.PaymentModeId, opt => opt.MapFrom(src => src.PaymentModeId))
                .ForMember(dest => dest.PickUpLatitude, opt => opt.MapFrom(src => src.lat))
                .ForMember(dest => dest.PickUpLongitude, opt => opt.MapFrom(src => src.lan))
                .ForMember(dest => dest.PickUpLocation, opt => opt.MapFrom(src => src.pickUpLocation))
                .ForMember(dest => dest.PreviousCaptainId, opt => opt.MapFrom(src => src.previousCaptainId))
                .ForMember(dest => dest.RequestTimeOut, opt => opt.MapFrom(src => src.requestTimeOut.ToString()))
                .ForMember(dest => dest.RequiredFacilities, opt => opt.MapFrom(src => src.requiredFacilities))
                .ForMember(dest => dest.ReRouteRequestTime, opt => opt.MapFrom(src => src.reRouteRequestTime))
                .ForMember(dest => dest.TripId, opt => opt.MapFrom(src => src.tripID))
                .ForMember(dest => dest.VoucherAmount, opt => opt.MapFrom(src => src.voucherAmount))
                .ForMember(dest => dest.VoucherCode, opt => opt.MapFrom(src => src.voucherCode))
                .ForMember(dest => dest.TotalFare, opt => opt.MapFrom(src => src.estimatedPrice))
                .ForMember(dest => dest.DispatcherId, opt => opt.MapFrom(src => src.dispatcherID))


                .ForMember(dest => dest.WalletBalance, opt => opt.MapFrom(src => src.WalletBalance))
                .ForMember(dest => dest.CustomerId, opt => opt.MapFrom(src => src.CustomerId))
                .ForMember(dest => dest.CardId, opt => opt.MapFrom(src => src.CardId))
                .ForMember(dest => dest.Last4Digits, opt => opt.MapFrom(src => src.Last4Digits))
                .ForMember(dest => dest.Brand, opt => opt.MapFrom(src => src.Brand))
                .ForMember(dest => dest.PaymentModeId, opt => opt.MapFrom(src => src.PaymentModeId))

                .ForMember(dest => dest.Facilities, opt => opt.MapFrom(src => src.facilities))
                .ForMember(dest => dest.CancelReasons, opt => opt.MapFrom(src => src.lstCancel)).ReverseMap();

            CreateMap<DriverFacilityDTO, PassengerFacilityDTO>()
                .ForMember(dest => dest.FacilityID, opt => opt.MapFrom(src => src.facilityID))
                .ForMember(dest => dest.FacilityIcon, opt => opt.MapFrom(src => src.facilityIcon))
                .ForMember(dest => dest.FacilityName, opt => opt.MapFrom(src => src.facilityName)).ReverseMap();

            CreateMap<DriverCancelReasonsDTO, PassengerCancelReasonsDTO>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.id))
                .ForMember(dest => dest.Reason, opt => opt.MapFrom(src => src.reason)).ReverseMap();

            //.ForMember(dest => dest.DeviceToken, opt => opt.MapFrom(src => src.deviceToken))


            //.ForMember(dest => dest.EstimatedPrice, opt => opt.MapFrom(src => src.estimatedPrice))

            //.ForMember(dest => dest.IsLaterBookingStarted, opt => opt.MapFrom(src => src.isLaterBookingStarted.ToString()))
            //.ForMember(dest => dest.DriverContactNumber, opt => opt.MapFrom(src => src.))
            //.ForMember(dest => dest.DriverId, opt => opt.MapFrom(src => src.id))
            //.ForMember(dest => dest.DriverName, opt => opt.MapFrom(src => src.))
            //.ForMember(dest => dest.DriverPicture, opt => opt.MapFrom(src => src.))
            //.ForMember(dest => dest.DriverRating, opt => opt.MapFrom(src => src.))
            //.ForMember(dest => dest.SeatingCapacity, opt => opt.MapFrom(src => src.numberOfPerson.ToString()))
            //.ForMember(dest => dest.Model, opt => opt.MapFrom(src => src.))
            //.ForMember(dest => dest.Make, opt => opt.MapFrom(src => src.))
            //.ForMember(dest => dest.VehicleCategory, opt => opt.MapFrom(src => src.))
            //.ForMember(dest => dest.VehicleNumber, opt => opt.MapFrom(src => src.))
            //.ForMember(dest => dest.VehicleRating, opt => opt.MapFrom(src => src.))

            //CreateMap<Trip, TripDTO>().ReverseMap();



            //DestinationType obj = Mapper.Map<SourceType, DestinationType>(sourceValueObject);
            //List<DestinationType> listObj = Mapper.Map<List<SourceType>, List<DestinationType>>(enumarableSourceValueObject);

            //int noOfRowUpdated = ctx.Database.ExecuteSqlCommand("Update student set studentname = 'changed student by command' where studentid = 1");
            //int noOfRowInserted = ctx.Database.ExecuteSqlCommand("insert into student(studentname) values('New Student')");
            //int noOfRowDeleted = ctx.Database.ExecuteSqlCommand("delete from student where studentid = 1");

            //var result = await dbContext.UserProfiles.Where(up => up.UserID.Equals(profile.UserID)).FirstOrDefaultAsync();



            //CreateMap<UserDTO, UserJson>().ReverseMap();
            //CreateMap<UserDTO, UserProfileModelJson>().ReverseMap();
            //CreateMap<UserDTO, RegistrationModelJson>().ReverseMap();
            //CreateMap<UserInvoiceDTO, UserInvoiceJson>().ReverseMap();
            //CreateMap<AddNewInvoiceModelJson, InvoiceDTO>().ReverseMap();
            //CreateMap<UpdateInvoiceModelJson, InvoiceDTO>()
            //    .ForMember(dest => dest.TotalNetValue, opt => opt.MapFrom(src => src.InvoiceAmount));
            //CreateMap<UserNotificationDTO, NotificationJson>().ReverseMap();
            //CreateMap<UserFullInvoiceDTO, UserFullInvoiceJson>().ReverseMap();
            //CreateMap<SubscribeUserDTO, MySubscriptionModelJson>().ReverseMap();
            //CreateMap<UserFullInvoiceDTO.NotificationDTO, UserFullInvoiceJson.NotificationJson>().ReverseMap();
            //CreateMap<CountryDTO, CountryJson>().ReverseMap();
            //CreateMap<CountryCurrencyDTO, CurrencyJson>()
            //    .ForMember(dest => dest.Code, opt => opt.MapFrom(src => src.CurrencyCode))
            //    .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.CurrencyName)).ReverseMap();
            //CreateMap<UpdateStripeModelJson, StripeAccountInformationDTO>().ReverseMap();
            //CreateMap<InvoicePaymentDTO, InvoiceToPayModelJson>().ReverseMap();
        }
    }
}
