// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Http.Telemetry.Logging.Bench;

internal sealed class NoRemoteCallHandler : DelegatingHandler
{
    private readonly byte[] _data;

    private NoRemoteCallHandler(byte[] data)
    {
        _data = data;
    }

    [SuppressMessage("Performance", "R9A017:Switch to an asynchronous method for increased performance.",
        Justification = "No async overload for `Directory.GetFiles`.")]
    [SuppressMessage("Performance Analysis", "CPR120:File.ReadAllXXX should be replaced by using a StreamReader to avoid adding objects to the large object heap (LOH).",
        Justification = "We can live with it here")]
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
            Content = new StreamContent(new MemoryStream(_data))
        };

        response.Content.Headers.ContentType = new("application/json");

        return Task.FromResult(response);
    }
}
