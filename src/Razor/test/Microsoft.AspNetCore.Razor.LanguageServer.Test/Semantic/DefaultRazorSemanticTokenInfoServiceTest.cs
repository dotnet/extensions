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

            AssertSemanticTokens(txt, expectedData, isRazor: false);
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

            AssertSemanticTokens(txt, expectedData, isRazor: false);
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

            AssertSemanticTokens(txt, expectedData, isRazor: false);
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

            AssertSemanticTokens(txt, expectedData, isRazor: false);
        }

        [Fact]
        public void GetSemanticTokens_TagHelpersNotAvailableInRazor()
        {
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test1 bool-val='true' class='display:none'></test1>";
            var expectedData = new List<uint> { };

            AssertSemanticTokens(txt, expectedData, isRazor: true);
        }

        [Fact]
        public void GetSemanticTokens_DoesNotApplyOnNonTagHelpers()
        {
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<p bool-val='true'></p>";
            var expectedData = new List<uint> { };

            AssertSemanticTokens(txt, expectedData, isRazor: false);
        }
        #endregion TagHelpers

        #region DirectiveAttributes
        [Fact]
        public void GetSemanticTokens_Razor_MinimizedDirectiveAttributeParameters()
        {
            // Capitalized, non-well-known-HTML elements are always marked as TagHelpers
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<NotATagHelp @minimized:something />";
            var expectedData = new List<uint> {
                1, 1, 11, 0, 0,
                0, 12, 1, 2, 0,
                0, 1, 9, 4, 0,
                0, 9, 1, 3, 0,
                0, 1, 9, 4, 0
            };

            AssertSemanticTokens(txt, expectedData, isRazor: true);
        }

        [Fact]
        public void GetSemanticTokens_Razor_DirectiveAttributesParameters()
        {
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test1 @test:something='Function'></test1>";
            var expectedData = new List<uint> {
                1, 1, 5, 0, 0, //line, character pos, length, tokenType, modifier
                0, 6, 1, 2, 0,
                0, 1, 4, 4, 0,
                0, 4, 1, 3, 0,
                0, 1, 9, 4, 0,
                0, 23, 5, 0, 0
            };

            AssertSemanticTokens(txt, expectedData, isRazor: true);
        }

        [Fact]
        public void GetSemanticTokens_Razor_NonComponentsDoNotShowInRazor()
        {
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test1 bool-val='true'></test1>";
            var expectedData = new List<uint> { };

            AssertSemanticTokens(txt, expectedData, isRazor: true);
        }

        [Fact]
        public void GetSemanticTokens_Razor_Directives()
        {
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test1 @test='Function'></test1>";
            var expectedData = new List<uint> {
                1, 1, 5, 0, 0, //line, character pos, length, tokenType, modifier
                0, 6, 1, 2, 0,
                0, 1, 4, 4, 0,
                0, 18, 5, 0, 0
            };

            AssertSemanticTokens(txt, expectedData, isRazor: true);
        }

        [Fact]
        public void GetSemanticTokens_Razor_DoNotColorNonTagHelpers()
        {
            var txt = $"@addTaghelper *, TestAssembly{Environment.NewLine}<p @test='Function'></p>";
            var expectedData = new List<uint> {
                1, 3, 1, 2, 0,
                0, 1, 4, 4, 0
            };

            AssertSemanticTokens(txt, expectedData, isRazor: true);
        }

        [Fact]
        public void GetSemanticTokens_Razor_DoesNotApplyOnNonTagHelpers()
        {
            var txt = $"@addTagHelpers *, TestAssembly{Environment.NewLine}<p></p>";
            var expectedData = new List<uint> { };

            AssertSemanticTokens(txt, expectedData, isRazor: true);
        }
        #endregion DirectiveAttributes

        private void AssertSemanticTokens(string txt, IEnumerable<uint> expectedData, bool isRazor)
        {
            // Arrange
            var service = GetDefaultRazorSemanticTokenInfoService();
            RazorCodeDocument codeDocument;
            if (isRazor)
            {
                codeDocument = CreateRazorDocument(txt, DefaultTagHelpers);
            }
            else
            {
                codeDocument = CreateCodeDocument(txt, DefaultTagHelpers);
            }
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
