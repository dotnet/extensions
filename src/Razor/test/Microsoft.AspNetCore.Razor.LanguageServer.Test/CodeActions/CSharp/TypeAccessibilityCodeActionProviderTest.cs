// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Components;
using Microsoft.AspNetCore.Razor.LanguageServer.CodeActions.Models;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;
using Moq;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;
using Newtonsoft.Json.Linq;
using Microsoft.CodeAnalysis.Razor.Workspaces;

namespace Microsoft.AspNetCore.Razor.LanguageServer.CodeActions
{
    public class TypeAccessibilityCodeActionProviderTest : LanguageServerTestBase
    {
        [Fact]
        public async Task Handle_MissingDiagnostics_ReturnsEmpty()
        {
            // Arrange
            var documentPath = "c:/Test.razor";
            var contents = "";
            var request = new CodeActionParams()
            {
                TextDocument = new TextDocumentIdentifier(new Uri(documentPath)),
                Range = new Range(),
                Context = new CodeActionContext()
                {
                    Diagnostics = null
                }
            };

            var location = new SourceLocation(0, -1, -1);
            var context = CreateRazorCodeActionContext(request, location, documentPath, contents, new SourceSpan(0, 0));
            context.CodeDocument.SetFileKind(FileKinds.Legacy);

            var provider = new TypeAccessibilityCodeActionProvider();
            var csharpCodeActions = new[] {
                new RazorCodeAction()
                {
                    Title = "System.Net.Dns"
                }
            };

            // Act
            var results = await provider.ProvideAsync(context, csharpCodeActions, default);

            // Assert
            Assert.Empty(results);
        }

        [Fact]
        public async Task Handle_InvalidDiagnostics_VSCode_ReturnsEmpty()
        {
            // Arrange
            var documentPath = "c:/Test.razor";
            var contents = "";
            var request = new CodeActionParams()
            {
                TextDocument = new TextDocumentIdentifier(new Uri(documentPath)),
                Range = new Range(),
                Context = new CodeActionContext()
                {
                    Diagnostics = new Container<Diagnostic>(
                        new Diagnostic()
                        {
                            // Invalid as Error is expected
                            Severity = DiagnosticSeverity.Warning,
                            Code = new DiagnosticCode("CS0246")
                        },
                        new Diagnostic()
                        {
                            // Invalid as CS error code is expected
                            Severity = DiagnosticSeverity.Error,
                            Code = new DiagnosticCode(0246)
                        },
                        new Diagnostic()
                        {
                            // Invalid as CS0246 or CS0103 is expected
                            Severity = DiagnosticSeverity.Error,
                            Code = new DiagnosticCode("CS0183")
                        }
                    )
                }
            };

            var location = new SourceLocation(0, -1, -1);
            var context = CreateRazorCodeActionContext(request, location, documentPath, contents, new SourceSpan(0, 0), supportsCodeActionResolve: false);
            context.CodeDocument.SetFileKind(FileKinds.Legacy);

            var provider = new TypeAccessibilityCodeActionProvider();
            var csharpCodeActions = new[] {
                new RazorCodeAction()
                {
                    Title = "System.Net.Dns"
                }
            };

            // Act
            var results = await provider.ProvideAsync(context, csharpCodeActions, default);

            // Assert
            Assert.Empty(results);
        }

        [Fact]
        public async Task Handle_EmptyCodeActions_ReturnsEmpty()
        {
            // Arrange
            var documentPath = "c:/Test.razor";
            var contents = "";
            var request = new CodeActionParams()
            {
                TextDocument = new TextDocumentIdentifier(new Uri(documentPath)),
                Range = new Range(),
                Context = new CodeActionContext()
                {
                    Diagnostics = new Container<Diagnostic>(
                        new Diagnostic()
                        {
                            Severity = DiagnosticSeverity.Error,
                            Code = new DiagnosticCode("CS0246")
                        }
                    )
                }
            };

            var location = new SourceLocation(0, -1, -1);
            var context = CreateRazorCodeActionContext(request, location, documentPath, contents, new SourceSpan(0, 0));
            context.CodeDocument.SetFileKind(FileKinds.Legacy);

            var provider = new TypeAccessibilityCodeActionProvider();
            var csharpCodeActions = Array.Empty<RazorCodeAction>();

            // Act
            var results = await provider.ProvideAsync(context, csharpCodeActions, default);

            // Assert
            Assert.Empty(results);
        }


