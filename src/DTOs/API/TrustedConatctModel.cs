﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTOs.API
{
    public class UpdateTrustedContactRequest : TrustedContactDetails
    {
        [Required]
        public string PassengerId { get; set; } = "";
    }

    public class UpdateTrustedContactResponse
    {
        public string FirstName { get; set; } = "";
        public string CountryCode { get; set; } = "";
        public string MobileNo { get; set; } = "";
        public string Email { get; set; } = "";
    }

    public class TrustedContactDetails
    {
        [Required]
        public string FirstName { get; set; } = "";
        [Required]
        public string CountryCode { get; set; } = "";
        [Required]
        public string MobileNo { get; set; } = "";
        [Required]
        public string Email { get; set; } = "";
        
    }

    public class GetTrustedContactRequest
    {
        [Required]
        public string PassengerId { get; set; }
    }

    public class GetTrustedContact
    {
        [Required]
        public string Id { get; set; } = "";
        [Required]
        public string FirstName { get; set; } = "";
        [Required]
        public string CountryCode { get; set; } = "";
        [Required]
        public string MobileNo { get; set; } = "";
        [Required]
        public string Email { get; set; } = "";
    }

    public class GetTrustedContactResponse
    {
        public List<GetTrustedContact> Contact { get; set; } = new List<GetTrustedContact>();
    }
}
