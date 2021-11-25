using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTOs.API
{
    public class GetPassengerPlacesRequest
    {
        [Required]
        public string PassengerID { get; set; }
    }

    public class GetPassengerPlaces
    {
        public string ID { get; set; } = "";
        [Required]
        public string PlaceTypeId { get; set; } = "";

        [Required]
        public string Name { get; set; } = "";

        [Required]
        public string Address { get; set; } = "";

        [Required]
        public string Latitude { get; set; } = "";

        [Required]
        public string Longitutde { get; set; } = "";

        [Required]
        public string PostalCode { get; set; } = "";
    }


    public class PlaceDetails
    {
        [Required]
        public string Name { get; set; } = "";

        [Required]
        public string Address { get; set; } = "";

        [Required]
        public string Latitude { get; set; } = "";

        [Required]
        public string Longitutde { get; set; } = "";

        [Required]
        public string PostalCode { get; set; } = "";
    }


    public class AddPassengerPlaceRequest : PlaceDetails
    {
        [Required]
        public string PassengerId { get; set; } = "";

        [Required]
        public string PlaceTypeId { get; set; } = "";
    }

    public class AddPassengerPlaceResponse : PlaceDetails
    {
        public string ID { get; set; } = "";

        [Required]
        public string PlaceTypeId { get; set; } = "";
    }

    public class RecentLocationsListRequest
    {
        [Required]
        public string PassengerId { get; set; }
    }

    public class UpdatePassengerPlaceRequest : PlaceDetails
    {
        [Required]
        public string ID { get; set; } = "";

        [Required]
        public string PlaceTypeId { get; set; } = "";
    }

    public class GetPassengerPlaceRequest
    {
        [Required]
        public string PassengerId { get; set; }
    }


    //public class GetRecentLocationDetails
    //{
    //    public string DropOffLatitude { get; set; } = "";
    //    public string DropOffLongitude { get; set; } = "";
    //    public string DropOffLocation { get; set; } = "";
    //    public string DropOffLocationPostalCode { get; set; } = "";
    //    public string PickupLocationLatitude { get; set; } = "";
    //    public string PickupLocationLongitude { get; set; } = "";
    //    public string PickUpLocation { get; set; } = "";
    //    public string PickupLocationPostalCode { get; set; } = "";
    //    public string MidwayStop1Latitude { get; set; } = "";
    //    public string MidwayStop1Longitude { get; set; } = "";
    //    public string MidwayStop1Location { get; set; } = "";
    //    public string MidwayStop1PostalCode { get; set; } = "";
    //}
}
