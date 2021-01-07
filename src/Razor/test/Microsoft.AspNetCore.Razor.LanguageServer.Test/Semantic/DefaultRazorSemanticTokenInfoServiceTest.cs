// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
#pragma warning disable CS0618
#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.AspNetCore.Razor.LanguageServer.Formatting;
using Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem;
using Microsoft.AspNetCore.Razor.LanguageServer.Semantic;
using Microsoft.AspNetCore.Razor.LanguageServer.Semantic.Models;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.Extensions.Logging;
using Moq;
using OmniSharp.Extensions.JsonRpc;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Models.Proposals;
using Xunit;
using OmniSharpRange = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Test.Semantic
{
    // Sets the FileName static variable.
    // Finds the test method name using reflection, and uses
    // that to find the expected input/output test files as Embedded resources.
    [IntializeTestFile]
    public class DefaultRazorSemanticTokenInfoServiceTest : SemanticTokenTestBase
    {
        #region CSharp
        [Fact]
        public async Task GetSemanticTokens_CSharpBlock_HTML()
        {
            var txt = @$"@{{
    var d = ""t"";
    <p>HTML @d</p>
}}";

            var cSharpTokens = new SemanticTokens
            {
                Data = new int[] {
                    14, 0, 3, RazorSemanticTokensLegend.CSharpKeyword, 0,
                    1, 0, 1, RazorSemanticTokensLegend.CSharpVariable, 0,
                    1, 0, 1, RazorSemanticTokensLegend.CSharpPunctuation, 0,
                    1, 0, 1, RazorSemanticTokensLegend.CSharpPunctuation, 0,
                    1, 0, 1, RazorSemanticTokensLegend.CSharpString, 0,
                    1, 0, 1, RazorSemanticTokensLegend.CSharpPunctuation, 0,
                    1, 0, 1, RazorSemanticTokensLegend.CSharpPunctuation, 0,
                    1, 0, 1, RazorSemanticTokensLegend.CSharpVariable, 0,
                }.ToImmutableArray(),
                ResultId = null,
            };
            var cSharpResponse = new ProvideSemanticTokensResponse(cSharpTokens, hostDocumentSyncVersion: 0);

            var mappings = new (OmniSharpRange, OmniSharpRange?)[] {
                (new OmniSharpRange(new Position(14, 0), new Position(14, 3)), new OmniSharpRange(new Position(1, 4), new Position(1, 7))),
                (new OmniSharpRange(new Position(15, 0), new Position(15, 1)), new OmniSharpRange(new Position(1, 8), new Position(1,9))),
                (new OmniSharpRange(new Position(16, 0), new Position(16, 1)), new OmniSharpRange(new Position(1, 10), new Position(1, 11))),
                (new OmniSharpRange(new Position(17, 0), new Position(17, 1)), new OmniSharpRange(new Position(1, 12), new Position(1, 13))),
                (new OmniSharpRange(new Position(18, 0), new Position(18, 1)), new OmniSharpRange(new Position(1, 13), new Position(1, 14))),
                (new OmniSharpRange(new Position(19, 0), new Position(19, 1)), new OmniSharpRange(new Position(1, 14), new Position(1, 15))),
                (new OmniSharpRange(new Position(20, 0), new Position(20, 1)), new OmniSharpRange(new Position(1, 15), new Position(1, 16))),
                (new OmniSharpRange(new Position(21, 0), new Position(21, 1)), new OmniSharpRange(new Position(2, 13), new Position(2, 14))),
            };

            await AssertSemanticTokens(txt, isRazor: false, csharpTokens: cSharpResponse, documentMappings: mappings);
        }

        [Fact]
        public async Task GetSemanticTokens_CSharp_Nested_HTML()
        {
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<!--" +
                $"@{{" +
                $"var d = \"string\";" +
                $"@<a></a>" +
                $"}}" +
                $"-->";

            var cSharpTokens = new SemanticTokens
            {
                Data = new int[] {
                    14, 0, 3, RazorSemanticTokensLegend.CSharpKeyword, 0,
                    1, 0, 1, RazorSemanticTokensLegend.CSharpVariable, 0,
                    1, 0, 1, RazorSemanticTokensLegend.CSharpOperator, 0,
                    1, 0, 1, RazorSemanticTokensLegend.CSharpPunctuation, 0,
                    1, 0, 6, RazorSemanticTokensLegend.CSharpString, 0,
                    1, 0, 1, RazorSemanticTokensLegend.CSharpPunctuation, 0,
                    1, 0, 1, RazorSemanticTokensLegend.CSharpPunctuation, 0,
                }.ToImmutableArray(),
                ResultId = null,
            };
            var cSharpResponse = new ProvideSemanticTokensResponse(cSharpTokens, hostDocumentSyncVersion: 0);

            var mappings = new (OmniSharpRange, OmniSharpRange?)[] {
                (new OmniSharpRange(new Position(14, 0), new Position(14, 3)), new OmniSharpRange(new Position(1, 6), new Position(1, 9))),
                (new OmniSharpRange(new Position(15, 0), new Position(15, 1)), new OmniSharpRange(new Position(1, 10), new Position(1, 11))),
                (new OmniSharpRange(new Position(16, 0), new Position(16, 1)), new OmniSharpRange(new Position(1, 12), new Position(1, 13))),
                (new OmniSharpRange(new Position(17, 0), new Position(17, 1)), new OmniSharpRange(new Position(1, 13), new Position(1, 14))),
                (new OmniSharpRange(new Position(18, 0), new Position(18, 6)), new OmniSharpRange(new Position(1, 14), new Position(1, 20))),
                (new OmniSharpRange(new Position(19, 0), new Position(19, 1)), new OmniSharpRange(new Position(1, 20), new Position(1, 21))),
                (new OmniSharpRange(new Position(20, 0), new Position(20, 1)), new OmniSharpRange(new Position(1, 21), new Position(1, 22))),
            };

            await AssertSemanticTokens(txt, isRazor: false, csharpTokens: cSharpResponse, documentMappings: mappings);
        }

        [Fact]
        public async Task GetSemanticTokens_CSharp_VSCodeWorks()
        {
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}@{{ var d = }}";

            var cSharpTokens = new SemanticTokens
            {
                Data = ImmutableArray<int>.Empty,
                ResultId = null,
            };

            var cSharpResponse = new ProvideSemanticTokensResponse(cSharpTokens, hostDocumentSyncVersion: null);

            var mappings = Array.Empty<(OmniSharpRange, OmniSharpRange?)>();

            await AssertSemanticTokens(txt, isRazor: false, csharpTokens: cSharpResponse, documentMappings: mappings, documentVersion: 1);
        }

        [Fact]
        public async Task GetSemanticTokens_CSharp_Explicit()
        {
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}@(DateTime.Now)";

            var cSharpTokens = new SemanticTokens
            {
                Data = new int[] {
                    14, 0, 8, RazorSemanticTokensLegend.CSharpVariable, 0,
                    1, 0, 1, RazorSemanticTokensLegend.CSharpPunctuation, 0,
                    1, 0, 3, RazorSemanticTokensLegend.CSharpVariable, 0,
                }.ToImmutableArray(),
                ResultId = "35",
            };

            var cSharpResponse = new ProvideSemanticTokensResponse(cSharpTokens, hostDocumentSyncVersion: 0);

            var mappings = new (OmniSharpRange, OmniSharpRange?)[] {
                (new OmniSharpRange(new Position(14, 0),new Position(14, 8)), new OmniSharpRange(new Position(1, 2), new Position(1, 10))),
                (new OmniSharpRange(new Position(15, 0),new Position(15, 1)), new OmniSharpRange(new Position(1, 10), new Position(1, 11))),
                (new OmniSharpRange(new Position(16, 0),new Position(16, 3)), new OmniSharpRange(new Position(1, 11), new Position(1, 14))),
            };

            await AssertSemanticTokens(txt, isRazor: false, csharpTokens: cSharpResponse, documentMappings: mappings);
        }

        [Fact]
        public async Task GetSemanticTokens_CSharp_Implicit()
        {
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}@{{ var d = \"txt\";}}{Environment.NewLine}" +
                $"@d";

            var cSharpTokens = new SemanticTokens
            {
                Data = new int[] {
                    14, 0, 3, RazorSemanticTokensLegend.CSharpKeyword, 0,
                    1, 0, 1, RazorSemanticTokensLegend.CSharpVariable, 0,
                    1, 0, 1, RazorSemanticTokensLegend.CSharpOperator, 0,
                    1, 0, 1, RazorSemanticTokensLegend.CSharpOperator, 0,
                    1, 0, 3, RazorSemanticTokensLegend.CSharpString, 0,
                    1, 0, 1, RazorSemanticTokensLegend.CSharpOperator, 0,
                    1, 0, 1, RazorSemanticTokensLegend.CSharpOperator, 0,
                    1, 0, 1, RazorSemanticTokensLegend.CSharpVariable, 0,
                }.ToImmutableArray(),
                ResultId = "35",
            };

            var cSharpResponse = new ProvideSemanticTokensResponse(cSharpTokens, hostDocumentSyncVersion: 0);

            var mappings = new (OmniSharpRange, OmniSharpRange?)[] {
                 (new OmniSharpRange(new Position(14, 0), new Position(14, 3)), new OmniSharpRange(new Position(1, 3), new Position(1, 6))),
                (new OmniSharpRange(new Position(15, 0), new Position(15, 1)), new OmniSharpRange(new Position(1, 7), new Position(1, 8))),
                (new OmniSharpRange(new Position(16, 0), new Position(16, 1)), new OmniSharpRange(new Position(1, 9), new Position(1, 10))),
                (new OmniSharpRange(new Position(17, 0), new Position(17, 1)), new OmniSharpRange(new Position(1, 11), new Position(1, 12))),
                (new OmniSharpRange(new Position(18, 0), new Position(18, 3)), new OmniSharpRange(new Position(1, 12), new Position(1, 15))),
                (new OmniSharpRange(new Position(19, 0), new Position(19, 1)), new OmniSharpRange(new Position(1, 15), new Position(1, 16))),
                (new OmniSharpRange(new Position(20, 0), new Position(20, 1)), new OmniSharpRange(new Position(1, 16), new Position(1, 17))),
                (new OmniSharpRange(new Position(21, 0), new Position(21, 1)), new OmniSharpRange(new Position(2, 1), new Position(2, 2))),
            };

            await AssertSemanticTokens(txt, isRazor: false, csharpTokens: cSharpResponse, documentMappings: mappings);
        }

        [Fact]
        public async Task GetSemanticTokens_CSharp_VersionMismatch()
        {
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}@{{ var d = }}";

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

            var cSharpResponse = new ProvideSemanticTokensResponse(cSharpTokens, hostDocumentSyncVersion: 42);

            var mappings = new (OmniSharpRange, OmniSharpRange?)[] {
               (new OmniSharpRange(new Position(14, 12), new Position(14, 15)), new OmniSharpRange(new Position(1, 3), new Position(1, 6))),
               (new OmniSharpRange(new Position(27, 15), new Position(27, 16)), new OmniSharpRange(new Position(1, 7), new Position(1, 8))),
               (new OmniSharpRange(new Position(39, 25), new Position(39, 26)), new OmniSharpRange(new Position(1, 9), new Position(1, 10))),
               (new OmniSharpRange(new Position(50, 10), new Position(50, 35)), null)
            };

            await AssertSemanticTokens(txt, isRazor: false, csharpTokens: cSharpResponse, documentMappings: mappings, documentVersion: 21);
        }

        [Fact]
        public async Task GetSemanticTokens_CSharp_FunctionAsync()
        {
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}@{{ var d = }}";

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

            var cSharpResponse = new ProvideSemanticTokensResponse(cSharpTokens, hostDocumentSyncVersion: 0);

            var mappings = new (OmniSharpRange, OmniSharpRange?)[] {
               (new OmniSharpRange(new Position(14, 12), new Position(14, 15)), new OmniSharpRange(new Position(1, 3), new Position(1, 6))),
               (new OmniSharpRange(new Position(27, 15), new Position(27, 16)), new OmniSharpRange(new Position(1, 7), new Position(1, 8))),
               (new OmniSharpRange(new Position(39, 25), new Position(39, 26)), new OmniSharpRange(new Position(1, 9), new Position(1, 10))),
               (new OmniSharpRange(new Position(50, 10), new Position(50, 35)), null)
            };

            await AssertSemanticTokens(txt, isRazor: false, csharpTokens: cSharpResponse, documentMappings: mappings);
        }

        [Fact]
        public async Task GetSemanticTokens_CSharp_StaticModifierAsync()
        {
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}@{{ var d = }}";

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

            var cSharpResponse = new ProvideSemanticTokensResponse(cSharpTokens, hostDocumentSyncVersion: 0);

            var mappings = new (OmniSharpRange, OmniSharpRange?)[] {
               (new OmniSharpRange(new Position(14, 12), new Position(14, 15)), new OmniSharpRange(new Position(1, 3), new Position(1, 6))),
               (new OmniSharpRange(new Position(27, 15), new Position(27, 16)), new OmniSharpRange(new Position(1, 7), new Position(1, 8))),
               (new OmniSharpRange(new Position(39, 25), new Position(39, 26)), new OmniSharpRange(new Position(1, 9), new Position(1, 10))),
               (new OmniSharpRange(new Position(50, 10), new Position(50, 35)), null)
            };

            await AssertSemanticTokens(txt, isRazor: false, csharpTokens: cSharpResponse, documentMappings: mappings);
        }

        [Fact]
        public async Task GetSemanticTokens_CSharp_UsesCache()
        {
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}@{{ var d = }}";

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

            var cSharpResponse = new ProvideSemanticTokensResponse(cSharpTokens, hostDocumentSyncVersion: 0);

            var mappings = new (OmniSharpRange, OmniSharpRange?)[] {
               (new OmniSharpRange(new Position(14, 12), new Position(14, 15)), new OmniSharpRange(new Position(1, 3), new Position(1, 6))),
               (new OmniSharpRange(new Position(27, 15), new Position(27, 16)), new OmniSharpRange(new Position(1, 7), new Position(1, 8))),
               (new OmniSharpRange(new Position(39, 25), new Position(39, 26)), new OmniSharpRange(new Position(1, 9), new Position(1, 10))),
               (new OmniSharpRange(new Position(50, 10), new Position(50, 35)), null)
            };

            var isRazor = false;
            var (previousResultId, service, mockClient, document) = await AssertSemanticTokens(txt, isRazor, csharpTokens: cSharpResponse, documentMappings: mappings);

            await AssertSemanticTokenEdits(txt: null, expectDelta: true, isRazor, previousResultId: previousResultId, service: service);
            mockClient.Verify(l => l.SendRequestAsync(LanguageServerConstants.RazorProvideSemanticTokensEndpoint, It.IsAny<SemanticTokensParams>()), Times.Once());
        }
        #endregion

        #region HTML
        [Fact]
        public async Task GetSemanticTokens_HTMLCommentWithCSharp()
        {
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<!-- @DateTime.Now -->";

            var cSharpTokens = new SemanticTokens
            {
                Data = new int[] {
                    14, 12, 8, 1, 0, // CSharpType
                    0, 8, 1, 21, 0, // operator
                    0, 1, 3, 9, 0, // property
                }.ToImmutableArray(),
                ResultId = "35",
            };
            var cSharpResponse = new ProvideSemanticTokensResponse(cSharpTokens, hostDocumentSyncVersion: 0);

            var mappings = new (OmniSharpRange, OmniSharpRange?)[] {
                (new OmniSharpRange(new Position(14, 12), new Position(14, 20)), new OmniSharpRange(new Position(1, 6), new Position(1, 14))),
                (new OmniSharpRange(new Position(14, 20), new Position(14, 21)), new OmniSharpRange(new Position(1, 14), new Position(1, 15))),
                (new OmniSharpRange(new Position(14, 21),  new Position(14, 24)), new OmniSharpRange(new Position(1, 15), new Position(1, 18))),
            };

            await AssertSemanticTokens(txt, isRazor: true, csharpTokens: cSharpResponse, documentMappings: mappings);
        }

        [Fact]
        public async Task GetSemanticTokens_IncompleteTag()
        {
            var txt = "<str";

            await AssertSemanticTokens(txt, isRazor: false);
        }

        [Fact]
        public async Task GetSemanticTokens_MinimizedHTMLAttribute()
        {
            var txt = "<p attr />";

            await AssertSemanticTokens(txt, isRazor: false);
        }

        [Fact]
        public async Task GetSemanticTokens_MinimizedHTMLAsync()
        {
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<input/> ";

            await AssertSemanticTokens(txt, isRazor: false);
        }

        [Fact]
        public async Task GetSemanticTokens_HTMLCommentAsync()
        {
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<!-- comment with comma's --> ";

            await AssertSemanticTokens(txt, isRazor: false);
        }

        [Fact]
        public async Task GetSemanticTokens_PartialHTMLCommentAsync()
        {
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<!-- comment";

            await AssertSemanticTokens(txt, isRazor: true);
        }

        [Fact]
        public async Task GetSemanticTokens_HTMLIncludesBang()
        {
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<!input/>";

            await AssertSemanticTokens(txt, isRazor: false);
        }
        #endregion

        #region TagHelpers
        [Fact]
        public async Task GetSemanticTokens_HalfOfCommentAsync()
        {
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}@* comment";

            await AssertSemanticTokens(txt, isRazor: true);
        }

        [Fact]
        public async Task GetSemanticTokens_NoAttributesAsync()
        {
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test1></test1> ";

            await AssertSemanticTokens(txt, isRazor: false);
        }

        [Fact]
        public async Task GetSemanticTokens_WithAttributeAsync()
        {
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test1 bool-val='true'></test1> ";

            await AssertSemanticTokens(txt, isRazor: false);
        }

        [Fact]
        public async Task GetSemanticTokens_MinimizedAttributeAsync()
        {
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test1 bool-val></test1> ";

            await AssertSemanticTokens(txt, isRazor: false);
        }

        [Fact]
        public async Task GetSemanticTokens_IgnoresNonTagHelperAttributesAsync()
        {
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test1 bool-val='true' class='display:none'></test1> ";

            await AssertSemanticTokens(txt, isRazor: false);
        }

        [Fact]
        public async Task GetSemanticTokens_TagHelpersNotAvailableInRazorAsync()
        {
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test1 bool-val='true' class='display:none'></test1> ";

            await AssertSemanticTokens(txt, isRazor: true);
        }

        [Fact]
        public async Task GetSemanticTokens_DoesNotApplyOnNonTagHelpersAsync()
        {
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<p bool-val='true'></p> ";

            await AssertSemanticTokens(txt, isRazor: false);
        }
        #endregion TagHelpers

        #region DirectiveAttributes
        [Fact]
        public async Task GetSemanticTokens_Razor_MinimizedDirectiveAttributeParameters()
        {
            // Capitalized, non-well-known-HTML elements are always marked as TagHelpers
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<NotATagHelp @minimized:something /> ";

            await AssertSemanticTokens(txt, isRazor: true);
        }

        [Fact]
        public async Task GetSemanticTokens_Razor_DirectiveAttributesParametersAsync()
        {
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<Component1 @test:something='Function'></Component1> ";

            await AssertSemanticTokens(txt, isRazor: true);
        }

        [Fact]
        public async Task GetSemanticTokens_Razor_NonComponentsDoNotShowInRazorAsync()
        {
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test1 bool-val='true'></test1> ";

            await AssertSemanticTokens(txt, isRazor: true);
        }

        [Fact]
        public async Task GetSemanticTokens_Razor_DirectivesAsync()
        {
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<Component1 @test='Function'></Component1> ";

            await AssertSemanticTokens(txt, isRazor: true);
        }

        [Fact]
        public async Task GetSemanticTokens_HandleTransitionEscape()
        {
            var txt = $"@@text";

            await AssertSemanticTokens(txt, isRazor: true);
        }

        [Fact]
        public async Task GetSemanticTokens_Razor_DoNotColorNonTagHelpersAsync()
        {
            var txt = $"{Environment.NewLine}<p @test='Function'></p> ";

            await AssertSemanticTokens(txt, isRazor: true);
        }

        [Fact]
        public async Task GetSemanticTokens_Razor_DoesNotApplyOnNonTagHelpersAsync()
        {
            var txt = $"@addTagHelpers *, TestAssembly{Environment.NewLine}<p></p> ";

            await AssertSemanticTokens(txt, isRazor: true);
        }

        [Fact]
        public async Task GetSemanticTokens_Razor_InRangeAsync()
        {
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test1></test1> ";

            var startIndex = txt.IndexOf("test1", StringComparison.Ordinal);
            var endIndex = startIndex + 5;

            var codeDocument = CreateCodeDocument(txt, DefaultTagHelpers);

            codeDocument.GetSourceText().GetLineAndOffset(startIndex, out var startLine, out var startChar);
            codeDocument.GetSourceText().GetLineAndOffset(endIndex, out var endLine, out var endChar);

            var startPosition = new Position(startLine, startChar);
            var endPosition = new Position(endLine, endChar);
            var location = new OmniSharpRange(startPosition, endPosition);

            await AssertSemanticTokens(txt, isRazor: false, location: location);
        }
        #endregion DirectiveAttributes

        #region Directive
        [Fact]
        public async Task GetSemanticTokens_Razor_CodeDirectiveAsync()
        {
            var txt = $"@code {{}}";

            await AssertSemanticTokens(txt, isRazor: true);
        }

        [Fact]
        public async Task GetSemanticTokens_Razor_CodeDirectiveBodyAsync()
        {
            var txt = @$"@code {{
    public void SomeMethod()
    {{
@DateTime.Now
    }}
}}";

            await AssertSemanticTokens(txt, isRazor: true);
        }

        [Fact]
        public async Task GetSemanticTokens_Razor_UsingDirective()
        {
            var txt = $"@using Microsoft.AspNetCore.Razor";

            await AssertSemanticTokens(txt, isRazor: true);
        }

        [Fact]
        public async Task GetSemanticTokens_Razor_FunctionsDirectiveAsync()
        {
            var txt = $"@functions {{}}";

            await AssertSemanticTokens(txt, isRazor: true);
        }
        #endregion

        [Fact]
        public async Task GetSemanticTokens_Razor_CommentAsync()
        {
            var txt = $"@* A comment *@";

            await AssertSemanticTokens(txt, isRazor: true);
        }

        [Fact]
        public async Task GetSemanticTokens_Razor_EditTwiceAsync()
        {
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test1></test1>";
            var secondTxt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test1>T</test1>";
            var thirdTxt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test1>Test</test1>";
            var isRazor = true;
            var (firstResultId, service, _, _) = await AssertSemanticTokens(new string[] { txt, secondTxt, thirdTxt }, new bool[] { isRazor, isRazor, isRazor });

            var (secondResultId, _, _) = await AssertSemanticTokenEdits(secondTxt, expectDelta: true, isRazor, previousResultId: firstResultId, service);

            var (_, _, _) = await AssertSemanticTokenEdits(thirdTxt, expectDelta: true, isRazor, previousResultId: secondResultId, service);
        }

        [Fact]
        public async Task GetSemanticTokens_Razor_NoDifferenceAsync()
        {
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test1></test1> ";

            var isRazor = false;
            var (previousResultId, service, _, _) = await AssertSemanticTokens(txt, isRazor);

            var (newResultId, _, _) = await AssertSemanticTokenEdits(txt, expectDelta: true, isRazor, previousResultId: previousResultId, service: service);
            Assert.Equal(previousResultId, newResultId);
        }

        [Fact]
        public async Task GetSemanticTokens_Razor_RemoveTokensAsync()
        {
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test1></test1><test1></test1><test1></test1> ";
            var newTxt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test1></test1> ";
            var isRazor = false;
            var (previousResultId, service, _, _) = await AssertSemanticTokens(new string[] { txt, newTxt }, new bool[] { isRazor, isRazor });

            var (newResultId, _, _) = await AssertSemanticTokenEdits(newTxt, expectDelta: true, isRazor, previousResultId: previousResultId, service);
            Assert.NotEqual(previousResultId, newResultId);
        }

        [Fact]
        public async Task GetSemanticTokens_Razor_OnlyDifferences_AppendAsync()
        {
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test1></test1> ";

            var newTxt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test1 bool-val='true'></test1> ";
            var isRazor = false;
            var (previousResultId, service, _, _) = await AssertSemanticTokens(new string[] { txt, newTxt }, new bool[] { isRazor, isRazor });

            var (newResultId, _, _) = await AssertSemanticTokenEdits(newTxt, expectDelta: true, isRazor, previousResultId: previousResultId, service: service);
            Assert.NotEqual(previousResultId, newResultId);
        }

        [Fact]
        public async Task GetSemanticTokens_Razor_CoalesceDeleteAndAddAsync()
        {
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test1 /> ";

            var newTxt = $"@addTagHelper *, TestAssembly{Environment.NewLine}{Environment.NewLine}<p @minimized /> ";

            var (previousResultId, service, _, _) = await AssertSemanticTokens(new string[] { txt, newTxt }, new bool[] { false, true });

            var (newResultId, _, _) = await AssertSemanticTokenEdits(newTxt, expectDelta: true, isRazor: true, previousResultId: previousResultId, service: service);
            Assert.NotEqual(previousResultId, newResultId);
        }

        [Fact]
        public async Task GetSemanticTokens_Razor_OriginallyNone_ThenSomeAsync()
        {
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}";
            var newTxt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test1></test1> ";

            var isRazor = false;
            var (previousResultId, service, _, _) = await AssertSemanticTokens(new string[] { txt, newTxt }, new bool[] { isRazor, isRazor });

            var (newResultId, _, _) = await AssertSemanticTokenEdits(newTxt, expectDelta: true, isRazor, previousResultId: previousResultId, service);
            Assert.NotEqual(previousResultId, newResultId);
        }

        [Fact]
        public async Task GetSemanticTokens_Razor_GetEditsWithNoPreviousAsync()
        {
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test1></test1> ";

            var (previousResultId, _, _) = await AssertSemanticTokenEdits(txt, expectDelta: false, isRazor: false, previousResultId: null);
            Assert.NotNull(previousResultId);
        }

        [Fact]
        public async Task GetSemanticTokens_Razor_SomeTagHelpers_ThenNoneAsync()
        {
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test1></test1> ";

            var newTxt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<p></p> ";

            var isRazor = false;
            var (previousResultId, service, _, _) = await AssertSemanticTokens(new string[] { txt, newTxt }, new bool[] { isRazor, isRazor });

            var (newResultId, _, _) = await AssertSemanticTokenEdits(newTxt, expectDelta: true, isRazor, previousResultId: previousResultId, service);
            Assert.NotEqual(previousResultId, newResultId);
        }

        [Fact]
        public async Task GetSemanticTokens_Razor_OnlyDifferences_InternalAsync()
        {
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test1></test1> ";

            var newTxt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test1></test1><test1></test1> ";

            var isRazor = false;
            var (previousResultId, service, _, _) = await AssertSemanticTokens(new string[] { txt, newTxt }, new bool[] { isRazor, isRazor });

            var (newResultId, _, _) = await AssertSemanticTokenEdits(newTxt, expectDelta: true, isRazor, previousResultId: previousResultId, service);
            Assert.NotEqual(previousResultId, newResultId);
        }

        [Fact]
        public async Task GetSemanticTokens_Razor_ModifyAsync()
        {
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}" +
                $"<test1 bool-val=\"true\" />{Environment.NewLine}" +
                $"<test1 bool-val=\"true\" />{Environment.NewLine}" +
                $"<test1 bool-val=\"true\" />{Environment.NewLine}";

            var newTxt = $"@addTagHelper *, TestAssembly{Environment.NewLine}" +
                $"<test1 bool-va=\"true\" />{Environment.NewLine}" +
                $"<test1 bool-val=\"true\" />{Environment.NewLine}" +
                $"<test1 bool-val=\"true\" />{Environment.NewLine}";

            var isRazor = false;
            var (previousResultId, service, _, _) = await AssertSemanticTokens(new string[] { txt, newTxt }, new bool[] { isRazor, isRazor });

            var (newResultId, _, _) = await AssertSemanticTokenEdits(newTxt, expectDelta: true, isRazor, previousResultId: previousResultId, service: service);
            Assert.NotEqual(previousResultId, newResultId);
        }

        [Fact]
        public async Task GetSemanticTokens_Razor_OnlyDifferences_NewLinesAsync()
        {
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test1></test1> ";
            var newTxt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test1></test1>{Environment.NewLine}" +
                $"<test1></test1> ";

            var isRazor = false;
            var (previousResultId, service, _, _) = await AssertSemanticTokens(new string[] { txt, newTxt }, new bool[] { isRazor, isRazor });

            var (newResultId, _, _) = await AssertSemanticTokenEdits(newTxt, expectDelta: true, isRazor, previousResultId: previousResultId, service);
            Assert.NotEqual(previousResultId, newResultId);
        }

        private Task<(string?, RazorSemanticTokensInfoService, Mock<ClientNotifierServiceBase>, Queue<DocumentSnapshot>)> AssertSemanticTokens(
            string txt,
            bool isRazor,
            RazorSemanticTokensInfoService? service = null,
            OmniSharpRange? location = null,
            ProvideSemanticTokensResponse? csharpTokens = null,
            (OmniSharpRange, OmniSharpRange?)[]? documentMappings = null,
            int? documentVersion = 0)
        {
            return AssertSemanticTokens(new string[] { txt }, new bool[] { isRazor }, service, location, csharpTokens, documentMappings, documentVersion);
        }

        private async Task<(string?, RazorSemanticTokensInfoService, Mock<ClientNotifierServiceBase>, Queue<DocumentSnapshot>)> AssertSemanticTokens(
            string[] txtArray,
            bool[] isRazorArray,
            RazorSemanticTokensInfoService? service = null,
            OmniSharpRange? location = null,
            ProvideSemanticTokensResponse? csharpTokens = null,
            (OmniSharpRange, OmniSharpRange?)[]? documentMappings = null,
            int? documentVersion = 0)
        {
            // Arrange
            if (documentVersion == 0 && csharpTokens != null)
            {
                documentVersion = (int?)csharpTokens.HostDocumentSyncVersion;
            }

            if (csharpTokens is null)
            {
                var semanticTokens = new SemanticTokens { };
                csharpTokens = new ProvideSemanticTokensResponse(semanticTokens, documentVersion);
            }

            Mock<ClientNotifierServiceBase>? serviceMock = null;
            var (documentSnapshots, textDocumentIdentifiers) = CreateDocumentSnapshot(txtArray, isRazorArray, DefaultTagHelpers);

            if (service is null)
            {
                (service, serviceMock) = GetDefaultRazorSemanticTokenInfoService(documentSnapshots, csharpTokens, documentMappings, documentVersion);
            }
            var outService = service;

            var textDocumentIdentifier = textDocumentIdentifiers.Dequeue();

            // Act
            var tokens = await service.GetSemanticTokensAsync(textDocumentIdentifier, location, CancellationToken.None);

            // Assert
            AssertSemanticTokensMatchesBaseline(tokens?.Data);

            return (tokens?.ResultId, outService, serviceMock!, documentSnapshots);
        }

        private async Task<(string?, RazorSemanticTokensInfoService, Mock<ClientNotifierServiceBase>)> AssertSemanticTokenEdits(
            string? txt,
            bool expectDelta,
            bool isRazor,
            string? previousResultId,
            RazorSemanticTokensInfoService? service = null,
            long? documentVersion = 0)
        {
            // Arrange
            var semanticTokens = new SemanticTokens { };
            var cSharpTokens = new ProvideSemanticTokensResponse(semanticTokens, documentVersion);

            Mock<ClientNotifierServiceBase>? clientMock = null;
            if (service is null)
            {
                var (documentSnapshots, _) = CreateDocumentSnapshot(new string?[] { txt }, new bool[] { isRazor }, DefaultTagHelpers);
                (service, clientMock) = GetDefaultRazorSemanticTokenInfoService(documentSnapshots, cSharpTokens);
            }

            var textDocumentIdentifier = GetIdentifier(isRazor);

            // Act
            var edits = (await service.GetSemanticTokensEditsAsync(textDocumentIdentifier, previousResultId, CancellationToken.None)).Value;

            // Assert
            if (expectDelta)
            {
                AssertSemanticTokensEditsMatchesBaseline(edits);

                return (edits.Delta!.ResultId, service, clientMock!);
            }
            else
            {
                AssertSemanticTokensMatchesBaseline(edits.Full!.Data);

                return (edits.Full.ResultId, service, clientMock!);
            }
        }

        private static (RazorSemanticTokensInfoService, Mock<ClientNotifierServiceBase>) GetDefaultRazorSemanticTokenInfoService(
            Queue<DocumentSnapshot> documentSnapshots,
            ProvideSemanticTokensResponse? cSharpTokens = null,
            (OmniSharpRange, OmniSharpRange?)[]? documentMappings = null,
            int? documentVersion = 0)
        {
            var responseRouterReturns = new Mock<IResponseRouterReturns>(MockBehavior.Strict);
            responseRouterReturns
                .Setup(l => l.Returning<ProvideSemanticTokensResponse?>(It.IsAny<CancellationToken>()))
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
#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                        .Returns(razorRange != null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning restore CS8604 // Possible null reference argument.
                }
            }
            var loggingFactory = new Mock<LoggerFactory>();


            var foregroundDispatcher = new DefaultForegroundDispatcher();

            var documentResolver = new TestDocumentResolver(documentSnapshots);

            var documentVersionCache = new Mock<DocumentVersionCache>(MockBehavior.Strict);
            documentVersionCache.Setup(c => c.TryGetDocumentVersion(It.IsAny<DocumentSnapshot>(), out documentVersion))
                .Returns(true);

            return (new DefaultRazorSemanticTokensInfoService(
                languageServer.Object,
                documentMappingService.Object,
                foregroundDispatcher,
                documentResolver,
                documentVersionCache.Object,
                loggingFactory.Object), languageServer);
        }

        private class TestDocumentResolver : DocumentResolver
        {
            private readonly Queue<DocumentSnapshot> _documentSnapshots;

            public TestDocumentResolver(Queue<DocumentSnapshot> documentSnapshots)
            {
                _documentSnapshots = documentSnapshots;
            }

            public override bool TryResolveDocument(string documentFilePath, out DocumentSnapshot document)
            {
                document = _documentSnapshots.Count == 1 ? _documentSnapshots.Peek() : _documentSnapshots.Dequeue();

                return true;
            }
        }
    }
}
