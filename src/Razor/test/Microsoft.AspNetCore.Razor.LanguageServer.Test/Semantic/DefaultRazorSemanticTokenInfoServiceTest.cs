// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
#pragma warning disable CS0618
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.AspNetCore.Razor.LanguageServer.Completion;
using Microsoft.AspNetCore.Razor.LanguageServer.Semantic;
using Microsoft.AspNetCore.Razor.LanguageServer.Semantic.Models;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.Extensions.Logging;
using Moq;
using OmniSharp.Extensions.JsonRpc;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Models.Proposals;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using Xunit;
using OmniSharpRange = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Test.Semantic
{
    public class DefaultRazorSemanticTokenInfoServiceTest : DefaultTagHelperServiceTestBase
    {
        #region CSharp
        [Fact]
        public async Task GetSemanticTokens_CSharp_FunctionAsync()
        {
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}@{{ var d = }}";
            var expectedData = new List<int> {
                0, 0, 1, RazorSemanticTokensLegend.RazorTransition, 0, //line, character pos, length, tokenType, modifier
                0, 1, 12, RazorSemanticTokensLegend.RazorDirective, 0,
                1, 3, 3, RazorSemanticTokensLegend.CSharpKeyword, 0,
                0, 4, 1, RazorSemanticTokensLegend.CSharpVariable, 0,
                0, 2, 1, RazorSemanticTokensLegend.CSharpOperator, 0,
            };

            var cSharpTokens = new SemanticTokens
            {
                Data = new int[] {
                    14, 12, 3, RazorSemanticTokensLegend.CSharpKeyword, 0,
                    13, 15, 1, RazorSemanticTokensLegend.CSharpVariable, 0,
                    12, 25, 1, RazorSemanticTokensLegend.CSharpOperator, 0,
                    11, 10, 25, RazorSemanticTokensLegend.CSharpKeyword, 0, // No mapping
                }.ToImmutableArray(),
                ResultId = "35",
            };

            var mappings = new (OmniSharpRange, OmniSharpRange)[] {
               (new OmniSharpRange(new Position(14, 12), new Position(14, 15)), new OmniSharpRange(new Position(1, 3), new Position(1, 6))),
               (new OmniSharpRange(new Position(27, 15), new Position(27, 16)), new OmniSharpRange(new Position(1, 7), new Position(1, 8))),
               (new OmniSharpRange(new Position(39, 25), new Position(39, 26)), new OmniSharpRange(new Position(1, 9), new Position(1, 10))),
               (new OmniSharpRange(new Position(50, 10), new Position(50, 35)), null)
            };

            await AssertSemanticTokens(txt, expectedData, isRazor: false, cSharpTokens: cSharpTokens, documentMappings: mappings);
        }

        [Fact]
        public async Task GetSemanticTokens_CSharp_StaticModifierAsync()
        {
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}@{{ var d = }}";
            var expectedData = new List<int> {
                0, 0, 1, RazorSemanticTokensLegend.RazorTransition, 0, //line, character pos, length, tokenType, modifier
                0, 1, 12, RazorSemanticTokensLegend.RazorDirective, 0,
                1, 3, 3, RazorSemanticTokensLegend.CSharpKeyword, 0,
                0, 4, 1, RazorSemanticTokensLegend.CSharpVariable, 1,
                0, 2, 1, RazorSemanticTokensLegend.CSharpOperator, 0,
            };

            var cSharpTokens = new SemanticTokens
            {
                Data = new int[] {
                    14, 12, 3, RazorSemanticTokensLegend.CSharpKeyword, 0,
                    13, 15, 1, RazorSemanticTokensLegend.CSharpVariable, 1,
                    12, 25, 1, RazorSemanticTokensLegend.CSharpOperator, 0,
                    11, 10, 25, RazorSemanticTokensLegend.CSharpKeyword, 0, // No mapping
                }.ToImmutableArray(),
                ResultId = "35",
            };

            var mappings = new (OmniSharpRange, OmniSharpRange)[] {
               (new OmniSharpRange(new Position(14, 12), new Position(14, 15)), new OmniSharpRange(new Position(1, 3), new Position(1, 6))),
               (new OmniSharpRange(new Position(27, 15), new Position(27, 16)), new OmniSharpRange(new Position(1, 7), new Position(1, 8))),
               (new OmniSharpRange(new Position(39, 25), new Position(39, 26)), new OmniSharpRange(new Position(1, 9), new Position(1, 10))),
               (new OmniSharpRange(new Position(50, 10), new Position(50, 35)), null)
            };

            await AssertSemanticTokens(txt, expectedData, isRazor: false, cSharpTokens: cSharpTokens, documentMappings: mappings);
        }

        [Fact]
        public async Task GetSemanticTokens_CSharp_UsesCache()
        {
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}@{{ var d = }}";
            var expectedData = new List<int> {
                0, 0, 1, RazorSemanticTokensLegend.RazorTransition, 0, //line, character pos, length, tokenType, modifier
                0, 1, 12, RazorSemanticTokensLegend.RazorDirective, 0,
                1, 3, 3, RazorSemanticTokensLegend.CSharpKeyword, 0,
                0, 4, 1, RazorSemanticTokensLegend.CSharpVariable, 0,
                0, 2, 1, RazorSemanticTokensLegend.CSharpOperator, 0,
            };

            var cSharpTokens = new SemanticTokens
            {
                Data = new int[] {
                    14, 12, 3, RazorSemanticTokensLegend.CSharpKeyword, 0,
                    13, 15, 1, RazorSemanticTokensLegend.CSharpVariable, 0,
                    12, 25, 1, RazorSemanticTokensLegend.CSharpOperator, 0,
                    11, 10, 25, RazorSemanticTokensLegend.CSharpKeyword, 0, // No mapping
                }.ToImmutableArray(),
                ResultId = "35",
            };

            var mappings = new (OmniSharpRange, OmniSharpRange)[] {
               (new OmniSharpRange(new Position(14, 12), new Position(14, 15)), new OmniSharpRange(new Position(1, 3), new Position(1, 6))),
               (new OmniSharpRange(new Position(27, 15), new Position(27, 16)), new OmniSharpRange(new Position(1, 7), new Position(1, 8))),
               (new OmniSharpRange(new Position(39, 25), new Position(39, 26)), new OmniSharpRange(new Position(1, 9), new Position(1, 10))),
               (new OmniSharpRange(new Position(50, 10), new Position(50, 35)), null)
            };

            var isRazor = false;
            var (previousResultId, service, mockClient, document) = await AssertSemanticTokens(txt, expectedData, isRazor, cSharpTokens: cSharpTokens, documentMappings: mappings);
            var expectedDelta = new SemanticTokensFullOrDelta(new SemanticTokensDelta
            {
                Edits = new Container<SemanticTokensEdit>(),
                ResultId = previousResultId
            });

            await AssertSemanticTokenEdits(txt, expectedDelta, isRazor, previousResultId: previousResultId, document, service: service);
            mockClient.Verify(l => l.SendRequestAsync(LanguageServerConstants.RazorProvideSemanticTokensEndpoint, It.IsAny<SemanticTokensParams>()), Times.Once());
        }
        #endregion

        #region HTML
        [Fact]
        public async Task GetSemanticTokens_IncompleteTag()
        {
            var txt = "<str";
            var expectedData = new List<int>
            {
                0, 0, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
                0, 1, 3, RazorSemanticTokensLegend.MarkupElement, 0,
            };

            await AssertSemanticTokens(txt, expectedData, isRazor: false);
        }

        [Fact]
        public async Task GetSemanticTokens_MinimizedHTMLAttribute()
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

            await AssertSemanticTokens(txt, expectedData, isRazor: false);
        }

        [Fact]
        public async Task GetSemanticTokens_MinimizedHTMLAsync()
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

            await AssertSemanticTokens(txt, expectedData, isRazor: false);
        }

        [Fact]
        public async Task GetSemanticTokens_HTMLCommentAsync()
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

            await AssertSemanticTokens(txt, expectedData, isRazor: false);
        }

        [Fact]
        public async Task GetSemanticTokens_PartialHTMLCommentAsync()
        {
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<!-- comment";
            var expectedData = new List<int>
            {
                0, 0, 1, RazorSemanticTokensLegend.RazorTransition, 0, //line, character pos, length, tokenType, modifier
                0, 1, 12, RazorSemanticTokensLegend.RazorDirective, 0,
            };

            await AssertSemanticTokens(txt, expectedData, isRazor: true);
        }
        #endregion

        #region TagHelpers
        [Fact]
        public async Task GetSemanticTokens_HalfOfCommentAsync()
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

            await AssertSemanticTokens(txt, expectedData, isRazor: true);
        }

        [Fact]
        public async Task GetSemanticTokens_NoAttributesAsync()
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

            await AssertSemanticTokens(txt, expectedData, isRazor: false);
        }

        [Fact]
        public async Task GetSemanticTokens_WithAttributeAsync()
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

            await AssertSemanticTokens(txt, expectedData, isRazor: false);
        }

        [Fact]
        public async Task GetSemanticTokens_MinimizedAttributeAsync()
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

            await AssertSemanticTokens(txt, expectedData, isRazor: false);
        }

        [Fact]
        public async Task GetSemanticTokens_IgnoresNonTagHelperAttributesAsync()
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

            await AssertSemanticTokens(txt, expectedData, isRazor: false);
        }

        [Fact]
        public async Task GetSemanticTokens_TagHelpersNotAvailableInRazorAsync()
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

            await AssertSemanticTokens(txt, expectedData, isRazor: true);
        }

        [Fact]
        public async Task GetSemanticTokens_DoesNotApplyOnNonTagHelpersAsync()
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

            await AssertSemanticTokens(txt, expectedData, isRazor: false);
        }
        #endregion TagHelpers

        #region DirectiveAttributes
        [Fact]
        public async Task GetSemanticTokens_Razor_MinimizedDirectiveAttributeParameters()
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

            await AssertSemanticTokens(txt, expectedData, isRazor: true);
        }

        [Fact]
        public async Task GetSemanticTokens_Razor_DirectiveAttributesParametersAsync()
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

            await AssertSemanticTokens(txt, expectedData, isRazor: true);
        }

        [Fact]
        public async Task GetSemanticTokens_Razor_NonComponentsDoNotShowInRazorAsync()
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

            await AssertSemanticTokens(txt, expectedData, isRazor: true);
        }

        [Fact]
        public async Task GetSemanticTokens_Razor_DirectivesAsync()
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

            await AssertSemanticTokens(txt, expectedData, isRazor: true);
        }

        [Fact]
        public async Task GetSemanticTokens_Razor_DoNotColorNonTagHelpersAsync()
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

            await AssertSemanticTokens(txt, expectedData, isRazor: true);
        }

        [Fact]
        public async Task GetSemanticTokens_Razor_DoesNotApplyOnNonTagHelpersAsync()
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

            await AssertSemanticTokens(txt, expectedData, isRazor: true);
        }

        [Fact]
        public async Task GetSemanticTokens_Razor_InRangeAsync()
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

            await AssertSemanticTokens(txt, expectedData, isRazor: false, location: location);
        }
        #endregion DirectiveAttributes

        #region Directive
        [Fact]
        public async Task GetSemanticTokens_Razor_CodeDirectiveAsync()
        {
            var txt = $"@code {{}}";
            var expectedData = new List<int>
            {
                0, 0, 1, RazorSemanticTokensLegend.RazorTransition, 0,
                0, 1, 4, RazorSemanticTokensLegend.RazorDirective, 0
            }.ToImmutableArray();

            await AssertSemanticTokens(txt, expectedData, isRazor: true);
        }

        [Fact]
        public async Task GetSemanticTokens_Razor_UsingDirective()
        {
            var txt = $"@using Microsoft.AspNetCore.Razor";
            var expectedData = new List<int>
            {
                0, 0, 1, RazorSemanticTokensLegend.RazorTransition, 0,
            }.ToImmutableArray();

            await AssertSemanticTokens(txt, expectedData, isRazor: true);
        }

        [Fact]
        public async Task GetSemanticTokens_Razor_FunctionsDirectiveAsync()
        {
            var txt = $"@functions {{}}";
            var expectedData = new List<int>
            {
                0, 0, 1, RazorSemanticTokensLegend.RazorTransition, 0,
                0, 1, 9, RazorSemanticTokensLegend.RazorDirective, 0
            }.ToImmutableArray();

            await AssertSemanticTokens(txt, expectedData, isRazor: true);
        }
        #endregion

        [Fact]
        public async Task GetSemanticTokens_Razor_CommentAsync()
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

            await AssertSemanticTokens(txt, expectedData, isRazor: true);
        }

        [Fact]
        public async Task GetSemanticTokens_Razor_NoDifferenceAsync()
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

            var isRazor = false;
            var (previousResultId, service, _, document) = await AssertSemanticTokens(txt, expectedData, isRazor);

            var (newResultId, _, _) = await AssertSemanticTokenEdits(txt, new SemanticTokensDelta
            {
                Edits = new List<SemanticTokensEdit>()
            }, isRazor, previousResultId: previousResultId, document, service: service);
            Assert.Equal(previousResultId, newResultId);
        }

        [Fact]
        public async Task GetSemanticTokens_Razor_RemoveTokensAsync()
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

            var isRazor = false;
            var (previousResultId, service, _, _) = await AssertSemanticTokens(txt, expectedData, isRazor);

            var newTxt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test1></test1> ";
            var (newResultId, _, _) = await AssertSemanticTokenEdits(newTxt, new SemanticTokensDelta
            {
                Edits = new List<SemanticTokensEdit>(){
                    new SemanticTokensEdit
                    {
                        Data = Array.Empty<int>().ToImmutableArray(),
                        DeleteCount = 70,
                        Start = 45
                    }
            }
            }, isRazor, previousResultId: previousResultId, documentSnapshot: null, service);
            Assert.NotEqual(previousResultId, newResultId);
        }

        [Fact]
        public async Task GetSemanticTokens_Razor_OnlyDifferences_AppendAsync()
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

            var isRazor = false;
            var (previousResultId, service, _, _) = await AssertSemanticTokens(txt, expectedData, isRazor);

            var newTxt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test1 bool-val='true'></test1> ";
            var newExpectedData = new SemanticTokensDelta
            {
                Edits = new SemanticTokensEdit[] {
                    new SemanticTokensEdit
                    {
                        Start = 21,
                        Data = new List<int>{
                            6, 8, RazorSemanticTokensLegend.RazorTagHelperAttribute, 0,
                            0, 8, 1, RazorSemanticTokensLegend.MarkupOperator, 0,
                            0, 7,
                        }.ToImmutableArray(),
                        DeleteCount = 1,
                    },
                }
            };
            var (newResultId, _, _) = await AssertSemanticTokenEdits(newTxt, newExpectedData, isRazor, previousResultId: previousResultId, service: service);
            Assert.NotEqual(previousResultId, newResultId);
        }

        [Fact]
        public async Task GetSemanticTokens_Razor_CoalesceDeleteAndAddAsync()
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

            var isRazor = false;
            var (previousResultId, service, _, _) = await AssertSemanticTokens(txt, expectedData, isRazor);

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
                            RazorSemanticTokensLegend.MarkupTagDelimiter,
                        }.ToImmutableArray(),
                    },
                    new SemanticTokensEdit
                    {
                        Start = 12,
                        DeleteCount = 0,
                        Data = new List<int>
                        {
                            0, 1
                        }.ToImmutableArray()
                    },
                    new SemanticTokensEdit
                    {
                        Start = 13,
                        DeleteCount = 1,
                        Data = new List<int>
                        {
                            RazorSemanticTokensLegend.MarkupElement, 0, 0, 2, 1, RazorSemanticTokensLegend.RazorTransition
                        }.ToImmutableArray()
                    },
                    new SemanticTokensEdit
                    {
                        Start = 17,
                        DeleteCount = 2,
                        Data = new List<int>
                        {
                            9, RazorSemanticTokensLegend.RazorDirectiveAttribute
                        }.ToImmutableArray()
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

            var (newResultId, _, _) = await AssertSemanticTokenEdits(newTxt, newExpectedData, isRazor: true, previousResultId: previousResultId, service: service);
            Assert.NotEqual(previousResultId, newResultId);
        }

        [Fact]
        public async Task GetSemanticTokens_Razor_OriginallyNone_ThenSomeAsync()
        {
            var expectedData = new List<int> {
                0, 0, 1, RazorSemanticTokensLegend.RazorTransition, 0, //line, character pos, length, tokenType, modifier
                0, 1, 12, RazorSemanticTokensLegend.RazorDirective, 0,
            };
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}";

            var isRazor = false;
            var (previousResultId, service, _, _) = await AssertSemanticTokens(txt, expectedData, isRazor);

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

            var (newResultId, _, _) = await AssertSemanticTokenEdits(newTxt, newExpectedData, isRazor, previousResultId: previousResultId, documentSnapshot: null, service);
            Assert.NotEqual(previousResultId, newResultId);
        }

        [Fact]
        public async Task GetSemanticTokens_Razor_GetEditsWithNoPreviousAsync()
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
            var (previousResultId, _, _) = await AssertSemanticTokenEdits(txt, expectedEdits, isRazor: false, previousResultId: null);
            Assert.NotNull(previousResultId);
        }

        [Fact]
        public async Task GetSemanticTokens_Razor_SomeTagHelpers_ThenNoneAsync()
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

            var isRazor = false;
            var (previousResultId, service, _, _) = await AssertSemanticTokens(txt, expectedData, isRazor);

            var newTxt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<p></p> ";
            var newExpectedData = new SemanticTokensDelta
            {
                Edits = new List<SemanticTokensEdit>
                {
                    new SemanticTokensEdit
                    {
                        Start = 17,
                        Data = new int[]{ 1, RazorSemanticTokensLegend.MarkupElement }.ToImmutableArray(),
                        DeleteCount = 2,
                    },
                    new SemanticTokensEdit
                    {
                        Start = 21,
                        Data = new int[]{ 1 }.ToImmutableArray(),
                        DeleteCount = 1,
                    },
                    new SemanticTokensEdit
                    {
                        Start = 37,
                        Data = new int[]{ 1, RazorSemanticTokensLegend.MarkupElement }.ToImmutableArray(),
                        DeleteCount = 2,
                    },
                    new SemanticTokensEdit
                    {
                        Start = 41,
                        Data = new int[]{ 1 }.ToImmutableArray(),
                        DeleteCount = 1,
                    }
                }
            };

            var (newResultId, _, _) = await AssertSemanticTokenEdits(newTxt, newExpectedData, isRazor, previousResultId: previousResultId, documentSnapshot: null, service);
            Assert.NotEqual(previousResultId, newResultId);
        }

        [Fact]
        public async Task GetSemanticTokens_Razor_OnlyDifferences_InternalAsync()
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

            var isRazor = false;
            var (previousResultId, service, _, _) = await AssertSemanticTokens(txt, expectedData, isRazor);

            var newTxt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test1></test1><test1></test1> ";
            var newExpectedData = new SemanticTokensDelta
            {
                Edits = new List<SemanticTokensEdit> {
                    new SemanticTokensEdit
                    {
                        Start = 45,
                        Data = new int[]{
                            0, 1, 1, RazorSemanticTokensLegend.MarkupTagDelimiter, 0,
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
            var (newResultId, _, _) = await AssertSemanticTokenEdits(newTxt, newExpectedData, isRazor, previousResultId: previousResultId, documentSnapshot: null, service);
            Assert.NotEqual(previousResultId, newResultId);
        }

        [Fact]
        public async Task GetSemanticTokens_Razor_ModifyAsync()
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

            var isRazor = false;
            var (previousResultId, service, _, _) = await AssertSemanticTokens(txt, expectedData, isRazor);

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
                        Data = new int[]{ 7, RazorSemanticTokensLegend.MarkupAttribute }.ToImmutableArray(),
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
            var (newResultId, _, _) = await AssertSemanticTokenEdits(newTxt, newExpectedData, isRazor, previousResultId: previousResultId, service: service);
            Assert.NotEqual(previousResultId, newResultId);
        }

        [Fact]
        public async Task GetSemanticTokens_Razor_OnlyDifferences_NewLinesAsync()
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

            var isRazor = false;
            var (previousResultId, service, _, _) = await AssertSemanticTokens(txt, expectedData, isRazor);

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
            var (newResultId, _, _) = await AssertSemanticTokenEdits(newTxt, newExpectedData, isRazor, previousResultId: previousResultId, documentSnapshot: null, service);
            Assert.NotEqual(previousResultId, newResultId);
        }

        private async Task<(string, RazorSemanticTokensInfoService, Mock<ClientNotifierServiceBase>, DocumentSnapshot)> AssertSemanticTokens(
            string txt,
            IEnumerable<int> expectedData,
            bool isRazor,
            RazorSemanticTokensInfoService service = null,
            OmniSharpRange location = null,
            SemanticTokens cSharpTokens = null,
            (OmniSharpRange, OmniSharpRange)[] documentMappings = null)
        {
            // Arrange
            Mock<ClientNotifierServiceBase> serviceMock = null;
            if (service is null)
            {
                (service, serviceMock) = GetDefaultRazorSemanticTokenInfoService(cSharpTokens, documentMappings);
            }
            var outService = service;

            var (documentSnapshot, textDocumentIdentifier) = CreateDocumentSnapshot(txt, isRazor, DefaultTagHelpers);

            // Act
            var tokens = await service.GetSemanticTokensAsync(documentSnapshot, textDocumentIdentifier, location, CancellationToken.None);

            // Assert
            Assert.True(ArrayEqual(expectedData, tokens.Data));

            return (tokens.ResultId, outService, serviceMock, documentSnapshot);
        }

        private static bool ArrayEqual(IEnumerable<int> expectedData, IEnumerable<int> actualData)
        {
            var expectedArray = expectedData.ToArray();
            var actualArray = actualData.ToArray();
            for (var i = 0; i < Math.Min(expectedData.Count(), actualData.Count()); i++)
            {
                if (expectedArray[i] != actualArray[i])
                {
                    Assert.True(false, $"expected: {expectedArray[i]}, actual: {actualArray[i]} i: {i}");
                    return false;
                }
            }

            Assert.Equal(expectedArray.Length, actualArray.Length);

            return true;
        }

        private async Task<(string, RazorSemanticTokensInfoService, Mock<ClientNotifierServiceBase>)> AssertSemanticTokenEdits(string txt, SemanticTokensFullOrDelta expectedEdits, bool isRazor, string previousResultId, DocumentSnapshot documentSnapshot = null, RazorSemanticTokensInfoService service = null)
        {
            // Arrange
            Mock<ClientNotifierServiceBase> clientMock = null;
            if (service is null)
            {
                (service, clientMock) = GetDefaultRazorSemanticTokenInfoService();
            }
            var outService = service;
            TextDocumentIdentifier textDocumentIdentifier;
            if (documentSnapshot is null)
            {
                var tuple = CreateDocumentSnapshot(txt, isRazor, DefaultTagHelpers);
                documentSnapshot = tuple.Item1;
                textDocumentIdentifier = tuple.Item2;
            }
            else
            {
                textDocumentIdentifier = new TextDocumentIdentifier(new Uri($"C:\\{RazorFile}"));
            }

            // Act
            var edits = await service.GetSemanticTokensEditsAsync(documentSnapshot, textDocumentIdentifier, previousResultId, CancellationToken.None);

            // Assert
            if (expectedEdits.IsDelta)
            {
                for (var i = 0; i < Math.Min(expectedEdits.Delta.Edits.Count(), edits.Delta.Edits.Count()); i++)
                {
                    Assert.Equal(expectedEdits.Delta.Edits.ElementAt(i), edits.Delta.Edits.ElementAt(i), SemanticEditComparer.Instance);
                }
                Assert.Equal(expectedEdits.Delta.Edits.Count(), edits.Delta.Edits.Count());

                return (edits.Delta.ResultId, outService, clientMock);
            }
            else
            {
                Assert.Equal(expectedEdits.Full.Data, edits.Full.Data, ImmutableArrayIntComparer.Instance);

                return (edits.Full.ResultId, outService, clientMock);
            }
        }

        private static (RazorSemanticTokensInfoService, Mock<ClientNotifierServiceBase>) GetDefaultRazorSemanticTokenInfoService(SemanticTokens cSharpTokens = null, (OmniSharpRange, OmniSharpRange)[] documentMappings = null)
        {
            var responseRouterReturns = new Mock<IResponseRouterReturns>(MockBehavior.Strict);
            responseRouterReturns
                .Setup(l => l.Returning<SemanticTokens>(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(cSharpTokens));

            var languageServer = new Mock<ClientNotifierServiceBase>(MockBehavior.Strict);
            languageServer
                .Setup(l => l.SendRequestAsync(LanguageServerConstants.RazorProvideSemanticTokensEndpoint, It.IsAny<SemanticTokensParams>()))
                .Returns(Task.FromResult(responseRouterReturns.Object));
            var documentMappingService = new Mock<RazorDocumentMappingService>(MockBehavior.Strict);
            if (documentMappings != null)
            {
                foreach (var (cSharpRange, razorRange) in documentMappings)
                {
                    var passingRange = razorRange;
                    documentMappingService
                        .Setup(s => s.TryMapFromProjectedDocumentRange(It.IsAny<RazorCodeDocument>(), cSharpRange, out passingRange))
                        .Returns(razorRange != null);
                }
            }
            var loggingFactory = new Mock<LoggerFactory>();

            return (new DefaultRazorSemanticTokensInfoService(languageServer.Object, documentMappingService.Object, loggingFactory.Object), languageServer);
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
                Assert.Equal(x.Data.Value, y.Data.Value, ImmutableArrayIntComparer.Instance);

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
