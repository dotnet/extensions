// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    public class CSharpImplicitExpressionTest : CodeParserTestBase
    {
        [Fact]
        public void ParsesNullConditionalOperatorImplicitExpression_Bracket1()
        {
            // Act & Assert
            ParseBlockTest("@val??[", expectedParseLength: 4);
        }

        [Fact]
        public void ParsesNullConditionalOperatorImplicitExpression_Bracket2()
        {
            // Act & Assert
            ParseBlockTest("@val??[0", expectedParseLength: 4);
        }

        [Fact]
        public void ParsesNullConditionalOperatorImplicitExpression_Bracket3()
        {
            // Act & Assert
            ParseBlockTest("@val?[");
        }

        [Fact]
        public void ParsesNullConditionalOperatorImplicitExpression_Bracket4()
        {
            // Act & Assert
            ParseBlockTest("@val?(", expectedParseLength: 4);
        }

        [Fact]
        public void ParsesNullConditionalOperatorImplicitExpression_Bracket5()
        {
            // Act & Assert
            ParseBlockTest("@val?[more");
        }

        [Fact]
        public void ParsesNullConditionalOperatorImplicitExpression_Bracket6()
        {
            // Act & Assert
            ParseBlockTest("@val?[0]");
        }

        [Fact]
        public void ParsesNullConditionalOperatorImplicitExpression_Bracket7()
        {
            // Act & Assert
            ParseBlockTest("@val?[<p>", expectedParseLength: 6);
        }

        [Fact]
        public void ParsesNullConditionalOperatorImplicitExpression_Bracket8()
        {
            // Act & Assert
            ParseBlockTest("@val?[more.<p>", expectedParseLength: 11);
        }

        [Fact]
        public void ParsesNullConditionalOperatorImplicitExpression_Bracket9()
        {
            // Act & Assert
            ParseBlockTest("@val??[more<p>", expectedParseLength: 4);
        }

        [Fact]
        public void ParsesNullConditionalOperatorImplicitExpression_Bracket10()
        {
            // Act & Assert
            ParseBlockTest("@val?[-1]?", expectedParseLength: 9);
        }

        [Fact]
        public void ParsesNullConditionalOperatorImplicitExpression_Bracket11()
        {
            // Act & Assert
            ParseBlockTest("@val?[abc]?[def");
        }

        [Fact]
        public void ParsesNullConditionalOperatorImplicitExpression_Bracket12()
        {
            // Act & Assert
            ParseBlockTest("@val?[abc]?[2]");
        }

        [Fact]
        public void ParsesNullConditionalOperatorImplicitExpression_Bracket13()
        {
            // Act & Assert
            ParseBlockTest("@val?[abc]?.more?[def]");
        }

        [Fact]
        public void ParsesNullConditionalOperatorImplicitExpression_Bracket14()
        {
            // Act & Assert
            ParseBlockTest("@val?[abc]?.more?.abc");
        }

        [Fact]
        public void ParsesNullConditionalOperatorImplicitExpression_Bracket15()
        {
            // Act & Assert
            ParseBlockTest("@val?[null ?? true]");
        }

        [Fact]
        public void ParsesNullConditionalOperatorImplicitExpression_Bracket16()
        {
            // Act & Assert
            ParseBlockTest("@val?[abc?.gef?[-1]]");
        }

        [Fact]
        public void ParsesNullConditionalOperatorImplicitExpression_Dot1()
        {
            // Act & Assert
            ParseBlockTest("@val?", expectedParseLength: 4);
        }

        [Fact]
        public void ParsesNullConditionalOperatorImplicitExpression_Dot2()
        {
            // Act & Assert
            ParseBlockTest("@val??", expectedParseLength: 4);
        }

        [Fact]
        public void ParsesNullConditionalOperatorImplicitExpression_Dot3()
        {
            // Act & Assert
            ParseBlockTest("@val??more", expectedParseLength: 4);
        }

        [Fact]
        public void ParsesNullConditionalOperatorImplicitExpression_Dot4()
        {
            // Act & Assert
            ParseBlockTest("@val?!", expectedParseLength: 4);
        }

        [Fact]
        public void ParsesNullConditionalOperatorImplicitExpression_Dot5()
        {
            // Act & Assert
            ParseBlockTest("@val?.");
        }

        [Fact]
        public void ParsesNullConditionalOperatorImplicitExpression_Dot6()
        {
            // Act & Assert
            ParseBlockTest("@val??.", expectedParseLength: 4);
        }

        [Fact]
        public void ParsesNullConditionalOperatorImplicitExpression_Dot7()
        {
            // Act & Assert
            ParseBlockTest("@val?.(abc)", expectedParseLength: 6);
        }

        [Fact]
        public void ParsesNullConditionalOperatorImplicitExpression_Dot8()
        {
            // Act & Assert
            ParseBlockTest("@val?.<p>", expectedParseLength: 6);
        }

        [Fact]
        public void ParsesNullConditionalOperatorImplicitExpression_Dot9()
        {
            // Act & Assert
            ParseBlockTest("@val?.more");
        }

        [Fact]
        public void ParsesNullConditionalOperatorImplicitExpression_Dot10()
        {
            // Act & Assert
            ParseBlockTest("@val?.more<p>", expectedParseLength: 10);
        }

        [Fact]
        public void ParsesNullConditionalOperatorImplicitExpression_Dot11()
        {
            // Act & Assert
            ParseBlockTest("@val??.more<p>", expectedParseLength: 4);
        }

        [Fact]
        public void ParsesNullConditionalOperatorImplicitExpression_Dot12()
        {
            // Act & Assert
            ParseBlockTest("@val?.more(false)?.<p>", expectedParseLength: 19);
        }

        [Fact]
        public void ParsesNullConditionalOperatorImplicitExpression_Dot13()
        {
            // Act & Assert
            ParseBlockTest("@val?.more(false)?.abc");
        }

        [Fact]
        public void ParsesNullConditionalOperatorImplicitExpression_Dot14()
        {
            // Act & Assert
            ParseBlockTest("@val?.more(null ?? true)?.abc");
        }

        [Fact]
        public void NestedImplicitExpression()
        {
            ParseBlockTest("if (true) { @foo }");
        }

        [Fact]
        public void AcceptsNonEnglishCharactersThatAreValidIdentifiers()
        {
            ParseBlockTest("@हळूँजद॔.", expectedParseLength: 8);
        }

        [Fact]
        public void OutputsZeroLengthCodeSpanIfInvalidCharacterFollowsTransition()
        {
            ParseBlockTest("@/", expectedParseLength: 1);
        }

        [Fact]
        public void OutputsZeroLengthCodeSpanIfEOFOccursAfterTransition()
        {
            ParseBlockTest("@");
        }

        [Fact]
        public void SupportsSlashesWithinComplexImplicitExpressions()
        {
            ParseBlockTest("@DataGridColumn.Template(\"Years of Service\", e => (int)Math.Round((DateTime.Now - dt).TotalDays / 365))");
        }

        [Fact]
        public void ParsesSingleIdentifierAsImplicitExpression()
        {
            ParseBlockTest("@foo");
        }

        [Fact]
        public void DoesNotAcceptSemicolonIfExpressionTerminatedByWhitespace()
        {
            ParseBlockTest("@foo ;", expectedParseLength: 4);
        }

        [Fact]
        public void IgnoresSemicolonAtEndOfSimpleImplicitExpression()
        {
            ParseBlockTest("@foo;", expectedParseLength: 4);
        }

        [Fact]
        public void ParsesDottedIdentifiersAsImplicitExpression()
        {
            ParseBlockTest("@foo.bar.baz");
        }

        [Fact]
        public void IgnoresSemicolonAtEndOfDottedIdentifiers()
        {
            ParseBlockTest("@foo.bar.baz;", expectedParseLength: 12);
        }

        [Fact]
        public void DoesNotIncludeDotAtEOFInImplicitExpression()
        {
            ParseBlockTest("@foo.bar.", expectedParseLength: 8);
        }

        [Fact]
        public void DoesNotIncludeDotFollowedByInvalidIdentifierCharInImplicitExpr1()
        {
            // ParseBlockMethodDoesNotIncludeDotFollowedByInvalidIdentifierCharacterInImplicitExpression1
            ParseBlockTest("@foo.bar.0", expectedParseLength: 8);
        }

        [Fact]
        public void DoesNotIncludeDotFollowedByInvalidIdentifierCharInImplicitExpr2()
        {
            // ParseBlockMethodDoesNotIncludeDotFollowedByInvalidIdentifierCharacterInImplicitExpression2
            ParseBlockTest("@foo.bar.</p>", expectedParseLength: 8);
        }

        [Fact]
        public void DoesNotIncludeSemicolonAfterDot()
        {
            ParseBlockTest("@foo.bar.;", expectedParseLength: 8);
        }

        [Fact]
        public void TerminatesAfterIdentifierUnlessFollowedByDotOrParenInImplicitExpr()
        {
            // ParseBlockMethodTerminatesAfterIdentifierUnlessFollowedByDotOrParenInImplicitExpression
            ParseBlockTest("@foo.bar</p>", expectedParseLength: 8);
        }

        [Fact]
        public void ProperlyParsesParenthesesAndBalancesThemInImplicitExpression()
        {
            ParseBlockTest(@"@foo().bar(""bi\""z"", 4)(""chained method; call"").baz(@""bo""""z"", '\'', () => { return 4; }, (4+5+new { foo = bar[4] }))");
        }

        [Fact]
        public void ProperlyParsesBracketsAndBalancesThemInImplicitExpression()
        {
            ParseBlockTest(@"@foo.bar[4 * (8 + 7)][""fo\""o""].baz");
        }

        [Fact]
        public void TerminatesImplicitExpressionAtHtmlEndTag()
        {
            ParseBlockTest("@foo().bar.baz</p>zoop", expectedParseLength: 14);
        }

        [Fact]
        public void TerminatesImplicitExpressionAtHtmlStartTag()
        {
            ParseBlockTest("@foo().bar.baz<p>zoop", expectedParseLength: 14);
        }

        [Fact]
        public void TerminatesImplicitExprBeforeDotIfDotNotFollowedByIdentifierStartChar()
        {
            // ParseBlockTerminatesImplicitExpressionBeforeDotIfDotNotFollowedByIdentifierStartCharacter
            ParseBlockTest("@foo().bar.baz.42", expectedParseLength: 14);
        }

        [Fact]
        public void StopsBalancingParenthesesAtEOF()
        {
            ParseBlockTest("@foo(()");
        }

        [Fact]
        public void TerminatesImplicitExpressionIfCloseParenFollowedByAnyWhiteSpace()
        {
            ParseBlockTest("@foo.bar() (baz)", expectedParseLength: 10);
        }

        [Fact]
        public void TerminatesImplicitExpressionIfIdentifierFollowedByAnyWhiteSpace()
        {
            ParseBlockTest("@foo .bar() (baz)", expectedParseLength: 4);
        }

        [Fact]
        public void TerminatesImplicitExpressionAtLastValidPointIfDotFollowedByWhitespace()
        {
            ParseBlockTest("@foo. bar() (baz)", expectedParseLength: 4);
        }

        [Fact]
        public void OutputExpressionIfModuleTokenNotFollowedByBrace()
        {
            ParseBlockTest("@module.foo()");
        }
    }
}
