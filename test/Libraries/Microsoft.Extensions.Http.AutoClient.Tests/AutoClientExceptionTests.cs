// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.Http.AutoClient.Test;

public class AutoClientExceptionTests
{
    [Fact]
    public async Task RestApiExceptionConstructor()
    {
        var e = new AutoClientException("Message", "api/users/{userId}");
        Assert.Equal("Message", e.Message);
        Assert.Null(e.StatusCode);
        Assert.Null(e.HttpError);

        var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            ReasonPhrase = "Reason",
            Content = new StringContent("someContent")
        };

        var error = await AutoClientHttpError.CreateAsync(response, default);

        e = new AutoClientException("Message", "api/users/{userId}", error);
        var contentHeaders = response.Content.Headers;
        response.Dispose();
        Assert.Equal("Message", e.Message);
        Assert.Equal(400, e.StatusCode);
        Assert.Equal(400, e.HttpError!.StatusCode);
        Assert.Equal("someContent", e.HttpError!.RawContent);
        Assert.Equal(response.ReasonPhrase, e.HttpError!.ReasonPhrase);
        Assert.Equal("api/users/{userId}", e.Path);

        foreach (var responseHeader in response.Headers)
        {
            Assert.Equal(responseHeader.Value, e.HttpError!.ResponseHeaders[responseHeader.Key]);
        }

        foreach (var contentHeader in response.Content.Headers)
        {
            Assert.Equal(contentHeader.Value, e.HttpError!.ResponseHeaders[contentHeader.Key]);
        }
    }
}
