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
            CreateMap<AspNetUser, PassengerIdentityDTO>();
            CreateMap<UserProfile, PassengerProfileDTO>();
            CreateMap<Facility, FacilitiyDTO>();
            CreateMap<PassengerPlace, AddPassengerPlaceRequest>().ReverseMap();
            CreateMap<TrustedContact, UpdateTrustedContactRequest>().ReverseMap();
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
