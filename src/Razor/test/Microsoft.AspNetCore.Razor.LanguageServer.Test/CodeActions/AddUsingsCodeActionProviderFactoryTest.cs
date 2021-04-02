// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using OmniSharp.Extensions.LanguageServer.Protocol;
using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer.CodeActions
{
    public class AddUsingsCodeActionProviderFactoryTest
    {
        [Fact]
        public void GetNamespaceFromFQN_Invalid_ReturnsEmpty()
        {
            // Arrange
            var fqn = "Abc";

            // Act
            var namespaceName = AddUsingsCodeActionProviderFactory.GetNamespaceFromFQN(fqn);

            // Assert
            Assert.Empty(namespaceName);
        }

        [Fact]
        public void GetNamespaceFromFQN_Valid_ReturnsNamespace()
        {
            // Arrange
            var fqn = "Abc.Xyz";

            // Act
            var namespaceName = AddUsingsCodeActionProviderFactory.GetNamespaceFromFQN(fqn);

            // Assert
            Assert.Equal("Abc", namespaceName);
        }

        [Fact]
        public void CreateAddUsingCodeAction_CreatesCodeAction()
        {
            // Arrange
            var fqn = "Abc.Xyz";
            var docUri = DocumentUri.From("c:/path");

            // Act
            var codeAction = AddUsingsCodeActionProviderFactory.CreateAddUsingCodeAction(fqn, docUri);

            // Assert
            Assert.Equal("@using Abc", codeAction.Title);
        }

        [Fact]
        public void TryExtractNamespace_Invalid_ReturnsFalse()
        {
            // Arrange
            var csharpAddUsing = "Abc.Xyz;";

            // Act
            var res = AddUsingsCodeActionProviderFactory.TryExtractNamespace(csharpAddUsing, out var @namespace);

            // Assert
            Assert.False(res);
            Assert.Empty(@namespace);
        }

        [Fact]
        public void TryExtractNamespace_ReturnsTrue()
        {
            // Arrange
            var csharpAddUsing = "using Abc.Xyz;";

            // Act
            var res = AddUsingsCodeActionProviderFactory.TryExtractNamespace(csharpAddUsing, out var @namespace);

            // Assert
            Assert.True(res);
            Assert.Equal("Abc.Xyz", @namespace);
        }

        [Fact]
        public void TryExtractNamespace_WithStatic_ReturnsTruue()
        {
            // Arrange
            var csharpAddUsing = "using static X.Y.Z;";

            // Act
            var res = AddUsingsCodeActionProviderFactory.TryExtractNamespace(csharpAddUsing, out var @namespace);

            // Assert
            Assert.True(res);
            Assert.Equal("static X.Y.Z", @namespace);
        }
    }
}
