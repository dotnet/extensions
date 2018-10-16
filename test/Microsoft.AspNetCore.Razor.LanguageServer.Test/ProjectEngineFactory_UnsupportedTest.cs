// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.Test.Infrastructure;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    public class ProjectEngineFactory_UnsupportedTest
    {
        [Fact]
        public void Create_IgnoresConfigureParameter()
        {
            // Arrange
            var factory = new ProjectEngineFactory_Unsupported();

            // Act & Assert
            factory.Create(UnsupportedRazorConfiguration.Instance, RazorProjectFileSystem.Empty, (builder) =>
            {
                throw new XunitException("There should not be an opportunity to configure the project engine in the unsupported scenario.");
            });
        }

        // This is more of an integration test to validate that all the pieces work together
        [Fact]
        public void Create_ProcessDesignTime_AlwaysGeneratesEmptyGeneratedCSharp()
        {
            // Arrange
            var factory = new ProjectEngineFactory_Unsupported();
            var engine = factory.Create(UnsupportedRazorConfiguration.Instance, RazorProjectFileSystem.Empty, (_) => { });
            var sourceDocument = TestRazorSourceDocument.Create("<strong>Hello World!</strong>", RazorSourceDocumentProperties.Default);

            // Act
            var codeDocument = engine.ProcessDesignTime(sourceDocument, Array.Empty<RazorSourceDocument>(), Array.Empty<TagHelperDescriptor>());

            // Assert
            Assert.Equal(UnsupportedCSharpLoweringPhase.UnsupportedDisclaimer, codeDocument.GetCSharpDocument().GeneratedCode);
        }
    }
}
