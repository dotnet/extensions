// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

#pragma warning disable CA2000 // Dispose objects before losing scope
#pragma warning disable CA2016 // Forward the 'CancellationToken' parameter to methods
#pragma warning disable CA1031 // Do not catch general exception types
#pragma warning disable S108 // Nested blocks of code should not be left empty

namespace Microsoft.Extensions.AI;

/// <summary>
/// An <see cref="HttpMessageHandler"/> that checks the request body against an expected one
/// and sends back an expected response.
/// </summary>
public sealed class VerbatimHttpHandler(string expectedInput, string expectedOutput, bool validateExpectedResponse = false) :
    DelegatingHandler(new HttpClientHandler())
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Assert.NotNull(request.Content);

        string? actualInput = await request.Content.ReadAsStringAsync().ConfigureAwait(false);

        Assert.NotNull(actualInput);
        AssertEqualNormalized(expectedInput, actualInput);

        if (validateExpectedResponse)
        {
            ByteArrayContent newContent = new(Encoding.UTF8.GetBytes(actualInput));
            foreach (var header in request.Content.Headers)
            {
                newContent.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            request.Content = newContent;

            using var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            string? actualOutput = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            Assert.NotNull(actualOutput);
            AssertEqualNormalized(expectedOutput, actualOutput);
        }

        return new() { Content = new StringContent(expectedOutput) };
    }

    public static string? RemoveWhiteSpace(string? text) =>
        text is null ? null :
        Regex.Replace(text, @"\s*", string.Empty);

    private static void AssertEqualNormalized(string expected, string actual)
    {
        // First try to compare as JSON.
        JsonNode? expectedNode = null;
        JsonNode? actualNode = null;
        try
        {
            expectedNode = JsonNode.Parse(expected);
            actualNode = JsonNode.Parse(actual);
        }
        catch
        {
        }

        if (expectedNode is not null && actualNode is not null)
        {
            if (!JsonNode.DeepEquals(expectedNode, actualNode))
            {
                FailNotEqual(expected, actual);
            }

            return;
        }

        // Legitimately may not have been JSON. Fall back to whitespace normalization.
        if (RemoveWhiteSpace(expected) != RemoveWhiteSpace(actual))
        {
            FailNotEqual(expected, actual);
        }
    }

    private static void FailNotEqual(string expected, string actual) =>
        Assert.Fail(
            $"Expected:{Environment.NewLine}" +
            $"{expected}{Environment.NewLine}" +
            $"Actual:{Environment.NewLine}" +
            $"{actual}");
}
