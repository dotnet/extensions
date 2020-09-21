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
        public const string RazorTagHelperElement = "razorTagHelperElement";
        public const string RazorTagHelperAttribute = "razorTagHelperAttribute";
        public const string RazorTransition = "razorTransition";
        public const string RazorDirectiveAttribute = "razorDirectiveAttribute";
        public const string RazorDirectiveColon = "razorDirectiveColon";

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
