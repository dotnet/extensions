// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Http.Telemetry.Logging.Test.Internal;

internal class NoRemoteCallHandler : DelegatingHandler
{
    private const string FileName = "Text.txt";
    private readonly byte[] _fileContent;

    public NoRemoteCallHandler()
    {
        var assemblyFileLocation = Path.GetDirectoryName(typeof(NoRemoteCallHandler).Assembly.Location)!;
        var uri = new Uri(assemblyFileLocation).LocalPath;

        var responseFilePath = Path.Combine(Directory.GetFiles(
            path: uri,
            searchPattern: FileName));

        _fileContent = File.ReadAllBytes(responseFilePath);
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = new HttpResponseMessage
        {
            StatusCode = System.Net.HttpStatusCode.OK,
            RequestMessage = request,
            Content = new StreamContent(new MemoryStream(_fileContent))
        };

        response.Content.Headers.ContentType = new("application/json");

        return Task.FromResult(response);
    }
}
