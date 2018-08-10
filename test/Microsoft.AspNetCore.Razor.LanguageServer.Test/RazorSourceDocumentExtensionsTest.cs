// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    public class RazorSourceDocumentExtensionsTest
    {
        [Fact]
        public void GetAbsoluteIndex_ReturnsExpectedIndex()
        {
            // Arrange
            var sourceDocument = TestRazorSourceDocument.Create(
@"<div>
    <p>Hello World</p>
</div>");
            var position = new Position(1, 7);

            // Act
            var absoluteIndex = sourceDocument.GetAbsoluteIndex(position);

            // Assert
            Assert.Equal(5 /* "<div>" */ + Environment.NewLine.Length + 7 /* "    <p>" */, absoluteIndex);
        }
    }
}
