// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.AI;

/// <summary>
/// An <see cref="HttpMessageHandler"/> that checks the request body against an expected one
/// and sends back an expected response.
/// </summary>
public sealed class VerbatimHttpHandler(string expectedInput, string sentOutput) : HttpMessageHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Assert.NotNull(request.Content);

        string? input = await request.Content
#if NET
            .ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
#else
            .ReadAsStringAsync().ConfigureAwait(false);
#endif

        Assert.NotNull(input);
        Assert.Equal(RemoveWhiteSpace(expectedInput), RemoveWhiteSpace(input));

        return new() { Content = new StringContent(sentOutput) };
    }

    public static string? RemoveWhiteSpace(string? text) =>
        text is null ? null :
        Regex.Replace(text, @"\s*", string.Empty);
}
