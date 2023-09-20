// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Extensions.LocalAnalyzers.ApiLifecycle.Model;

namespace Microsoft.Extensions.LocalAnalyzers.ApiLifecycle;

internal static class ModelLoader
{
    private static JsonSerializerOptions _serializationOptions = new()
    {
        Converters =
        {
            new TypeDefConverter(),
            new JsonStringEnumConverter<Stage>()
        }
    };

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
            assembly = JsonSerializer.Deserialize<Assembly>(publicInterface!, _serializationOptions);

            if (assembly == null || !assembly.Name.Contains(assemblyName))
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
