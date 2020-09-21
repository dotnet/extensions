// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.LanguageServer.CodeActions;
using OmniSharp.Extensions.LanguageServer.Protocol;
using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Test.CodeActions
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
    }
}
