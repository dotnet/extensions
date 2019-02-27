// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    public class EmptyTextLoaderTest
    {
        // See https://github.com/aspnet/AspNetCore/issues/7997
        [Fact]
        public async Task LoadAsync_SpecifiesEncoding()
        {
            // Arrange
            var loader = new EmptyTextLoader("file.cshtml");

            // Act
            var textAndVersion = await loader.LoadTextAndVersionAsync(default, default, default);

            // Assert
            Assert.True(textAndVersion.Text.CanBeEmbedded);
            Assert.Same(Encoding.UTF8, textAndVersion.Text.Encoding);
        }
    }
}
