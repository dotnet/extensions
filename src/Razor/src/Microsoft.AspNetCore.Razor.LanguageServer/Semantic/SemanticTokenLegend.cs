// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Semantic
{
    public static class SemanticTokenLegend
    {
        public const string RazorTagHelperElement = "razorTagHelperElement";
        public const string RazorTagHelperAttribute = "razorTagHelperAttribute";
        public const string RazorTransition = "razorTransition";
        public const string RazorDirectiveAttribute = "razorDirectiveAttribute";
        public const string RazorDirectiveColon = "razorDirectiveColon";

        private static readonly string[] _tokenTypes = new string[] {
            RazorTagHelperElement,
            RazorTagHelperAttribute,
            RazorTransition,
            RazorDirectiveColon,
            RazorDirectiveAttribute,
        };

        private static readonly IReadOnlyCollection<string> _tokenModifiers = new string[] { };

        public static IReadOnlyDictionary<string, uint> TokenTypesLegend
        {
            get
            {
                return GetMap(_tokenTypes);
            }
        }

        public static IReadOnlyDictionary<string, uint> TokenModifiersLegend
        {
            get
            {
                return GetMap(_tokenModifiers);
            }
        }

        public static SemanticTokenLegendResponse GetResponse()
        {
            return new SemanticTokenLegendResponse
            {
                TokenModifiers = _tokenModifiers,
                TokenTypes = _tokenTypes
            };
        }

        private static IReadOnlyDictionary<string, uint> GetMap(IReadOnlyCollection<string> tokens)
        {
            var result = new Dictionary<string, uint>();
            for (var i = 0; i < tokens.Count(); i++)
            {
                result.Add(tokens.ElementAt(i), (uint)i);
            }

            return result;
        }
    }
}
