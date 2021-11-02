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

    public class GetPassengerPlacesResponse
    {
        public List<PlaceDetails> Places { get; set; } = new List<PlaceDetails>();
    }


    public class PlaceDetails
    {
        [Required]
        public int PlaceTypeId { get; set; } 

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
        public string PassengerId { get; set; }
    }

    public class AddPassengerPlaceResponse : PlaceDetails
    {
        public string ID { get; set; }
    }

    public class UpdatePassengerPlaceRequest : PlaceDetails
    {
        [Required]
        public int ID { get; set; }
    }


    public class GetRecentLocationResponse
    {
        public List<GetRecentLocationDetails> Locations { get; set; } = new List<GetRecentLocationDetails>();
    }

    public class GetRecentLocationDetails
    {
        public string DropOffLatitude { get; set; } = "";
        public string DropOffLongitude { get; set; } = "";
        public string DropOffLocation { get; set; } = "";
        public string PickUpLocPostalCode { get; set; } = "";
        public string DropOffLoacPostalCode { get; set; } = "";
        public string MidWayLocPostalCode { get; set; } = "";
    }
}