        [Theory]
        [InlineData("CS0246")]
        [InlineData("CS0103")]
        [InlineData("IDE1007")]
        public async Task Handle_ValidDiagnostic_ValidCodeAction_VSCode_ReturnsCodeActions(string errorCode)
        {
            // Arrange
            var documentPath = "c:/Test.razor";
            var contents = "@code { Path; }";
            var request = new CodeActionParams()
            {
                TextDocument = new TextDocumentIdentifier(new Uri(documentPath)),
                Range = new Range(),
                Context = new CodeActionContext()
                {
                    Diagnostics = new Container<Diagnostic>(
                        new Diagnostic()
                        {
                            Severity = DiagnosticSeverity.Error,
                            Code = new DiagnosticCode("CS0132")
                        },
                        new Diagnostic()
                        {
                            Severity = DiagnosticSeverity.Error,
                            Code = new DiagnosticCode(errorCode),
                            Range = new Range(
                                new Position(0, 8),
                                new Position(0, 12)
                            )
                        },
                        new Diagnostic()
                        {
                            Severity = DiagnosticSeverity.Error,
                            Code = new DiagnosticCode("CS0183")
                        }
                    )
                }
            };

            var location = new SourceLocation(0, -1, -1);
            var context = CreateRazorCodeActionContext(request, location, documentPath, contents, new SourceSpan(8, 4), supportsCodeActionResolve: false);
            context.CodeDocument.SetFileKind(FileKinds.Legacy);

            var provider = new TypeAccessibilityCodeActionProvider();
            var csharpCodeActions = new[] {
                new RazorCodeAction()
                {
                    Title = "System.IO.Path",
                    Name = "CodeActionFromVSCode"
                },
                new RazorCodeAction()
                {
                    Title = "System.IO.SomethingElse",
                    Name = "CodeActionFromVSCode"
                }
            };

            // Act
            var results = await provider.ProvideAsync(context, csharpCodeActions, default);

            // Assert
            Assert.Collection(results,
                r =>
                {
                    Assert.Equal("@using System.IO", r.Title);
                    Assert.Null(r.Edit);
                    Assert.NotNull(r.Data);
                    var resolutionParams = (r.Data as JObject).ToObject<RazorCodeActionResolutionParams>();
                    Assert.Equal(LanguageServerConstants.CodeActions.AddUsing, resolutionParams.Action);
                },
                r =>
                {
                    Assert.Equal("System.IO.Path", r.Title);
                    Assert.NotNull(r.Edit);
                    Assert.Null(r.Data);
                }
            );
        }

        [Fact]
        public async Task Handle_ValidCodeAction_VS_ReturnsCodeActions()
        {
            // Arrange
            var documentPath = "c:/Test.razor";
            var contents = "@code { Path; }";
            var request = new CodeActionParams()
            {
                TextDocument = new TextDocumentIdentifier(new Uri(documentPath)),
                Range = new Range(),
                Context = new CodeActionContext()
                {
                    Diagnostics = new Container<Diagnostic>()
                }
            };

            var location = new SourceLocation(0, -1, -1);
            var context = CreateRazorCodeActionContext(request, location, documentPath, contents, new SourceSpan(8, 4), supportsCodeActionResolve: true);
            context.CodeDocument.SetFileKind(FileKinds.Legacy);

            var provider = new TypeAccessibilityCodeActionProvider();
            var csharpCodeActions = new[] {
                new RazorCodeAction()
                {
                    Title = "System.IO.Path",
                    Name = "FullyQualify"
                },
                new RazorCodeAction()
                {
                    Title = "using System.IO;",
                    Name = "AddImport"
                }
            };

            // Act
            var results = await provider.ProvideAsync(context, csharpCodeActions, default);

            // Assert
            Assert.Collection(results,
                r =>
                {
                    Assert.Equal("@using System.IO", r.Title);
                    Assert.Null(r.Edit);
                    Assert.NotNull(r.Data);
                    var resolutionParams = (r.Data as JObject).ToObject<RazorCodeActionResolutionParams>();
                    Assert.Equal(LanguageServerConstants.CodeActions.AddUsing, resolutionParams.Action);
                },
                r =>
                {
                    Assert.Equal("System.IO.Path", r.Title);
                    Assert.Null(r.Edit);
                    Assert.NotNull(r.Data);
                }
            );
        }

