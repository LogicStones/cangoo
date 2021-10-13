using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTOs.API
{
    public class PromoCodeDetails
    {
        public string Title { get; set; }
        public string ShortDescription { get; set; }
        public string Details { get; set; }
        public string CreatedAt { get; set; }
    }

    public class GetPromoCodeRespose
    {
        public List<PromoCodeDetails> Codes { get; set; } = new List<PromoCodeDetails>();
    }
}
