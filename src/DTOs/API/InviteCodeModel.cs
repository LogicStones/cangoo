using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTOs.API
{
    public class ApplyInviteCodeRequest
    {
        [Required]
        public string PassengerId { get; set; }
        [Required]
        public string InviteCode { get; set; }
    }
}
