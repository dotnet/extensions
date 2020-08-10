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
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using Xunit;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Formatting
{
    public class FormattingStructureValidationPassTest : LanguageServerTestBase
    {
        [Fact]
        public void Execute_RegularFormatting_Noops()
        {
            // Arrange
            var source = SourceText.From(@"
@code {
    public class Foo { }
}
");
            var context = CreateFormattingContext(source, isFormatOnType: false);
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
        public void Execute_OnTypeFormatting_EditInsidePureCSharpCodeDirective_Allowed()
        {
            // Arrange
            var source = SourceText.From(@"
@{
if (true) { }
}
@code {
public class Foo { }
}
");
            var context = CreateFormattingContext(source, isFormatOnType: true);
            var edits = new[]
            {
                new TextEdit()
                {
                    NewText = "    ",
                    Range = new Range(new Position(5, 0), new Position(5, 0))
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
        public void Execute_OnTypeFormatting_EditInsideMixedCodeDirective_Rejected()
        {
            // Arrange
            var source = SourceText.From(@"
@{
if (true) { }
}
@code {
@* some comment *@
public class Foo { }
}
");
            var context = CreateFormattingContext(source, isFormatOnType: true);
            var edits = new[]
            {
                new TextEdit()
                {
                    NewText = "    ",
                    Range = new Range(new Position(5, 0), new Position(5, 0))
                }
            };
            var input = new FormattingResult(edits, RazorLanguageKind.Razor);
            var pass = GetPass(context.CodeDocument);

            // Act
            var result = pass.Execute(context, input);

            // Assert
            Assert.Empty(result.Edits);
        }

        [Fact]
        public void Execute_OnTypeFormatting_EditOutsideCodeDirective_Rejected()
        {
            // Arrange
            var source = SourceText.From(@"
@{
if (true) { }
}

@code {
    public class Foo { }
}
");
            var context = CreateFormattingContext(source, isFormatOnType: true);
            var edits = new[]
            {
                new TextEdit()
                {
                    NewText = "    ",
                    Range = new Range(new Position(4, 0), new Position(4, 0))
                }
            };
            var input = new FormattingResult(edits, RazorLanguageKind.Razor);
            var pass = GetPass(context.CodeDocument);

            // Act
            var result = pass.Execute(context, input);

            // Assert
            Assert.Empty(result.Edits);
        }

        [Fact]
        public void Execute_OnTypeFormatting_EditInsidePureCSharpStatementBlock_Allowed()
        {
            // Arrange
            var source = SourceText.From(@"
@{
if (true) { }
}
@code {
    public class Foo { }
}
");
            var context = CreateFormattingContext(source, isFormatOnType: true);
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
        public void Execute_OnTypeFormatting_EditInsideMixedStatementBlock_Rejected()
        {
            // Arrange
            var source = SourceText.From(@"
@{
if (true) { <p></p> }
}
@code {
    public class Foo { }
}
");
            var context = CreateFormattingContext(source, isFormatOnType: true);
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
            Assert.Empty(result.Edits);
        }

        [Fact]
        public void Execute_OnTypeFormatting_EditOutsidePureCSharpStatementBlock_Rejected()
        {
            // Arrange
            var source = SourceText.From(@"
@{
if (true) { }
}
@code {
    public class Foo { }
}
");
            var context = CreateFormattingContext(source, isFormatOnType: true);
            var edits = new[]
            {
                new TextEdit()
                {
                    NewText = "    ",
                    Range = new Range(new Position(0, 0), new Position(0, 0))
                }
            };
            var input = new FormattingResult(edits, RazorLanguageKind.Razor);
            var pass = GetPass(context.CodeDocument);

            // Act
            var result = pass.Execute(context, input);

            // Assert
            Assert.Empty(result.Edits);
        }

        private FormattingStructureValidationPass GetPass(RazorCodeDocument codeDocument)
        {
            var mappingService = new DefaultRazorDocumentMappingService();

            var client = new FormattingLanguageServerClient();
            client.AddCodeDocument(codeDocument);
            var languageServer = Mock.Of<ILanguageServer>(ls => ls.Client == client);
            var pass = new FormattingStructureValidationPass(mappingService, FilePathNormalizer, languageServer, LoggerFactory);

            return pass;
        }

        private FormattingContext CreateFormattingContext(SourceText source, int tabSize = 4, bool insertSpaces = true, string fileKind = null, bool isFormatOnType = false)
        {
            var path = "file:///path/to/document.razor";
            var uri = new Uri(path);
            var (codeDocument, documentSnapshot) = CreateCodeDocumentAndSnapshot(source, uri.AbsolutePath, fileKind: fileKind);
            var options = new FormattingOptions()
            {
                TabSize = tabSize,
                InsertSpaces = insertSpaces,
            };

            var context = FormattingContext.Create(uri, documentSnapshot, codeDocument, options, isFormatOnType: isFormatOnType);
            return context;
        }

        private static (RazorCodeDocument, DocumentSnapshot) CreateCodeDocumentAndSnapshot(SourceText text, string path, IReadOnlyList<TagHelperDescriptor> tagHelpers = null, string fileKind = default)
        {
            fileKind ??= FileKinds.Component;
            tagHelpers ??= Array.Empty<TagHelperDescriptor>();
            var sourceDocument = text.GetRazorSourceDocument(path, path);
            var projectEngine = RazorProjectEngine.Create(builder => { builder.SetRootNamespace("Test"); });
            var codeDocument = projectEngine.ProcessDesignTime(sourceDocument, fileKind, Array.Empty<RazorSourceDocument>(), tagHelpers);

            var documentSnapshot = new Mock<DocumentSnapshot>();
            documentSnapshot.Setup(d => d.GetGeneratedOutputAsync()).Returns(Task.FromResult(codeDocument));
            documentSnapshot.Setup(d => d.Project.GetProjectEngine()).Returns(projectEngine);
            documentSnapshot.Setup(d => d.TargetPath).Returns(path);
            documentSnapshot.Setup(d => d.Project.TagHelpers).Returns(tagHelpers);
            documentSnapshot.Setup(d => d.FileKind).Returns(fileKind);

            return (codeDocument, documentSnapshot.Object);
        }
    }
}
