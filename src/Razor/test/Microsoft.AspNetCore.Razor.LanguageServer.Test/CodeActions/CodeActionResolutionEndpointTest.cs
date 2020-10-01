// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.LanguageServer.CodeActions;
using Microsoft.AspNetCore.Razor.LanguageServer.CodeActions.Models;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.AspNetCore.Razor.Test.Common;
using Moq;
using Newtonsoft.Json.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Test.CodeActions
{
    public class CodeActionResolutionEndpointTest : LanguageServerTestBase
    {
        [Fact]
        public async Task Handle_Valid_RazorCodeAction_WithResolver()
        {
            // Arrange
            var codeActionEndpoint = new CodeActionResolutionEndpoint(
                new RazorCodeActionResolver[] {
                    new MockRazorCodeActionResolver("Test"),
                },
                Array.Empty<CSharpCodeActionResolver>(),
                LoggerFactory);
            var requestParams = new RazorCodeActionResolutionParams()
            {
                Action = "Test",
                Language = LanguageServerConstants.CodeActions.Languages.Razor,
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
        public async Task Handle_Valid_CSharpCodeAction_WithResolver()
        {
            // Arrange
            var codeActionEndpoint = new CodeActionResolutionEndpoint(
                Array.Empty<RazorCodeActionResolver>(),
                new CSharpCodeActionResolver[] {
                    new MockCSharpCodeActionResolver("Test"),
                },
                LoggerFactory);
            var requestParams = new RazorCodeActionResolutionParams()
            {
                Action = "Test",
                Language = LanguageServerConstants.CodeActions.Languages.CSharp,
                Data = JObject.FromObject(new CSharpCodeActionParams())
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
        public async Task Handle_Valid_CSharpCodeAction_WithMultipleLanguageResolvers()
        {
            // Arrange
            var codeActionEndpoint = new CodeActionResolutionEndpoint(
                new RazorCodeActionResolver[] {
                    new MockRazorCodeActionResolver("TestRazor"),
                },
                new CSharpCodeActionResolver[] {
                    new MockCSharpCodeActionResolver("TestCSharp"),
                },
                LoggerFactory);
            var requestParams = new RazorCodeActionResolutionParams()
            {
                Action = "TestCSharp",
                Language = LanguageServerConstants.CodeActions.Languages.CSharp,
                Data = JObject.FromObject(new CSharpCodeActionParams())
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

        [Fact(Skip = "Debug.Fail fails in CI")]
        public async Task Handle_Valid_RazorCodeAction_WithoutResolver()
        {
            // Arrange
            var codeActionEndpoint = new CodeActionResolutionEndpoint(
                Array.Empty<RazorCodeActionResolver>(),
                Array.Empty<CSharpCodeActionResolver>(),
                LoggerFactory);
            var requestParams = new RazorCodeActionResolutionParams()
            {
                Action = "Test",
                Language = LanguageServerConstants.CodeActions.Languages.Razor,
                Data = new AddUsingsCodeActionParams()
            };
            var request = new RazorCodeAction()
            {
                Title = "Valid request",
                Data = JObject.FromObject(requestParams)
            };

#if DEBUG
            // Act & Assert (Throws due to debug assert on no Razor.Test resolver)
            await Assert.ThrowsAnyAsync<Exception>(async () => await codeActionEndpoint.Handle(request, default));
#else
            // Act
            var resolvedCodeAction = await codeActionEndpoint.Handle(request, default);

            // Assert
            Assert.Null(resolvedCodeAction.Edit);
#endif
        }

        [Fact(Skip = "Debug.Fail fails in CI")]
        public async Task Handle_Valid_CSharpCodeAction_WithoutResolver()
        {
            // Arrange
            var codeActionEndpoint = new CodeActionResolutionEndpoint(
                Array.Empty<RazorCodeActionResolver>(),
                Array.Empty<CSharpCodeActionResolver>(),
                LoggerFactory);
            var requestParams = new RazorCodeActionResolutionParams()
            {
                Action = "Test",
                Language = LanguageServerConstants.CodeActions.Languages.CSharp,
                Data = JObject.FromObject(new CSharpCodeActionParams())
            };
            var request = new RazorCodeAction()
            {
                Title = "Valid request",
                Data = JObject.FromObject(requestParams)
            };

#if DEBUG
            // Act & Assert (Throws due to debug assert on no resolver registered for CSharp.Test)
            await Assert.ThrowsAnyAsync<Exception>(async () => await codeActionEndpoint.Handle(request, default));
#else
            // Act
            var resolvedCodeAction = await codeActionEndpoint.Handle(request, default);

            // Assert
            Assert.Null(resolvedCodeAction.Edit);
#endif
        }

        [Fact(Skip = "Debug.Fail fails in CI")]
        public async Task Handle_Valid_RazorCodeAction_WithCSharpResolver_ResolvesNull()
        {
            // Arrange
            var codeActionEndpoint = new CodeActionResolutionEndpoint(
                Array.Empty<RazorCodeActionResolver>(),
                new CSharpCodeActionResolver[] {
                    new MockCSharpCodeActionResolver("Test"),
                },
                LoggerFactory);
            var requestParams = new RazorCodeActionResolutionParams()
            {
                Action = "Test",
                Language = LanguageServerConstants.CodeActions.Languages.Razor,
                Data = new AddUsingsCodeActionParams()
            };
            var request = new RazorCodeAction()
            {
                Title = "Valid request",
                Data = JObject.FromObject(requestParams)
            };

#if DEBUG
            // Act & Assert (Throws due to debug assert on no resolver registered for Razor.Test)
            await Assert.ThrowsAnyAsync<Exception>(async () => await codeActionEndpoint.Handle(request, default));
#else
            // Act
            var resolvedCodeAction = await codeActionEndpoint.Handle(request, default);

            // Assert
            Assert.Null(resolvedCodeAction.Edit);
#endif
        }

        [Fact(Skip = "Debug.Fail fails in CI")]
        public async Task Handle_Valid_CSharpCodeAction_WithRazorResolver_ResolvesNull()
        {
            // Arrange
            var codeActionEndpoint = new CodeActionResolutionEndpoint(
                new RazorCodeActionResolver[] {
                    new MockRazorCodeActionResolver("Test"),
                },
                Array.Empty<CSharpCodeActionResolver>(),
                LoggerFactory);
            var requestParams = new RazorCodeActionResolutionParams()
            {
                Action = "Test",
                Language = LanguageServerConstants.CodeActions.Languages.CSharp,
                Data = JObject.FromObject(new CSharpCodeActionParams())
            };
            var request = new RazorCodeAction()
            {
                Title = "Valid request",
                Data = JObject.FromObject(requestParams)
            };

#if DEBUG
            // Act & Assert (Throws due to debug asserts)
            await Assert.ThrowsAnyAsync<Exception>(async () => await codeActionEndpoint.Handle(request, default));
#else
            // Act
            var resolvedCodeAction = await codeActionEndpoint.Handle(request, default);

            // Assert
            Assert.Null(resolvedCodeAction.Edit);
#endif
        }

        [Fact]
        public async Task ResolveRazorCodeAction_ResolveMultipleRazorProviders_FirstMatches()
        {
            // Arrange
            var codeActionEndpoint = new CodeActionResolutionEndpoint(
                    new RazorCodeActionResolver[] {
                    new MockRazorCodeActionResolver("A"),
                    new MockRazorNullCodeActionResolver("B"),
                },
                Array.Empty<CSharpCodeActionResolver>(),
                LoggerFactory);
            var codeAction = new RazorCodeAction();
            var request = new RazorCodeActionResolutionParams()
            {
                Action = "A",
                Language = LanguageServerConstants.CodeActions.Languages.Razor,
                Data = new AddUsingsCodeActionParams()
            };

            // Act
            var resolvedCodeAction = await codeActionEndpoint.ResolveRazorCodeAction(codeAction, request, default);

            // Assert
            Assert.NotNull(resolvedCodeAction.Edit);
        }

        [Fact]
        public async Task ResolveRazorCodeAction_ResolveMultipleRazorProviders_SecondMatches()
        {
            // Arrange
            var codeActionEndpoint = new CodeActionResolutionEndpoint(
                new RazorCodeActionResolver[] {
                    new MockRazorNullCodeActionResolver("A"),
                    new MockRazorCodeActionResolver("B"),
                },
                Array.Empty<CSharpCodeActionResolver>(),
                LoggerFactory);
            var codeAction = new RazorCodeAction();
            var request = new RazorCodeActionResolutionParams()
            {
                Action = "B",
                Language = LanguageServerConstants.CodeActions.Languages.Razor,
                Data = new AddUsingsCodeActionParams()
            };

            // Act
            var resolvedCodeAction = await codeActionEndpoint.ResolveRazorCodeAction(codeAction, request, default);

            // Assert
            Assert.NotNull(resolvedCodeAction.Edit);
        }

        [Fact]
        public async Task ResolveCSharpCodeAction_ResolveMultipleCSharpProviders_FirstMatches()
        {
            // Arrange
            var codeActionEndpoint = new CodeActionResolutionEndpoint(
                Array.Empty<RazorCodeActionResolver>(),
                new CSharpCodeActionResolver[] {
                    new MockCSharpCodeActionResolver("A"),
                    new MockCSharpNullCodeActionResolver("B"),
                },
                LoggerFactory);
            var codeAction = new RazorCodeAction();
            var request = new RazorCodeActionResolutionParams()
            {
                Action = "A",
                Language = LanguageServerConstants.CodeActions.Languages.CSharp,
                Data = JObject.FromObject(new CSharpCodeActionParams())
            };

            // Act
            var resolvedCodeAction = await codeActionEndpoint.ResolveCSharpCodeAction(codeAction, request, default);

            // Assert
            Assert.NotNull(resolvedCodeAction.Edit);
        }

        [Fact]
        public async Task ResolveCSharpCodeAction_ResolveMultipleCSharpProviders_SecondMatches()
        {
            // Arrange
            var codeActionEndpoint = new CodeActionResolutionEndpoint(
                Array.Empty<RazorCodeActionResolver>(),
                new CSharpCodeActionResolver[] {
                    new MockCSharpNullCodeActionResolver("A"),
                    new MockCSharpCodeActionResolver("B"),
                },
                LoggerFactory);
            var codeAction = new RazorCodeAction();
            var request = new RazorCodeActionResolutionParams()
            {
                Action = "B",
                Language = LanguageServerConstants.CodeActions.Languages.Razor,
                Data = JObject.FromObject(new CSharpCodeActionParams())
            };

            // Act
            var resolvedCodeAction = await codeActionEndpoint.ResolveCSharpCodeAction(codeAction, request, default);

            // Assert
            Assert.NotNull(resolvedCodeAction.Edit);
        }

        [Fact]
        public async Task ResolveCSharpCodeAction_ResolveMultipleLanguageProviders()
        {
            // Arrange
            var codeActionEndpoint = new CodeActionResolutionEndpoint(
                new RazorCodeActionResolver[] {
                    new MockRazorNullCodeActionResolver("A"),
                    new MockRazorCodeActionResolver("B"),
                },
                new CSharpCodeActionResolver[] {
                    new MockCSharpNullCodeActionResolver("C"),
                    new MockCSharpCodeActionResolver("D"),
                },
                LoggerFactory);
            var codeAction = new RazorCodeAction();
            var request = new RazorCodeActionResolutionParams()
            {
                Action = "D",
                Language = LanguageServerConstants.CodeActions.Languages.CSharp,
                Data = JObject.FromObject(new CSharpCodeActionParams())
            };

            // Act
            var resolvedCodeAction = await codeActionEndpoint.ResolveCSharpCodeAction(codeAction, request, default);

            // Assert
            Assert.NotNull(resolvedCodeAction.Edit);
        }

        private class MockRazorCodeActionResolver : RazorCodeActionResolver
        {
            public override string Action { get; }

            internal MockRazorCodeActionResolver(string action)
            {
                Action = action;
            }

            public override Task<WorkspaceEdit> ResolveAsync(JObject data, CancellationToken cancellationToken)
            {
                return Task.FromResult(new WorkspaceEdit());
            }
        }

        private class MockRazorNullCodeActionResolver : RazorCodeActionResolver
        {
            public override string Action { get; }

            internal MockRazorNullCodeActionResolver(string action)
            {
                Action = action;
            }

            public override Task<WorkspaceEdit> ResolveAsync(JObject data, CancellationToken cancellationToken)
            {
                return null;
            }
        }

        private class MockCSharpCodeActionResolver : CSharpCodeActionResolver
        {
            public override string Action { get; }

            internal MockCSharpCodeActionResolver(string action)
                : base(Mock.Of<IClientLanguageServer>())
            {
                Action = action;
            }

            public override Task<RazorCodeAction> ResolveAsync(CSharpCodeActionParams csharpParams, RazorCodeAction codeAction, CancellationToken cancellationToken)
            {
                codeAction.Edit = new WorkspaceEdit();
                return Task.FromResult(codeAction);
            }
        }

        private class MockCSharpNullCodeActionResolver : CSharpCodeActionResolver
        {
            public override string Action { get; }

            internal MockCSharpNullCodeActionResolver(string action)
                : base(Mock.Of<IClientLanguageServer>())
            {
                Action = action;
            }

            public override Task<RazorCodeAction> ResolveAsync(CSharpCodeActionParams csharpParams, RazorCodeAction codeAction, CancellationToken cancellationToken)
            {
                return null;
            }
        }
    }
}
