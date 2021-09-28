using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTOs.API.Notificatons
{
	public class NewDeviceLogInNotification
	{
		public string PassengerId { get; set; }
		public string DeviceToken { get; set; }
	}
}

