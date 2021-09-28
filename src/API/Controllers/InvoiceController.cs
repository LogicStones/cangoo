//using CanTaxi_Api.App_Start;
//using CanTaxi_Api.Models;
//using Rotativa;
using System;
using System.Web.Mvc;

namespace API.Controllers
{
    public class InvoiceController : Controller
	{
//		public ActionResult Header()
//		{
//			return View();
//		}
//		public ActionResult Footer()
//		{
//			return View();
//		}

//		public void SendInvoice(InvoiceModel model, string headerLink, string footerLink)
//		{
			
//			EmailManager.SendInvoice(model.CustomerEmail,
//												@"
//<pre>Hallo!

//Danke für deine Taxifahrt mit cangoo.
//Wir hoffen, dass du mit cangoo zufrieden bist und freuen uns dich bald wieder zu sehen.
//In der Beilage findest du deine Rechnung.

//Viel Freude mit cangoo,
//dein cangoo-Team

//Für weitere Fragen wende dich bitte an den Support <a href='mailto:info@cangoo.at'>hier</a>.</pre>
//",
//												"Deine cangoo Rechnung",
//												"rechnung@cangoo.at",
//												"Cangoo Rechnung",
//												"invoice.pdf",
//												GenerateInvoice(model, headerLink, footerLink));

//			//EmailManager.SendInvoice(model.CustomerEmail,
//			//                                 "<b>Your invoice is attached.</b>",
//			//                                 "Trip invoice with pdf",
//			//                                 "cs@cangoo.at",
//			//                                 "Customer Service",
//			//                                 "invoice.pdf",
//			//                                 GenerateInvoice(model, headerLink, footerLink));
//		}

//		public Byte[] GenerateInvoice(InvoiceModel model, string headerLink, string footerLink)
//		{
//			var invoice = new ViewAsPdf(model)
//			{
//				PageMargins = { Left = 0, Bottom = 42, Right = 0, Top = 44 },
//				FileName = "invoice.pdf",
//				PageOrientation = Rotativa.Options.Orientation.Portrait,
//				PageSize = Rotativa.Options.Size.A4,
//				CustomSwitches = string.Format("--print-media-type --allow {0} --footer-html {0} --header-html {1}", footerLink, headerLink)
//			};

//			return invoice.BuildFile(this.ControllerContext);
//		}
	}
}