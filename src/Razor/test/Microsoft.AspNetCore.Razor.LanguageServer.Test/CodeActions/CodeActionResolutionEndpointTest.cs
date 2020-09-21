// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.LanguageServer.CodeActions;
using Microsoft.AspNetCore.Razor.LanguageServer.CodeActions.Models;
using Microsoft.AspNetCore.Razor.Test.Common;
using Newtonsoft.Json.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Test.CodeActions
{
    public class CodeActionResolutionEndpointTest : LanguageServerTestBase
    {
        [Fact]
        public async Task Handle_Valid_RazorCodeAction_Resolve()
        {
            // Arrange
            var codeActionEndpoint = new CodeActionResolutionEndpoint(new RazorCodeActionResolver[] {
                new MockCodeActionResolver("Test"),
            }, LoggerFactory);
            var requestParams = new RazorCodeActionResolutionParams()
            {
                Action = "Test",
                Data = new AddUsingsCodeActionParams()
            };
            var request = new RazorCodeAction()
            {
                Title = "Valid request",
                Data = JObject.FromObject(requestParams)
            };

            // Act
            var razorCodeAction = await codeActionEndpoint.Handle(request, default);

            // Assert
            Assert.NotNull(razorCodeAction.Edit);
        }

        [Fact]
        public async Task GetWorkspaceEditAsync_ResolveMultipleProviders_FirstMatches()
        {
            // Arrange
            var codeActionEndpoint = new CodeActionResolutionEndpoint(new RazorCodeActionResolver[] {
                new MockCodeActionResolver("A"),
                new NullMockCodeActionResolver("B"),
            }, LoggerFactory);
            var request = new RazorCodeActionResolutionParams()
            {
                Action = "A",
                Data = new AddUsingsCodeActionParams()
            };

            // Act
            var workspaceEdit = await codeActionEndpoint.GetWorkspaceEditAsync(request, default);

            // Assert
            Assert.NotNull(workspaceEdit);
        }

        [Fact]
        public async Task GetWorkspaceEditAsync_ResolveMultipleProviders_SecondMatches()
        {
            // Arrange
            var codeActionEndpoint = new CodeActionResolutionEndpoint(new RazorCodeActionResolver[] {
                new NullMockCodeActionResolver("A"),
                new MockCodeActionResolver("B"),
            }, LoggerFactory);
            var request = new RazorCodeActionResolutionParams()
            {
                Action = "B",
                Data = new AddUsingsCodeActionParams()
            };

            // Act
            var workspaceEdit = await codeActionEndpoint.GetWorkspaceEditAsync(request, default);

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
