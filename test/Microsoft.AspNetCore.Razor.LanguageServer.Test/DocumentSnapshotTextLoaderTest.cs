// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    public class DocumentSnapshotTextLoaderTest
    {
        [Fact]
        public async Task LoadTextAndVersionAsync_CreatesTextAndVersionFromDocumentsText()
        {
            // Arrange
            var expectedSourceText = SourceText.From("Hello World");
            var result = Task.FromResult(expectedSourceText);
            var snapshot = Mock.Of<DocumentSnapshot>(doc => doc.GetTextAsync() == result);
            var textLoader = new DocumentSnapshotTextLoader(snapshot);

            // Act
            var actual = await textLoader.LoadTextAndVersionAsync(default, default, default);

            // Assert
            Assert.Same(expectedSourceText, actual.Text);
        }
    }
}
