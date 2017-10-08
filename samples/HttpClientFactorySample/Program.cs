// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;

// See: https://github.com/aspnet/HttpClientFactory/issues/11
using HttpClientFactory = Microsoft.Extensions.Http.HttpClientFactory;

namespace HttpClientFactorySample
{
    public class Program
    {
        public static void Main(string[] args) => Run().GetAwaiter().GetResult();

        public static async Task Run()
        {
            var services = new ServiceCollection()
                .AddHttpClient()
                .AddLogging()
                .BuildServiceProvider();

            var factory = services.GetRequiredService<HttpClientFactory>();

            Console.WriteLine("Creating an HttpClient");
            var client = factory.CreateClient();

            Console.WriteLine("Creating and sending a request");
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/");
            request.Headers.Add("Accept", "application/vnd.github.v3+json"); // Github API versioning
            request.Headers.Add("User-Agent", "HttpClientFactory-Sample"); // Github requires a user-agent

            var response = await client.SendAsync(request).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var data = await response.Content.ReadAsAsync<JObject>();
            Console.WriteLine("Response data:");
            Console.WriteLine(data);
        }
    }
}
