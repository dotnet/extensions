// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
#pragma warning disable CS0618
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.AspNetCore.Razor.LanguageServer.Completion;
using Microsoft.AspNetCore.Razor.LanguageServer.Semantic;
using Microsoft.AspNetCore.Razor.LanguageServer.Semantic.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Models.Proposals;
using Xunit;
using OmniSharpRange = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Test.Semantic
{
    public class DefaultRazorSemanticTokenInfoServiceTest : DefaultTagHelperServiceTestBase
    {
        #region HTML
        [Fact]
        public void GetSemanticTokens_IncompleteTag()
        {
            var txt = "<str";
            var expectedData = new List<int>
            {
                0, 0, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 3, RazorSemanticTokensLegend.MarkupElement, 0,
            };

            AssertSemanticTokens(txt, expectedData, isRazor: false, out var _);
        }

        [Fact]
        public void GetSemanticTokens_MinimizedHTMLAttribute()
        {
            var txt = "<p attr />";
            var expectedData = new List<int>
            {
                0, 0, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 1, RazorSemanticTokensLegend.MarkupElement, 0,
                0, 2, 4, RazorSemanticTokensLegend.MarkupAttribute, 0,
                0, 5, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
            };

            AssertSemanticTokens(txt, expectedData, isRazor: false, out var _);
        }

        [Fact]
        public void GetSemanticTokens_MinimizedHTML()
        {
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<input/> ";
            var expectedData = new List<int> {
                0, 0, 1, RazorSemanticTokensLegend.RazorTransition, 0, //line, character pos, length, tokenType, modifier
                0, 1, 12, RazorSemanticTokensLegend.RazorDirective, 0,
                1, 0, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 5, RazorSemanticTokensLegend.MarkupElement, 0,
                0, 5, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
            };

            AssertSemanticTokens(txt, expectedData, isRazor: false, out var _);
        }

        [Fact]
        public void GetSemanticTokens_HTMLComment()
        {
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<!-- comment with comma's --> ";
            var expectedData = new List<int>
            {
                0, 0, 1, RazorSemanticTokensLegend.RazorTransition, 0, //line, character pos, length, tokenType, modifier
                0, 1, 12, RazorSemanticTokensLegend.RazorDirective, 0,
                1, 0, 4, RazorSemanticTokensLegend.MarkupCommentPunctuation, 0,
                0, 4, 22, RazorSemanticTokensLegend.MarkupComment, 0,
                0, 22, 3, RazorSemanticTokensLegend.MarkupCommentPunctuation, 0,
            };

            AssertSemanticTokens(txt, expectedData, isRazor: false, out var _);
        }

        [Fact]
        public void GetSemanticTokens_PartialHTMLComment()
        {
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<!-- comment";
            var expectedData = new List<int>
            {
                0, 0, 1, RazorSemanticTokensLegend.RazorTransition, 0, //line, character pos, length, tokenType, modifier
                0, 1, 12, RazorSemanticTokensLegend.RazorDirective, 0,
            };

            AssertSemanticTokens(txt, expectedData, isRazor: true, out var _);
        }
        #endregion

        #region TagHelpers
        [Fact]
        public void GetSemanticTokens_HalfOfComment()
        {
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}@* comment";
            var expectedData = new List<int>
            {
                0, 0, 1, RazorSemanticTokensLegend.RazorTransition, 0, //line, character pos, length, tokenType, modifier
                0, 1, 12, RazorSemanticTokensLegend.RazorDirective, 0,
                1, 0, 1, RazorSemanticTokensLegend.RazorCommentTransition, 0,
                0, 1, 1, RazorSemanticTokensLegend.RazorCommentStar, 0,
                0, 1, 8, RazorSemanticTokensLegend.RazorComment, 0,
            };

            AssertSemanticTokens(txt, expectedData, isRazor: false, out var _);
        }

        [Fact]
        public void GetSemanticTokens_NoAttributes()
        {
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test1></test1> ";
            var expectedData = new List<int> {
                0, 0, 1, RazorSemanticTokensLegend.RazorTransition, 0, //line, character pos, length, tokenType, modifier
                0, 1, 12, RazorSemanticTokensLegend.RazorDirective, 0,
                1, 0, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 5, RazorSemanticTokensLegend.RazorTagHelperElement, 0,
                0, 5, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 5, RazorSemanticTokensLegend.RazorTagHelperElement, 0,
                0, 5, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
            };

            AssertSemanticTokens(txt, expectedData, isRazor: false, out var _);
        }

        [Fact]
        public void GetSemanticTokens_WithAttribute()
        {
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test1 bool-val='true'></test1> ";
            var expectedData = new List<int> {
                0, 0, 1, RazorSemanticTokensLegend.RazorTransition, 0, //line, character pos, length, tokenType, modifier
                0, 1, 12, RazorSemanticTokensLegend.RazorDirective, 0,
                1, 0, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 5, RazorSemanticTokensLegend.RazorTagHelperElement, 0,
                0, 6, 8, RazorSemanticTokensLegend.RazorTagHelperAttribute, 0,
                0, 8, 1, RazorSemanticTokensLegend.MarkupOperator, 0,
                0, 7, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 5, RazorSemanticTokensLegend.RazorTagHelperElement, 0,
                0, 5, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
            };

            AssertSemanticTokens(txt, expectedData, isRazor: false, out var _);
        }

        [Fact]
        public void GetSemanticTokens_MinimizedAttribute()
        {
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test1 bool-val></test1> ";
            var expectedData = new List<int> {
                0, 0, 1, RazorSemanticTokensLegend.RazorTransition, 0, //line, character pos, length, tokenType, modifier
                0, 1, 12, RazorSemanticTokensLegend.RazorDirective, 0,
                1, 0, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 5, RazorSemanticTokensLegend.RazorTagHelperElement, 0,
                0, 6, 8, RazorSemanticTokensLegend.RazorTagHelperAttribute, 0,
                0, 8, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 5, RazorSemanticTokensLegend.RazorTagHelperElement, 0,
                0, 5, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
            };

            AssertSemanticTokens(txt, expectedData, isRazor: false, out var _);
        }

        [Fact]
        public void GetSemanticTokens_IgnoresNonTagHelperAttributes()
        {
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test1 bool-val='true' class='display:none'></test1> ";
            var expectedData = new List<int> {
                0, 0, 1, RazorSemanticTokensLegend.RazorTransition, 0, //line, character pos, length, tokenType, modifier
                0, 1, 12, RazorSemanticTokensLegend.RazorDirective, 0,
                1, 0, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 5, RazorSemanticTokensLegend.RazorTagHelperElement, 0,
                0, 6, 8, RazorSemanticTokensLegend.RazorTagHelperAttribute, 0,
                0, 8, 1, RazorSemanticTokensLegend.MarkupOperator, 0,
                0, 8, 5, RazorSemanticTokensLegend.MarkupAttribute, 0,
                0, 5, 1, RazorSemanticTokensLegend.MarkupOperator, 0,
                0, 15, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 5, RazorSemanticTokensLegend.RazorTagHelperElement, 0,
                0, 5, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
            };

            AssertSemanticTokens(txt, expectedData, isRazor: false, out var _);
        }

        [Fact]
        public void GetSemanticTokens_TagHelpersNotAvailableInRazor()
        {
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test1 bool-val='true' class='display:none'></test1> ";
            var expectedData = new List<int> {
                0, 0, 1, RazorSemanticTokensLegend.RazorTransition, 0, //line, character pos, length, tokenType, modifier
                0, 1, 12, RazorSemanticTokensLegend.RazorDirective, 0,
                1, 0, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 5, RazorSemanticTokensLegend.MarkupElement, 0,
                0, 6, 8, RazorSemanticTokensLegend.MarkupAttribute, 0,
                0, 8, 1, RazorSemanticTokensLegend.MarkupOperator, 0,
                0, 8, 5, RazorSemanticTokensLegend.MarkupAttribute, 0,
                0, 5, 1, RazorSemanticTokensLegend.MarkupOperator, 0,
                0, 15, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 5, RazorSemanticTokensLegend.MarkupElement, 0,
                0, 5, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
            };

            AssertSemanticTokens(txt, expectedData, isRazor: true, out var _);
        }

        [Fact]
        public void GetSemanticTokens_DoesNotApplyOnNonTagHelpers()
        {
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<p bool-val='true'></p> ";
            var expectedData = new List<int> {
                0, 0, 1, RazorSemanticTokensLegend.RazorTransition, 0, //line, character pos, length, tokenType, modifier
                0, 1, 12, RazorSemanticTokensLegend.RazorDirective, 0,
                1, 0, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 1, RazorSemanticTokensLegend.MarkupElement, 0,
                0, 2, 8, RazorSemanticTokensLegend.MarkupAttribute, 0,
                0, 8, 1, RazorSemanticTokensLegend.MarkupOperator, 0,
                0, 7, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 1, RazorSemanticTokensLegend.MarkupElement, 0,
                0, 1, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0
            };

            AssertSemanticTokens(txt, expectedData, isRazor: false, out var _);
        }
        #endregion TagHelpers

        #region DirectiveAttributes
        [Fact]
        public void GetSemanticTokens_Razor_MinimizedDirectiveAttributeParameters()
        {
            // Capitalized, non-well-known-HTML elements are always marked as TagHelpers
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<NotATagHelp @minimized:something /> ";
            var expectedData = new List<int> {
                0, 0, 1, RazorSemanticTokensLegend.RazorTransition, 0, //line, character pos, length, tokenType, modifier
                0, 1, 12, RazorSemanticTokensLegend.RazorDirective, 0,
                1, 0, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 11, RazorSemanticTokensLegend.RazorTagHelperElement, 0,
                0, 12, 1, RazorSemanticTokensLegend.RazorTransition, 0,
                0, 1, 9, RazorSemanticTokensLegend.RazorDirectiveAttribute, 0,
                0, 9, 1, RazorSemanticTokensLegend.RazorDirectiveColon, 0,
                0, 1, 9, RazorSemanticTokensLegend.RazorDirectiveAttribute, 0,
                0, 10, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
            };

            AssertSemanticTokens(txt, expectedData, isRazor: true, out var _);
        }

        [Fact]
        public void GetSemanticTokens_Razor_DirectiveAttributesParameters()
        {
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test1 @test:something='Function'></test1> ";
            var expectedData = new List<int> {
                0, 0, 1, RazorSemanticTokensLegend.RazorTransition, 0, //line, character pos, length, tokenType, modifier
                0, 1, 12, RazorSemanticTokensLegend.RazorDirective, 0,
                1, 0, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 5, RazorSemanticTokensLegend.RazorTagHelperElement, 0,
                0, 6, 1, RazorSemanticTokensLegend.RazorTransition, 0,
                0, 1, 4, RazorSemanticTokensLegend.RazorDirectiveAttribute, 0,
                0, 4, 1, RazorSemanticTokensLegend.RazorDirectiveColon, 0,
                0, 1, 9, RazorSemanticTokensLegend.RazorDirectiveAttribute, 0,
                0, 9, 1, RazorSemanticTokensLegend.MarkupOperator, 0,
                0, 11, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 5, RazorSemanticTokensLegend.RazorTagHelperElement, 0,
                0, 5, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
            };

            AssertSemanticTokens(txt, expectedData, isRazor: true, out var _);
        }

        [Fact]
        public void GetSemanticTokens_Razor_NonComponentsDoNotShowInRazor()
        {
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test1 bool-val='true'></test1> ";
            var expectedData = new List<int> {
                0, 0, 1, RazorSemanticTokensLegend.RazorTransition, 0, //line, character pos, length, tokenType, modifier
                0, 1, 12, RazorSemanticTokensLegend.RazorDirective, 0,
                1, 0, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 5, RazorSemanticTokensLegend.MarkupElement, 0,
                0, 6, 8, RazorSemanticTokensLegend.MarkupAttribute, 0,
                0, 8, 1, RazorSemanticTokensLegend.MarkupOperator, 0,
                0, 7, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 5, RazorSemanticTokensLegend.MarkupElement, 0,
                0, 5, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
            };

            AssertSemanticTokens(txt, expectedData, isRazor: true, out var _);
        }

        [Fact]
        public void GetSemanticTokens_Razor_Directives()
        {
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test1 @test='Function'></test1> ";
            var expectedData = new List<int> {
                0, 0, 1, RazorSemanticTokensLegend.RazorTransition, 0, //line, character pos, length, tokenType, modifier
                0, 1, 12, RazorSemanticTokensLegend.RazorDirective, 0,
                1, 0, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 5, RazorSemanticTokensLegend.RazorTagHelperElement, 0,
                0, 6, 1, RazorSemanticTokensLegend.RazorTransition, 0,
                0, 1, 4, RazorSemanticTokensLegend.RazorDirectiveAttribute, 0,
                0, 4, 1, RazorSemanticTokensLegend.MarkupOperator, 0,
                0, 11, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 5, RazorSemanticTokensLegend.RazorTagHelperElement, 0,
                0, 5, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
            };

            AssertSemanticTokens(txt, expectedData, isRazor: true, out var _);
        }

        [Fact]
        public void GetSemanticTokens_Razor_DoNotColorNonTagHelpers()
        {
            var txt = $"{Environment.NewLine}<p @test='Function'></p> ";
            var expectedData = new List<int> {
                1, 0, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 1, RazorSemanticTokensLegend.MarkupElement, 0,
                0, 2, 1, RazorSemanticTokensLegend.RazorTransition, 0,
                0, 1, 4, RazorSemanticTokensLegend.RazorDirectiveAttribute, 0,
                0, 4, 1, RazorSemanticTokensLegend.MarkupOperator, 0,
                0, 11, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 1, RazorSemanticTokensLegend.MarkupElement, 0,
                0, 1, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
            };

            AssertSemanticTokens(txt, expectedData, isRazor: true, out var _);
        }

        [Fact]
        public void GetSemanticTokens_Razor_DoesNotApplyOnNonTagHelpers()
        {
            var txt = $"@addTagHelpers *, TestAssembly{Environment.NewLine}<p></p> ";
            var expectedData = new List<int> {
                1, 0, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 1, RazorSemanticTokensLegend.MarkupElement, 0,
                0, 1, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 1, RazorSemanticTokensLegend.MarkupElement, 0,
                0, 1, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
            };

            AssertSemanticTokens(txt, expectedData, isRazor: true, out var _);
        }

        [Fact]
        public void GetSemanticTokens_Razor_InRange()
        {
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test1></test1> ";
            var expectedData = new List<int> {
                1, 1, 5, RazorSemanticTokensLegend.RazorTagHelperElement, 0, //line, character pos, length, tokenType, modifier
            };

            var startIndex = txt.IndexOf("test1", StringComparison.Ordinal);
            var endIndex = startIndex + 5;

            var codeDocument = CreateCodeDocument(txt, DefaultTagHelpers);

            codeDocument.GetSourceText().GetLineAndOffset(startIndex, out var startLine, out var startChar);
            codeDocument.GetSourceText().GetLineAndOffset(endIndex, out var endLine, out var endChar);

            var startPosition = new Position(startLine, startChar);
            var endPosition = new Position(endLine, endChar);
            var location = new OmniSharpRange(startPosition, endPosition);

            AssertSemanticTokens(txt, expectedData, isRazor: false, out var _, location: location);
        }
        #endregion DirectiveAttributes

        #region Directive
        [Fact]
        public void GetSemanticTokens_Razor_CodeDirective()
        {
            var txt = $"@code {{}}";
            var expectedData = new List<int>
            {
                0, 0, 1, RazorSemanticTokensLegend.RazorTransition, 0,
                0, 1, 4, RazorSemanticTokensLegend.RazorDirective, 0
            }.ToImmutableArray();

            AssertSemanticTokens(txt, expectedData, isRazor: true, out var _);
        }

        [Fact]
        public void GetSemanticTokens_Razor_UsingDirective()
        {
            var txt = $"@using Microsoft.AspNetCore.Razor";
            var expectedData = new List<int>
            {
                0, 0, 1, RazorSemanticTokensLegend.RazorTransition, 0,
            }.ToImmutableArray();

            AssertSemanticTokens(txt, expectedData, isRazor: true, out var _);
        }

        [Fact]
        public void GetSemanticTokens_Razor_FunctionsDirective()
        {
            var txt = $"@functions {{}}";
            var expectedData = new List<int>
            {
                0, 0, 1, RazorSemanticTokensLegend.RazorTransition, 0,
                0, 1, 9, RazorSemanticTokensLegend.RazorDirective, 0
            }.ToImmutableArray();

            AssertSemanticTokens(txt, expectedData, isRazor: true, out var _);
        }
        #endregion

        [Fact]
        public void GetSemanticTokens_Razor_Comment()
        {
            var txt = $"@* A comment *@";
            var expectedData = new List<int>
            {
                0, 0, 1, RazorSemanticTokensLegend.RazorCommentTransition, 0,
                0, 1, 1, RazorSemanticTokensLegend.RazorCommentStar, 0,
                0, 1, 11, RazorSemanticTokensLegend.RazorComment, 0,
                0, 11, 1, RazorSemanticTokensLegend.RazorCommentStar, 0,
                0, 1, 1, RazorSemanticTokensLegend.RazorCommentTransition, 0,
            };

            AssertSemanticTokens(txt, expectedData, isRazor: true, out var _);
        }

        [Fact]
        public void GetSemanticTokens_Razor_NoDifference()
        {
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test1></test1> ";
            var expectedData = new List<int> {
                0, 0, 1, RazorSemanticTokensLegend.RazorTransition, 0, //line, character pos, length, tokenType, modifier
                0, 1, 12, RazorSemanticTokensLegend.RazorDirective, 0,
                1, 0, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 5, RazorSemanticTokensLegend.RazorTagHelperElement, 0,
                0, 5, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 5, RazorSemanticTokensLegend.RazorTagHelperElement, 0,
                0, 5, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
            };

            var previousResultId = AssertSemanticTokens(txt, expectedData, isRazor: false, out var service);

            var newResultId = AssertSemanticTokenEdits(txt, new SemanticTokensDelta
            {
                Edits = new List<SemanticTokensEdit>()
            }, isRazor: false, previousResultId: previousResultId, out var _, service: service);
            Assert.NotEqual(previousResultId, newResultId);
        }

        [Fact]
        public void GetSemanticTokens_Razor_RemoveTokens()
        {
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test1></test1><test1></test1><test1></test1> ";
            var expectedData = new List<int> {
                0, 0, 1, RazorSemanticTokensLegend.RazorTransition, 0, //line, character pos, length, tokenType, modifier
                0, 1, 12, RazorSemanticTokensLegend.RazorDirective, 0,

                1, 0, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 5, RazorSemanticTokensLegend.RazorTagHelperElement, 0,
                0, 5, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 5, RazorSemanticTokensLegend.RazorTagHelperElement, 0,
                0, 5, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,

                0, 1, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 5, RazorSemanticTokensLegend.RazorTagHelperElement, 0,
                0, 5, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 5, RazorSemanticTokensLegend.RazorTagHelperElement, 0,
                0, 5, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,

                0, 1, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 5, RazorSemanticTokensLegend.RazorTagHelperElement, 0,
                0, 5, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 5, RazorSemanticTokensLegend.RazorTagHelperElement, 0,
                0, 5, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
            };

            var previousResultId = AssertSemanticTokens(txt, expectedData, isRazor: false, out var service);

            var newTxt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test1></test1> ";
            var newResultId = AssertSemanticTokenEdits(newTxt, new SemanticTokensDelta
            {
                Edits = new List<SemanticTokensEdit>(){
                    new SemanticTokensEdit
                    {
                        Data = Array.Empty<int>().ToImmutableArray(),
                        DeleteCount = 70,
                        Start = 45
                    }
            }
            }, isRazor: false, previousResultId: previousResultId, out var _, service);
            Assert.NotEqual(previousResultId, newResultId);
        }

        [Fact]
        public void GetSemanticTokens_Razor_OnlyDifferences_Append()
        {
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test1></test1> ";
            var expectedData = new List<int> {
                0, 0, 1, RazorSemanticTokensLegend.RazorTransition, 0, //line, character pos, length, tokenType, modifier
                0, 1, 12, RazorSemanticTokensLegend.RazorDirective, 0,
                1, 0, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 5, RazorSemanticTokensLegend.RazorTagHelperElement, 0,
                0, 5, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 5, RazorSemanticTokensLegend.RazorTagHelperElement, 0,
                0, 5, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
            };

            var previousResultId = AssertSemanticTokens(txt, expectedData, isRazor: false, out var service);

            var newTxt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test1 bool-val='true'></test1> ";
            var newExpectedData = new SemanticTokensDelta
            {
                Edits = new SemanticTokensEdit[] {
                    new SemanticTokensEdit
                    {
                        Start = 21,
                        Data = new List<int>{ 6, 8, 1, 0, 0, 8, 1, 11, 0, 0, 7, }.ToImmutableArray(),
                        DeleteCount = 1,
                    },
                }
            };
            var newResultId = AssertSemanticTokenEdits(newTxt, newExpectedData, isRazor: false, previousResultId: previousResultId, out var _, service);
            Assert.NotEqual(previousResultId, newResultId);
        }

        [Fact]
        public void GetSemanticTokens_Razor_CoalesceDeleteAndAdd()
        {
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test1 /> ";
            var expectedData = new List<int>
            {
                0, 0, 1, RazorSemanticTokensLegend.RazorTransition, 0, //line, character pos, length, tokenType, modifier
                0, 1, 12, RazorSemanticTokensLegend.RazorDirective, 0,
                1, 0, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 5, RazorSemanticTokensLegend.RazorTagHelperElement, 0,
                0, 6, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
            };

            var previousResultId = AssertSemanticTokens(txt, expectedData, isRazor: false, out var service);

            var newTxt = $"@addTagHelper *, TestAssembly{Environment.NewLine}{Environment.NewLine}<p @minimized /> ";
            var newExpectedData = new SemanticTokensDelta
            {
                Edits = new SemanticTokensEdit[] {
                    new SemanticTokensEdit
                    {
                        Start = 10,
                        DeleteCount = 0,
                        Data = new List<int>{
                           2, 0
                        }.ToImmutableArray(),
                    },
                    new SemanticTokensEdit
                    {
                        Start = 11,
                        DeleteCount = 0,
                        Data = new List<int>
                        {
                            9, 0,
                        }.ToImmutableArray(),
                    },
                    new SemanticTokensEdit
                    {
                        Start = 13,
                        DeleteCount = 1,
                        Data = new List<int>
                        {
                            1, 10
                        }.ToImmutableArray()
                    },
                    new SemanticTokensEdit
                    {
                        Start = 16,
                        DeleteCount = 0,
                        Data = new List<int>
                        {
                            2
                        }.ToImmutableArray()
                    },
                    new SemanticTokensEdit
                    {
                        Start = 17,
                        DeleteCount = 1,
                        Data = new List<int>
                        {
                            2
                        }.ToImmutableArray()
                    },
                    new SemanticTokensEdit
                    {
                        Start = 20,
                        DeleteCount = 0,
                        Data = new List<int>
                        {
                            1, 9, 4, 0
                        }.ToImmutableArray(),
                    },
                    new SemanticTokensEdit
                    {
                        Start = 21,
                        DeleteCount = 1,
                        Data = new List<int>
                        {
                            10
                        }.ToImmutableArray(),
                    }
                }
            };

            var newResultId = AssertSemanticTokenEdits(newTxt, newExpectedData, isRazor: true, previousResultId: previousResultId, out var _, service);
            Assert.NotEqual(previousResultId, newResultId);
        }

        [Fact]
        public void GetSemanticTokens_Razor_OriginallyNone_ThenSome()
        {
            var expectedData = new List<int> {
                0, 0, 1, RazorSemanticTokensLegend.RazorTransition, 0, //line, character pos, length, tokenType, modifier
                0, 1, 12, RazorSemanticTokensLegend.RazorDirective, 0,
            };
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}";

            var previousResultId = AssertSemanticTokens(txt, expectedData, isRazor: false, out var service);

            var newTxt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test1></test1> ";
            var newExpectedData = new SemanticTokensDelta
            {
                Edits = new List<SemanticTokensEdit> {
                    new SemanticTokensEdit
                    {
                        Start = 10,
                        Data = new int[]{
                            1, 0, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                            0, 1, 5, RazorSemanticTokensLegend.RazorTagHelperElement, 0,
                            0, 5, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                            0, 1, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                            0, 1, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                            0, 1, 5, RazorSemanticTokensLegend.RazorTagHelperElement, 0,
                            0, 5, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                        }.ToImmutableArray(),
                        DeleteCount = 0,
                    }
                }
            };

            var newResultId = AssertSemanticTokenEdits(newTxt, newExpectedData, isRazor: false, previousResultId: previousResultId, out var _, service);
            Assert.NotEqual(previousResultId, newResultId);
        }

        [Fact]
        public void GetSemanticTokens_Razor_GetEditsWithNoPrevious()
        {
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test1></test1> ";
            var expectedEdits = new SemanticTokens
            {
                Data = new int[] {
                    0, 0, 1, RazorSemanticTokensLegend.RazorTransition, 0, //line, character pos, length, tokenType, modifier
                    0, 1, 12, RazorSemanticTokensLegend.RazorDirective, 0,
                    1, 0, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                    0, 1, 5, RazorSemanticTokensLegend.RazorTagHelperElement, 0,
                    0, 5, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                    0, 1, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                    0, 1, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                    0, 1, 5, RazorSemanticTokensLegend.RazorTagHelperElement, 0,
                    0, 5, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                }.ToImmutableArray(),
            };
            var previousResultId = AssertSemanticTokenEdits(txt, expectedEdits, isRazor: false, previousResultId: null, out _);
            Assert.NotNull(previousResultId);
        }

        [Fact]
        public void GetSemanticTokens_Razor_SomeTagHelpers_ThenNone()
        {
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test1></test1> ";
            var expectedData = new int[]{
                0, 0, 1, RazorSemanticTokensLegend.RazorTransition, 0, //line, character pos, length, tokenType, modifier
                0, 1, 12, RazorSemanticTokensLegend.RazorDirective, 0,
                1, 0, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 5, RazorSemanticTokensLegend.RazorTagHelperElement, 0,
                0, 5, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 5, RazorSemanticTokensLegend.RazorTagHelperElement, 0,
                0, 5, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
            };

            var previousResultId = AssertSemanticTokens(txt, expectedData, isRazor: false, out var service);

            var newTxt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<p></p> ";
            var newExpectedData = new SemanticTokensDelta
            {
                Edits = new List<SemanticTokensEdit>
                {
                    new SemanticTokensEdit
                    {
                        Start = 17,
                        Data = new int[]{ 1, 10 }.ToImmutableArray(),
                        DeleteCount = 1,
                    },
                    new SemanticTokensEdit
                    {
                        Start = 20,
                        Data = new int[]{ 1 }.ToImmutableArray(),
                        DeleteCount = 2,
                    },
                    new SemanticTokensEdit
                    {
                        Start = 37,
                        Data = new int[]{ 1, 10 }.ToImmutableArray(),
                        DeleteCount = 1,
                    },
                    new SemanticTokensEdit
                    {
                        Start = 40,
                        Data = new int[]{ 1 }.ToImmutableArray(),
                        DeleteCount = 2,
                    }
                }
            };

            var newResultId = AssertSemanticTokenEdits(newTxt, newExpectedData, isRazor: false, previousResultId: previousResultId, out var _, service);
            Assert.NotEqual(previousResultId, newResultId);
        }

        [Fact]
        public void GetSemanticTokens_Razor_OnlyDifferences_Internal()
        {
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test1></test1> ";
            var expectedData = new List<int> {
                0, 0, 1, RazorSemanticTokensLegend.RazorTransition, 0, //line, character pos, length, tokenType, modifier
                0, 1, 12, RazorSemanticTokensLegend.RazorDirective, 0,
                1, 0, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 5, RazorSemanticTokensLegend.RazorTagHelperElement, 0,
                0, 5, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 5, RazorSemanticTokensLegend.RazorTagHelperElement, 0,
                0, 5, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
            };

            var previousResultId = AssertSemanticTokens(txt, expectedData, isRazor: false, out var service);

            var newTxt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test1></test1><test1></test1> ";
            var newExpectedData = new SemanticTokensDelta
            {
                Edits = new List<SemanticTokensEdit> {
                    new SemanticTokensEdit
                    {
                        Start = 45,
                        Data = new int[]{
                            0, 1, 1, 9, 0,
                            0, 1, 5, 0, 0,
                            0, 5, 1, 9, 0,
                            0, 1, 1, 9, 0,
                            0, 1, 1, 9, 0,
                            0, 1, 5, 0, 0,
                            0, 5, 1, 9, 0,
                        }.ToImmutableArray(),
                        DeleteCount = 0,
                    }
                }
            };
            var newResultId = AssertSemanticTokenEdits(newTxt, newExpectedData, isRazor: false, previousResultId: previousResultId, out var _, service);
            Assert.NotEqual(previousResultId, newResultId);
        }

        [Fact]
        public void GetSemanticTokens_Razor_Modify()
        {
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}" +
                $"<test1 bool-val=\"true\" />{Environment.NewLine}" +
                $"<test1 bool-val=\"true\" />{Environment.NewLine}" +
                $"<test1 bool-val=\"true\" />{Environment.NewLine}";
            var expectedData = new List<int> {
                0, 0, 1, RazorSemanticTokensLegend.RazorTransition, 0, //line, character pos, length, tokenType, modifier
                0, 1, 12, RazorSemanticTokensLegend.RazorDirective, 0,

                1, 0, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 5, RazorSemanticTokensLegend.RazorTagHelperElement, 0,
                0, 6, 8, RazorSemanticTokensLegend.RazorTagHelperAttribute, 0,
                0, 8, 1, RazorSemanticTokensLegend.MarkupOperator, 0,
                0, 8, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,

                1, 0, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 5, RazorSemanticTokensLegend.RazorTagHelperElement, 0,
                0, 6, 8, RazorSemanticTokensLegend.RazorTagHelperAttribute, 0,
                0, 8, 1, RazorSemanticTokensLegend.MarkupOperator, 0,
                0, 8, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,

                1, 0, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 5, RazorSemanticTokensLegend.RazorTagHelperElement, 0,
                0, 6, 8, RazorSemanticTokensLegend.RazorTagHelperAttribute, 0,
                0, 8, 1, RazorSemanticTokensLegend.MarkupOperator, 0,
                0, 8, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
            };

            var previousResultId = AssertSemanticTokens(txt, expectedData, isRazor: false, out var service);

            var newTxt = $"@addTagHelper *, TestAssembly{Environment.NewLine}" +
                $"<test1 bool-va=\"true\" />{Environment.NewLine}" +
                $"<test1 bool-val=\"true\" />{Environment.NewLine}" +
                $"<test1 bool-val=\"true\" />{Environment.NewLine}";
            var newExpectedData = new SemanticTokensDelta
            {
                Edits = new List<SemanticTokensEdit>
                {
                    new SemanticTokensEdit
                    {
                        Start = 22,
                        Data = new int[]{ 7, 12 }.ToImmutableArray(),
                        DeleteCount = 2,
                    },
                    new SemanticTokensEdit
                    {
                        Start = 26,
                        Data = new int[]{ 7 }.ToImmutableArray(),
                        DeleteCount = 1,
                    }
                }
            };
            var newResultId = AssertSemanticTokenEdits(newTxt, newExpectedData, isRazor: false, previousResultId: previousResultId, out _, service);
            Assert.NotEqual(previousResultId, newResultId);
        }

        [Fact]
        public void GetSemanticTokens_Razor_OnlyDifferences_NewLines()
        {
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test1></test1> ";
            var expectedData = new List<int> {
                0, 0, 1, RazorSemanticTokensLegend.RazorTransition, 0, //line, character pos, length, tokenType, modifier
                0, 1, 12, RazorSemanticTokensLegend.RazorDirective, 0,
                1, 0, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 5, RazorSemanticTokensLegend.RazorTagHelperElement, 0,
                0, 5, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 5, RazorSemanticTokensLegend.RazorTagHelperElement, 0,
                0, 5, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
            };

            var previousResultId = AssertSemanticTokens(txt, expectedData, isRazor: false, out var service);

            var newTxt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test1></test1>{Environment.NewLine}" +
                $"<test1></test1> ";
            var newExpectedData = new SemanticTokensDelta
            {
                Edits = new List<SemanticTokensEdit> {
                    new SemanticTokensEdit
                    {
                        Start = 45,
                        Data = new int[]{
                            1, 0, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                            0, 1, 5, RazorSemanticTokensLegend.RazorTagHelperElement, 0,
                            0, 5, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                            0, 1, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                            0, 1, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                            0, 1, 5, RazorSemanticTokensLegend.RazorTagHelperElement, 0,
                            0, 5, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                        }.ToImmutableArray(),
                        DeleteCount = 0,
                    }
                }
            };
            var newResultId = AssertSemanticTokenEdits(newTxt, newExpectedData, isRazor: false, previousResultId: previousResultId, out _, service);
            Assert.NotEqual(previousResultId, newResultId);
        }

        private string AssertSemanticTokens(string txt, IEnumerable<int> expectedData, bool isRazor, out RazorSemanticTokensInfoService outService, RazorSemanticTokensInfoService service = null, OmniSharpRange location = null)
        {
            // Arrange
            if (service is null)
            {
                service = GetDefaultRazorSemanticTokenInfoService();
            }
            outService = service;

            RazorCodeDocument codeDocument;
            if (isRazor)
            {
                codeDocument = CreateRazorDocument(txt, DefaultTagHelpers);
            }
            else
            {
                codeDocument = CreateCodeDocument(txt, DefaultTagHelpers);
            }

            // Act
            var tokens = service.GetSemanticTokens(codeDocument, location);

            // Assert
            Assert.True(ArrayEqual(expectedData, tokens.Data));

            return tokens.ResultId;
        }

        private bool ArrayEqual(IEnumerable<int> expectedData, IEnumerable<int> actualData)
        {
            var expectedArray = expectedData.ToArray();
            var actualArray = actualData.ToArray();
            for(var i = 0; i < Math.Min(expectedData.Count(), actualData.Count()); i++)
            {
                if(expectedArray[i] != actualArray[i])
                {
                    Assert.True(false, $"expected: {expectedArray[i]}, actual: {actualArray[i]} i: {i}");
                    return false;
                }
            }

            Assert.Equal(expectedArray.Length, actualArray.Length);

            return true;
        }

        private string AssertSemanticTokenEdits(string txt, SemanticTokensFullOrDelta expectedEdits, bool isRazor, string previousResultId, out RazorSemanticTokensInfoService outService, RazorSemanticTokensInfoService service = null)
        {
            // Arrange
            if (service is null)
            {
                service = GetDefaultRazorSemanticTokenInfoService();
            }
            outService = service;

            RazorCodeDocument codeDocument;
            if (isRazor)
            {
                codeDocument = CreateRazorDocument(txt, DefaultTagHelpers);
            }
            else
            {
                codeDocument = CreateCodeDocument(txt, DefaultTagHelpers);
            }

            // Act
            var edits = service.GetSemanticTokensEdits(codeDocument, previousResultId);

            // Assert
            if (expectedEdits.IsDelta)
            {
                for (var i = 0; i < Math.Min(expectedEdits.Delta.Edits.Count(), edits.Delta.Edits.Count()); i++)
                {
                    Assert.Equal(expectedEdits.Delta.Edits.ElementAt(i), edits.Delta.Edits.ElementAt(i), SemanticEditComparer.Instance);
                }
                Assert.Equal(expectedEdits.Delta.Edits.Count(), edits.Delta.Edits.Count());

                return edits.Delta.ResultId;
            }
            else
            {
                Assert.Equal(expectedEdits.Full.Data, edits.Full.Data, ImmutableArrayIntComparer.Instance);

                return edits.Full.ResultId;
            }
        }

        private RazorSemanticTokensInfoService GetDefaultRazorSemanticTokenInfoService()
        {
            return new DefaultRazorSemanticTokensInfoService();
        }

        private class SemanticEditComparer : IEqualityComparer<SemanticTokensEdit>
        {
            public static SemanticEditComparer Instance = new SemanticEditComparer();

            public bool Equals([AllowNull] SemanticTokensEdit x, [AllowNull] SemanticTokensEdit y)
            {
                if (x == null && y == null)
                {
                    return true;
                }
                else if (x is null || y is null)
                {
                    return false;
                }

                Assert.Equal(x.DeleteCount, y.DeleteCount);
                Assert.Equal(x.Start, y.Start);
                Assert.Equal(x.Data, y.Data, ImmutableArrayIntComparer.Instance);

                return x.DeleteCount == y.DeleteCount &&
                    x.Start == y.Start;
            }

            public int GetHashCode([DisallowNull] SemanticTokensEdit obj)
            {
                throw new NotImplementedException();
            }
        }

        private class ImmutableArrayIntComparer : IEqualityComparer<ImmutableArray<int>>
        {
            public static ImmutableArrayIntComparer Instance = new ImmutableArrayIntComparer();

            public bool Equals([AllowNull] ImmutableArray<int> x, [AllowNull] ImmutableArray<int> y)
            {
                for (var i = 0; i < Math.Min(x.Length, y.Length); i++)
                {
                    Assert.True(x[i] == y[i], $"x {x[i]} y {y[i]} i {i}");
                }
                Assert.Equal(x.Length, y.Length);

                return true;
            }

            public int GetHashCode([DisallowNull] ImmutableArray<int> obj)
            {
                throw new NotImplementedException();
            }
        }
    }
}
