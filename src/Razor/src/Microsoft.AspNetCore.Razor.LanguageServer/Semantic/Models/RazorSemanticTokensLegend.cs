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

        private static readonly IReadOnlyCollection<string> _tokenTypes = new string[] {
            RazorTagHelperElementString,
            RazorTagHelperAttributeString,
            RazorTransitionString,
            RazorDirectiveColonString,
            RazorDirectiveAttributeString,
            RazorDirectiveString,
            RazorCommentString,
            RazorCommentTransitionString,
            RazorCommentStarString,
            MarkupTagDelimiterString,
            MarkupElementString,
            MarkupOperatorString,
            MarkupAttributeString,
            MarkupAttributeQuoteString,
            MarkupTextLiteralString,
            MarkupCommentPunctuationString,
            MarkupCommentString,
        };

        private static readonly string[] _tokenModifiers = new string[] {
            "None"
        };

        public static readonly IReadOnlyDictionary<string, int> TokenTypesLegend = GetMap(_tokenTypes);

        private RazorSemanticTokensLegend()
        {
        }

        public static readonly SemanticTokensLegend Instance = new SemanticTokensLegend
        {
            TokenModifiers = new Container<string>(_tokenModifiers),
            TokenTypes = new Container<string>(_tokenTypes),
        };

        public Container<string> TokenTypes { get; private set; }

        public Container<string> TokenModifiers { get; private set; }

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
