// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;
using Moq;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Formatting
{
    public class FormattingContentValidationPassTest : LanguageServerTestBase
    {
        [Fact]
        public void Execute_LanguageKindCSharp_Noops()
        {
            // Arrange
            var source = SourceText.From(@"
@code {
    public class Foo { }
}
");
            var context = CreateFormattingContext(source);
            var input = new FormattingResult(Array.Empty<TextEdit>(), RazorLanguageKind.CSharp);
            var pass = GetPass(context.CodeDocument);

            // Act
            var result = pass.Execute(context, input);

            // Assert
            Assert.Equal(input, result);
        }

        [Fact]
        public void Execute_LanguageKindHtml_Noops()
        {
            // Arrange
            var source = SourceText.From(@"
@code {
    public class Foo { }
}
");
            var context = CreateFormattingContext(source);
            var input = new FormattingResult(Array.Empty<TextEdit>(), RazorLanguageKind.Html);
            var pass = GetPass(context.CodeDocument);

            // Act
            var result = pass.Execute(context, input);

            // Assert
            Assert.Equal(input, result);
        }

        [Fact]
        public void Execute_NonDestructiveEdit_Allowed()
        {
            // Arrange
            var source = SourceText.From(@"
@code {
public class Foo { }
}
");
            var context = CreateFormattingContext(source);
            var edits = new[]
            {
                new TextEdit()
                {
                    NewText = "    ",
                    Range = new Range(new Position(2, 0), new Position(2, 0))
                }
            };
            var input = new FormattingResult(edits, RazorLanguageKind.Razor);
            var pass = GetPass(context.CodeDocument);

            // Act
            var result = pass.Execute(context, input);

            // Assert
            Assert.Equal(input, result);
        }

        [Fact]
        public void Execute_DestructiveEdit_Rejected()
        {
            // Arrange
            var source = SourceText.From(@"
@code {
public class Foo { }
}
");
            var context = CreateFormattingContext(source);
            var edits = new[]
            {
                new TextEdit()
                {
                    NewText = "    ",
                    Range = new Range(new Position(2, 0), new Position(3, 0)) // Nukes a line
                }
            };
            var input = new FormattingResult(edits, RazorLanguageKind.Razor);
            var pass = GetPass(context.CodeDocument);

            // Act
            var result = pass.Execute(context, input);

            // Assert
            Assert.Empty(result.Edits);
        }

        private FormattingContentValidationPass GetPass(RazorCodeDocument codeDocument)
        {
            var mappingService = new DefaultRazorDocumentMappingService();

            var client = Mock.Of<ClientNotifierServiceBase>(MockBehavior.Strict);
            var pass = new FormattingContentValidationPass(mappingService, FilePathNormalizer, client, LoggerFactory);
            pass.DebugAssertsEnabled = false;

            return pass;
        }

        private FormattingContext CreateFormattingContext(SourceText source, int tabSize = 4, bool insertSpaces = true, string fileKind = null)
        {
            var path = "file:///path/to/document.razor";
            var uri = new Uri(path);
            var (codeDocument, documentSnapshot) = CreateCodeDocumentAndSnapshot(source, uri.AbsolutePath, fileKind: fileKind);
            var options = new FormattingOptions()
            {
                TabSize = tabSize,
                InsertSpaces = insertSpaces,
            };

            var context = FormattingContext.Create(uri, documentSnapshot, codeDocument, options);
            return context;
        }

        private static (RazorCodeDocument, DocumentSnapshot) CreateCodeDocumentAndSnapshot(SourceText text, string path, IReadOnlyList<TagHelperDescriptor> tagHelpers = null, string fileKind = default)
        {
            fileKind ??= FileKinds.Component;
            tagHelpers ??= Array.Empty<TagHelperDescriptor>();
            var sourceDocument = text.GetRazorSourceDocument(path, path);
            var projectEngine = RazorProjectEngine.Create(builder => { builder.SetRootNamespace("Test"); });
            var codeDocument = projectEngine.ProcessDesignTime(sourceDocument, fileKind, Array.Empty<RazorSourceDocument>(), tagHelpers);

            var documentSnapshot = new Mock<DocumentSnapshot>(MockBehavior.Strict);
            documentSnapshot.Setup(d => d.GetGeneratedOutputAsync()).Returns(Task.FromResult(codeDocument));
            documentSnapshot.Setup(d => d.Project.GetProjectEngine()).Returns(projectEngine);
            documentSnapshot.Setup(d => d.TargetPath).Returns(path);
            documentSnapshot.Setup(d => d.Project.TagHelpers).Returns(tagHelpers);
            documentSnapshot.Setup(d => d.FileKind).Returns(fileKind);

            return (codeDocument, documentSnapshot.Object);
        }
    }
}
