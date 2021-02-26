// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Moq;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.VisualStudio.LanguageServices.Razor.ProjectSystem
{
    public class DefaultRazorDynamicFileInfoProviderTest
    {
        public DefaultRazorDynamicFileInfoProviderTest()
        {
            DocumentServiceFactory = Mock.Of<RazorDocumentServiceProviderFactory>(MockBehavior.Strict);
            EditorFeatureDetector = Mock.Of<LSPEditorFeatureDetector>(MockBehavior.Strict);
        }

        private RazorDocumentServiceProviderFactory DocumentServiceFactory { get; }
        private LSPEditorFeatureDetector EditorFeatureDetector { get; }

        [Fact]
        public void UpdateLSPFileInfo_UnknownFile_Noops()
        {
            // Arrange
            var provider = new DefaultRazorDynamicFileInfoProvider(DocumentServiceFactory, EditorFeatureDetector);
            provider.Updated += (sender, args) => throw new XunitException("Should not have been called.");

            // Act & Assert
            var documentContainer = new Mock<DynamicDocumentContainer>(MockBehavior.Strict);
            documentContainer.SetupSet(c => c.SupportsDiagnostics = true).Verifiable();
            provider.UpdateLSPFileInfo(new Uri("C:/this/does/not/exist.razor"), documentContainer.Object);
        }

        // Can't currently add any more tests because of IVT restrictions from Roslyn
    }
}
