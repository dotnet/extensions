// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.AspNetCore.Razor.LanguageServer.CodeActions.Models;
using Xunit;
using System.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Moq;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.Razor.LanguageServer.CodeActions
{
    public class DefaultCSharpCodeActionProviderTest : LanguageServerTestBase
    {
        private static readonly RazorCodeAction[] SupportedCodeActions = new RazorCodeAction[]
        {
            new RazorCodeAction()
            {
                Title = "Generate Equals and GetHashCode"
            },
            new RazorCodeAction()
            {
                Title = "Add null check"
            },
            new RazorCodeAction()
            {
                Title = "Add null checks for all parameters"
            },
            new RazorCodeAction()
            {
                Title = "Add null checks for all parameters"
            },
            new RazorCodeAction()
            {
                Title = "Generate constructor 'Counter(int)'"
            }
        };

        [Fact]
        public async Task ProvideAsync_ValidCodeActions_ReturnsProvidedCodeAction()
        {
            // Arrange
            var provider = new DefaultCSharpCodeActionProvider();
            var context = CreateCodeActionContext(supportsCodeActionResolve: true);

            // Act
            var providedCodeActions = await provider.ProvideAsync(context, SupportedCodeActions, default);

            // Assert
            Assert.Equal(SupportedCodeActions.Length, providedCodeActions.Count);

            for (var i = 0; i < SupportedCodeActions.Length; i++)
            {
                Assert.Equal(SupportedCodeActions[i].Title, providedCodeActions.ElementAt(i).Title);
            }
        }

        [Fact]
        public async Task ProvideAsync_SupportsCodeActionResolveFalse_ValidCodeActions_ReturnsEmpty()
        {
            // Arrange
            var provider = new DefaultCSharpCodeActionProvider();
            var context = CreateCodeActionContext(supportsCodeActionResolve: false);

            // Act
            var providedCodeActions = await provider.ProvideAsync(context, SupportedCodeActions, default);

            // Assert
            Assert.Empty(providedCodeActions);
        }

        [Fact]
        public async Task ProvideAsync_InvalidCodeActions_ReturnsNoCodeActions()
        {
            // Arrange
            var provider = new DefaultCSharpCodeActionProvider();
            var context = CreateCodeActionContext(supportsCodeActionResolve: true);

            var codeActions = new RazorCodeAction[]
            {
                new RazorCodeAction()
                {
                    Title = "Do something not really supported in razor"
                },
                new RazorCodeAction()
                {
                    // Invalid regex pattern shouldn't match
                    Title = "Generate constructor 'Counter(int)' xyz"
                }
            };

            // Act
            var providedCodeActions = await provider.ProvideAsync(context, codeActions, default);

            // Assert
            Assert.Empty(providedCodeActions);
        }

        private static RazorCodeActionContext CreateCodeActionContext(bool supportsCodeActionResolve)
        {
            var codeActionParams = new CodeActionParams()
            {
                TextDocument = new TextDocumentIdentifier("c:/path/to/file.razor")
            };
            var context = new RazorCodeActionContext(
                codeActionParams,
                Mock.Of<DocumentSnapshot>(),
                Mock.Of<RazorCodeDocument>(),
                new SourceLocation(),
                SourceText.From(string.Empty),
                supportsFileCreation: false,
                supportsCodeActionResolve);
            return context;
        }
    }
}
