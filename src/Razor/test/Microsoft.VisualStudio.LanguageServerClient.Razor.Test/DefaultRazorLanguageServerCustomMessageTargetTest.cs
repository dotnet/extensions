// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.LanguageServer;
using Microsoft.CodeAnalysis.Text;
using Moq;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    public class DefaultRazorLanguageServerCustomMessageTargetTest
    {
        [Fact]
        public void UpdateCSharpBuffer_CanNotDeserializeRequest_NoopsGracefully()
        {
            // Arrange
            LSPDocumentSnapshot document;
            var documentManager = new Mock<TrackingLSPDocumentManager>();
            documentManager.Setup(manager => manager.TryGetDocument(It.IsAny<Uri>(), out document))
                .Throws<XunitException>();
            var target = new DefaultRazorLanguageServerCustomMessageTarget(documentManager.Object);
            var token = JToken.FromObject(new { });

            // Act & Assert
            target.UpdateCSharpBuffer(token);
        }

        [Fact]
        public void UpdateCSharpBuffer_CannotLookupDocument_NoopsGracefully()
        {
            // Arrange
            LSPDocumentSnapshot document;
            var documentManager = new Mock<TrackingLSPDocumentManager>();
            documentManager.Setup(manager => manager.TryGetDocument(It.IsAny<Uri>(), out document))
                .Returns(false);
            var target = new DefaultRazorLanguageServerCustomMessageTarget(documentManager.Object);
            var request = new UpdateBufferRequest()
            {
                HostDocumentFilePath = "C:/path/to/file.razor",
            };
            var token = JToken.FromObject(request);

            // Act & Assert
            target.UpdateCSharpBuffer(token);
        }

        [Fact]
        public void UpdateCSharpBuffer_UpdatesDocument()
        {
            // Arrange
            var documentManager = new Mock<TrackingLSPDocumentManager>();
            documentManager.Setup(manager => manager.UpdateVirtualDocument<CSharpVirtualDocument>(It.IsAny<Uri>(), It.IsAny<IReadOnlyList<TextChange>>(), 1337, false /* UpdateCSharpBuffer request should never be provisional */))
                .Verifiable();
            var target = new DefaultRazorLanguageServerCustomMessageTarget(documentManager.Object);
            var request = new UpdateBufferRequest()
            {
                HostDocumentFilePath = "C:/path/to/file.razor",
                HostDocumentVersion = 1337,
                Changes = Array.Empty<TextChange>(),
            };
            var token = JToken.FromObject(request);

            // Act
            target.UpdateCSharpBuffer(token);

            // Assert
            documentManager.VerifyAll();
        }
    }
}
