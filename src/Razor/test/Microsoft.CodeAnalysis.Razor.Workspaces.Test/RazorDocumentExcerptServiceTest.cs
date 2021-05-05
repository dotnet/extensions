// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Xunit;

namespace Microsoft.CodeAnalysis.Razor
{
    public class RazorDocumentExcerptServiceTest : DocumentExcerptServiceTestBase
    {
        protected override void ConfigureWorkspaceServices(List<IWorkspaceService> services)
        {
            services.Add(new TestTagHelperResolver());
        }

        [Fact]
        public async Task TryGetExcerptInternalAsync_SingleLine_CanClassifyCSharp()
        {
            // Arrange
            var razorSource = @"
<html>
@{
    var [|foo|] = ""Hello, World!"";
}
  <body>@foo</body>
  <div>@(3 + 4)</div><div>@(foo + foo)</div>
</html>
";

            var (primary, secondary, secondarySpan) = await InitializeWithSnapshotAsync(razorSource);

            var service = CreateExcerptService(primary);

            // Act
            var result = await service.TryGetExcerptInternalAsync(secondary, secondarySpan, ExcerptModeInternal.SingleLine, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(secondarySpan, result.Value.Span);
            Assert.Same(secondary, result.Value.Document);

            // Verifies that the right part of the primary document will be highlighted.
            Assert.Equal(
                (await secondary.GetTextAsync()).GetSubText(secondarySpan).ToString(),
                result.Value.Content.GetSubText(result.Value.MappedSpan).ToString(),
                ignoreLineEndingDifferences: true);

            Assert.Equal(@"var foo = ""Hello, World!"";", result.Value.Content.ToString(), ignoreLineEndingDifferences: true);
            Assert.Collection(
                result.Value.ClassifiedSpans,
                c =>
                {
                    Assert.Equal(ClassificationTypeNames.Keyword, c.ClassificationType);
                    Assert.Equal("var", result.Value.Content.GetSubText(c.TextSpan).ToString());
                },
                c =>
                {
                    Assert.Equal(ClassificationTypeNames.Text, c.ClassificationType);
                    Assert.Equal(" ", result.Value.Content.GetSubText(c.TextSpan).ToString());
                },
                c =>
                {
                    Assert.Equal(ClassificationTypeNames.LocalName, c.ClassificationType);
                    Assert.Equal("foo", result.Value.Content.GetSubText(c.TextSpan).ToString());
                },
                c =>
                {
                    Assert.Equal(ClassificationTypeNames.Text, c.ClassificationType);
                    Assert.Equal(" ", result.Value.Content.GetSubText(c.TextSpan).ToString());
                },
                c =>
                {
                    Assert.Equal(ClassificationTypeNames.Operator, c.ClassificationType);
                    Assert.Equal("=", result.Value.Content.GetSubText(c.TextSpan).ToString());
                },
                c =>
                {
                    Assert.Equal(ClassificationTypeNames.Text, c.ClassificationType);
                    Assert.Equal(" ", result.Value.Content.GetSubText(c.TextSpan).ToString());
                },
                c =>
                {
                    Assert.Equal(ClassificationTypeNames.StringLiteral, c.ClassificationType);
                    Assert.Equal("\"Hello, World!\"", result.Value.Content.GetSubText(c.TextSpan).ToString());
                },
                c =>
                {
                    Assert.Equal(ClassificationTypeNames.Punctuation, c.ClassificationType);
                    Assert.Equal(";", result.Value.Content.GetSubText(c.TextSpan).ToString());
                });
        }

        [Fact]
        public async Task TryGetExcerptInternalAsync_SingleLine_CanClassifyCSharp_ImplicitExpression()
        {
            // Arrange
            var razorSource = @"
<html>
@{
    var foo = ""Hello, World!"";
}
  <body>@[|foo|]</body>
  <div>@(3 + 4)</div><div>@(foo + foo)</div>
</html>
";

            var (primary, secondary, secondarySpan) = await InitializeWithSnapshotAsync(razorSource);

            var service = CreateExcerptService(primary);

            // Act
            var result = await service.TryGetExcerptInternalAsync(secondary, secondarySpan, ExcerptModeInternal.SingleLine, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(secondarySpan, result.Value.Span);
            Assert.Same(secondary, result.Value.Document);

            // Verifies that the right part of the primary document will be highlighted.
            Assert.Equal(
                (await secondary.GetTextAsync()).GetSubText(secondarySpan).ToString(),
                result.Value.Content.GetSubText(result.Value.MappedSpan).ToString(),
                ignoreLineEndingDifferences: true);

            Assert.Equal(@"<body>@foo</body>", result.Value.Content.ToString(), ignoreLineEndingDifferences: true);
            Assert.Collection(
                result.Value.ClassifiedSpans,
                c =>
                {
                    Assert.Equal(ClassificationTypeNames.Text, c.ClassificationType);
                    Assert.Equal("<body>@", result.Value.Content.GetSubText(c.TextSpan).ToString());
                },
                c =>
                {
                    Assert.Equal(ClassificationTypeNames.LocalName, c.ClassificationType);
                    Assert.Equal("foo", result.Value.Content.GetSubText(c.TextSpan).ToString());
                },
                c =>
                {
                    Assert.Equal(ClassificationTypeNames.Text, c.ClassificationType);
                    Assert.Equal("</body>", result.Value.Content.GetSubText(c.TextSpan).ToString());
                });
        }

        [Fact]
        public async Task TryGetExcerptInternalAsync_SingleLine_CanClassifyCSharp_ComplexLine()
        {
            // Arrange
            var razorSource = @"
<html>
@{
    var foo = ""Hello, World!"";
}
  <body>@foo</body>
  <div>@(3 + 4)</div><div>@(foo + [|foo|])</div>
</html>
";

            var (primary, secondary, secondarySpan) = await InitializeWithSnapshotAsync(razorSource);

            var service = CreateExcerptService(primary);

            // Act
            var result = await service.TryGetExcerptInternalAsync(secondary, secondarySpan, ExcerptModeInternal.SingleLine, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(secondarySpan, result.Value.Span);
            Assert.Same(secondary, result.Value.Document);

            Assert.Equal(
                (await secondary.GetTextAsync()).GetSubText(secondarySpan).ToString(),
                result.Value.Content.GetSubText(result.Value.MappedSpan).ToString(),
                ignoreLineEndingDifferences: true);

            // Verifies that the right part of the primary document will be highlighted.
            Assert.Equal(@"<div>@(3 + 4)</div><div>@(foo + foo)</div>", result.Value.Content.ToString(), ignoreLineEndingDifferences: true);
            Assert.Collection(
                result.Value.ClassifiedSpans,
                c =>
                {
                    Assert.Equal(ClassificationTypeNames.Text, c.ClassificationType);
                    Assert.Equal("<div>@(", result.Value.Content.GetSubText(c.TextSpan).ToString());
                },
                c =>
                {
                    Assert.Equal(ClassificationTypeNames.NumericLiteral, c.ClassificationType);
                    Assert.Equal("3", result.Value.Content.GetSubText(c.TextSpan).ToString());
                },
                c =>
                {
                    Assert.Equal(ClassificationTypeNames.Text, c.ClassificationType);
                    Assert.Equal(" ", result.Value.Content.GetSubText(c.TextSpan).ToString());
                },
                c =>
                {
                    Assert.Equal(ClassificationTypeNames.Operator, c.ClassificationType);
                    Assert.Equal("+", result.Value.Content.GetSubText(c.TextSpan).ToString());
                },
                c =>
                {
                    Assert.Equal(ClassificationTypeNames.Text, c.ClassificationType);
                    Assert.Equal(" ", result.Value.Content.GetSubText(c.TextSpan).ToString());
                },
                c =>
                {
                    Assert.Equal(ClassificationTypeNames.NumericLiteral, c.ClassificationType);
                    Assert.Equal("4", result.Value.Content.GetSubText(c.TextSpan).ToString());
                },
                c =>
                {
                    Assert.Equal(ClassificationTypeNames.Text, c.ClassificationType);
                    Assert.Equal(")</div><div>@(", result.Value.Content.GetSubText(c.TextSpan).ToString());
                },
                c =>
                {
                    Assert.Equal(ClassificationTypeNames.LocalName, c.ClassificationType);
                    Assert.Equal("foo", result.Value.Content.GetSubText(c.TextSpan).ToString());
                },
                c =>
                {
                    Assert.Equal(ClassificationTypeNames.Text, c.ClassificationType);
                    Assert.Equal(" ", result.Value.Content.GetSubText(c.TextSpan).ToString());
                },
                c =>
                {
                    Assert.Equal(ClassificationTypeNames.Operator, c.ClassificationType);
                    Assert.Equal("+", result.Value.Content.GetSubText(c.TextSpan).ToString());
                },
                c =>
                {
                    Assert.Equal(ClassificationTypeNames.Text, c.ClassificationType);
                    Assert.Equal(" ", result.Value.Content.GetSubText(c.TextSpan).ToString());
                },
                c =>
                {
                    Assert.Equal(ClassificationTypeNames.LocalName, c.ClassificationType);
                    Assert.Equal("foo", result.Value.Content.GetSubText(c.TextSpan).ToString());
                },
                c =>
                {
                    Assert.Equal(ClassificationTypeNames.Text, c.ClassificationType);
                    Assert.Equal(")</div>", result.Value.Content.GetSubText(c.TextSpan).ToString());
                });
        }

        [Fact]
        public async Task TryGetExcerptInternalAsync_MultiLine_CanClassifyCSharp()
        {
            // Arrange
            var razorSource = @"
<html>
@{
    var [|foo|] = ""Hello, World!"";
}
  <body></body>
  <div></div>
</html>
";

            var (primary, secondary, secondarySpan) = await InitializeWithSnapshotAsync(razorSource);

            var service = CreateExcerptService(primary);

            // Act
            var result = await service.TryGetExcerptInternalAsync(secondary, secondarySpan, ExcerptModeInternal.Tooltip, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(secondarySpan, result.Value.Span);
            Assert.Same(secondary, result.Value.Document);

            // Verifies that the right part of the primary document will be highlighted.
            Assert.Equal(
                (await secondary.GetTextAsync()).GetSubText(secondarySpan).ToString(),
                result.Value.Content.GetSubText(result.Value.MappedSpan).ToString(),
                ignoreLineEndingDifferences: true);

            Assert.Equal(
@"
<html>
@{
    var foo = ""Hello, World!"";
}
  <body></body>
  <div></div>",
                result.Value.Content.ToString(), ignoreLineEndingDifferences: true);

            Assert.Collection(
                result.Value.ClassifiedSpans,
                c =>
                {
                    Assert.Equal(ClassificationTypeNames.Text, c.ClassificationType);
                    Assert.Equal(
@"
<html>
@{",
                            result.Value.Content.GetSubText(c.TextSpan).ToString(),
                            ignoreLineEndingDifferences: true);
                },
                c =>
                {
                    Assert.Equal(ClassificationTypeNames.Text, c.ClassificationType);
                    Assert.Equal("\r\n    ", result.Value.Content.GetSubText(c.TextSpan).ToString(), ignoreLineEndingDifferences: true);
                },
                c =>
                {
                    Assert.Equal(ClassificationTypeNames.Keyword, c.ClassificationType);
                    Assert.Equal("var", result.Value.Content.GetSubText(c.TextSpan).ToString());
                },
                c =>
                {
                    Assert.Equal(ClassificationTypeNames.Text, c.ClassificationType);
                    Assert.Equal(" ", result.Value.Content.GetSubText(c.TextSpan).ToString());
                },
                c =>
                {
                    Assert.Equal(ClassificationTypeNames.LocalName, c.ClassificationType);
                    Assert.Equal("foo", result.Value.Content.GetSubText(c.TextSpan).ToString());
                },
                c =>
                {
                    Assert.Equal(ClassificationTypeNames.Text, c.ClassificationType);
                    Assert.Equal(" ", result.Value.Content.GetSubText(c.TextSpan).ToString());
                },
                c =>
                {
                    Assert.Equal(ClassificationTypeNames.Operator, c.ClassificationType);
                    Assert.Equal("=", result.Value.Content.GetSubText(c.TextSpan).ToString());
                },
                c =>
                {
                    Assert.Equal(ClassificationTypeNames.Text, c.ClassificationType);
                    Assert.Equal(" ", result.Value.Content.GetSubText(c.TextSpan).ToString());
                },
                c =>
                {
                    Assert.Equal(ClassificationTypeNames.StringLiteral, c.ClassificationType);
                    Assert.Equal("\"Hello, World!\"", result.Value.Content.GetSubText(c.TextSpan).ToString());
                },
                c =>
                {
                    Assert.Equal(ClassificationTypeNames.Punctuation, c.ClassificationType);
                    Assert.Equal(";", result.Value.Content.GetSubText(c.TextSpan).ToString());
                },
                c =>
                {
                    Assert.Equal(ClassificationTypeNames.Text, c.ClassificationType);
                    Assert.Equal("\r\n", result.Value.Content.GetSubText(c.TextSpan).ToString(), ignoreLineEndingDifferences: true);
                },
                c =>
                {
                    Assert.Equal(ClassificationTypeNames.Text, c.ClassificationType);
                    Assert.Equal(
@"}
  <body></body>
  <div></div>",
                        result.Value.Content.GetSubText(c.TextSpan).ToString(),
                        ignoreLineEndingDifferences: true);
                });
        }

        [Fact]
        public async Task TryGetExcerptInternalAsync_MultiLine_Boundaries_CanClassifyCSharp()
        {
            // Arrange
            var razorSource = @"@{ var [|foo|] = ""Hello, World!""; }";

            var (primary, secondary, secondarySpan) = await InitializeWithSnapshotAsync(razorSource);

            var service = CreateExcerptService(primary);

            // Act
            var result = await service.TryGetExcerptInternalAsync(secondary, secondarySpan, ExcerptModeInternal.Tooltip, CancellationToken.None);

            // Assert
            // Verifies that the right part of the primary document will be highlighted.
            Assert.NotNull(result);
            Assert.Equal(secondarySpan, result.Value.Span);
            Assert.Same(secondary, result.Value.Document);

            Assert.Equal(
                (await secondary.GetTextAsync()).GetSubText(secondarySpan).ToString(),
                result.Value.Content.GetSubText(result.Value.MappedSpan).ToString(),
                ignoreLineEndingDifferences: true);

            Assert.Equal(
@"@{ var foo = ""Hello, World!""; }",
                result.Value.Content.ToString(), ignoreLineEndingDifferences: true);

            Assert.Collection(
                result.Value.ClassifiedSpans,
                c =>
                {
                    Assert.Equal(ClassificationTypeNames.Text, c.ClassificationType);
                    Assert.Equal("@{", result.Value.Content.GetSubText(c.TextSpan).ToString());
                },
                c =>
                {
                    Assert.Equal(ClassificationTypeNames.Text, c.ClassificationType);
                    Assert.Equal(" ", result.Value.Content.GetSubText(c.TextSpan).ToString());
                },
                c =>
                {
                    Assert.Equal(ClassificationTypeNames.Keyword, c.ClassificationType);
                    Assert.Equal("var", result.Value.Content.GetSubText(c.TextSpan).ToString());
                },
                c =>
                {
                    Assert.Equal(ClassificationTypeNames.Text, c.ClassificationType);
                    Assert.Equal(" ", result.Value.Content.GetSubText(c.TextSpan).ToString());
                },
                c =>
                {
                    Assert.Equal(ClassificationTypeNames.LocalName, c.ClassificationType);
                    Assert.Equal("foo", result.Value.Content.GetSubText(c.TextSpan).ToString());
                },
                c =>
                {
                    Assert.Equal(ClassificationTypeNames.Text, c.ClassificationType);
                    Assert.Equal(" ", result.Value.Content.GetSubText(c.TextSpan).ToString());
                },
                c =>
                {
                    Assert.Equal(ClassificationTypeNames.Operator, c.ClassificationType);
                    Assert.Equal("=", result.Value.Content.GetSubText(c.TextSpan).ToString());
                },
                c =>
                {
                    Assert.Equal(ClassificationTypeNames.Text, c.ClassificationType);
                    Assert.Equal(" ", result.Value.Content.GetSubText(c.TextSpan).ToString());
                },
                c =>
                {
                    Assert.Equal(ClassificationTypeNames.StringLiteral, c.ClassificationType);
                    Assert.Equal("\"Hello, World!\"", result.Value.Content.GetSubText(c.TextSpan).ToString());
                },
                c =>
                {
                    Assert.Equal(ClassificationTypeNames.Punctuation, c.ClassificationType);
                    Assert.Equal(";", result.Value.Content.GetSubText(c.TextSpan).ToString());
                },
                c =>
                {
                    Assert.Equal(ClassificationTypeNames.Text, c.ClassificationType);
                    Assert.Equal(" ", result.Value.Content.GetSubText(c.TextSpan).ToString());
                },
                c =>
                {
                    Assert.Equal(ClassificationTypeNames.Text, c.ClassificationType);
                    Assert.Equal("}", result.Value.Content.GetSubText(c.TextSpan).ToString());
                });
        }

        private RazorDocumentExcerptService CreateExcerptService(DocumentSnapshot document)
        {
            return new RazorDocumentExcerptService(document, new RazorSpanMappingService(document));
        }
    }
}
