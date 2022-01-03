using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTOs.API
{
    public class LanguagesDetail
    {
        public string Id { get; set; } = "";
        public string Language { get; set; } = "";
        public string ShortName { get; set; } = "";
    }

    public class UpdateLanguageRequest
    {
        [Required]
        [DefaultValue("1")]
        public string LanguageId { get; set; } = "";

        [Required]
        [DefaultValue("edf49e84-06fb-4a6d-9448-6011fc1bc611")]
        public string PassengerId { get; set; } = "";
    }
}
