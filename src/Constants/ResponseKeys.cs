using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Constants
{
    public static class ResponseKeys
    {
        #region response.error = false

        public const string msgSuccess = "success";
       
        #endregion

        #region response.error = true

        public const string failedToAdd = "failedToAdd";
        public const string failedToUpdate = "failedToUpdate";
        public const string failedToDelete = "failedToDelete";
        public const string notFound = "notFound";
        public const string serverError = "serverError";

        public const string laterBookingConflict = "laterBookingConflict";  //When going to accept another laterbooking
        public const string userNotVerified = "userNotVerified";    //User attemplts login without verification

        public const string tripAlreadyBooked = "tripAlreadyBooked";
        public const string invalidParameters = "invalidParameters"; //API hit with invalid parameters.
        public const string userAlreadyVerified = "userAlreadyVerified"; //User tries to verify phone number again.
        public const string captainNotRegistered = "captainNotRegistered";  //In PhoneVerification API if code is not found from db
        public const string captainBlocked = "captainBlocked";  //Captain account is blocked by admin
        public const string captainNotFound = "captainNotFound";
        public const string contactDetailsNotFound = "contactDetailsNotFound";
        public const string facilitiesNotFound = "contactDetailsNotFound";
        public const string agreementTypesNotFound = "agreementTypesNotFound";
        public const string agreementsNotFound = "agreementsNotFound";
        public const string serviceNotAvailable = "serviceNotAvailable";    //Application is blocked by reseller
        public const string faqNotFound = "faqNotFound";
        public const string feedNotFound = "feedNotFound";
        public const string failedToResetPassword = "failedToResetPassword";    //If oldPassword and phone number don't match.
        public const string inCorrectCurrentPassword = "inCorrectCurrentPassword";    //If oldPassword and phone number don't match.
        public const string vehicleNotFound = "vehicleNotFound";
        public const string userInTrip = "userInTrip";
        public const string driverInTrip = "driverInTrip";
        public const string invalidUser = "invalidUser";  //user don't belong to current application
        public const string authenticationFailed = "authenticationFailed";  //If access token is not returned in case of login API.
        public const string tripNotFound = "tripNotFound";
        public const string userNotFound = "userNotFound";
        public const string userProfileNotFound = "userProfileNotFound";
        public const string userBlocked = "userBlocked";    //User account is blocked by admin
        public const string failedToRegisterUser = "failedToRegisterUser";
        public const string profileImageNotFound = "profileImageNotFound";  //Image is not posted from application to server to save.
        public const string invalidFileExtension = "invalidFileExtension";  //Posted image extension don't meet allowed extenstions i.e. ".jpg", ".gif", ".png"
        public const string userAlreadyRegistered = "userAlreadyRegistered";
        public const string paymentGetwayError = "paymentGetwayError"; //In case of stripe API error, whenever fails to get details
        public const string userOutOfRange = "userOutOfRange";  //Ride request and Fare estimate API
        public const string promoLimitExceeded = "promoLimitExceeded";
        public const string promoExpired = "promoExpired";
        public const string invalidPromo = "invalidPromo";
        //public const string promoCodeApplied = "promoCodeApplied";
        //public const string promoCodeRemoved = "promoCodeRemoved";
        public const string promoAlreadyApplied = "promoAlreadyApplied";
        public const string invalidInviteCode = "invalidInviteCode";
        public const string inviteCodeNotApplicable = "inviteCodeNotApplicable";
        public const string inviteCodeAlreadyApplied = "inviteCodeAlreadyApplied";
        public const string phoneNumberNotConfirmed = "phoneNumberNotConfirmed";
        public const string fareAlreadyPaid = "amountAlreadyPaid";
        public const string invalidCouponCode = "invalidCouponCode";
        public const string couponCodeAlreadyApplied = "couponCodeAlreadyApplied";
        public const string insufficientWalletBalance = "insufficientWalletBalance";
        public const string tipNotPaid = "tipNotPaid";
        #endregion

    }
}
