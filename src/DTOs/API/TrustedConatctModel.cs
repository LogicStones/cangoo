using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTOs.API
{
    public class UpdateTrustedContact : TrustedContactDetails
    {
        [Required]
        public string PassengerId { get; set; }
    }

    public class UpdateTrustedContactResponse
    {
        public string Name { get; set; }
        public string CountryCode { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
    }

    public class TrustedContactDetails
    {
        [Required]
        public string Name { get; set; }
        [Required]
        public string CountryCode { get; set; }
        [Required]
        public string PhoneNumber { get; set; }
        [Required]
        public string Email { get; set; }
        
    }

    public class GetTrustedContactResponse
    {
        public List<TrustedContactDetails> Contact { get; set; } = new List<TrustedContactDetails>();
    }
}
