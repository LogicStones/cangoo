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
            CreateMap<CancelReason, PassengerCancelReasonsDTO>();
            CreateMap<CancelReason, DriverCancelReasonsDTO>();

            CreateMap<Facility, PassengerFacilitiyDTO>();
            CreateMap<Facility, DriverFacilitiyDTO>();

            CreateMap<AspNetUser, PassengerIdentityDTO>();
            CreateMap<UserProfile, PassengerProfileDTO>();
            CreateMap<PassengerPlace, AddPassengerPlaceRequest>().ReverseMap();
            CreateMap<TrustedContact, UpdateTrustedContactRequest>().ReverseMap();
            CreateMap<spGetOnlineDriver_Result, DatabaseOlineDriversDTO>().ReverseMap();
            CreateMap<TripRequestLogDTO, TripRequestLog>().ReverseMap();
            CreateMap<DispatchedRideLogDTO, DispatchedRidesLog>();
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
