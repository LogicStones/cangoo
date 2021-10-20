using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTOs.API
{
	public class GetVehicleListRequest
	{
		[Required]
		public string fleetID { get; set; }
	}

	public class GetVehicleListResponse
	{
		public dynamic vehicle { get; set; }
	}

	public class ChangeVehicleStatusRequest
	{
		[Required]
		public Boolean isBooked { get; set; }
		[Required]
		public string vehicleID { get; set; }
		[Required]
		public string driverID { get; set; }
		[Required]
		public string driverName { get; set; }
		[Required]
		public string DeviceToken { get; set; }
	}

	public class ChangeVehicleStatusResponse
	{
		public dynamic vehicle { get; set; }
	}
}
