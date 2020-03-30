// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.LanguageServer;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Moq;
using Xunit;
using OmniSharpPosition = OmniSharp.Extensions.LanguageServer.Protocol.Models.Position;
using OmniSharpRange = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;
using Range = Microsoft.VisualStudio.LanguageServer.Protocol.Range;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.HtmlCSharp
{
    public class DefaultLSPDocumentMappingProviderTest
    {
        [Fact]
        public async Task RazorMapToDocumentRangeAsync_InvokesLanguageServer()
        {
            // Arrange
            var uri = new Uri("file:///some/folder/to/file.razor");

            var response = new RazorMapToDocumentRangeResponse()
            {
                Range = new OmniSharpRange(new OmniSharpPosition(1, 1), new OmniSharpPosition(3, 3)),
                HostDocumentVersion = 1
            };
            var requestInvoker = new Mock<LSPRequestInvoker>(MockBehavior.Strict);
            requestInvoker
                .Setup(r => r.RequestServerAsync<RazorMapToDocumentRangeParams, RazorMapToDocumentRangeResponse>(LanguageServerConstants.RazorMapToDocumentRangeEndpoint, LanguageServerKind.Razor, It.IsAny<RazorMapToDocumentRangeParams>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));

            var mappingProvider = new DefaultLSPDocumentMappingProvider(requestInvoker.Object);
            var projectedRange = new Range()
            {
                Start = new Position(10, 10),
                End = new Position(15, 15)
            };

            // Act
            var result = await mappingProvider.MapToDocumentRangeAsync(RazorLanguageKind.CSharp, uri, projectedRange, CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.HostDocumentVersion);
            Assert.Equal(new Position(1, 1), result.Range.Start);
            Assert.Equal(new Position(3, 3), result.Range.End);
        }
    }
}
