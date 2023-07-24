// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.Extensions.ExtraAnalyzers.Test;

/// <remarks>
/// Creates a view over a file in the analyzer compilation directory with given name.
/// Can be used in tests for checking file related tasks.
/// </remarks>
internal class FileVisibleToAnalyzer : AdditionalText
{
    private readonly string _fileName;

    public FileVisibleToAnalyzer(string fileName)
    {
        _fileName = fileName;
    }

    public override string Path => System.IO.Path.Combine(Directory.GetCurrentDirectory(), _fileName);

    public override SourceText? GetText(CancellationToken cancellationToken = default) => SourceText.From(File.ReadAllText(Path));
}
