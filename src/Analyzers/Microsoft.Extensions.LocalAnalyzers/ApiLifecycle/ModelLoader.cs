// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Extensions.LocalAnalyzers.ApiLifecycle.Model;
using Microsoft.Extensions.LocalAnalyzers.Json;

namespace Microsoft.Extensions.LocalAnalyzers.ApiLifecycle;

internal static class ModelLoader
{
#pragma warning disable RS1012 // Start action has no registered actions
    internal static bool TryLoadAssemblyModel(CompilationStartAnalysisContext context, out Assembly? assembly)
#pragma warning restore RS1012 // Start action has no registered actions
    {
        assembly = null;

        var files = context.Options.AdditionalFiles;
        var compilation = context.Compilation;
        var assemblyName = compilation.AssemblyName!;

        var assemblyBaselineFile = files.FirstOrDefault(file =>
        {
            var filePath = file.Path;
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            var fileExtension = Path.GetExtension(filePath);

            if (assemblyName.EndsWith(fileName, StringComparison.OrdinalIgnoreCase) && string.Equals(fileExtension, ".json", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        });

        if (assemblyBaselineFile == null)
        {
            return false;
        }

        var publicInterface = string.Empty;

        try
        {
            publicInterface = assemblyBaselineFile.GetText()?.ToString();
        }
        catch (FileNotFoundException)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(publicInterface))
        {
            return false;
        }

#pragma warning disable CA1031 // Do not catch general exception types
        try
        {
            using var reader = new StringReader(publicInterface);
            var value = JsonReader.Parse(reader);

            assembly = new Assembly(value.AsJsonObject!);
            if (!assembly.Name.Contains(assemblyName))
            {
                return false;
            }
        }
        catch (Exception)
        {
            // failed to deserialize.
            return false;
        }
#pragma warning restore CA1031 // Do not catch general exception types

        return true;
    }
}
