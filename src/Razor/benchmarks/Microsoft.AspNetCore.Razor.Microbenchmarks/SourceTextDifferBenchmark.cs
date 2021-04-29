// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Razor.LanguageServer;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.Razor.Microbenchmarks
{
    public class SourceTextDifferBenchmark
    {
        private readonly SourceText _largeFileOriginal;
        private readonly SourceText _largeFileMinimalChanges;
        private readonly SourceText _largeFileSignificantChanges;

        public SourceTextDifferBenchmark()
        {
            var current = new DirectoryInfo(AppContext.BaseDirectory);
            while (current != null && !File.Exists(Path.Combine(current.FullName, "MSN.cshtml")))
            {
                current = current.Parent;
            }

            var largeFilePath = Path.Combine(current.FullName, "MSN.cshtml");
            var largeFileText = File.ReadAllText(largeFilePath);

            _largeFileOriginal = SourceText.From(largeFileText);

            var changedText = largeFileText.Insert(100, "<");
            _largeFileMinimalChanges = SourceText.From(changedText);

            changedText = largeFileText.Substring(largeFileText.Length / 2).Reverse().ToString();
            _largeFileSignificantChanges = SourceText.From(changedText);
        }

        [Benchmark(Description = "Line Diff - One line change (Typing)")]
        public void LineDiff_LargeFile_OneLineChanged()
        {
            SourceTextDiffer.GetMinimalTextChanges(_largeFileOriginal, _largeFileMinimalChanges, lineDiffOnly: true);
        }

        [Benchmark(Description = "Line Diff - Significant Changes (Copy-paste)")]
        public void LineDiff_LargeFile_SignificantlyDifferent()
        {
            SourceTextDiffer.GetMinimalTextChanges(_largeFileOriginal, _largeFileSignificantChanges, lineDiffOnly: true);
        }

        [Benchmark(Description = "Character Diff - One character change (Typing)")]
        public void CharDiff_LargeFile_OneCharChanged()
        {
            SourceTextDiffer.GetMinimalTextChanges(_largeFileOriginal, _largeFileMinimalChanges, lineDiffOnly: false);
        }
    }
}
