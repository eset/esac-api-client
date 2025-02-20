/**
 * ESET Secure Authentication API Client
 * @copyright (c) 2012-2025 ESET, spol. s r.o. All rights reserved.
 */

using ESA.API.Api;
using ESA.API.Client;
//using ESA.API.Model;
using System.Diagnostics;

namespace ESA.API.Example.Simple
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Configuration config = new Configuration();
            config.BasePath = "https://esac.eset.com/";
            // Configure HTTP basic authorization: basicAuth
            config.Username = "<esa_api_username_here>";
            config.Password = "<esa_api_password_here>";

            var apiInstance = new ESAApi(config);
            var requestObject = new object();

            try
            {
                var result = apiInstance.Ping(requestObject);
                Debug.WriteLine(result);
            }
            catch (ApiException e)
            {
                Debug.Print("Exception when calling EsaApi.Ping: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}