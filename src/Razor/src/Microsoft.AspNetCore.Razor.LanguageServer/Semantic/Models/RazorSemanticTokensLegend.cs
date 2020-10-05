// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
#pragma warning disable CS0618
using System.Collections.Generic;
using System.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Models.Proposals;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Semantic.Models
{
    internal class RazorSemanticTokensLegend
    {
        private const string RazorTagHelperElementString = "razorTagHelperElement";
        private const string RazorTagHelperAttributeString = "razorTagHelperAttribute";
        private const string RazorTransitionString = "razorTransition";
        private const string RazorDirectiveAttributeString = "razorDirectiveAttribute";
        private const string RazorDirectiveColonString = "razorDirectiveColon";
        private const string RazorDirectiveString = "razorDirective";
        private const string RazorCommentString = "razorComment";
        private const string RazorCommentTransitionString = "razorCommentTransition";
        private const string RazorCommentStarString = "razorCommentStar";

        private const string MarkupTagDelimiterString = "markupTagDelimiter";
        private const string MarkupOperatorString = "markupOperator";
        private const string MarkupElementString = "markupElement";
        private const string MarkupAttributeString = "markupAttribute";
        private const string MarkupAttributeQuoteString = "markupAttributeQuoteString";
        private const string MarkupTextLiteralString = "markupTextLiteral";
        private const string MarkupCommentPunctuationString = "markupCommentPunctuation";
        private const string MarkupCommentString = "markupComment";

        public static int RazorCommentTransition => TokenTypesLegend[RazorCommentTransitionString];
        public static int RazorCommentStar => TokenTypesLegend[RazorCommentStarString];
        public static int RazorComment => TokenTypesLegend[RazorCommentString];
        public static int RazorTransition => TokenTypesLegend[RazorTransitionString];
        public static int RazorTagHelperElement => TokenTypesLegend[RazorTagHelperElementString];
        public static int RazorTagHelperAttribute => TokenTypesLegend[RazorTagHelperAttributeString];
        public static int MarkupTagDelimiter => TokenTypesLegend[MarkupTagDelimiterString];
        public static int MarkupOperator => TokenTypesLegend[MarkupOperatorString];
        public static int MarkupElement => TokenTypesLegend[MarkupElementString];
        public static int MarkupAttribute => TokenTypesLegend[MarkupAttributeString];
        public static int MarkupAttributeQuote => TokenTypesLegend[MarkupAttributeQuoteString];
        public static int RazorDirectiveAttribute => TokenTypesLegend[RazorDirectiveAttributeString];
        public static int RazorDirectiveColon => TokenTypesLegend[RazorDirectiveColonString];
        public static int RazorDirective => TokenTypesLegend[RazorDirectiveString];
        public static int MarkupTextLiteral => TokenTypesLegend[MarkupTextLiteralString];
        public static int MarkupCommentPunctuation => TokenTypesLegend[MarkupCommentPunctuationString];
        public static int MarkupComment => TokenTypesLegend[MarkupCommentString];

        public static int CSharpKeyword => TokenTypesLegend["keyword"];
        public static int CSharpVariable => TokenTypesLegend["variable"];
        public static int CSharpOperator => TokenTypesLegend["operator"];

        private static readonly IReadOnlyCollection<string> _tokenTypes = new string[] {
            // C# token types
            "namespace", // 0
            "type",
            "class",
            "enum",
            "interface",
            "struct",
            "typeParameter",
            "parameter",
            "variable",
            "property",
            "enumMember", // 10
            "event",
            "function",
            "member",
            "macro",
            "keyword",
            "modifier",
            "comment",
            "string",
            "number",
            "regexp", // 20
            "operator",
            "constant name",
            "keyword - control",
            "delegate name",
            "excluded code",
            "extension method name",
            "field name",
            "label name",
            "local name",
            "method name", // 30
            "module name",
            "operator - overloaded",
            "preprocessor keyword",
            "preprocessor text",
            "punctuation",
            "regex - alternation",
            "regex - anchor",
            "regex - character class",
            "regex - comment",
            "regex - grouping", // 40
            "regex - other escape",
            "regex - quantifier",
            "regex - self escaped character",
            "regex - text",
            "string - escape character",
            "text",
            "string - verbatim",
            "whitespace",
            "xml doc comment - attribute name",
            "xml doc comment - attribute quotes", // 50
            "xml doc comment - attribute value",
            "xml doc comment - cdata section",
            "xml doc comment - comment",
            "xml doc comment - delimiter",
            "xml doc comment - entity reference",
            "xml doc comment - name",
            "xml doc comment - processing instruction",
            "xml doc comment - text",
            "xml literal - attribute name",
            "xml literal - attribute quotes", // 60
            "xml literal - attribute value",
            "xml literal - cdata section",
            "xml literal - comment",
            "xml literal - delimiter",
            "xml literal - embedded expression",
            "xml literal - entity reference",
            "xml literal - name",
            "xml literal - processing instruction",
            "xml literal - text",
            RazorTagHelperElementString, // 70 Razor token types
            RazorTagHelperAttributeString,
            RazorTransitionString,
            RazorDirectiveColonString,
            RazorDirectiveAttributeString,
            RazorDirectiveString,
            RazorCommentString,
            RazorCommentTransitionString,
            RazorCommentStarString,
            MarkupTagDelimiterString,
            MarkupElementString, // 80
            MarkupOperatorString,
            MarkupAttributeString,
            MarkupAttributeQuoteString,
            MarkupTextLiteralString,
            MarkupCommentPunctuationString,
            MarkupCommentString,
        };

        private static readonly string[] _tokenModifiers = new string[] {
            // Razor
            "None",
            // C# Modifiers
            "static",
        };

        public static readonly IReadOnlyDictionary<string, int> TokenTypesLegend = GetMap(_tokenTypes);

        public static readonly SemanticTokensLegend Instance = new SemanticTokensLegend
        {
            TokenModifiers = new Container<string>(_tokenModifiers),
            TokenTypes = new Container<string>(_tokenTypes),
        };

        private static IReadOnlyDictionary<string, int> GetMap(IReadOnlyCollection<string> tokens)
        {
            var result = new Dictionary<string, int>();
            for (var i = 0; i < tokens.Count; i++)
            {
                result[tokens.ElementAt(i)] = i;
            }

            return result;
        }
    }
}
