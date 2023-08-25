/**
 * ESET Secure Authentication API Client
 * @copyright (c) 2012-2023 ESET, spol. s r.o. All rights reserved.
 */

using ESA.API.Api;
using ESA.API.Client;
using ESA.API.Model;
using System;
using System.Collections.Generic;

namespace ESA.API.Example.TwoFAMethods
{
    /// <summary>
    /// Sample console application for ESET Secure Authentication .NET API Client.
    /// </summary>
    internal class Program
    {
        private const string EsaUrl = "https://esac.eset.com/";
        private const string EsaCompanyApiUsername = "<esa_api_username_here>";
        private const string EsaCompanyApiPassword = "<esa_api_password_here>";

        private static readonly UserRealm EsaUserRealm = new UserRealm { Type = "auth", Id = ".NET-API-Client-Test" };

        private static ESAApi _authenticator;

        private static void Main()
        {
            // initialisation
            _authenticator = GetTwoFactorAuthenticator();

            var user = ProvisionUser();
            if (user.username == null)
            {
                Console.WriteLine("No username was provided; exiting...");
                return;
            }

            var pushData = PreAuthenticateUser(user.username, user.sms, user.push);

            if (user.push && pushData.pushChallengeId != null)
                AuthenticateUserPush(user.username, pushData.pushChallengeId, pushData.pushIdenticon);
            else
                AuthenticateUserOtp(user.username);

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        /// <summary>
        /// Prompts for username and then provisions the user for two-factor authentication.
        /// </summary>
        /// <returns>Username and two-factor authentication information.</returns>
        private static (string username, bool sms, bool push) ProvisionUser()
        {
            Console.WriteLine("Enter username:");
            var username = Console.ReadLine();

            if (string.IsNullOrEmpty(username))
            {
                return (null, false, false);
            }

            var twoFactorUser = GetOrCreateUser(username);
            if (twoFactorUser.TwoFactorAuthEnabled)
            {
                Console.WriteLine("The user is already provisioned");
                return (username, twoFactorUser.AuthenticationTypes.Sms, twoFactorUser.AuthenticationTypes.SoftTokensPush);
            }

            Console.WriteLine("Use Mobile Application? [y/N]:");
            var userMobileAppText = Console.ReadLine();
            var userMobileApp = !string.IsNullOrEmpty(userMobileAppText) &&
                                userMobileAppText.Equals("y", StringComparison.InvariantCultureIgnoreCase);

            Console.WriteLine("Use Mobile Application Push? [y/N]:");
            var userMobileAppPushText = Console.ReadLine();
            var userMobileAppPush = !string.IsNullOrEmpty(userMobileAppPushText) &&
                                    userMobileAppPushText.Equals("y", StringComparison.InvariantCultureIgnoreCase);

            bool userSms = !(userMobileApp || userMobileAppPush);

            _authenticator.ManageSetAuthenticationTypes(new RequestSetAuthenticationTypes(EsaUserRealm, username, new AuthenticationTypesOptional
            {
                Sms = userSms,
                SoftTokens = userMobileApp,
                SoftTokensPush = userMobileAppPush
            }));

            if (userMobileApp || userMobileAppPush)
            {
                var provisionResult = _authenticator.ManageProvision(new RequestProvision(EsaUserRealm, username));
                Console.WriteLine($"The application URL {System.Text.RegularExpressions.Regex.Unescape(provisionResult)} has been sent to the user.");

                if (userMobileApp) Console.WriteLine("The user has been provisioned for Mobile Application OTPs");
                if (userMobileAppPush) Console.WriteLine("The user has been provisioned for Mobile Application Push");
            }
            else
            {
                if (userSms) Console.WriteLine("The user has been provisioned for text message OTPs");
            }

            return (username, userSms, userMobileAppPush);
        }

        /// <summary>
        /// Attempt to get a user. If the user does not exist it will be created, prompting for the OTP.
        /// </summary>
        /// <param name="username">Name of user.</param>
        /// <returns>The user.</returns>
        private static UserProfile GetOrCreateUser(string username)
        {
            try
            {
                return _authenticator.ManageGetUserProfile(new RequestGetUserProfile(EsaUserRealm, username));
            }
            catch (ApiException)
            {
                Console.WriteLine("Enter mobile number for user in international format (eg \"42612345678\" where \"4\" is the country code and \"26\" is the area code):");
                var mobileNumber = Console.ReadLine();

                _authenticator.ManageCreateUser(new RequestCreateUser(EsaUserRealm, username, mobileNumber));
                return _authenticator.ManageGetUserProfile(new RequestGetUserProfile(EsaUserRealm, username));
            }
        }

        /// <summary>
        /// Pre-authenticate a user, which will return the type of credential that it expected and
        /// send a text message OTP or Push notification if required.
        /// </summary>
        /// <param name="username">Name of user.</param>
        private static (string pushChallengeId, string pushIdenticon) PreAuthenticateUser(string username, bool sendSms, bool sendPush)
        {
            Console.WriteLine("Calling PreAuthenticate for user");

            var result = _authenticator.AuthStartTwoFactorAuthentication(new RequestStartTwoFactorAuthentication(new UserRealmWithAutoRegParams(EsaUserRealm.Type, EsaUserRealm.Id), username, null, sendSms, sendPush, "custom"));

            List<string> expectedCredential = new List<string>();
            if (result.SuccessfulTwoFaTypes.SoftTokens)
                expectedCredential.Add("Soft Token");
            if (result.SuccessfulTwoFaTypes.SoftTokensPush)
                expectedCredential.Add("Soft Token Push");
            if (result.SuccessfulTwoFaTypes.Sms)
                expectedCredential.Add("SMS");

            Console.WriteLine($"Expected credential: {string.Join(", ", expectedCredential.ToArray())}");
            return (result.PushChallengeId, result.PushIdenticon);
        }

        /// <summary>
        /// Prompt for an OTP which will be authenticated.
        /// </summary>
        /// <param name="username">Name of user.</param>
        private static void AuthenticateUserOtp(string username)
        {
            // keep prompting for OTP until success or user becomes locked
            while (true)
            {
                Console.WriteLine("Enter OTP:");

                var otp = Console.ReadLine();

                try
                {
                    var result = _authenticator.AuthAuthenticate(new RequestAuthenticate(EsaUserRealm, username, otp, "custom"));
                    if (result.Authenticated)
                    {
                        Console.WriteLine("OTP has been authenticated");
                        break;
                    }

                    Console.WriteLine("OTP has not been authenticated");
                }
                catch (ApiException)
                {
                    Console.WriteLine("OTP cannot be authenticated - user has been locked");
                    break;
                }
            }
        }

        /// <summary>
        /// Wait for a push notification to be approved.
        /// </summary>
        /// <param name="username">Name of user.</param>
        private static void AuthenticateUserPush(string username, string pushChallengeId, string pushIdenticon)
        {
            Console.WriteLine($"Push ID: {pushIdenticon}");
            bool waitForPush = true;
            // keep waiting for push notification approval
            while (waitForPush)
            {
                try
                {
                    var result = _authenticator.AuthCheckPushChallengeNew(new RequestCheckPushChallengeNew(EsaUserRealm, username, pushChallengeId, "custom"));
                    switch (result.Result)
                    {
                        case 0:
                            Console.WriteLine("Waiting to approve Push notification...");
                            System.Threading.Thread.Sleep(1000);
                            break;

                        case 1:
                            Console.WriteLine("Push notification has been approved");
                            waitForPush = false;
                            break;

                        case 2:
                            Console.WriteLine("Push notification has been declined");
                            waitForPush = false;
                            break;

                        case 3:
                            Console.WriteLine("Push notification has timed out");
                            waitForPush = false;
                            break;
                    }
                }
                catch (ApiException)
                {
                    Console.WriteLine("Push cannot be authenticated");
                    break;
                }
            }
        }

        /// <summary>
        /// Create instance of "ESAApi"
        /// </summary>
        /// <returns></returns>
        private static ESAApi GetTwoFactorAuthenticator()
        {
            Configuration config = new Configuration();
            config.BasePath = EsaUrl;
            config.Username = EsaCompanyApiUsername;
            config.Password = EsaCompanyApiPassword;

            var authenticator = new ESAApi(config);

            authenticator.ManageSetCoreSettings(new RequestSetCoreSettings(new CoreSettings
            {
                MobileAppTokenName = ".NET API Client Test"
            }));

            try
            {
                authenticator.ManageCreateRealm(new RequestCreateRealm(new UserRealmWithRegParams
                {
                    Name = ".NET API Client Test",
                    Id = EsaUserRealm.Id,
                    Type = EsaUserRealm.Type,
                    Category = "custom"
                }));
            }
            catch (ApiException)
            {
                Console.WriteLine("User realm already exists");
            }

            return authenticator;
        }
    }
}