// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates; //Only import this if you are using certificate
using System.Threading.Tasks;

namespace daemon_console
{
    /// <summary>
    /// This sample shows how to query the Microsoft Graph from a daemon application
    /// which uses application permissions.
    /// For more information see https://aka.ms/msal-net-client-credentials
    /// </summary>
    class Program
    {
        private static GraphServiceClient authenticatedClient = null;
        static void Main(string[] args)
        {
            try
            {
                RunAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                Console.ResetColor();
            }

            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }


        private static async Task RunAsync()
        {
            AuthenticationConfig config = AuthenticationConfig.ReadFromJsonFile("appsettings.json");

            Console.WriteLine("Google Cloud - Authenticating as a service account.");

            // calling this directly just for clarity, this should be a callback
            var credential = GoogleCredential.FromFile("verdant-tempest-354310-922a6279a7a4.json");
            var storage = StorageClient.Create(credential);

            OidcToken oidcToken = await credential.GetOidcTokenAsync(OidcTokenOptions.FromTargetAudience("api://AzureADTokenExchange").WithTokenFormat(OidcTokenFormat.Standard)).ConfigureAwait(false);
            string tt = await oidcToken.GetAccessTokenAsync().ConfigureAwait(false);

            Console.WriteLine("Make an authenticated Google Cloud Storage API request.");
            Console.WriteLine("");

            // Make an authenticated API request.
            var buckets = storage.ListBuckets("verdant-tempest-354310");
            foreach (var bucket in buckets)
            {
                Console.WriteLine(bucket.Name);
            }
            Console.WriteLine("");

            Console.WriteLine("Exchange a Google token for an access token.");
            Console.WriteLine("");

            // pass token as a client assertion to the confidential client app
            var app = ConfidentialClientApplicationBuilder.Create(config.ClientId)
                                            .WithClientAssertion(tt)
                                            .Build();

            var authResult = app.AcquireTokenForClient(new string[] { ".default" })
                .WithAuthority(config.Authority)
                .ExecuteAsync();

            var authenticatedClient = new GraphServiceClient(
                new DelegateAuthenticationProvider(async (requestMessage) =>
                {
                    requestMessage.Headers.Authorization
                        = new AuthenticationHeaderValue("bearer", authResult.Result.AccessToken);
                }));


            var c = await authenticatedClient.Users.Request().GetAsync();
            var user = await authenticatedClient.Users["a5a9bf6a-4661-43df-b2f3-b0f727c36be6"].Request().GetAsync();

            Console.WriteLine($"displayName: {user.DisplayName}");
            Console.WriteLine($"givenName: {user.GivenName}");
            Console.WriteLine($"jobTitle: {user.JobTitle}");
            Console.WriteLine($"mail: {user.Mail}");
            Console.WriteLine($"mobilePhone: {user.MobilePhone}");
            Console.WriteLine($"officeLocation: {user.OfficeLocation}");
            Console.WriteLine($"preferredLanguage: {user.PreferredLanguage}");
            Console.WriteLine($"surname: {user.Surname}");
            Console.WriteLine($"userPrincipalName: {user.UserPrincipalName}");

        }
    }
}
