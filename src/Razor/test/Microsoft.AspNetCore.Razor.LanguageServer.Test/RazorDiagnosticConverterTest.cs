// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Text;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    public class RazorDiagnosticConverterTest
    {
        [Fact]
        public void Convert_Converts()
        {
            // Arrange
            var razorDiagnostic = RazorDiagnosticFactory.CreateDirective_BlockDirectiveCannotBeImported("test");
            var sourceText = SourceText.From(string.Empty);

            // Act
            var diagnostic = RazorDiagnosticConverter.Convert(razorDiagnostic, sourceText);

            // Assert
            Assert.Equal(razorDiagnostic.Id, diagnostic.Code);
            Assert.Equal(razorDiagnostic.GetMessage(), diagnostic.Message);
            Assert.Null(diagnostic.Range);
            Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        }

        [Fact]
        public void ConvertSeverity_ErrorReturnsError()
        {
            // Arrange
            var expectedSeverity = DiagnosticSeverity.Error;

            // Act
            var severity = RazorDiagnosticConverter.ConvertSeverity(RazorDiagnosticSeverity.Error);

            // Assert
            Assert.Equal(expectedSeverity, severity);
        }

        [Fact]
        public void ConvertSpanToRange_ReturnsConvertedRange()
        {
            // Arrange
            var sourceSpan = new SourceSpan(3, 0, 3, 4);
            var sourceText = SourceText.From("Hello World");
            var expectedRange = new Range(
                new Position(0, 3),
                new Position(0, 7));

            // Act
            var range = RazorDiagnosticConverter.ConvertSpanToRange(sourceSpan, sourceText);

            // Assert
            Assert.Equal(expectedRange, range);
        }

        [Fact]
        public void ConvertSpanToRange_StartsOutsideOfDocument_EmptyDocument_NormalizesTo0()
        {
            // Arrange
            var sourceText = SourceText.From(string.Empty);
            var sourceSpan = new SourceSpan(5, 0, 5, 4);
            var expectedRange = new Range(
                new Position(0, 0),
                new Position(0, 0));

            // Act
            var range = RazorDiagnosticConverter.ConvertSpanToRange(sourceSpan, sourceText);

            // Assert
            Assert.Equal(expectedRange, range);
        }

        [Fact]
        public void ConvertSpanToRange_StartsOutsideOfDocument_NormalizesToEnd()
        {
            // Arrange
            var sourceText = SourceText.From("Hello World");
            var sourceSpan = new SourceSpan(sourceText.Length + 5, 0, sourceText.Length + 5, 4);
            var expectedRange = new Range(
                new Position(0, 10),
                new Position(0, 10));

            // Act
            var range = RazorDiagnosticConverter.ConvertSpanToRange(sourceSpan, sourceText);

            // Assert
            Assert.Equal(expectedRange, range);
        }

        [Fact]
        public void ConvertSpanToRange_EndsOutsideOfDocument_NormalizesToEnd()
        {
            // Arrange
            var sourceText = SourceText.From("Hello World");
            var sourceSpan = new SourceSpan(6, 0, 6, 15);
            var expectedRange = new Range(
                new Position(0, 6),
                new Position(0, 10));

            // Act
            var range = RazorDiagnosticConverter.ConvertSpanToRange(sourceSpan, sourceText);

            // Assert
            Assert.Equal(expectedRange, range);
        }

        [Fact]
        public void ConvertSpanToRange_ReturnsNullIfSpanIsUndefined()
        {
            // Arrange
            var sourceSpan = SourceSpan.Undefined;
            var sourceText = SourceText.From(string.Empty);

            // Act
            var range = RazorDiagnosticConverter.ConvertSpanToRange(sourceSpan, sourceText);

            // Assert
            Assert.Null(range);
        }
    }
}
