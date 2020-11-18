// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates; //Only import this if you are using certificate
using System.Text.Json;
using System.Threading.Tasks;
using daemon_console.Models;

namespace daemon_console
{
    /// <summary>
    /// This sample shows how to query the Microsoft Graph from a daemon application
    /// which uses application permissions.
    /// For more information see https://aka.ms/msal-net-client-credentials
    /// </summary>
    class Program
    {
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

        public static string ConvertToImmutableId(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(plainTextBytes);
        }

        public static string ConvertFromImmutableId(string base64EncodedData)
        {
            var base64EncodedBytes = Convert.FromBase64String(base64EncodedData);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }

        private static async Task RunAsync()
        {
            AuthenticationConfig config = AuthenticationConfig.ReadFromJsonFile("appsettings.json");

            // You can run this sample using ClientSecret or Certificate. The code will differ only when instantiating the IConfidentialClientApplication
            bool isUsingClientSecret = AppUsesClientSecret(config);

            // Even if this is a console application here, a daemon application is a confidential client application
            IConfidentialClientApplication app;

            if (isUsingClientSecret)
            {
                app = ConfidentialClientApplicationBuilder.Create(config.ClientId)
                    .WithClientSecret(config.ClientSecret)
                    .WithAuthority(new Uri(config.Authority))
                    .Build();
            }
            else
            {
                X509Certificate2 certificate = ReadCertificate(config.CertificateName);
                app = ConfidentialClientApplicationBuilder.Create(config.ClientId)
                    .WithCertificate(certificate)
                    .WithAuthority(new Uri(config.Authority))
                    .Build();
            }

            // With client credentials flows the scopes is ALWAYS of the shape "resource/.default", as the 
            // application permissions need to be set statically (in the portal or by PowerShell), and then granted by
            // a tenant administrator
            string[] scopes = new string[] { config.MsGraphScope };
            AuthenticationResult result = await AquireToken(app, scopes);

            // Create an HttpClient to handle requests. 
            // Recommended reading before implementing in production: https://docs.microsoft.com/en-us/aspnet/core/fundamentals/http-requests?view=aspnetcore-5.0#typed-clients
            var httpClient = new HttpClient();
            var apiCaller = new ProtectedApiCallHelper(httpClient);

            //
            // The following lines shows how one could take the objectGUID of an Active Directory user, convert and retrive the id of the Azure Active Directory user based on the converted id.
            //
            //var adUserObjectId = "<Replace with on-premises objectGuid for user>";
            ////Convert to ImmutableId
            //var aadImmutableId = ConvertToImmutableId(adUserObjectId);
            ////Look for user in Ms Graph
            //var users = await apiCaller.GetAsync<GraphResponse<GraphUser>>($"{config.MsGraphBaseAddress}{config.MsGraphApiVersion}/users?$filter=onPremisesImmutableId eq {aadImmutableId}", result.AccessToken);
            ////Get the id of the one and only user matching the immutable id.
            //var aadUserObjectId = users.Value.Single().Id.ToString();

            // Sample call to Microsft Graph
            var usersResponse = await apiCaller.GetAsync<GraphResponse<GraphUser>>($"{config.MsGraphBaseAddress}{config.MsGraphApiVersion}/users?$top=5", result.AccessToken);
            foreach (GraphUser user in usersResponse.Value)
            {
                Console.WriteLine($"User found in Graph with id: {user.Id}");
            }

            // Get token for own API
            // Note: We need to get a new token since scopes for different applications cannot be mixed in the same "aquire token process"
            scopes = new string[] { config.TodoListScope };
            result = await AquireToken(app, scopes);

            // Sample Get data from protected api
            var apiObjects = await apiCaller.GetAsync<IEnumerable<TodoItem>>($"{config.TodoListBaseAddress}/api/todolist", result.AccessToken);
            PrintTodoItems(apiObjects);

            // Sample Post to protected api
            var todoItem = new TodoItem()
            {
                Id = apiObjects.Count() + 1,
                Task = $"Posting a sample task to the protected WebAPI"
            };
            await apiCaller.PostAsync($"{config.TodoListBaseAddress}/api/todolist", result.AccessToken, JsonSerializer.Serialize(todoItem));

            // Show that an item was added...
            apiObjects = await apiCaller.GetAsync<IEnumerable<TodoItem>>($"{config.TodoListBaseAddress}/api/todolist", result.AccessToken);
            PrintTodoItems(apiObjects);
        }

        private static async Task<AuthenticationResult> AquireToken(IConfidentialClientApplication app, string[] scopes)
        {
            try
            {
                AuthenticationResult result = await app.AcquireTokenForClient(scopes)
                    .ExecuteAsync();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Token acquired \n");
                return result;
            }
            catch (MsalServiceException ex) when (ex.Message.Contains("AADSTS70011"))
            {
                // Invalid scope. The scope has to be of the form "https://resourceurl/.default"
                // Mitigation: change the scope to be as expected
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Scope provided is not supported");
                throw ex;
            }
            finally
            {
                Console.ResetColor();
            }
        }

        /// <summary>
        /// Display the result of the Web API call
        /// </summary>
        /// <param name="result">Object to display</param>
        private static void PrintTodoItems(IEnumerable<TodoItem> result)
        {
            Console.WriteLine("Web Api result:");
            foreach (var item in result)
            {
                Console.WriteLine($"Id: {item.Id}, Task: {item.Task}");
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

            if (!string.IsNullOrWhiteSpace(config.ClientSecret) && config.ClientSecret != clientSecretPlaceholderValue)
            {
                return true;
            }

            else if (!string.IsNullOrWhiteSpace(config.CertificateName) && config.CertificateName != certificatePlaceholderValue)
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
            X509Certificate2 cert = null;

            using (X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            {
                store.Open(OpenFlags.ReadOnly);
                X509Certificate2Collection certCollection = store.Certificates;

                // Find unexpired certificates.
                X509Certificate2Collection currentCerts = certCollection.Find(X509FindType.FindByTimeValid, DateTime.Now, false);

                // From the collection of unexpired certificates, find the ones with the correct name.
                X509Certificate2Collection signingCert = currentCerts.Find(X509FindType.FindBySubjectDistinguishedName, certificateName, false);

                // Return the first certificate in the collection, has the right name and is current.
                cert = signingCert.OfType<X509Certificate2>().OrderByDescending(c => c.NotBefore).FirstOrDefault();
            }
            return cert;
        }

    }
}
