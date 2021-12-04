﻿using DTOs.API;
using Serilog;
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
        public static string CreateCustomer(string passengerId, string email)
        {
            SetStripAPIKey();
            var customerOptions = new CustomerCreateOptions
            {
                Description = passengerId,
                Email = email,
            };

            var customerService = new CustomerService();
            Customer customer = customerService.Create(customerOptions);
            return customer.Id;
        }

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

        public static string GetSetupIntentClientSecret(string customerId)
        {
            SetStripAPIKey();

            var service = new SetupIntentService();
            var options = new SetupIntentCreateOptions
            {
                Customer = customerId,
                Usage = "on_session",
                PaymentMethodTypes = new List<string>
                {
                    "card"
                },

            };
            var setupIntent = service.Create(options);
            return setupIntent.ClientSecret;
        }

        public static Customer UpdateDefaultPaymentMethod(string defaultSourceId, string customerId)
        {
            SetStripAPIKey();

            var options = new CustomerUpdateOptions
            {
                InvoiceSettings = new CustomerInvoiceSettingsOptions
                {
                    DefaultPaymentMethod = defaultSourceId
                }
            };

            var service = new CustomerService();

            Customer customer = service.Update(customerId, options);

            if (customer != null)
                return customer;
            else
                return new Customer();
        }

        public static PaymentMethod DeleteCard(string cardId, string customerId)
        {
            SetStripAPIKey();

            var service = new PaymentMethodService();
            var card = service.Detach(cardId);

            if (card != null)
            {
                var cust = GetCustomer(customerId);
                if (string.IsNullOrEmpty(cust.InvoiceSettings.DefaultPaymentMethodId))
                {
                    var cards = GetCardsList(customerId);
                    if (cards.Any())
                    {
                        UpdateDefaultPaymentMethod(cards.FirstOrDefault().CardId, customerId);
                    }
                }
                return card;
            }
            else
            {
                return new PaymentMethod();
            }
        }

        public static PaymentIntent AuthorizePayment(string customerId, string cardId, long amount)
        {
            try
            {
                SetStripAPIKey();
                var customer = GetCustomer(customerId);

                var service = new PaymentIntentService();
                var options = new PaymentIntentCreateOptions
                {
                    Customer = customer.Id,
                    //SetupFutureUsage = "on_session",
                    Amount = amount,
                    Currency = "eur",
                    PaymentMethodTypes = new List<string>
                    {
                        "card"
                    },
                    //PaymentMethod = customer.InvoiceSettings.DefaultPaymentMethodId,
                    PaymentMethod = cardId,
                    Confirm = true,
                    OffSession = true,
                    CaptureMethod = "manual"
                };

                return service.Create(options);
            }
            catch (StripeException e)
            {
                switch (e.StripeError.Type)
                {
                    case "card_error":
                        // Error code will be authentication_required if authentication is needed
                        Log.Error("Error code: " + e.StripeError.Code);
                        var paymentIntentId = e.StripeError.PaymentIntent.Id;
                        var service = new PaymentIntentService();
                        var paymentIntent = service.Get(paymentIntentId);

                        Log.Error(paymentIntent.Id);
                        break;
                    default:
                        break;
                }
                return e.StripeError.PaymentIntent;
            }
        }

        public static PaymentIntent CancelAuthorizedPayment(string paymentIntentId)
        {
            SetStripAPIKey();
            
            var service = new PaymentIntentService();
            return service.Cancel(paymentIntentId);
        }

        //public static PaymentIntent UpdateAuthorizedPayment(string paymentIntentId, long amount)
        //{
        //    SetStripAPIKey();

        //    var options = new PaymentIntentUpdateOptions
        //    {
        //        Amount = amount
        //    };


        //    //Metadata = new Dictionary<string, string>
        //    //{
        //    //  { "order_id", "6735" },
        //    //},

        //    var service = new PaymentIntentService();
        //    return service.Update(paymentIntentId, options);
        //}

        public static PaymentIntent CaptureAuthorizedPaymentPartially(string paymentIntentId, long amount)
        {
            SetStripAPIKey();
            var options = new PaymentIntentCaptureOptions
            {
                AmountToCapture = amount,
            };

            var service = new PaymentIntentService();
            return service.Capture(paymentIntentId, options);
        }

        public static PaymentIntent CaptureAuthorizedPayment(string paymentIntentId)
        {
            SetStripAPIKey();
            
            var service = new PaymentIntentService();
            return service.Capture(paymentIntentId);
        }
    
    }
}