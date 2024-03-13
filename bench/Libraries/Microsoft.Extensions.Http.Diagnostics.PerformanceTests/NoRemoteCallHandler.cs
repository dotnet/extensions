// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Http.Logging.Bench;

internal sealed class NoRemoteCallHandler : DelegatingHandler
{
    private readonly byte[] _data;

    private NoRemoteCallHandler(byte[] data)
    {
        _data = data;
    }

    public static NoRemoteCallHandler Create(string fileName)
    {
        var assemblyFileLocation = Path.GetDirectoryName(typeof(NoRemoteCallHandler).Assembly.Location)!;
        var uri = new Uri(assemblyFileLocation).LocalPath;

        var responseFilePath = Path.Combine(Directory.GetFiles(
            path: uri,
            searchPattern: fileName));

        var fileContent = File.ReadAllBytes(responseFilePath);

        return new NoRemoteCallHandler(fileContent);
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = new HttpResponseMessage
        {
            StatusCode = System.Net.HttpStatusCode.OK,
            RequestMessage = request,
            Content = new StreamContent(new MemoryStream(_data, writable: false))
        };

        response.Content.Headers.ContentType = new("application/json");

        return Task.FromResult(response);
    }
}
