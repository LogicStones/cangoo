using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTOs.API
{
    public class InvoiceModel
    {
        public string FleetEmail { get; set; }
        public string CustomerEmail { get; set; }
        public string TripDate { get; set; }
        public string CustomerName { get; set; }
        public string InvoiceNumber { get; set; }
        public string FleetName { get; set; }
        public string ATUNumber { get; set; }
        public string PostCode { get; set; }
        public string City { get; set; }
        public string Street { get; set; }
        public string BuildingNumber { get; set; }
        public string CaptainName { get; set; }
        public string CaptainUserName { get; set; }
        public string VehicleNumber { get; set; }
        public string PickUpAddress { get; set; }
        public string DropOffAddress { get; set; }
        public string Distance { get; set; }
        public string TotalAmount { get; set; }
        public string PromoDiscountAmount { get; set; }
        public string WalletUsedAmount { get; set; }
        public string CashAmount { get; set; }
    }
}
