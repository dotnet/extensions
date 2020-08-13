// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.AspNetCore.Razor.LanguageServer.Completion;
using Microsoft.AspNetCore.Razor.LanguageServer.Semantic;
using Microsoft.AspNetCore.Razor.LanguageServer.Semantic.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit;
using OmniSharpRange = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

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

            AssertSemanticTokens(txt, expectedData, isRazor: false, out var _);
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

            AssertSemanticTokens(txt, expectedData, isRazor: false, out var _);
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

            AssertSemanticTokens(txt, expectedData, isRazor: false, out var _);
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

            AssertSemanticTokens(txt, expectedData, isRazor: false, out var _);
        }

        [Fact]
        public void GetSemanticTokens_TagHelpersNotAvailableInRazor()
        {
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test1 bool-val='true' class='display:none'></test1>";
            var expectedData = new List<uint> { };

            AssertSemanticTokens(txt, expectedData, isRazor: true, out var _);
        }

        [Fact]
        public void GetSemanticTokens_DoesNotApplyOnNonTagHelpers()
        {
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<p bool-val='true'></p>";
            var expectedData = new List<uint> { };

            AssertSemanticTokens(txt, expectedData, isRazor: false, out var _);
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

            AssertSemanticTokens(txt, expectedData, isRazor: true, out var _);
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

            AssertSemanticTokens(txt, expectedData, isRazor: true, out var _);
        }

        [Fact]
        public void GetSemanticTokens_Razor_NonComponentsDoNotShowInRazor()
        {
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test1 bool-val='true'></test1>";
            var expectedData = new List<uint> { };

            AssertSemanticTokens(txt, expectedData, isRazor: true, out var _);
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

            AssertSemanticTokens(txt, expectedData, isRazor: true, out var _);
        }

        [Fact]
        public void GetSemanticTokens_Razor_DoNotColorNonTagHelpers()
        {
            var txt = $"@addTaghelper *, TestAssembly{Environment.NewLine}<p @test='Function'></p>";
            var expectedData = new List<uint> {
                1, 3, 1, 2, 0,
                0, 1, 4, 4, 0
            };

            AssertSemanticTokens(txt, expectedData, isRazor: true, out var _);
        }

        [Fact]
        public void GetSemanticTokens_Razor_DoesNotApplyOnNonTagHelpers()
        {
            var txt = $"@addTagHelpers *, TestAssembly{Environment.NewLine}<p></p>";
            var expectedData = new List<uint> { };

            AssertSemanticTokens(txt, expectedData, isRazor: true, out var _);
        }

        [Fact]
        public void GetSemanticTokens_Razor_InRange()
        {
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test1></test1>";
            var expectedData = new List<uint> {
                1, 1, 5, 0, 0, //line, character pos, length, tokenType, modifier
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

        [Fact]
        public void GetSemanticTokens_Razor_NoDifference()
        {
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test1></test1>";
            var expectedData = new List<uint> {
                1, 1, 5, 0, 0, //line, character pos, length, tokenType, modifier
                0, 8, 5, 0, 0
            };

            var previousResultId = AssertSemanticTokens(txt, expectedData, isRazor: false, out var service);

            var newResultId = AssertSemanticTokenEdits(txt, new SemanticTokensEditCollection { Edits = new List<SemanticTokensEdit>() }, isRazor: false, previousResultId: previousResultId, out var _, service: service);
            Assert.NotEqual(previousResultId, newResultId);
        }

        [Fact]
        public void GetSemanticTokens_Razor_RemoveTokens()
        {
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test1></test1><test1></test1><test1></test1>";
            var expectedData = new List<uint> {
                1, 1, 5, 0, 0, //line, character pos, length, tokenType, modifier
                0, 8, 5, 0, 0,
                0, 7, 5, 0, 0,
                0, 8, 5, 0, 0,
                0, 7, 5, 0, 0,
                0, 8, 5, 0, 0
            };

            var previousResultId = AssertSemanticTokens(txt, expectedData, isRazor: false, out var service);

            var newTxt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test1></test1>";
            var newResultId = AssertSemanticTokenEdits(newTxt, new SemanticTokensEditCollection { Edits = new List<SemanticTokensEdit>(){
                new SemanticTokensEdit
                {
                    Data = Array.Empty<uint>(),
                    DeleteCount = 20,
                    Start = 10
                }
            }}, isRazor: false, previousResultId: previousResultId, out var _, service);
            Assert.NotEqual(previousResultId, newResultId);
        }

        [Fact]
        public void GetSemanticTokens_Razor_OnlyDifferences_Append()
        {
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test1></test1>";
            var expectedData = new List<uint> {
                1, 1, 5, 0, 0, //line, character pos, length, tokenType, modifier
                0, 8, 5, 0, 0
            };

            var previousResultId = AssertSemanticTokens(txt, expectedData, isRazor: false, out var service);

            var newTxt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test1 bool-val='true'></test1>";
            var newExpectedData = new SemanticTokensEditCollection {
                Edits = new SemanticTokensEdit[] {
                    new SemanticTokensEdit
                    {
                        Start = 6,
                        Data = new List<uint>{ 6 },
                        DeleteCount = 0,
                    },
                    new SemanticTokensEdit
                    {
                        Start = 7,
                        Data = new List<uint>{ 1, 0, 0, 18 }
                    }
                }
            };
            var newResultId = AssertSemanticTokenEdits(newTxt, newExpectedData, isRazor: false, previousResultId: previousResultId, out var _, service);
            Assert.NotEqual(previousResultId, newResultId);
        }

        [Fact]
        public void GetSemanticTokens_Razor_CoalesceDeleteAndAdd()
        {
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test1 />";
            var expectedData = new List<uint>
            {
                1, 1, 5, 0, 0, //line, character pos, length, tokenType, modifier
            };

            var previousResultId = AssertSemanticTokens(txt, expectedData, isRazor: false, out var service);

            var newTxt = $"@addTagHelper *, TestAssembly{Environment.NewLine}{Environment.NewLine}  <p @minimized></p>";
            var newExpectedData = new SemanticTokensEditCollection {
                Edits = new SemanticTokensEdit[] {
                    new SemanticTokensEdit
                    {
                        Start = 0,
                        DeleteCount = 0,
                        Data = new List<uint>{
                            2, 5,
                        },
                    },
                    new SemanticTokensEdit
                    {
                        Start = 1,
                        DeleteCount = 0,
                        Data = new List<uint>
                        {
                            2, 0, 0,
                        },
                    },
                    new SemanticTokensEdit
                    {
                        Start = 2,
                        DeleteCount = 2,
                        Data = new List<uint>
                        {
                            9, 4,
                        }
                    }
                }
            };

            var newResultId = AssertSemanticTokenEdits(newTxt, newExpectedData, isRazor: true, previousResultId: previousResultId, out var _, service);
            Assert.NotEqual(previousResultId, newResultId);
        }

        [Fact]
        public void GetSemanticTokens_Razor_OriginallyNone_ThenSome()
        {
            var expectedData = new List<uint> {};
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<p></p>";

            var previousResultId = AssertSemanticTokens(txt, expectedData, isRazor: false, out var service);

            var newTxt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test1></test1>";
            var newExpectedData = new SemanticTokensEditCollection
            {
                Edits = new List<SemanticTokensEdit> {
                    new SemanticTokensEdit
                    {
                        Start = 0,
                        Data = new uint[]{
                            1, 1, 5, 0, 0,
                            0, 8, 5, 0, 0,
                        },
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
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test1></test1>";
            var expectedEdits = new SemanticTokens
            {
                Data = new uint[] {
                    1, 1, 5, 0, 0,
                    0, 8, 5, 0, 0,
                },
            };

            var previousResultId = AssertSemanticTokenEdits(txt, expectedEdits, isRazor: false, previousResultId: null, out var service);
            Assert.NotNull(previousResultId);
        }

        [Fact]
        public void GetSemanticTokens_Razor_SomeTagHelpers_ThenNone()
        {
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test1></test1>";
            var expectedData = new uint[]{
                1, 1, 5, 0, 0,
                0, 8, 5, 0, 0,
            };

            var previousResultId = AssertSemanticTokens(txt, expectedData, isRazor: false, out var service);

            var newTxt = $"addTagHelper *, TestAssembly{Environment.NewLine}<p></p>";
            var newExpectedData = new SemanticTokensEditCollection
            {
                Edits = new List<SemanticTokensEdit>
                {
                    new SemanticTokensEdit
                    {
                        Start = 0,
                        Data = Array.Empty<uint>(),
                        DeleteCount = 10,
                    }
                }
            };

            var newResultId = AssertSemanticTokenEdits(newTxt, newExpectedData, isRazor: false, previousResultId: previousResultId, out var _, service);
            Assert.NotEqual(previousResultId, newResultId);
        }

        [Fact]
        public void GetSemanticTokens_Razor_OnlyDifferences_Internal()
        {
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test1></test1>";
            var expectedData = new List<uint> {
                1, 1, 5, 0, 0, //line, character pos, length, tokenType, modifier
                0, 8, 5, 0, 0
            };

            var previousResultId = AssertSemanticTokens(txt, expectedData, isRazor: false, out var service);

            var newTxt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test1></test1><test1></test1>";
            var newExpectedData = new SemanticTokensEditCollection
            {
                Edits = new List<SemanticTokensEdit> {
                    new SemanticTokensEdit
                    {
                        Start = 10,
                        Data = new uint[]{
                            0, 7, 5, 0, 0,
                            0, 8, 5, 0, 0,
                        },
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
            var expectedData = new List<uint> {
                1, 1, 5, 0, 0, //line, character pos, length, tokenType, modifier
                0, 6, 8, 1, 0,
                1, 1, 5, 0, 0,
                0, 6, 8, 1, 0,
                1, 1, 5, 0, 0,
                0, 6, 8, 1, 0,
            };

            var previousResultId = AssertSemanticTokens(txt, expectedData, isRazor: false, out var service);

            var newTxt = $"@addTagHelper *, TestAssembly{Environment.NewLine}" +
                $"<test1 bool-va=\"true\" />{Environment.NewLine}" +
                $"<test1 bool-val=\"true\" />{Environment.NewLine}" +
                $"<test1 bool-val=\"true\" />{Environment.NewLine}";
            var newExpectedData = new SemanticTokensEditCollection
            {
                Edits = new List<SemanticTokensEdit>
                {
                    new SemanticTokensEdit
                    {
                        Start = 5,
                        Data = Array.Empty<uint>(),
                        DeleteCount = 5,
                    },
                }
            };
            var newResultId = AssertSemanticTokenEdits(newTxt, newExpectedData, isRazor: false, previousResultId: previousResultId, out _, service);
            Assert.NotEqual(previousResultId, newResultId);
        }

        [Fact]
        public void GetSemanticTokens_Razor_OnlyDifferences_NewLines()
        {
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test1></test1>";
            var expectedData = new List<uint> {
                1, 1, 5, 0, 0, //line, character pos, length, tokenType, modifier
                0, 8, 5, 0, 0
            };

            var previousResultId = AssertSemanticTokens(txt, expectedData, isRazor: false, out var service);

            var newTxt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test1></test1>{Environment.NewLine}" +
                $"<test1></test1>";
            var newExpectedData = new SemanticTokensEditCollection
            {
                Edits = new List<SemanticTokensEdit> {
                    new SemanticTokensEdit
                    {
                        Start = 10,
                        Data = new uint[]{
                            1, 1, 5, 0, 0,
                            0, 8, 5, 0, 0,
                        },
                        DeleteCount = 0,
                    }
                }
            };
            var newResultId = AssertSemanticTokenEdits(newTxt, newExpectedData, isRazor: false, previousResultId: previousResultId, out _, service);
            Assert.NotEqual(previousResultId, newResultId);
        }

        private string AssertSemanticTokens(string txt, IEnumerable<uint> expectedData, bool isRazor, out RazorSemanticTokensInfoService outService, RazorSemanticTokensInfoService service = null, OmniSharpRange location = null)
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

            Assert.Equal(expectedData, tokens.Data);

            return tokens.ResultId;
        }

        private string AssertSemanticTokenEdits(string txt, SemanticTokensOrSemanticTokensEdits expectedEdits, bool isRazor, string previousResultId, out RazorSemanticTokensInfoService outService, RazorSemanticTokensInfoService service = null)
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
            if (expectedEdits.IsSemanticTokensEdits)
            {
                for (var i = 0; i < expectedEdits.SemanticTokensEdits.Edits.Count; i++)
                {
                    Assert.Equal(expectedEdits.SemanticTokensEdits.Edits[i], edits.SemanticTokensEdits.Edits[i]);
                }

                return edits.SemanticTokensEdits.ResultId;
            }
            else
            {
                Assert.Equal(expectedEdits.SemanticTokens.Data, edits.SemanticTokens.Data);

                return edits.SemanticTokens.ResultId;
            }
        }

        private RazorSemanticTokensInfoService GetDefaultRazorSemanticTokenInfoService()
        {
            return new DefaultRazorSemanticTokensInfoService();
        }
    }
}
