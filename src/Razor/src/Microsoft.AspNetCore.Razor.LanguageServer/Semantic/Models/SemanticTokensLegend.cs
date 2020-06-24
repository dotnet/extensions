// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Microsoft.AspNetCore.Razor.LanguageServer.Semantic.Models;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Semantic.Models
{
    internal class SemanticTokensLegend
    {
        public const string RazorTagHelperElement = "class";
        public const string RazorTagHelperAttribute = "class";
        public const string RazorTransition = "keyword";
        public const string RazorDirectiveAttribute = "keyword";
        public const string RazorDirectiveColon = "keyword";

        private static readonly IReadOnlyCollection<string> _tokenTypes = new string[] {
            RazorTagHelperElement,
            RazorTagHelperAttribute,
            RazorTransition,
            RazorDirectiveColon,
            RazorDirectiveAttribute,
        };

        private static readonly string[] _tokenModifiers = new string[] {
            "None"
        };

        public static readonly IReadOnlyDictionary<string, uint> TokenTypesLegend = GetMap(_tokenTypes);

        private SemanticTokensLegend()
        {
        }

        public static readonly SemanticTokensLegend Instance = new SemanticTokensLegend
        {
            TokenModifiers = new Container<string>(_tokenModifiers),
            TokenTypes = new Container<string>(_tokenTypes),
        };

        public Container<string> TokenTypes { get; private set; }

        public Container<string> TokenModifiers { get; private set; }

        private static IReadOnlyDictionary<string, uint> GetMap(IReadOnlyCollection<string> tokens)
        {
            var result = new Dictionary<string, uint>();
            for (var i = 0; i < tokens.Count(); i++)
            {
                result[tokens.ElementAt(i)] = (uint)i;
            }

            return result;
        }
    }
}
