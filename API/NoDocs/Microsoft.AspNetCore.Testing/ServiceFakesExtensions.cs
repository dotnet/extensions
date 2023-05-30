// Assembly 'Microsoft.AspNetCore.Testing'

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Testing;

public static class ServiceFakesExtensions
{
    public static IWebHostBuilder UseTestStartup(this IWebHostBuilder builder);
    public static IWebHostBuilder ListenHttpOnAnyPort(this IWebHostBuilder builder);
    public static IWebHostBuilder ListenHttpsOnAnyPort(this IWebHostBuilder builder, X509Certificate2? sslCertificate = null);
    public static HttpClient CreateClient(this IHost host, HttpMessageHandler? handler = null, Func<Uri, bool>? addressFilter = null);
    public static IEnumerable<Uri> GetListenUris(this IHost host);
}