        [Fact]
        public async Task Handle_ValidDiagnostic_MultipleValidCodeActions_VSCode_ReturnsMultipleCodeActions()
        {
            // Arrange
            var documentPath = "c:/Test.razor";
            var contents = "@code { Path; }";
            var request = new CodeActionParams()
            {
                TextDocument = new TextDocumentIdentifier(new Uri(documentPath)),
                Range = new Range(),
                Context = new CodeActionContext()
                {
                    Diagnostics = new Container<Diagnostic>(
                        new Diagnostic()
                        {
                            Severity = DiagnosticSeverity.Error,
                            Code = new DiagnosticCode("CS0132")
                        },
                        new Diagnostic()
                        {
                            Severity = DiagnosticSeverity.Error,
                            Code = new DiagnosticCode("CS0246"),
                            Range = new Range(
                                new Position(0, 8),
                                new Position(0, 12)
                            )
                        },
                        new Diagnostic()
                        {
                            Severity = DiagnosticSeverity.Error,
                            Code = new DiagnosticCode("CS0183")
                        }
                    )
                }
            };

            var location = new SourceLocation(0, -1, -1);
            var context = CreateRazorCodeActionContext(request, location, documentPath, contents, new SourceSpan(8, 4), supportsCodeActionResolve: false);
            context.CodeDocument.SetFileKind(FileKinds.Legacy);

            var provider = new TypeAccessibilityCodeActionProvider();
            var csharpCodeActions = new[] {
                new RazorCodeAction()
                {
                    Title = "Fully qualify 'Path' -> System.IO.Path",
                    Name = "CodeActionFromVSCode"
                },
                new RazorCodeAction()
                {
                    Title = "Fully qualify 'Path' -> SuperSpecialNamespace.Path",
                    Name = "CodeActionFromVSCode"
                }
            };

            // Act
            var results = await provider.ProvideAsync(context, csharpCodeActions, default);

            // Assert
            Assert.Collection(results,
                r =>
                {
                    Assert.Equal("@using SuperSpecialNamespace", r.Title);
                    Assert.Null(r.Edit);
                    Assert.NotNull(r.Data);
                    var resolutionParams = (r.Data as JObject).ToObject<RazorCodeActionResolutionParams>();
                    Assert.Equal(LanguageServerConstants.CodeActions.AddUsing, resolutionParams.Action);
                },
                r =>
                {
                    Assert.Equal("@using System.IO", r.Title);
                    Assert.Null(r.Edit);
                    Assert.NotNull(r.Data);
                    var resolutionParams = (r.Data as JObject).ToObject<RazorCodeActionResolutionParams>();
                    Assert.Equal(LanguageServerConstants.CodeActions.AddUsing, resolutionParams.Action);
                },
                r =>
                {
                    Assert.Equal("Fully qualify 'Path' -> SuperSpecialNamespace.Path", r.Title);
                    Assert.NotNull(r.Edit);
                    Assert.Null(r.Data);
                },
                r =>
                {
                    Assert.Equal("Fully qualify 'Path' -> System.IO.Path", r.Title);
                    Assert.NotNull(r.Edit);
                    Assert.Null(r.Data);
                }
            );
        }

        private static RazorCodeActionContext CreateRazorCodeActionContext(
            CodeActionParams request,
            SourceLocation location,
            string filePath,
            string text,
            SourceSpan componentSourceSpan,
            bool supportsFileCreation = true,
            bool supportsCodeActionResolve = true)
        {
            var shortComponent = TagHelperDescriptorBuilder.Create(ComponentMetadata.Component.TagHelperKind, "Fully.Qualified.Component", "TestAssembly");
            shortComponent.TagMatchingRule(rule => rule.TagName = "Component");
            var fullyQualifiedComponent = TagHelperDescriptorBuilder.Create(ComponentMetadata.Component.TagHelperKind, "Fully.Qualified.Component", "TestAssembly");
            fullyQualifiedComponent.TagMatchingRule(rule => rule.TagName = "Fully.Qualified.Component");

            var tagHelpers = new[] { shortComponent.Build(), fullyQualifiedComponent.Build() };

            var sourceDocument = TestRazorSourceDocument.Create(text, filePath: filePath, relativePath: filePath);
            var projectEngine = RazorProjectEngine.Create(builder => builder.AddTagHelpers(tagHelpers));
            var codeDocument = projectEngine.ProcessDesignTime(sourceDocument, FileKinds.Component, Array.Empty<RazorSourceDocument>(), tagHelpers);

            var cSharpDocument = codeDocument.GetCSharpDocument();
            var diagnosticDescriptor = new RazorDiagnosticDescriptor("RZ10012", () => "", RazorDiagnosticSeverity.Error);
            var diagnostic = RazorDiagnostic.Create(diagnosticDescriptor, componentSourceSpan);
            var cSharpDocumentWithDiagnostic = RazorCSharpDocument.Create(cSharpDocument.GeneratedCode, cSharpDocument.Options, new[] { diagnostic });
            codeDocument.SetCSharpDocument(cSharpDocumentWithDiagnostic);

            var documentSnapshot = Mock.Of<DocumentSnapshot>(document =>
                document.GetGeneratedOutputAsync() == Task.FromResult(codeDocument) &&
                document.GetTextAsync() == Task.FromResult(codeDocument.GetSourceText()) &&
                document.Project.TagHelpers == tagHelpers, MockBehavior.Strict);

            var sourceText = SourceText.From(text);

            var context = new RazorCodeActionContext(request, documentSnapshot, codeDocument, location, sourceText, supportsFileCreation, supportsCodeActionResolve: supportsCodeActionResolve);

            return context;
        }
    }
}
