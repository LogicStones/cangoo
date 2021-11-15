using DTOs.API;
using Stripe;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integrations
{
    public class StripeIntegration
    {
        public static Customer GetCustomer(string customerID)
        {
            SetStripAPIKey();
            var custService = new CustomerService();
            return custService.Get(customerID);
        }

        private static void SetStripAPIKey()
        {
            StripeConfiguration.ApiKey = ConfigurationManager.AppSettings["StripeSecretKey"].ToString();
        }

        public static List<StripeCard> GetCardsList(string customerID)
        {
            SetStripAPIKey();

            var service = new PaymentMethodService();
            var options = new PaymentMethodListOptions
            {
                Customer = customerID,
                Limit = 3,
                Type = "card"
            };

            var cards = service.List(options);

            List<StripeCard> lstCards = new List<StripeCard>();

            foreach (var item in cards)
            {
                //var temp = JsonConvert.DeserializeObject<Card>(card.ToString().Substring(card.ToString().LastIndexOf('>') + 8));
                lstCards.Add(new StripeCard()
                {
                    CardId = item.Id,
                    Brand = item.Card.Brand,
                    CardDescription = item.BillingDetails.Address.Line1,
                    CardHolderName = item.BillingDetails.Name,
                    ExpiryMonth = item.Card.ExpMonth.ToString(),
                    ExpiryYear = item.Card.ExpYear.ToString().Substring(2),
                    Last4Digits = item.Card.Last4
                });

                //var temp = JsonConvert.DeserializeObject<Card>(card.ToString().Substring(card.ToString().LastIndexOf('>') + 8));
                //lstCards.Add(new StripeCard()
                //{
                //    brand = temp.Brand,
                //    cardDescription = temp.AddressLine1,
                //    cardHolderName = temp.Name,
                //    cardId = temp.Id,
                //    expiryMonth = temp.ExpMonth.ToString(),
                //    expiryYear = temp.ExpYear.ToString().Substring(2),
                //    last4Digits = temp.Last4
                //});
            }
            return lstCards;
        }
    }
}
