// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.Gen.BuildMetadata;

[Generator]
public class BuildMetadataGenerator : IIncrementalGenerator
{
    private static DateTimeOffset _initializeDateTime = DateTimeOffset.UtcNow;

    /// <inheritdoc/>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterSourceOutput(context.CompilationProvider, (spc, compilation) => Execute(compilation, spc));
    }

    private static void Execute(Compilation compilation, SourceProductionContext context)
    {
        Model.BuildDateTime = _initializeDateTime;

        OverwriteModelValuesForTesting(compilation.Assembly.GetAttributes());

        var e = new Emitter();
        var result = e.EmitExtensions(context.CancellationToken);
        context.AddSource("BuildMetadataExtensions.g.cs", SourceText.From(result, Encoding.UTF8));
    }

    private static void OverwriteModelValuesForTesting(ImmutableArray<AttributeData> attributes)
    {
        const int TestAttributeConstructorArgumentsLength = 5;

        if (attributes.IsDefaultOrEmpty || attributes[0].ConstructorArguments.Length != TestAttributeConstructorArgumentsLength)
        {
            return;
        }

        // internal attribute has five constructor args:
        // buildId, buildNumber, sourceBranchName, sourceVersion, buildDateTime
        var attribute = attributes[0];

#pragma warning disable S109 // Magic numbers should not be used
#pragma warning disable S1067 // Expressions should not be too complex
        if (
            attribute.ConstructorArguments[0].Value is string buildId &&
            attribute.ConstructorArguments[1].Value is string buildNumber &&
            attribute.ConstructorArguments[2].Value is string sourceBranchName &&
            attribute.ConstructorArguments[3].Value is string sourceVersion &&
            attribute.ConstructorArguments[4].Value is int buildDateTime)
        {
            Model.IsAzureDevOps = true;
            Model.AzureBuildId = buildId;
            Model.AzureBuildNumber = buildNumber;
            Model.AzureSourceBranchName = sourceBranchName;
            Model.AzureSourceVersion = sourceVersion;
            Model.BuildDateTime = DateTimeOffset.FromUnixTimeSeconds(buildDateTime);
        }
#pragma warning restore S1067 // Expressions should not be too complex
#pragma warning restore S109 // Magic numbers should not be used
    }
}
