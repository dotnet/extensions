// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.Completion;
using Microsoft.AspNetCore.Razor.LanguageServer.Semantic.Models;
using Microsoft.CodeAnalysis;
using OmniSharp.Extensions.LanguageServer.Protocol.Models.Proposals;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Semantic
{
    public abstract class SemanticTokenTestBase : TagHelperServiceTestBase
    {
        private static readonly AsyncLocal<string?> _fileName = new AsyncLocal<string?>();

        private static readonly string _projectPath = TestProject.GetProjectDirectory(typeof(TagHelperServiceTestBase));

        // Used by the test framework to set the 'base' name for test files.
        public static string? FileName
        {
            get { return _fileName.Value; }
            set { _fileName.Value = value; }
        }

#if GENERATE_BASELINES
        protected bool GenerateBaselines { get; set; } = true;
#else
        protected bool GenerateBaselines { get; set; } = false;
#endif

        protected int BaselineTestCount { get; set; }
        protected int BaselineEditTestCount { get; set; }

        internal void AssertSemanticTokensMatchesBaseline(IEnumerable<int>? actualSemanticTokens)
        {
            if (FileName is null)
            {
                var message = $"{nameof(AssertSemanticTokensMatchesBaseline)} should only be called from a Semantic test ({nameof(FileName)} is null).";
                throw new InvalidOperationException(message);
            }

            var fileName = BaselineTestCount > 0 ? FileName + $"_{BaselineTestCount}" : FileName;
            var baselineFileName = Path.ChangeExtension(fileName, ".semantic.txt");
            var actual = actualSemanticTokens?.ToArray();

            BaselineTestCount++;
            if (GenerateBaselines)
            {
                GenerateSemanticBaseline(actual, baselineFileName);
            }

            var semanticFile = TestFile.Create(baselineFileName, GetType().GetTypeInfo().Assembly);
            if (!semanticFile.Exists())
            {
                throw new XunitException($"The resource {baselineFileName} was not found.");
            }
            var semanticIntStr = semanticFile.ReadAllText();
            var semanticArray = ParseSemanticBaseline(semanticIntStr);

            if (semanticArray is null && actual is null)
            {
                return;
            }
            else if (semanticArray is null || actual is null)
            {
                Assert.False(true, $"Expected: {semanticArray}; Actual: {actual}");
            }

            for (var i = 0; i < Math.Min(semanticArray!.Length, actual!.Length); i += 5)
            {
                var end = i + 5;
                var actualTokens = actual[i..end];
                var expectedTokens = semanticArray[i..end];
                Assert.True(Enumerable.SequenceEqual(expectedTokens, actualTokens), $"Expected: {string.Join(',', expectedTokens)} Actual: {string.Join(',', actualTokens)} index: {i}");
            }
            Assert.True(semanticArray.Length == actual.Length, $"Expected length: {semanticArray.Length}, Actual length: {actual.Length}");
        }

#pragma warning disable CS0618 // Type or member is obsolete
        internal void AssertSemanticTokensEditsMatchesBaseline(SemanticTokensFullOrDelta edits)
#pragma warning restore CS0618 // Type or member is obsolete
        {
            if (FileName is null)
            {
                var message = $"{nameof(AssertSemanticTokensEditsMatchesBaseline)} should only be called from a Semantic test ({nameof(FileName)} is null).";
                throw new InvalidOperationException(message);
            }

            var fileName = BaselineEditTestCount > 0 ? FileName + $"_{BaselineEditTestCount}" : FileName;
            var baselineFileName = Path.ChangeExtension(fileName, ".semanticedit.txt");

            BaselineEditTestCount++;
            if (GenerateBaselines)
            {
                GenerateSemanticEditBaseline(edits, baselineFileName);
            }

            var semanticEditFile = TestFile.Create(baselineFileName, GetType().GetTypeInfo().Assembly);
            if (!semanticEditFile.Exists())
            {
                throw new XunitException($"The resource {baselineFileName} was not found.");
            }
            var semanticEditStr = semanticEditFile.ReadAllText();
            var semanticEdits = ParseSemanticEditBaseline(semanticEditStr);

            if (semanticEdits!.Value.IsDelta && edits.IsDelta)
            {
                // We can't compare the ResultID because it's from a previous run
                Assert.Equal(semanticEdits.Value.Delta?.Edits, edits.Delta?.Edits, SemanticEditComparer.Instance);
            }
            else if (semanticEdits.Value.IsFull && edits.IsFull)
            {
                Assert.Equal(semanticEdits.Value.Full, edits.Full);
            }
            else
            {
                Assert.True(false, $"Expected and actual semantic edits did not match.");
            }
        }

#pragma warning disable CS0618 // Type or member is obsolete
        private static void GenerateSemanticEditBaseline(SemanticTokensFullOrDelta edits, string baselineFileName)
#pragma warning restore CS0618 // Type or member is obsolete
        {
            var builder = new StringBuilder();
            if (edits.IsDelta)
            {
                builder.AppendLine("Delta");
                foreach (var edit in edits.Delta!.Edits)
                {
                    builder.Append(edit.Start).Append(' ');
                    builder.Append(edit.DeleteCount).Append(" [ ");

                    foreach (var i in edit.Data!)
                    {
                        builder.Append(i).Append(' ');
                    }
                    builder.AppendLine("]");
                }
            }
            else
            {
                foreach (var d in edits.Full!.Data)
                {
                    builder.Append(d).Append(' ');
                }
            }

            var semanticBaselineEditPath = Path.Combine(_projectPath, baselineFileName);
            File.WriteAllText(semanticBaselineEditPath, builder.ToString());
        }

        private static void GenerateSemanticBaseline(IEnumerable<int>? actual, string baselineFileName)
        {
            var builder = new StringBuilder();
            if (actual != null)
            {
                var actualArray = actual.ToArray();
                builder.AppendLine("//line,characterPos,length,tokenType,modifier");
                var legendArray = RazorSemanticTokensLegend.TokenTypes.ToArray();
                for (var i = 0; i < actualArray.Length; i += 5)
                {
                    var typeString = legendArray[actualArray[i + 3]];
                    builder.Append(actualArray[i]).Append(' ');
                    builder.Append(actualArray[i + 1]).Append(' ');
                    builder.Append(actualArray[i + 2]).Append(' ');
                    builder.Append(actualArray[i + 3]).Append(' ');
                    builder.Append(actualArray[i + 4]).Append(" //").Append(typeString);
                    builder.AppendLine();
                }
            }

            var semanticBaselinePath = Path.Combine(_projectPath, baselineFileName);
            File.WriteAllText(semanticBaselinePath, builder.ToString());
        }

        private static int[]? ParseSemanticBaseline(string semanticIntStr)
        {
            if (string.IsNullOrEmpty(semanticIntStr))
            {
                return null;
            }

            var strArray = semanticIntStr.Split(new string[] { " ", Environment.NewLine }, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            var results = new List<int>();
            foreach (var str in strArray)
            {
                if (str.StartsWith("//", StringComparison.Ordinal))
                {
                    continue;
                }

                var intResult = int.Parse(str, Thread.CurrentThread.CurrentCulture);
                results.Add(intResult);
            }

            return results.ToArray();
        }

#pragma warning disable CS0618 // Type or member is obsolete
        private static SemanticTokensFullOrDelta? ParseSemanticEditBaseline(string semanticEditStr)
        {
            if (string.IsNullOrEmpty(semanticEditStr))
            {
                return null;
            }

            var strArray = semanticEditStr.Split(new string[] { " ", Environment.NewLine }, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

            if (strArray[0].Equals("Delta", StringComparison.Ordinal))
            {
                var delta = new SemanticTokensDelta();
                var edits = new List<SemanticTokensEdit>();
                var i = 1;
                while (i < strArray.Length - 1)
                {
                    var edit = new SemanticTokensEdit
                    {
                        Start = int.Parse(strArray[i], Thread.CurrentThread.CurrentCulture),
                        DeleteCount = int.Parse(strArray[i + 1], Thread.CurrentThread.CurrentCulture)
                    };
                    i += 3;
                    var inArray = true;
                    var data = new List<int>();
                    while (inArray)
                    {
                        var str = strArray[i];
                        if (str.Equals("]", StringComparison.Ordinal))
                        {
                            inArray = false;
                        }
                        else
                        {
                            data.Add(int.Parse(str, Thread.CurrentThread.CurrentCulture));
                        }

                        i++;
                    }
                    edit.Data = data.ToImmutableArray();
                    edits.Add(edit);
                }
                delta.Edits = edits;

                return new SemanticTokensFullOrDelta(delta);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private class SemanticEditComparer : IEqualityComparer<SemanticTokensEdit>
        {
            public static SemanticEditComparer Instance = new SemanticEditComparer();

            public bool Equals(SemanticTokensEdit? x, SemanticTokensEdit? y)
            {
                if (x == null && y == null)
                {
                    return true;
                }
                else if (x is null || y is null)
                {
                    return false;
                }

                Assert.Equal(x.DeleteCount, y.DeleteCount);
                Assert.Equal(x.Start, y.Start);
                if (x.Data.HasValue && y.Data.HasValue)
                {
                    Assert.Equal(x.Data.Value, y.Data.Value, ImmutableArrayIntComparer.Instance);
                }

                return x.DeleteCount == y.DeleteCount &&
                    x.Start == y.Start;
            }

            public int GetHashCode(SemanticTokensEdit obj)
            {
                throw new NotImplementedException();
            }
        }
#pragma warning restore CS0618 // Type or member is obsolete

        private class ImmutableArrayIntComparer : IEqualityComparer<ImmutableArray<int>>
        {
            public static ImmutableArrayIntComparer Instance = new ImmutableArrayIntComparer();

            public bool Equals(ImmutableArray<int> x, ImmutableArray<int> y)
            {
                for (var i = 0; i < Math.Min(x.Length, y.Length); i++)
                {
                    Assert.True(x[i] == y[i], $"x {x[i]} y {y[i]} i {i}");
                }
                Assert.Equal(x.Length, y.Length);

                return true;
            }

            public int GetHashCode(ImmutableArray<int> obj)
            {
                throw new NotImplementedException();
            }
        }
    }
}
