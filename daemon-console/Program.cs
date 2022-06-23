// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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

        private static async Task<string> GetTokenForUserAsync(AuthenticationConfig config)
        {
            AuthenticationResult result = null;
            IPublicClientApplication app;

            // PublicClientApplicationを生成（MSALの初期化）
            app = PublicClientApplicationBuilder.Create(config.ClientId)
                   .WithAuthority(new Uri(config.Authority))
                   .WithRedirectUri("http://localhost")
                   .Build();

            IEnumerable<IAccount> accounts = await app.GetAccountsAsync().ConfigureAwait(false);
            IAccount firstAccount = accounts.FirstOrDefault();

            // With client credentials flows the scopes is ALWAYS of the shape "resource/.default", as the 
            // application permissions need to be set statically (in the portal or by PowerShell), and then granted by
            // a tenant administrator. 
            string[] scopes = new string[] { "user.read" };


            //まずは自動でサインインできるか試す
            try
            {
                result = await app.AcquireTokenSilent(scopes, firstAccount)
                                                    .ExecuteAsync();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Token acquired \n");
                Console.ResetColor();
            }
            //自動でサインインできなければ、対話ウインドウでサインインする
            catch (MsalUiRequiredException)
            {
                result = await app.AcquireTokenInteractive(scopes)
                                                    .WithUseEmbeddedWebView(false)
                                                    .ExecuteAsync()
                                                    .ConfigureAwait(false);
            }
            return result.AccessToken;
        }

        private static async Task RunAsync()
        {
            AuthenticationConfig config = AuthenticationConfig.ReadFromJsonFile("appsettings.json");



            //var httpClient = new HttpClient();
            //var apiCaller = new ProtectedApiCallHelper(httpClient);
            //await apiCaller.CallWebApiAndProcessResultASync($"{config.ApiUrl}v1.0/users", await GetTokenForUserAsync(config), Display);

            //認証済みのGraphServiceClientインスタンスがない場合は、
            //サインインして新たに生成する
            if (authenticatedClient == null)
            {
                authenticatedClient = new GraphServiceClient(
                    new DelegateAuthenticationProvider(async (requestMessage) =>
                    {
                        requestMessage.Headers.Authorization
                            = new AuthenticationHeaderValue("bearer", await GetTokenForUserAsync(config));
                    }));
            }
            var user = await authenticatedClient.Me.Request().GetAsync();
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

        /// <summary>
        /// Display the result of the Web API call
        /// </summary>
        /// <param name="result">Object to display</param>
        private static void Display(JObject result)
        {
            foreach (JProperty child in result.Properties().Where(p => !p.Name.StartsWith("@")))
            {
                Console.WriteLine($"{child.Name} = {child.Value}");
            }
        }

        /// <summary>
        /// Checks if the sample is configured for using ClientSecret or Certificate. This method is just for the sake of this sample.
        /// You won't need this verification in your production application since you will be authenticating in AAD using one mechanism only.
        /// </summary>
        /// <param name="config">Configuration from appsettings.json</param>
        /// <returns></returns>
        private static bool AppUsesClientSecret(AuthenticationConfig config)
        {
            string clientSecretPlaceholderValue = "[Enter here a client secret for your application]";
            string certificatePlaceholderValue = "[Or instead of client secret: Enter here the name of a certificate (from the user cert store) as registered with your application]";

            if (!String.IsNullOrWhiteSpace(config.ClientSecret) && config.ClientSecret != clientSecretPlaceholderValue)
            {
                return true;
            }

            else if (!String.IsNullOrWhiteSpace(config.CertificateName) && config.CertificateName != certificatePlaceholderValue)
            {
                return false;
            }

            else
                throw new Exception("You must choose between using client secret or certificate. Please update appsettings.json file.");
        }

        private static X509Certificate2 ReadCertificate(string certificateName)
        {
            if (string.IsNullOrWhiteSpace(certificateName))
            {
                throw new ArgumentException("certificateName should not be empty. Please set the CertificateName setting in the appsettings.json", "certificateName");
            }
            CertificateDescription certificateDescription = CertificateDescription.FromStoreWithDistinguishedName(certificateName);
            DefaultCertificateLoader defaultCertificateLoader = new DefaultCertificateLoader();
            defaultCertificateLoader.LoadIfNeeded(certificateDescription);
            return certificateDescription.Certificate;
        }
    }
}
