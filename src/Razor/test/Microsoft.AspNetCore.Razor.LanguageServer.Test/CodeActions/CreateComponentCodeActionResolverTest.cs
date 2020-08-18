// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.AspNetCore.Razor.LanguageServer.CodeActions;
using Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;
using Moq;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Test.CodeActions
{
    public class CreateComponentCodeActionResolverTest : LanguageServerTestBase
    {
        private readonly DocumentResolver EmptyDocumentResolver = Mock.Of<DocumentResolver>();

        [Fact]
        public async Task Handle_MissingFile()
        {
            // Arrange
            var resolver = new CreateComponentCodeActionResolver(new DefaultForegroundDispatcher(), EmptyDocumentResolver);
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

            var resolver = new CreateComponentCodeActionResolver(new DefaultForegroundDispatcher(), CreateDocumentResolver(documentPath, codeDocument));
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

            var resolver = new CreateComponentCodeActionResolver(new DefaultForegroundDispatcher(), CreateDocumentResolver(documentPath, codeDocument));
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
        public async Task Handle_CreateComponent()
        {
            // Arrange
            var documentPath = "c:/Test.razor";
            var documentUri = new Uri(documentPath);
            var contents = $"@page \"/test\"";
            var codeDocument = CreateCodeDocument(contents);

            var resolver = new CreateComponentCodeActionResolver(new DefaultForegroundDispatcher(), CreateDocumentResolver(documentPath, codeDocument));
            var actionParams = new CreateComponentCodeActionParams
            {
                Uri = documentUri,
                Path = "c:/Another.razor",
            };
            var data = JObject.FromObject(actionParams);

            // Act
            var workspaceEdit = await resolver.ResolveAsync(data, default);

            // Assert
            Assert.NotNull(workspaceEdit);
            Assert.NotNull(workspaceEdit.DocumentChanges);
            Assert.Single(workspaceEdit.DocumentChanges);

            var documentChanges = workspaceEdit.DocumentChanges.ToArray();
            var createFileChange = documentChanges[0];
            Assert.True(createFileChange.IsCreateFile);
        }

        [Fact]
        public async Task Handle_CreateComponentWithNamespace()
        {
            // Arrange
            var documentPath = "c:/Test.razor";
            var documentUri = new Uri(documentPath);
            var contents = $"@page \"/test\"{Environment.NewLine}@namespace Another.Namespace";
            var codeDocument = CreateCodeDocument(contents);

            var resolver = new CreateComponentCodeActionResolver(new DefaultForegroundDispatcher(), CreateDocumentResolver(documentPath, codeDocument));
            var actionParams = new CreateComponentCodeActionParams
            {
                Uri = documentUri,
                Path = "c:/Another.razor",
            };
            var data = JObject.FromObject(actionParams);

            // Act
            var workspaceEdit = await resolver.ResolveAsync(data, default);

            // Assert
            Assert.NotNull(workspaceEdit);
            Assert.NotNull(workspaceEdit.DocumentChanges);
            Assert.Equal(2, workspaceEdit.DocumentChanges.Count());

            var documentChanges = workspaceEdit.DocumentChanges.ToArray();
            var createFileChange = documentChanges[0];
            Assert.True(createFileChange.IsCreateFile);

            var editNewComponentChange = documentChanges[1];
            var editNewComponentEdit = editNewComponentChange.TextDocumentEdit.Edits.First();
            Assert.Contains("@namespace Another.Namespace", editNewComponentEdit.NewText, StringComparison.Ordinal);
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
                builder.SetRootNamespace("test.Pages");
            });

            var codeDocument = projectEngine.Process(projectItem);
            codeDocument.SetFileKind(FileKinds.Component);

            return codeDocument;
        }
    }
}
