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
        private static readonly IReadOnlyCollection<string> _tokenTypes = new string[] {
            RazorTagHelperElement,
            RazorTagHelperAttribute,
        };

        private static readonly IReadOnlyCollection<string> _tokenModifiers = new string[] { };

        public static IReadOnlyDictionary<string, int> TokenTypesLegend
        {
            get
            {
                return GetMap(_tokenTypes);
            }
        }

        public static IReadOnlyDictionary<string, int> TokenModifiersLegend
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

        private static IReadOnlyDictionary<string, int> GetMap(IReadOnlyCollection<string> tokens)
        {
            var result = new Dictionary<string, int>();
            for (var i = 0; i < tokens.Count(); i++)
            {
                result.Add(tokens.ElementAt(i), i);
            }

            return result;
        }
    }
}
