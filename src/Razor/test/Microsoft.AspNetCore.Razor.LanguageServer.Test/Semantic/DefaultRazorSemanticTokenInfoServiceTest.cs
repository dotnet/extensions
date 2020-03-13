// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.Completion;
using Microsoft.AspNetCore.Razor.LanguageServer.Semantic;
using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Test.Semantic
{
    public class DefaultRazorSemanticTokenInfoServiceTest : DefaultTagHelperServiceTestBase
    {
        #region TagHelpers
        [Fact]
        public void GetSemanticTokens_NoAttributes()
        {
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test1></test1>";
            var expectedData = new List<uint> {
                1, 1, 5, 0, 0, //line, character pos, length, tokenType, modifier
                0, 8, 5, 0, 0
            };

            AssertSemanticTokens(txt, expectedData);
        }

        [Fact]
        public void GetSemanticTokens_WithAttribute()
        {
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test1 bool-val='true'></test1>";
            var expectedData = new List<uint> {
                1, 1, 5, 0, 0, //line, character pos, length, tokenType, modifier
                0, 6, 8, 1, 0,
                0, 18, 5, 0, 0
            };

            AssertSemanticTokens(txt, expectedData);
        }

        [Fact]
        public void GetSemanticTokens_MinimizedAttribute()
        {
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test1 bool-val></test1>";
            var expectedData = new List<uint> {
                1, 1, 5, 0, 0, //line, character pos, length, tokenType, modifier
                0, 6, 8, 1, 0,
                0, 11, 5, 0, 0
            };

            AssertSemanticTokens(txt, expectedData);
        }

        [Fact]
        public void GetSemanticTokens_IgnoresNonTagHelperAttributes()
        {
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test1 bool-val='true' class='display:none'></test1>";
            var expectedData = new List<uint> {
                1, 1, 5, 0, 0, //line, character pos, length, tokenType, modifier
                0, 6, 8, 1, 0,
                0, 39, 5, 0, 0
            };

            AssertSemanticTokens(txt, expectedData);
        }

        [Fact]
        public void GetSemanticTokens_DoesNotApplyOnNonTagHelpers()
        {
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<p bool-val='true'></p>";
            var expectedData = new List<uint> { };

            AssertSemanticTokens(txt, expectedData);
        }
        #endregion TagHelpers

        #region DirectiveAttributes
        [Fact(Skip = "Haven't implemented directive attributes yet")]
        public void GetSemanticTokens_DirectiveAttributes()
        {
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test1 @onclick='Function'></test1>";
            var expectedData = new List<uint> {
                1, 1, 5, 1, 0, //line, character pos, length, tokenType, modifier
                0, 8, 5, 2, 0
            };

            AssertSemanticTokens(txt, expectedData);
        }

        [Fact(Skip = "Haven't implemented directive attributes yet")]
        public void GetSemanticTokens_DirectiveAttributesWithParameters()
        {
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test1 @onclick:preventDefault='Function'></test1>";
            var expectedData = new List<uint> {
                1, 1, 5, 1, 0, //line, character pos, length, tokenType, modifier
                0, 8, 5, 2, 0
            };

            AssertSemanticTokens(txt, expectedData);
        }
        #endregion DirectiveAttributes

        private void AssertSemanticTokens(string txt, IEnumerable<uint> expectedData)
        {
            // Arrange
            var service = GetDefaultRazorSemanticTokenInfoService();
            var codeDocument = CreateCodeDocument(txt, DefaultTagHelpers);
            var location = new SourceLocation(txt.IndexOf("test1"), -1, -1);

            // Act
            var tokens = service.GetSemanticTokens(codeDocument, location);

            // Assert
            Assert.Equal(expectedData, tokens.Data);
        }

        private RazorSemanticTokenInfoService GetDefaultRazorSemanticTokenInfoService()
        {
            return new DefaultRazorSemanticTokenInfoService();
        }
    }
}
