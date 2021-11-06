using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Constants
{
    public static class NotificationKeys
    {
        public const string pas_NewDeviceLoggedIn = "pas_NewDeviceLoggedIn";
        public const string pas_InProcessLaterBookingReRouted = "pas_InProcessLaterBookingReRouted";
        public const string pas_rideReRouted = "pas_rideReRouted";
        public const string pas_rideAccepted = "pas_rideAccepted";

        public const string cap_rideCancel = "cap_rideCancel";
        public const string cap_laterRideCancel = "cap_laterRideCancel";
        public const string cap_rideRequest = "cap_rideRequest";
        public const string cap_NewDeviceLoggedIn = "cap_NewDeviceLoggedIn";
        public const string cap_rideDispatched = "cap_rideDispatched";
    }
}
