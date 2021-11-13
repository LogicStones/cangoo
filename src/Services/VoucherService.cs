using Constants;
using DatabaseModel;
using DTOs.API;
using Services.Automapper;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services
{
    public class VoucherService
    {
        public static void RefundFullVoucherAmount(Trip trip)
        {
            using (var dbContext = new CangooEntities())
            {
                var voucher = dbContext.CompanyVouchers.Where(cv => cv.VoucherID == trip.VoucherID && cv.isUsed == false).FirstOrDefault();
                if (voucher != null)
                {
                    var company = dbContext.Companies.Where(c => c.CompanyID == voucher.CompanyID).FirstOrDefault();
                    company.CompanyBalance += voucher.Amount;
                    voucher.Amount = Convert.ToDecimal(0);
                    voucher.isUsed = true;
                }

                dbContext.SaveChanges();
            }
        }
    }
}
