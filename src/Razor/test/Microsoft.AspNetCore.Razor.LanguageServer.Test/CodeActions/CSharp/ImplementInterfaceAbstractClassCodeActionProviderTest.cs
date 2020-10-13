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

namespace Microsoft.AspNetCore.Razor.LanguageServer.CodeActions
{
    public class ImplementInterfaceAbstractClassCodeActionProviderTest : LanguageServerTestBase
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

            var provider = new ImplementInterfaceAbstractClassCodeActionProvider();
            var csharpCodeActions = new[] {
                new RazorCodeAction()
                {
                    Title = "Implement abstract class"
                }
            };

            // Act
            var results = await provider.ProvideAsync(context, csharpCodeActions, default);

            // Assert
            Assert.Empty(results);
        }

        [Fact]
        public async Task Handle_InvalidDiagnostics_ReturnsEmpty()
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
                            Code = new DiagnosticCode("CS0534")
                        },
                        new Diagnostic()
                        {
                            // Invalid as CS error code is expected
                            Severity = DiagnosticSeverity.Error,
                            Code = new DiagnosticCode(0246)
                        },
                        new Diagnostic()
                        {
                            // Invalid as CS0534 or CS0535 is expected
                            Severity = DiagnosticSeverity.Error,
                            Code = new DiagnosticCode("CS0183")
                        }
                    )
                }
            };

            var location = new SourceLocation(0, -1, -1);
            var context = CreateRazorCodeActionContext(request, location, documentPath, contents, new SourceSpan(0, 0));
            context.CodeDocument.SetFileKind(FileKinds.Legacy);

            var provider = new ImplementInterfaceAbstractClassCodeActionProvider();
            var csharpCodeActions = new[] {
                new RazorCodeAction()
                {
                    Title = "Implement abstract class"
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
                            Code = new DiagnosticCode("CS0534")
                        }
                    )
                }
            };

            var location = new SourceLocation(0, -1, -1);
            var context = CreateRazorCodeActionContext(request, location, documentPath, contents, new SourceSpan(0, 0));
            context.CodeDocument.SetFileKind(FileKinds.Legacy);

            var provider = new ImplementInterfaceAbstractClassCodeActionProvider();
            var csharpCodeActions = Array.Empty<RazorCodeAction>();

            // Act
            var results = await provider.ProvideAsync(context, csharpCodeActions, default);

            // Assert
            Assert.Empty(results);
        }

        [Fact]
        public async Task Handle_ValidDiagnostic_InvalidCodeAction_ReturnsEmpty()
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
                            Code = new DiagnosticCode("CS0534"),
                            Range = new Range(
                                new Position(0, 8),
                                new Position(0, 12)
                            )
                        },
                        new Diagnostic()
                        {
                            Severity = DiagnosticSeverity.Error,
                            Code = new DiagnosticCode("CS0535"),
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
            var context = CreateRazorCodeActionContext(request, location, documentPath, contents, new SourceSpan(8, 4));
            context.CodeDocument.SetFileKind(FileKinds.Legacy);

            var provider = new ImplementInterfaceAbstractClassCodeActionProvider();

            // A valid code actions is expected to end with `Path` as that's the `associatedText`
            // indicated in the `Diagnostic.Range` for `CS0246` above.
            var csharpCodeActions = new[] {
                new RazorCodeAction()
                {
                    Title = "Implement non-abstract class"
                },
                new RazorCodeAction()
                {
                    Title = "Implement interface that doesn't exist"
                }
            };

            // Act
            var results = await provider.ProvideAsync(context, csharpCodeActions, default);

            // Assert
            Assert.Empty(results);
        }

        [Fact]
        public async Task Handle_ValidDiagnostic_ValidCodeAction_CodeBlock_ReturnsCodeActions()
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
                            Code = new DiagnosticCode("CS0534"),
                            Range = new Range(
                                new Position(0, 8),
                                new Position(0, 12)
                            )
                        },
                        new Diagnostic()
                        {
                            Severity = DiagnosticSeverity.Error,
                            Code = new DiagnosticCode("CS0535"),
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

            var location = new SourceLocation(8, -1, -1);
            var context = CreateRazorCodeActionContext(request, location, documentPath, contents, new SourceSpan(8, 4));
            context.CodeDocument.SetFileKind(FileKinds.Legacy);

            var provider = new ImplementInterfaceAbstractClassCodeActionProvider();
            var csharpCodeActions = new[] {
                new RazorCodeAction()
                {
                    Title = "Implement abstract class"
                },
                new RazorCodeAction()
                {
                    Title = "Implement interface"
                }
            };

            // Act
            var results = await provider.ProvideAsync(context, csharpCodeActions, default);

            // Assert
            Assert.Collection(results,
                r => {
                    Assert.Equal("Implement abstract class", r.Title);
                    Assert.Null(r.Edit);
                    Assert.NotNull(r.Data);
                    var resolutionParams = Assert.IsType<RazorCodeActionResolutionParams>(r.Data);
                    Assert.Equal(LanguageServerConstants.CodeActions.Languages.CSharp, resolutionParams.Language);
                    Assert.Equal(LanguageServerConstants.CodeActions.Default, resolutionParams.Action);
                    Assert.IsType<CSharpCodeActionParams>(resolutionParams.Data);
                },
                r => {
                    Assert.Equal("Implement interface", r.Title);
                    Assert.Null(r.Edit);
                    Assert.NotNull(r.Data);
                    var resolutionParams = Assert.IsType<RazorCodeActionResolutionParams>(r.Data);
                    Assert.Equal(LanguageServerConstants.CodeActions.Languages.CSharp, resolutionParams.Language);
                    Assert.Equal(LanguageServerConstants.CodeActions.Default, resolutionParams.Action);
                    Assert.IsType<CSharpCodeActionParams>(resolutionParams.Data);
                }
            );
        }

        [Fact]
        public async Task Handle_ValidDiagnostic_ValidCodeAction_FunctionsBlock_ReturnsEmpty()
        {
            // Arrange
            var documentPath = "c:/Test.razor";
            var contents = "@functions { Path; }";
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
                            Code = new DiagnosticCode("CS0534"),
                            Range = new Range(
                                new Position(0, 13),
                                new Position(0, 17)
                            )
                        },
                        new Diagnostic()
                        {
                            Severity = DiagnosticSeverity.Error,
                            Code = new DiagnosticCode("CS0535"),
                            Range = new Range(
                                new Position(0, 13),
                                new Position(0, 17)
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

            var location = new SourceLocation(13, -1, -1);
            var context = CreateRazorCodeActionContext(request, location, documentPath, contents, new SourceSpan(13, 4));
            context.CodeDocument.SetFileKind(FileKinds.Legacy);

            var provider = new ImplementInterfaceAbstractClassCodeActionProvider();
            var csharpCodeActions = new[] {
                new RazorCodeAction()
                {
                    Title = "Implement abstract class"
                },
                new RazorCodeAction()
                {
                    Title = "Implement interface"
                }
            };

            // Act
            var results = await provider.ProvideAsync(context, csharpCodeActions, default);

            // Assert
            Assert.Empty(results);
        }

        [Fact]
        public async Task Handle_SupportsCodeActionResolveFalse_ValidDiagnostic_ValidCodeAction_ReturnsEmpty()
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
                            Code = new DiagnosticCode("CS0534"),
                            Range = new Range(
                                new Position(0, 8),
                                new Position(0, 12)
                            )
                        },
                        new Diagnostic()
                        {
                            Severity = DiagnosticSeverity.Error,
                            Code = new DiagnosticCode("CS0535"),
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

            var provider = new ImplementInterfaceAbstractClassCodeActionProvider();
            var csharpCodeActions = new[] {
                new RazorCodeAction()
                {
                    Title = "Implement abstract class"
                },
                new RazorCodeAction()
                {
                    Title = "Implement interface"
                }
            };

            // Act
            var results = await provider.ProvideAsync(context, csharpCodeActions, default);

            // Assert
            Assert.Empty(results);
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
            var projectEngine = RazorProjectEngine.Create(builder => {
                builder.AddTagHelpers(tagHelpers);
            });
            var codeDocument = projectEngine.ProcessDesignTime(sourceDocument, FileKinds.Component, Array.Empty<RazorSourceDocument>(), tagHelpers);

            var cSharpDocument = codeDocument.GetCSharpDocument();
            var diagnosticDescriptor = new RazorDiagnosticDescriptor("RZ10012", () => "", RazorDiagnosticSeverity.Error);
            var diagnostic = RazorDiagnostic.Create(diagnosticDescriptor, componentSourceSpan);
            var cSharpDocumentWithDiagnostic = RazorCSharpDocument.Create(cSharpDocument.GeneratedCode, cSharpDocument.Options, new[] { diagnostic });
            codeDocument.SetCSharpDocument(cSharpDocumentWithDiagnostic);

            var documentSnapshot = Mock.Of<DocumentSnapshot>(document =>
                document.GetGeneratedOutputAsync() == Task.FromResult(codeDocument) &&
                document.GetTextAsync() == Task.FromResult(codeDocument.GetSourceText()) &&
                document.Project.TagHelpers == tagHelpers);

            var sourceText = SourceText.From(text);

            var context = new RazorCodeActionContext(request, documentSnapshot, codeDocument, location, sourceText, supportsFileCreation, supportsCodeActionResolve);

            return context;
        }
    }
}
