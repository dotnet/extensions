// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Test.Common;
using Moq;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
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
  ""autoClosingTags"": ""false""
}
".Trim();
            var result = new object[] { razorJsonString, htmlJsonString };
            var languageServer = GetLanguageServer(result);
            var configurationService = new DefaultRazorConfigurationService(languageServer, LoggerFactory);

            // Act
            var options = await configurationService.GetLatestOptionsAsync();

            // Assert
            Assert.Equal(expectedOptions, options);
        }

        [Fact]
        public async Task GetLatestOptionsAsync_EmptyResponse_ReturnsNull()
        {
            // Arrange
            var languageServer = GetLanguageServer(Array.Empty<object>());
            var configurationService = new DefaultRazorConfigurationService(languageServer, LoggerFactory);

            // Act
            var options = await configurationService.GetLatestOptionsAsync();

            // Assert
            Assert.Null(options);
        }

        [Fact]
        public async Task GetLatestOptionsAsync_ClientRequestThrows_ReturnsNull()
        {
            // Arrange
            var languageServer = GetLanguageServer(Array.Empty<object>(), shouldThrow: true);
            var configurationService = new DefaultRazorConfigurationService(languageServer, LoggerFactory);

            // Act
            var options = await configurationService.GetLatestOptionsAsync();

            // Assert
            Assert.Null(options);
        }

        private ILanguageServer GetLanguageServer(object[] result, bool shouldThrow = false)
        {
            var languageServer = new Mock<ILanguageServer>(MockBehavior.Strict);

            if (shouldThrow)
            {
                languageServer.Setup(l => l.Client).Throws(new Exception());
            }
            else
            {
                languageServer
                    .Setup(l => l.Client.SendRequest<ConfigurationParams, object[]>("workspace/configuration", It.IsAny<ConfigurationParams>()))
                    .Returns(Task.FromResult(result));
            }
            return languageServer.Object;
        }
    }
}
