// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.LanguageServer.ContainedLanguage.Test
{
    public class VirtualDocumentFactoryBaseTest
    {
        public VirtualDocumentFactoryBaseTest()
        {
            ContentTypeRegistry = Mock.Of<IContentTypeRegistryService>();
            var textBufferFactory = new Mock<ITextBufferFactoryService>();
            textBufferFactory
                .Setup(factory => factory.CreateTextBuffer())
                .Returns(Mock.Of<ITextBuffer>(buffer => buffer.CurrentSnapshot == Mock.Of<ITextSnapshot>() && buffer.Properties == new PropertyCollection() && buffer.ContentType == TestVirtualDocumentFactory.LanguageLSPContentTypeInstance));
            TextBufferFactory = textBufferFactory.Object;

            var hostContentType = Mock.Of<IContentType>(contentType => contentType.IsOfType(TestVirtualDocumentFactory.HostDocumentContentTypeNameConst) == true);
            HostLSPBuffer = Mock.Of<ITextBuffer>(textBuffer => textBuffer.ContentType == hostContentType);

            var nonHostLSPContentType = Mock.Of<IContentType>(contentType => contentType.IsOfType(It.IsAny<string>()) == false);
            NonHostLSPBuffer = Mock.Of<ITextBuffer>(textBuffer => textBuffer.ContentType == nonHostLSPContentType);

            TextDocumentFactoryService = Mock.Of<ITextDocumentFactoryService>();
        }

        private ITextBuffer NonHostLSPBuffer { get; }

        private ITextBuffer HostLSPBuffer { get; }

        private IContentTypeRegistryService ContentTypeRegistry { get; }

        private ITextBufferFactoryService TextBufferFactory { get; }

        private ITextDocumentFactoryService TextDocumentFactoryService { get; }

        [Fact]
        public void TryCreateFor_IncompatibleHostDocumentBuffer_ReturnsFalse()
        {
            // Arrange
            var uri = new Uri("C:/path/to/file.razor");
            var uriProvider = Mock.Of<FileUriProvider>(provider => provider.GetOrCreate(It.IsAny<ITextBuffer>()) == uri);
            var factory = new TestVirtualDocumentFactory(ContentTypeRegistry, TextBufferFactory, TextDocumentFactoryService, uriProvider);

            // Act
            var result = factory.TryCreateFor(NonHostLSPBuffer, out var virtualDocument);
            using (virtualDocument)
            {
                // Assert
                Assert.False(result);
                Assert.Null(virtualDocument);
            }
        }

        [Fact]
        public void TryCreateFor_ReturnsLanguageVirtualDocumentAndTrue()
        {
            // Arrange
            var uri = new Uri("C:/path/to/file.razor");
            var uriProvider = Mock.Of<FileUriProvider>(provider => provider.GetOrCreate(HostLSPBuffer) == uri);
            var factory = new TestVirtualDocumentFactory(ContentTypeRegistry, TextBufferFactory, TextDocumentFactoryService, uriProvider);

            // Act
            var result = factory.TryCreateFor(HostLSPBuffer, out var virtualDocument);

            using (virtualDocument)
            {
                // Assert
                Assert.True(result);
                Assert.NotNull(virtualDocument);
                Assert.EndsWith(TestVirtualDocumentFactory.LanguageFileNameSuffixConst, virtualDocument.Uri.OriginalString, StringComparison.Ordinal);
                Assert.Equal(TestVirtualDocumentFactory.LanguageLSPContentTypeInstance, virtualDocument.TextBuffer.ContentType);
                Assert.True(TestVirtualDocumentFactory.LanguageBufferPropertiesInstance.Keys.All(
                    (key) => virtualDocument.TextBuffer.Properties.TryGetProperty(key, out object value) && TestVirtualDocumentFactory.LanguageBufferPropertiesInstance[key] == value
                    ));
            }
        }

        private class TestVirtualDocumentFactory : VirtualDocumentFactoryBase
        {
            public const string HostDocumentContentTypeNameConst = "TestHostContentTypeName";
            public const string LanguageContentTypeNameConst = "TestLanguageContentTypeName";
            public const string LanguageFileNameSuffixConst = "__virtual.test";

            public static IContentType LanguageLSPContentTypeInstance { get; } = new TestContentType(LanguageContentTypeNameConst);
            public static Dictionary<object, object> LanguageBufferPropertiesInstance = new Dictionary<object, object>() { {"testKey", "testValue"} };

            public TestVirtualDocumentFactory(
                IContentTypeRegistryService contentTypeRegistryService,
                ITextBufferFactoryService textBufferFactoryService,
                ITextDocumentFactoryService textDocumentFactoryService,
                FileUriProvider fileUriProvider
                ) : base(contentTypeRegistryService, textBufferFactoryService, textDocumentFactoryService, fileUriProvider) { }

            protected override IContentType LanguageContentType => LanguageLSPContentTypeInstance;

            protected override string LanguageFileNameSuffix => LanguageFileNameSuffixConst;

            protected override string HostDocumentContentTypeName => HostDocumentContentTypeNameConst;

            protected override VirtualDocument CreateVirtualDocument(Uri uri, ITextBuffer textBuffer) => new TestVirtualDocument(uri, textBuffer);

            protected override IReadOnlyDictionary<object, object> LanguageBufferProperties => LanguageBufferPropertiesInstance;
        }
    }
}
