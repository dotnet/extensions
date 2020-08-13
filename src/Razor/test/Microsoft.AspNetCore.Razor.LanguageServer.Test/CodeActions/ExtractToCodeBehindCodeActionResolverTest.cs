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
    public class ExtractToCodeBehindCodeActionResolverTest : LanguageServerTestBase
    {
        private readonly DocumentResolver EmptyDocumentResolver = Mock.Of<DocumentResolver>();

        [Fact]
        public async Task Handle_MissingFile()
        {
            // Arrange
            var resolver = new ExtractToCodeBehindCodeActionResolver(new DefaultForegroundDispatcher(), EmptyDocumentResolver, FilePathNormalizer);
            var data = JObject.FromObject(new ExtractToCodeBehindCodeActionParams()
            {
                Uri = new Uri("c:/Test.razor"),
                RemoveStart = 14,
                ExtractStart = 19,
                ExtractEnd = 41,
                RemoveEnd = 41,
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
            var documentPath = "c:\\Test.razor";
            var contents = $"@page \"/test\"{Environment.NewLine}@code {{ private var x = 1; }}";
            var codeDocument = CreateCodeDocument(contents);
            codeDocument.SetUnsupported();

            var resolver = new ExtractToCodeBehindCodeActionResolver(new DefaultForegroundDispatcher(), CreateDocumentResolver(documentPath, codeDocument), FilePathNormalizer);
            var data = JObject.FromObject(new ExtractToCodeBehindCodeActionParams()
            {
                Uri = new Uri("c:/Test.razor"),
                RemoveStart = 14,
                ExtractStart = 20,
                ExtractEnd = 41,
                RemoveEnd = 41,
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
            var documentPath = "c:\\Test.razor";
            var contents = $"@page \"/test\"{Environment.NewLine}@code {{ private var x = 1; }}";
            var codeDocument = CreateCodeDocument(contents);
            codeDocument.SetFileKind(FileKinds.Legacy);

            var resolver = new ExtractToCodeBehindCodeActionResolver(new DefaultForegroundDispatcher(), CreateDocumentResolver(documentPath, codeDocument), FilePathNormalizer);
            var data = JObject.FromObject(new ExtractToCodeBehindCodeActionParams()
            {
                Uri = new Uri("c:/Test.razor"),
                RemoveStart = 14,
                ExtractStart = 20,
                ExtractEnd = 41,
                RemoveEnd = 41,
            });

            // Act
            var workspaceEdit = await resolver.ResolveAsync(data, default);

            // Assert
            Assert.Null(workspaceEdit);
        }

        [Fact]
        public async Task Handle_ExtractCodeBlock()
        {
            // Arrange
            var documentPath = "c:/Test.razor";
            var documentUri = new Uri(documentPath);
            var contents = $"@page \"/test\"{Environment.NewLine}@code {{ private var x = 1; }}";
            var codeDocument = CreateCodeDocument(contents);

            var resolver = new ExtractToCodeBehindCodeActionResolver(new DefaultForegroundDispatcher(), CreateDocumentResolver(documentPath, codeDocument), FilePathNormalizer);
            var actionParams = new ExtractToCodeBehindCodeActionParams
            {
                Uri = documentUri,
                RemoveStart = contents.IndexOf("@code", StringComparison.Ordinal),
                ExtractStart = contents.IndexOf("{", StringComparison.Ordinal),
                ExtractEnd = contents.IndexOf("}", StringComparison.Ordinal),
                RemoveEnd = contents.IndexOf("}", StringComparison.Ordinal),
            };
            var data = JObject.FromObject(actionParams);

            // Act
            var workspaceEdit = await resolver.ResolveAsync(data, default);

            // Assert
            Assert.NotNull(workspaceEdit);
            Assert.NotNull(workspaceEdit.DocumentChanges);
            Assert.Equal(3, workspaceEdit.DocumentChanges.Count());

            var documentChanges = workspaceEdit.DocumentChanges.ToArray();
            var createFileChange = documentChanges[0];
            Assert.True(createFileChange.IsCreateFile);

            var editCodeDocumentChange = documentChanges[1];
            var editCodeDocumentEdit = editCodeDocumentChange.TextDocumentEdit.Edits.First();
            Assert.Equal(actionParams.RemoveStart, editCodeDocumentEdit.Range.Start.GetAbsoluteIndex(codeDocument.GetSourceText()));
            Assert.Equal(actionParams.RemoveEnd, editCodeDocumentEdit.Range.End.GetAbsoluteIndex(codeDocument.GetSourceText()));

            var editCodeBehindChange = documentChanges[2];
            var editCodeBehindEdit = editCodeBehindChange.TextDocumentEdit.Edits.First();
            Assert.Contains("public partial class Test", editCodeBehindEdit.NewText, StringComparison.Ordinal);
            Assert.Contains("private var x = 1", editCodeBehindEdit.NewText, StringComparison.Ordinal);
            Assert.Contains("namespace test.Pages", editCodeBehindEdit.NewText, StringComparison.Ordinal);
        }

        [Fact]
        public async Task Handle_ExtractFunctionsBlock()
        {
            // Arrange
            var documentPath = "c:/Test.razor";
            var documentUri = new Uri(documentPath);
            var contents = $"@page \"/test\"{Environment.NewLine}@functions {{ private var x = 1; }}";
            var codeDocument = CreateCodeDocument(contents);

            var resolver = new ExtractToCodeBehindCodeActionResolver(new DefaultForegroundDispatcher(), CreateDocumentResolver(documentPath, codeDocument), FilePathNormalizer);
            var actionParams = new ExtractToCodeBehindCodeActionParams
            {
                Uri = documentUri,
                RemoveStart = contents.IndexOf("@functions", StringComparison.Ordinal),
                ExtractStart = contents.IndexOf("{", StringComparison.Ordinal),
                ExtractEnd = contents.IndexOf("}", StringComparison.Ordinal),
                RemoveEnd = contents.IndexOf("}", StringComparison.Ordinal),
            };
            var data = JObject.FromObject(actionParams);

            // Act
            var workspaceEdit = await resolver.ResolveAsync(data, default);

            // Assert
            Assert.NotNull(workspaceEdit);
            Assert.NotNull(workspaceEdit.DocumentChanges);
            Assert.Equal(3, workspaceEdit.DocumentChanges.Count());

            var documentChanges = workspaceEdit.DocumentChanges.ToArray();
            var createFileChange = documentChanges[0];
            Assert.True(createFileChange.IsCreateFile);

            var editCodeDocumentChange = documentChanges[1];
            var editCodeDocumentEdit = editCodeDocumentChange.TextDocumentEdit.Edits.First();
            Assert.Equal(actionParams.RemoveStart, editCodeDocumentEdit.Range.Start.GetAbsoluteIndex(codeDocument.GetSourceText()));
            Assert.Equal(actionParams.RemoveEnd, editCodeDocumentEdit.Range.End.GetAbsoluteIndex(codeDocument.GetSourceText()));

            var editCodeBehindChange = documentChanges[2];
            var editCodeBehindEdit = editCodeBehindChange.TextDocumentEdit.Edits.First();
            Assert.Contains("public partial class Test", editCodeBehindEdit.NewText, StringComparison.Ordinal);
            Assert.Contains("private var x = 1", editCodeBehindEdit.NewText, StringComparison.Ordinal);
            Assert.Contains("namespace test.Pages", editCodeBehindEdit.NewText, StringComparison.Ordinal);
        }

        [Fact]
        public async Task Handle_ExtractCodeBlockWithUsing()
        {
            // Arrange
            var documentPath = "c:/Test.razor";
            var documentUri = new Uri(documentPath);
            var contents = $"@page \"/test\"\n@using System.Diagnostics{Environment.NewLine}@code {{ private var x = 1; }}";
            var codeDocument = CreateCodeDocument(contents);

            var resolver = new ExtractToCodeBehindCodeActionResolver(new DefaultForegroundDispatcher(), CreateDocumentResolver(documentPath, codeDocument), FilePathNormalizer);
            var actionParams = new ExtractToCodeBehindCodeActionParams
            {
                Uri = documentUri,
                RemoveStart = contents.IndexOf("@code", StringComparison.Ordinal),
                ExtractStart = contents.IndexOf("{", StringComparison.Ordinal),
                ExtractEnd = contents.IndexOf("}", StringComparison.Ordinal),
                RemoveEnd = contents.IndexOf("}", StringComparison.Ordinal),
            };
            var data = JObject.FromObject(actionParams);

            // Act
            var workspaceEdit = await resolver.ResolveAsync(data, default);

            // Assert
            Assert.NotNull(workspaceEdit);
            Assert.NotNull(workspaceEdit.DocumentChanges);
            Assert.Equal(3, workspaceEdit.DocumentChanges.Count());

            var documentChanges = workspaceEdit.DocumentChanges.ToArray();
            var createFileChange = documentChanges[0];
            Assert.True(createFileChange.IsCreateFile);

            var editCodeDocumentChange = documentChanges[1];
            var editCodeDocumentEdit = editCodeDocumentChange.TextDocumentEdit.Edits.First();
            Assert.Equal(actionParams.RemoveStart, editCodeDocumentEdit.Range.Start.GetAbsoluteIndex(codeDocument.GetSourceText()));
            Assert.Equal(actionParams.RemoveEnd, editCodeDocumentEdit.Range.End.GetAbsoluteIndex(codeDocument.GetSourceText()));

            var editCodeBehindChange = documentChanges[2];
            var editCodeBehindEdit = editCodeBehindChange.TextDocumentEdit.Edits.First();
            Assert.Contains("using System.Diagnostics", editCodeBehindEdit.NewText, StringComparison.Ordinal);
            Assert.Contains("public partial class Test", editCodeBehindEdit.NewText, StringComparison.Ordinal);
            Assert.Contains("private var x = 1", editCodeBehindEdit.NewText, StringComparison.Ordinal);
            Assert.Contains("namespace test.Pages", editCodeBehindEdit.NewText, StringComparison.Ordinal);
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
