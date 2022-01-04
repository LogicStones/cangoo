using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTOs.API
{
    public class ApplyInviteCodeRequest
    {
        [Required]
        [DefaultValue("edf49e84-06fb-4a6d-9448-6011fc1bc611")]
        public string PassengerId { get; set; }

        [Required]
        [DefaultValue("aa3687")]
        public string InviteCode { get; set; }
    }
}