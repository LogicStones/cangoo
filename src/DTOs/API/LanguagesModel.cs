using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTOs.API
{

    public class GetLanguageRequestRespose
    {
        public List<LanguagesDetail> Languages { get; set; } = new List<LanguagesDetail>();
    }

    public class LanguagesDetail
    {
        [Required]
        public int Id { get; set; }
        [Required]
        public string Language { get; set; }
        [Required]
        public string ShortName { get; set; }
        [Required]
        public string Format { get; set; }
    }

    public class UpdateLanguageRequest
    {
        [Required]
        public int Id { get; set; }
        [Required]
        public string PassengerId { get; set; }
    }
}
