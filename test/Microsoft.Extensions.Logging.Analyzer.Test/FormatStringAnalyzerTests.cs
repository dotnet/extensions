// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging.Analyzers;
using Xunit;

namespace Microsoft.Extensions.Logging.Analyzer.Test
{
    public class FormatStringAnalyzerTests : DiagnosticVerifier
    {
        [Theory]
        [MemberData(nameof(GenerateTemplateAndDefineUsages), @"""{0}""", "1")]
        public void MEL0001IsProducedForNumericFormatArgument(string format)
        {
            // Enable MEL0005 because it shouldn't trigger on numeric arguments and we want to verify that.
            var diagnostic = Assert.Single(GetDiagnostics(format, "MEL0005"));
            Assert.Equal("MEL0001", diagnostic.Id);
        }

        [Theory]
        [MemberData(nameof(GenerateTemplateAndDefineUsages), @"$""{string.Empty}""", "")]
        [MemberData(nameof(GenerateTemplateAndDefineUsages), @"""string"" + 2", "")]
        public void MEL0002IsProducedForDynamicFormatArgument(string format)
        {
            var diagnostic = Assert.Single(GetDiagnostics(format));
            Assert.Equal("MEL0002", diagnostic.Id);
        }

        [Theory]
        [MemberData(nameof(GenerateTemplateUsages), @"""{string}""", "1, 2")]
        [MemberData(nameof(GenerateTemplateUsages), @"""{str"" + ""ing}""", "1, 2")]
        [MemberData(nameof(GenerateTemplateUsages), @"""{"" + nameof(ILogger) + ""}""", "")]
        [MemberData(nameof(GenerateTemplateUsages), @"""{"" + Const + ""}""", "")]
        public void MEL0003IsProducedForFormatArgumentCountMismatch(string format)
        {
            var diagnostic = Assert.Single(GetDiagnostics(format));
            Assert.Equal("MEL0003", diagnostic.Id);
        }

        [Theory]
        [InlineData(@"LoggerMessage.Define(LogLevel.Information, 42, ""{One} {Two} {Three}"");")]
        [InlineData(@"LoggerMessage.Define<int>(LogLevel.Information, 42, ""{One} {Two} {Three}"");")]
        [InlineData(@"LoggerMessage.Define<int, int>(LogLevel.Information, 42, ""{One} {Two} {Three}"");")]
        [InlineData(@"LoggerMessage.Define<int, int, int>(LogLevel.Information, 42, ""{One} {Two}"");")]
        [InlineData(@"LoggerMessage.Define<int, int, int, int>(LogLevel.Information, 42, ""{One} {Two} {Three}"");")]
        [InlineData(@"LoggerMessage.DefineScope<int>(""{One} {Two} {Three}"");")]
        [InlineData(@"LoggerMessage.DefineScope<int, int>(""{One} {Two} {Three}"");")]
        [InlineData(@"LoggerMessage.DefineScope<int, int, int>(""{One} {Two}"");")]
        public void MEL0003IsProducedForDefineMessageTypeParameterMismatch(string invocation)
        {
            var diagnostic = Assert.Single(GetDiagnostics(invocation));
            Assert.Equal("MEL0003", diagnostic.Id);
        }

        [Theory]
        [InlineData("LogTrace", @"""This is a test {Message}"", ""Foo""")]
        [InlineData("LogDebug", @"""This is a test {Message}"", ""Foo""")]
        [InlineData("LogInformation", @"""This is a test {Message}"", ""Foo""")]
        [InlineData("LogWarning", @"""This is a test {Message}"", ""Foo""")]
        [InlineData("LogError", @"""This is a test {Message}"", ""Foo""")]
        [InlineData("LogCritical", @"""This is a test {Message}"", ""Foo""")]
        [InlineData("BeginScope", @"""This is a test {Message}"", ""Foo""")]
        public void MEL0004IsProducedForInvocationsOfAllLoggerExtensions(string method, string args)
        {
            var diagnostic = Assert.Single(GetDiagnostics($"logger.{method}({args});", args, "MEL0004"));
            Assert.Equal("MEL0004", diagnostic.Id);
            Assert.Equal($"For improved performance, use pre-compiled log messages instead of calling '{method}' with a string message.", diagnostic.GetMessage());
        }

        [Theory]
        [MemberData(nameof(GenerateTemplateAndDefineUsages), @"""{camelCase}""", "1")]
        public void MEL0005IsProducedForCamelCasedFormatArgument(string format)
        {
            var diagnostic = Assert.Single(GetDiagnostics(format, "MEL0005"));
            Assert.Equal("MEL0005", diagnostic.Id);
        }

        [Theory]
        // Concat would be optimized by compiler
        [MemberData(nameof(GenerateTemplateAndDefineUsages), @"nameof(ILogger) + "" string""", "")]
        [MemberData(nameof(GenerateTemplateAndDefineUsages), @""" string"" + "" string""", "")]
        [MemberData(nameof(GenerateTemplateAndDefineUsages), @"$"" string"" + $"" string""", "")]
        [MemberData(nameof(GenerateTemplateAndDefineUsages), @"""{st"" + ""ring}""", "1")]

        // we are unable to parse expressions
        [MemberData(nameof(GenerateTemplateAndDefineUsages), @"""{string} {string}""", "new object[] { 1 }")]

        // MEL0005 is not enabled by default.
        [MemberData(nameof(GenerateTemplateAndDefineUsages), @"""{camelCase}""", "1")]
        public void TemplateDiagnosticsAreNotProduced(string format)
        {
            Assert.Empty(GetDiagnostics(format));
        }

        public static IEnumerable<object[]> GenerateTemplateAndDefineUsages(string template, string arguments)
        {
            return GenerateTemplateUsages(template, arguments).Concat(GenerateDefineUsages(template, arguments));
        }

        public static IEnumerable<object[]> GenerateTemplateUsages(string template, string arguments)
        {
            var templateAndArguments = template;
            if (!string.IsNullOrEmpty(arguments))
            {
                templateAndArguments = $"{template}, {arguments}";
            }
            var methods = new[] { "LogTrace", "LogError", "LogWarning", "LogInformation", "LogDebug", "LogCritical" };
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

        public static IEnumerable<object[]> GenerateDefineUsages(string template, string arguments)
        {
            // This is super rudimentary, but it works
            var braceCount = template.Count(c => c == '{');
            yield return new[] { $"LoggerMessage.{GenerateGenericInvocation(braceCount, "DefineScope")}({template});" };
            yield return new[] { $"LoggerMessage.{GenerateGenericInvocation(braceCount, "Define")}(LogLevel.Information, 42, {template});" };
        }

        private static string GenerateGenericInvocation(int i, string method)
        {
            if (i > 0)
            {
                var types = string.Join(", ", Enumerable.Range(0, i).Select(_ => "int"));
                method += $"<{types}>";
            }

            return method;
        }

        private static Diagnostic[] GetDiagnostics(string expression, params string[] additionalEnabledDiagnostics)
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
            return GetSortedDiagnosticsAsync(new[] { code }, new LogFormatAnalyzer(), additionalEnabledDiagnostics).Result;
        }
    }
}
