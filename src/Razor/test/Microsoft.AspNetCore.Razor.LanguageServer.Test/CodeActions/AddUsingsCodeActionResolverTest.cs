// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.CodeActions;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;
using Moq;
using Newtonsoft.Json.Linq;
using Xunit;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.AspNetCore.Mvc.Razor.Extensions;
using System.Runtime.ExceptionServices;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Test.CodeActions
{
    public class AddUsingsCodeActionResolverTest : LanguageServerTestBase
    {
        private readonly DocumentResolver EmptyDocumentResolver = Mock.Of<DocumentResolver>();

        [Fact]
        public async Task Handle_MissingFile()
        {
            // Arrange
            var resolver = new AddUsingsCodeActionResolver(new DefaultForegroundDispatcher(), EmptyDocumentResolver);
            var data = JObject.FromObject(new CreateComponentCodeActionParams()
            {
                Uri = new Uri("c:/Test.razor"),
                Path = "c:/Another.razor",
            });

            // Act
            var workspaceEdit = await resolver.ResolveAsync(data, default);

            // Assert
            Assert.Null(workspaceEdit);
        }

        [Fact]
        public async Task Handle_Unsupported()
        {
            // Arrange
            var documentPath = "c:/Test.razor";
            var contents = $"@page \"/test\"";
            var codeDocument = CreateCodeDocument(contents);
            codeDocument.SetUnsupported();

            var resolver = new AddUsingsCodeActionResolver(new DefaultForegroundDispatcher(), CreateDocumentResolver(documentPath, codeDocument));
            var data = JObject.FromObject(new CreateComponentCodeActionParams()
            {
                Uri = new Uri(documentPath),
                Path = "c:/Another.razor",
            });

            // Act
            var workspaceEdit = await resolver.ResolveAsync(data, default);

            // Assert
            Assert.Null(workspaceEdit);
        }

        [Fact]
        public async Task Handle_InvalidFileKind()
        {
            // Arrange
            var documentPath = "c:/Test.razor";
            var contents = $"@page \"/test\"";
            var codeDocument = CreateCodeDocument(contents);
            codeDocument.SetFileKind(FileKinds.Legacy);

            var resolver = new AddUsingsCodeActionResolver(new DefaultForegroundDispatcher(), CreateDocumentResolver(documentPath, codeDocument));
            var data = JObject.FromObject(new CreateComponentCodeActionParams()
            {
                Uri = new Uri(documentPath),
                Path = "c:/Another.razor",
            });

            // Act
            var workspaceEdit = await resolver.ResolveAsync(data, default);

            // Assert
            Assert.Null(workspaceEdit);
        }

        [Fact]
        public async Task Handle_AddOneUsingToEmpty()
        {
            // Arrange
            var documentPath = "c:/Test.razor";
            var documentUri = new Uri(documentPath);
            var contents = $"";
            var codeDocument = CreateCodeDocument(contents);

            var resolver = new AddUsingsCodeActionResolver(new DefaultForegroundDispatcher(), CreateDocumentResolver(documentPath, codeDocument));
            var actionParams = new AddUsingsCodeActionParams
            {
                Uri = documentUri,
                Namespaces = new string[]
                {
                    "System"
                }
            };
            var data = JObject.FromObject(actionParams);

            // Act
            var workspaceEdit = await resolver.ResolveAsync(data, default);

            // Assert
            Assert.NotNull(workspaceEdit);
            Assert.NotNull(workspaceEdit.DocumentChanges);
            Assert.Single(workspaceEdit.DocumentChanges);

            var documentChanges = workspaceEdit.DocumentChanges.ToArray();
            var addUsingsChange = documentChanges[0];
            Assert.True(addUsingsChange.IsTextDocumentEdit);
            Assert.Single(addUsingsChange.TextDocumentEdit.Edits);
            var firstEdit = addUsingsChange.TextDocumentEdit.Edits.First();
            Assert.Equal(1, firstEdit.Range.Start.Line);
            Assert.Equal($"@using System{Environment.NewLine}", firstEdit.NewText);
        }

        [Fact]
        public async Task Handle_AddMultipleUsingsToEmpty()
        {
            // Arrange
            var documentPath = "c:/Test.razor";
            var documentUri = new Uri(documentPath);
            var contents = $"";
            var codeDocument = CreateCodeDocument(contents);

            var resolver = new AddUsingsCodeActionResolver(new DefaultForegroundDispatcher(), CreateDocumentResolver(documentPath, codeDocument));
            var actionParams = new AddUsingsCodeActionParams
            {
                Uri = documentUri,
                Namespaces = new string[]
                {
                    "System",
                    "System.Linq",
                    "System.Threading.Task"
                }
            };
            var data = JObject.FromObject(actionParams);

            // Act
            var workspaceEdit = await resolver.ResolveAsync(data, default);

            // Assert
            Assert.NotNull(workspaceEdit);
            Assert.NotNull(workspaceEdit.DocumentChanges);
            Assert.Single(workspaceEdit.DocumentChanges);

            var documentChanges = workspaceEdit.DocumentChanges.ToArray();
            var addUsingsChange = documentChanges[0];
            Assert.True(addUsingsChange.IsTextDocumentEdit);
            Assert.Single(addUsingsChange.TextDocumentEdit.Edits);
            var firstEdit = addUsingsChange.TextDocumentEdit.Edits.First();
            Assert.Equal(1, firstEdit.Range.Start.Line);
            Assert.Equal($"@using System{Environment.NewLine}@using System.Linq{Environment.NewLine}@using System.Threading.Task{Environment.NewLine}", firstEdit.NewText);
        }

        [Fact]
        public async Task Handle_AddOneUsingToPage()
        {
            // Arrange
            var documentPath = "c:/Test.razor";
            var documentUri = new Uri(documentPath);
            var contents = $"@page \"/\"{Environment.NewLine}";
            var codeDocument = CreateCodeDocument(contents);

            var resolver = new AddUsingsCodeActionResolver(new DefaultForegroundDispatcher(), CreateDocumentResolver(documentPath, codeDocument));
            var actionParams = new AddUsingsCodeActionParams
            {
                Uri = documentUri,
                Namespaces = new string[]
                {
                    "System"
                }
            };
            var data = JObject.FromObject(actionParams);

            // Act
            var workspaceEdit = await resolver.ResolveAsync(data, default);

            // Assert
            Assert.NotNull(workspaceEdit);
            Assert.NotNull(workspaceEdit.DocumentChanges);
            Assert.Single(workspaceEdit.DocumentChanges);

            var documentChanges = workspaceEdit.DocumentChanges.ToArray();
            var addUsingsChange = documentChanges[0];
            Assert.True(addUsingsChange.IsTextDocumentEdit);
            Assert.Single(addUsingsChange.TextDocumentEdit.Edits);
            var firstEdit = addUsingsChange.TextDocumentEdit.Edits.First();
            Assert.Equal(1, firstEdit.Range.Start.Line);
            Assert.Equal($"@using System{Environment.NewLine}", firstEdit.NewText);
        }

        [Fact]
        public async Task Handle_AddOneUsingToNamespace()
        {
            // Arrange
            var documentPath = "c:/Test.razor";
            var documentUri = new Uri(documentPath);
            var contents = $"@namespace Testing{Environment.NewLine}";
            var codeDocument = CreateCodeDocument(contents);

            var resolver = new AddUsingsCodeActionResolver(new DefaultForegroundDispatcher(), CreateDocumentResolver(documentPath, codeDocument));
            var actionParams = new AddUsingsCodeActionParams
            {
                Uri = documentUri,
                Namespaces = new string[]
                {
                    "System"
                }
            };
            var data = JObject.FromObject(actionParams);

            // Act
            var workspaceEdit = await resolver.ResolveAsync(data, default);

            // Assert
            Assert.NotNull(workspaceEdit);
            Assert.NotNull(workspaceEdit.DocumentChanges);
            Assert.Single(workspaceEdit.DocumentChanges);

            var documentChanges = workspaceEdit.DocumentChanges.ToArray();
            var addUsingsChange = documentChanges[0];
            Assert.True(addUsingsChange.IsTextDocumentEdit);
            Assert.Single(addUsingsChange.TextDocumentEdit.Edits);
            var firstEdit = addUsingsChange.TextDocumentEdit.Edits.First();
            Assert.Equal(1, firstEdit.Range.Start.Line);
            Assert.Equal($"@using System{Environment.NewLine}", firstEdit.NewText);
        }

        [Fact]
        public async Task Handle_AddOneUsingToPageAndNamespace()
        {
            // Arrange
            var documentPath = "c:/Test.razor";
            var documentUri = new Uri(documentPath);
            var contents = $"@page \"/\"{Environment.NewLine}@namespace Testing{Environment.NewLine}";
            var codeDocument = CreateCodeDocument(contents);

            var resolver = new AddUsingsCodeActionResolver(new DefaultForegroundDispatcher(), CreateDocumentResolver(documentPath, codeDocument));
            var actionParams = new AddUsingsCodeActionParams
            {
                Uri = documentUri,
                Namespaces = new string[]
                {
                    "System"
                }
            };
            var data = JObject.FromObject(actionParams);

            // Act
            var workspaceEdit = await resolver.ResolveAsync(data, default);

            // Assert
            Assert.NotNull(workspaceEdit);
            Assert.NotNull(workspaceEdit.DocumentChanges);
            Assert.Single(workspaceEdit.DocumentChanges);

            var documentChanges = workspaceEdit.DocumentChanges.ToArray();
            var addUsingsChange = documentChanges[0];
            Assert.True(addUsingsChange.IsTextDocumentEdit);
            Assert.Single(addUsingsChange.TextDocumentEdit.Edits);
            var firstEdit = addUsingsChange.TextDocumentEdit.Edits.First();
            Assert.Equal(2, firstEdit.Range.Start.Line);
            Assert.Equal($"@using System{Environment.NewLine}", firstEdit.NewText);
        }

        [Fact]
        public async Task Handle_AddOneUsingToUsings()
        {
            // Arrange
            var documentPath = "c:/Test.razor";
            var documentUri = new Uri(documentPath);
            var contents = $"@using System";
            var codeDocument = CreateCodeDocument(contents);

            var resolver = new AddUsingsCodeActionResolver(new DefaultForegroundDispatcher(), CreateDocumentResolver(documentPath, codeDocument));
            var actionParams = new AddUsingsCodeActionParams
            {
                Uri = documentUri,
                Namespaces = new string[]
                {
                    "System.Linq"
                }
            };
            var data = JObject.FromObject(actionParams);

            // Act
            var workspaceEdit = await resolver.ResolveAsync(data, default);

            // Assert
            Assert.NotNull(workspaceEdit);
            Assert.NotNull(workspaceEdit.DocumentChanges);
            Assert.Single(workspaceEdit.DocumentChanges);

            var documentChanges = workspaceEdit.DocumentChanges.ToArray();
            var addUsingsChange = documentChanges[0];
            Assert.True(addUsingsChange.IsTextDocumentEdit);
            Assert.Single(addUsingsChange.TextDocumentEdit.Edits);
            var firstEdit = addUsingsChange.TextDocumentEdit.Edits.First();
            Assert.Equal(1, firstEdit.Range.Start.Line);
            Assert.Equal($"@using System.Linq{Environment.NewLine}", firstEdit.NewText);
        }

        [Fact]
        public async Task Handle_AddOneNonSystemUsingToSystemUsings()
        {
            // Arrange
            var documentPath = "c:/Test.razor";
            var documentUri = new Uri(documentPath);
            var contents = $"@using System{Environment.NewLine}@using System.Linq{Environment.NewLine}";
            var codeDocument = CreateCodeDocument(contents);

            var resolver = new AddUsingsCodeActionResolver(new DefaultForegroundDispatcher(), CreateDocumentResolver(documentPath, codeDocument));
            var actionParams = new AddUsingsCodeActionParams
            {
                Uri = documentUri,
                Namespaces = new string[]
                {
                    "Microsoft.AspNetCore.Razor.Language"
                }
            };
            var data = JObject.FromObject(actionParams);

            // Act
            var workspaceEdit = await resolver.ResolveAsync(data, default);

            // Assert
            Assert.NotNull(workspaceEdit);
            Assert.NotNull(workspaceEdit.DocumentChanges);
            Assert.Single(workspaceEdit.DocumentChanges);

            var documentChanges = workspaceEdit.DocumentChanges.ToArray();
            var addUsingsChange = documentChanges[0];
            Assert.Single(addUsingsChange.TextDocumentEdit.Edits);
            Assert.True(addUsingsChange.IsTextDocumentEdit);
            var firstEdit = addUsingsChange.TextDocumentEdit.Edits.First();
            Assert.Equal(2, firstEdit.Range.Start.Line);
            Assert.Equal($"@using Microsoft.AspNetCore.Razor.Language{Environment.NewLine}", firstEdit.NewText);
        }

        [Fact]
        public async Task Handle_AddMultipleUsingsToUsingsNonSystem()
        {
            // Arrange
            var documentPath = "c:/Test.razor";
            var documentUri = new Uri(documentPath);
            var contents = $"@using Microsoft.AspNetCore.Razor.LanguageServer.CodeActions{Environment.NewLine}@using Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem{Environment.NewLine}";
            var codeDocument = CreateCodeDocument(contents);

            var resolver = new AddUsingsCodeActionResolver(new DefaultForegroundDispatcher(), CreateDocumentResolver(documentPath, codeDocument));
            var actionParams = new AddUsingsCodeActionParams
            {
                Uri = documentUri,
                Namespaces = new string[]
                {
                    "Microsoft.AspNetCore.Razor.Language",
                    "Microsoft.AspNetCore.Razor.LanguageServer.Common",
                    "Microsoft.AspNetCore.Razor.Test.Common",
                }
            };
            var data = JObject.FromObject(actionParams);

            // Act
            var workspaceEdit = await resolver.ResolveAsync(data, default);

            // Assert
            Assert.NotNull(workspaceEdit);
            Assert.NotNull(workspaceEdit.DocumentChanges);
            Assert.Single(workspaceEdit.DocumentChanges);

            var documentChanges = workspaceEdit.DocumentChanges.ToArray();
            var addUsingsChange = documentChanges[0];
            Assert.True(addUsingsChange.IsTextDocumentEdit);
            Assert.Equal(3, addUsingsChange.TextDocumentEdit.Edits.Count());
            var firstEdit = addUsingsChange.TextDocumentEdit.Edits.First();
            Assert.Equal(0, firstEdit.Range.Start.Line);
            Assert.Equal($"@using Microsoft.AspNetCore.Razor.Language{Environment.NewLine}", firstEdit.NewText);
            var secondEdit = addUsingsChange.TextDocumentEdit.Edits.ElementAt(1);
            Assert.Equal(1, secondEdit.Range.Start.Line);
            Assert.Equal($"@using Microsoft.AspNetCore.Razor.LanguageServer.Common{Environment.NewLine}", secondEdit.NewText);
            var thirdEdit = addUsingsChange.TextDocumentEdit.Edits.ElementAt(2);
            Assert.Equal(2, thirdEdit.Range.Start.Line);
            Assert.Equal($"@using Microsoft.AspNetCore.Razor.Test.Common{Environment.NewLine}", thirdEdit.NewText);
        }

        [Fact]
        public async Task Handle_AddMultipleUsingsToUsingsSkippingSystem()
        {
            // Arrange
            var documentPath = "c:/Test.razor";
            var documentUri = new Uri(documentPath);
            var contents = $"@using System{Environment.NewLine}@using Microsoft.AspNetCore.Razor.LanguageServer.CodeActions{Environment.NewLine}@using Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem{Environment.NewLine}";
            var codeDocument = CreateCodeDocument(contents);

            var resolver = new AddUsingsCodeActionResolver(new DefaultForegroundDispatcher(), CreateDocumentResolver(documentPath, codeDocument));
            var actionParams = new AddUsingsCodeActionParams
            {
                Uri = documentUri,
                Namespaces = new string[]
                {
                    "Microsoft.AspNetCore.Razor.Language",
                    "Microsoft.AspNetCore.Razor.LanguageServer.Common",
                    "Microsoft.AspNetCore.Razor.Test.Common",
                }
            };
            var data = JObject.FromObject(actionParams);

            // Act
            var workspaceEdit = await resolver.ResolveAsync(data, default);

            // Assert
            Assert.NotNull(workspaceEdit);
            Assert.NotNull(workspaceEdit.DocumentChanges);
            Assert.Single(workspaceEdit.DocumentChanges);

            var documentChanges = workspaceEdit.DocumentChanges.ToArray();
            var addUsingsChange = documentChanges[0];
            Assert.True(addUsingsChange.IsTextDocumentEdit);
            Assert.Equal(3, addUsingsChange.TextDocumentEdit.Edits.Count());
            var firstEdit = addUsingsChange.TextDocumentEdit.Edits.First();
            Assert.Equal(1, firstEdit.Range.Start.Line);
            Assert.Equal($"@using Microsoft.AspNetCore.Razor.Language{Environment.NewLine}", firstEdit.NewText);
            var secondEdit = addUsingsChange.TextDocumentEdit.Edits.ElementAt(1);
            Assert.Equal(2, secondEdit.Range.Start.Line);
            Assert.Equal($"@using Microsoft.AspNetCore.Razor.LanguageServer.Common{Environment.NewLine}", secondEdit.NewText);
            var thirdEdit = addUsingsChange.TextDocumentEdit.Edits.ElementAt(2);
            Assert.Equal(3, thirdEdit.Range.Start.Line);
            Assert.Equal($"@using Microsoft.AspNetCore.Razor.Test.Common{Environment.NewLine}", thirdEdit.NewText);
        }

        private static DocumentResolver CreateDocumentResolver(string documentPath, RazorCodeDocument codeDocument)
        {
            var sourceTextChars = new char[codeDocument.Source.Length];
            codeDocument.Source.CopyTo(0, sourceTextChars, 0, codeDocument.Source.Length);
            var sourceText = SourceText.From(new string(sourceTextChars));
            var documentSnapshot = Mock.Of<DocumentSnapshot>(document =>
                document.GetGeneratedOutputAsync() == Task.FromResult(codeDocument) &&
                document.GetTextAsync() == Task.FromResult(sourceText));
            var documentResolver = new Mock<DocumentResolver>();
            documentResolver
                .Setup(resolver => resolver.TryResolveDocument(documentPath, out documentSnapshot))
                .Returns(true);
            return documentResolver.Object;
        }

        private static RazorCodeDocument CreateCodeDocument(string text)
        {
            var projectItem = new TestRazorProjectItem("c:/Test.razor", "c:/Test.razor", "Test.razor") { Content = text };
            var projectEngine = RazorProjectEngine.Create(RazorConfiguration.Default, TestRazorProjectFileSystem.Empty, (builder) =>
            {
                // NamespaceDirective.Register(builder);
                PageDirective.Register(builder);
            });
            var codeDocument = projectEngine.Process(projectItem);
            codeDocument.SetFileKind(FileKinds.Component);
            return codeDocument;
        }
    }
}
