// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Test.Common;
using Moq;
using Newtonsoft.Json.Linq;
using OmniSharp.Extensions.JsonRpc;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    public class DefaultRazorConfigurationServiceTest : LanguageServerTestBase
    {
        [Fact]
        public async Task GetLatestOptionsAsync_ReturnsExpectedOptions()
        {
            // Arrange
            var expectedOptions = new RazorLSPOptions(Trace.Messages, enableFormatting: false, autoClosingTags: false);
            var razorJsonString = @"
{
  ""trace"": ""Messages"",
  ""format"": {
    ""enable"": ""false""
  }
}
".Trim();
            var htmlJsonString = @"
{
  ""format"": ""true"",
  ""autoClosingTags"": ""false""
}
".Trim();
            var result = new JObject[] { JObject.Parse(razorJsonString), JObject.Parse(htmlJsonString) };
            var languageServer = GetLanguageServer(new ResponseRouterReturns(result));
            var configurationService = new DefaultRazorConfigurationService(languageServer, LoggerFactory);

            // Act
            var options = await configurationService.GetLatestOptionsAsync(CancellationToken.None);

            // Assert
            Assert.Equal(expectedOptions, options);
        }

        [Fact]
        public async Task GetLatestOptionsAsync_EmptyResponse_ReturnsNull()
        {
            // Arrange
            var languageServer = GetLanguageServer(result: null);
            var configurationService = new DefaultRazorConfigurationService(languageServer, LoggerFactory);

            // Act
            var options = await configurationService.GetLatestOptionsAsync(CancellationToken.None);

            // Assert
            Assert.Null(options);
        }

        [Fact]
        public async Task GetLatestOptionsAsync_ClientRequestThrows_ReturnsNull()
        {
            // Arrange
            var languageServer = GetLanguageServer(result: null, shouldThrow: true);
            var configurationService = new DefaultRazorConfigurationService(languageServer, LoggerFactory);

            // Act
            var options = await configurationService.GetLatestOptionsAsync(CancellationToken.None);

            // Assert
            Assert.Null(options);
        }

        private ClientNotifierServiceBase GetLanguageServer(IResponseRouterReturns result, bool shouldThrow = false)
        {
            var languageServer = new Mock<ClientNotifierServiceBase>(MockBehavior.Strict);

            if (shouldThrow)
            {
            }
            else
            {
                languageServer
                    .Setup(l => l.SendRequestAsync("workspace/configuration", It.IsAny<ConfigurationParams>()))
                    .Returns(Task.FromResult(result));
            }
            return languageServer.Object;
        }

        private class ResponseRouterReturns : IResponseRouterReturns
        {
            private object _result;

            public ResponseRouterReturns(object result)
            {
                _result = result;
            }

            public Task<Response> Returning<Response>(CancellationToken cancellationToken)
            {
                return Task.FromResult((Response)_result);
            }

            public Task ReturningVoid(CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }
        }
    }
}
