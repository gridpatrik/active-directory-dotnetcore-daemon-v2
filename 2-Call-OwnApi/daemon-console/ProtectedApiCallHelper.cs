// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text;

namespace daemon_console
{
    /// <summary>
    /// Helper class to call a protected API and process its result
    /// </summary>
    public class ProtectedApiCallHelper
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="httpClient">HttpClient used to call the protected API</param>
        public ProtectedApiCallHelper(HttpClient httpClient)
        {
            HttpClient = httpClient;
        }

        protected HttpClient HttpClient { get; private set; }


        /// <summary>
        /// Calls the protected web API and processes the result
        /// </summary>
        /// <param name="T">Type to deserialize result to</param>
        /// <param name="webApiUrl">URL of the web API to call (supposed to return Json)</param>
        /// <param name="accessToken">Access token used as a bearer security token to call the web API</param>
        public async Task<T> GetAsync<T>(string webApiUrl, string accessToken)
        {
            try
            {
                if (string.IsNullOrEmpty(accessToken))
                    throw new ArgumentException("Access Token is not valid");

                PrepareHeaders(HttpClient, accessToken);
                HttpResponseMessage response = await HttpClient.GetAsync(webApiUrl);
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<T>(json);
                }
                else
                {
                    var failureContent = await FailedResponseHandler(response);
                    throw new Exception(failureContent);
                }
            }
            finally
            {
                Console.ResetColor();
            }
        }

        private static async Task<string> FailedResponseHandler(HttpResponseMessage response)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Failed to call the web API: {response.StatusCode}");
            string content = await response.Content.ReadAsStringAsync();

            // Note that if you got reponse.Code == 403 and reponse.content.code == "Authorization_RequestDenied"
            // this is because the tenant admin as not granted consent for the application to call the Web API
            Console.WriteLine($"Content: {content}");
            Console.ResetColor();
            return content;
        }

        /// <summary>
        /// Calls the protected web API and processes the result
        /// </summary>
        /// <param name="T">Type to deserialize result to</param>
        /// <param name="webApiUrl">URL of the web API to call (supposed to return Json)</param>
        /// <param name="accessToken">Access token used as a bearer security token to call the web API</param>
        public async Task PostAsync(string webApiUrl, string accessToken, string payload)
        {
            try
            {
                if (string.IsNullOrEmpty(accessToken))
                    throw new ArgumentException("Access Token is not valid");

                PrepareHeaders(HttpClient, accessToken);

                var data = new StringContent(payload, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await HttpClient.PostAsync(webApiUrl, data);
                if (!response.IsSuccessStatusCode)
                {
                    var failureContent = await FailedResponseHandler(response);
                    throw new Exception(failureContent);
                }
            }
            finally
            {
                Console.ResetColor();
            }
        }

        private void PrepareHeaders(HttpClient httpClient, string accessToken)
        {
            var defaultRequestHeaders = httpClient.DefaultRequestHeaders;
            if (defaultRequestHeaders.Accept == null || !defaultRequestHeaders.Accept.Any(m => m.MediaType == "application/json"))
            {
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            }
            defaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }
    }
}
