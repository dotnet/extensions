// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.LanguageServer.CodeActions;
using Microsoft.AspNetCore.Razor.Test.Common;
using Newtonsoft.Json.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Test.CodeActions
{
    public class CodeActionResolutionEndpointTest : LanguageServerTestBase
    {
        [Fact]
        public async Task Handle_Resolve()
        {
            // Arrange
            var codeActionEndpoint = new CodeActionResolutionEndpoint(new RazorCodeActionResolver[] {
                new MockCodeActionResolver("Test"),
            }, LoggerFactory);
            var request = new RazorCodeActionResolutionParams()
            {
                Action = "Test",
                Data = null
            };

            // Act
            var workspaceEdit = await codeActionEndpoint.Handle(request, default);

            // Assert
            Assert.NotNull(workspaceEdit);
        }

        [Fact]
        public async Task Handle_ResolveMultipleProviders_FirstMatches()
        {
            // Arrange
            var codeActionEndpoint = new CodeActionResolutionEndpoint(new RazorCodeActionResolver[] {
                new MockCodeActionResolver("A"),
                new NullMockCodeActionResolver("B"),
            }, LoggerFactory);
            var request = new RazorCodeActionResolutionParams()
            {
                Action = "A",
                Data = null
            };

            // Act
            var workspaceEdit = await codeActionEndpoint.Handle(request, default);

            // Assert
            Assert.NotNull(workspaceEdit);
        }

        [Fact]
        public async Task Handle_ResolveMultipleProviders_SecondMatches()
        {
            // Arrange
            var codeActionEndpoint = new CodeActionResolutionEndpoint(new RazorCodeActionResolver[] {
                new NullMockCodeActionResolver("A"),
                new MockCodeActionResolver("B"),
            }, LoggerFactory);
            var request = new RazorCodeActionResolutionParams()
            {
                Action = "B",
                Data = null
            };

            // Act
            var workspaceEdit = await codeActionEndpoint.Handle(request, default);

            // Assert
            Assert.NotNull(workspaceEdit);
        }


        private class MockCodeActionResolver : RazorCodeActionResolver
        {
            public override string Action { get; }

            internal MockCodeActionResolver(string action)
            {
                Action = action;
            }

            public override Task<WorkspaceEdit> ResolveAsync(JObject data, CancellationToken cancellationToken)
            {
                return Task.FromResult(new WorkspaceEdit());
            }
        }

        private class NullMockCodeActionResolver : RazorCodeActionResolver
        {
            public override string Action { get; }

            internal NullMockCodeActionResolver(string action)
            {
                Action = action;
            }

            public override Task<WorkspaceEdit> ResolveAsync(JObject data, CancellationToken cancellationToken)
            {
                return null;
            }
        }
    }
}
