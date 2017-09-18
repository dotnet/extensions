// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging.Analyzers;
using Xunit;

namespace Microsoft.Extensions.Logging.Analyzer.Test
{
    public class FormatStringAnalyzerTests: DiagnosticVerifier
    {
        [Theory]
        [MemberData(nameof(GenerateTemplateUsages), @"""{0}"", 1")]
        public void DiagnosticIsProducedForNumericFormatArgument(string format)
        {
            var diagnostic = Assert.Single(GetDiagnostics(format));
            Assert.Equal("MEL1", diagnostic.Id);
        }


        [Theory]
        [MemberData(nameof(GenerateTemplateUsages), @"$""{string.Empty}""")]
        [MemberData(nameof(GenerateTemplateUsages), @"""string"" + 2")]
        public void DiagnosticIsProducedForDynamicFormatArgument(string format)
        {
            var diagnostic = Assert.Single(GetDiagnostics(format));
            Assert.Equal("MEL2", diagnostic.Id);
        }

        [Theory]
        [MemberData(nameof(GenerateTemplateUsages), @"""{string}"", 1, 2")]
        [MemberData(nameof(GenerateTemplateUsages), @"""{str"" + ""ing}"", 1, 2")]
        [MemberData(nameof(GenerateTemplateUsages), @"""{"" + nameof(ILogger) + ""}""")]
        [MemberData(nameof(GenerateTemplateUsages), @"""{"" + Const + ""}""")]
        public void DiagnosticIsProducedForFormatArgumentCountMismatch(string format)
        {
            var diagnostic = Assert.Single(GetDiagnostics(format));
            Assert.Equal("MEL3", diagnostic.Id);
        }

        [Theory]
        // Concat would be optimized by compiler
        [MemberData(nameof(GenerateTemplateUsages), @"nameof(ILogger) + "" string""")]
        [MemberData(nameof(GenerateTemplateUsages), @""" string"" + "" string""")]
        [MemberData(nameof(GenerateTemplateUsages), @"$"" string"" + $"" string""")]
        [MemberData(nameof(GenerateTemplateUsages), @"""{st"" + ""ring}"", 1")]

        // we are unable to parse expressions
        [MemberData(nameof(GenerateTemplateUsages), @"""{string} {string}"", new object [] {1}")]
        public void DiagnosticNotIsProduced(string format)
        {
            Assert.Empty(GetDiagnostics(format));
        }

        public static IEnumerable<object[]> GenerateTemplateUsages(string templateAndArguments)
        {
            var methods = new[] {"LogTrace", "LogError", "LogWarning", "LogInformation", "LogDebug", "LogCritical" };
            var formats = new[]
            {
                "",
                "0, ",
                "1, new System.Exception(), ",
                "2, null, "
            };
            foreach (var method in methods)
            {
                foreach (var format in formats)
                {
                    yield return new[] { $"logger.{method}({format}{templateAndArguments});" };
                }
            }

            yield return new[] { $"logger.BeginScope({templateAndArguments});" };
        }

        private static Diagnostic[] GetDiagnostics(string expression)
        {
            var code = $@"
using Microsoft.Extensions.Logging;
public class Program
{{
    public const string Const = ""const"";
    public static void Main()
    {{
        ILogger logger = null;
        {expression}
    }}
}}
";
            return GetSortedDiagnostics(new[] {code}, new LogFormatAnalyzer());
        }
    }
}
