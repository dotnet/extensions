// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.Gen.Shared;

namespace Microsoft.Gen.BuildMetadata;

internal sealed class Emitter : EmitterBase
{
    private readonly string? _buildId;
    private readonly string? _buildNumber;
    private readonly string? _sourceBranchName;
    private readonly string? _sourceVersion;

    public Emitter(string? buildId, string? buildNumber, string? sourceBranchName, string? sourceVersion)
    {
        _buildId = buildId;
        _buildNumber = buildNumber;
        _sourceBranchName = sourceBranchName;
        _sourceVersion = sourceVersion;
    }

    public string Emit(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        GenerateBuildMetadataExtensions();
        return Capture();
    }

    [SuppressMessage("Format", "IDE0055", Justification = "For better visualization of how the generated code will look like.")]
    private void GenerateBuildMetadataSource()
    {
        OutGeneratedCodeAttribute();
        OutLn("[EditorBrowsable(EditorBrowsableState.Never)]");
        OutLn("private sealed class BuildMetadataSource : IConfigurationSource");
        OutOpenBrace();
            OutLn("public string SectionName { get; }");
            OutLn();

            OutLn("public BuildMetadataSource(string sectionName)");
            OutOpenBrace();
                OutNullGuards(checkBuilder: false);
                OutLn("SectionName = sectionName;");
            OutCloseBrace();
            OutLn();

            OutLn("public IConfigurationProvider Build(IConfigurationBuilder builder)");
            OutOpenBrace();
                OutLn("return new MemoryConfigurationProvider(new MemoryConfigurationSource())");
                OutOpenBrace();
                    OutLn($$"""{ $"{SectionName}:buildid", "{{_buildId}}" },""");
                    OutLn($$"""{ $"{SectionName}:buildnumber", "{{_buildNumber}}" },""");
                    OutLn($$"""{ $"{SectionName}:sourcebranchname", "{{_sourceBranchName}}" },""");
                    OutLn($$"""{ $"{SectionName}:sourceversion", "{{_sourceVersion}}" },""");
                OutCloseBraceWithExtra(";");
            OutCloseBrace();
        OutCloseBrace();
    }

    [SuppressMessage("Format", "IDE0055", Justification = "For better visualization of how the generated code will look like.")]
    private void GenerateBuildMetadataExtensions()
    {
        OutLn("namespace Microsoft.Extensions.AmbientMetadata");
        OutOpenBrace();
            OutLn("using System;");
            OutLn("using System.ComponentModel;");
            OutLn("using System.Diagnostics.CodeAnalysis;");
            OutLn("using Microsoft.Extensions.Configuration;");
            OutLn("using Microsoft.Extensions.Configuration.Memory;");
            OutLn("using Microsoft.Extensions.DependencyInjection;");
            OutLn("using Microsoft.Extensions.Hosting;");
            OutLn();

            OutGeneratedCodeAttribute();
            OutLn("internal static class BuildMetadataGeneratedExtensions");
            OutOpenBrace();
                OutLn("private const string DefaultSectionName = \"ambientmetadata:build\";");
                OutLn();

                GenerateBuildMetadataSource();
                OutLn();

                OutLn("public static IHostBuilder UseBuildMetadata(this IHostBuilder builder, string sectionName = DefaultSectionName)");
                OutOpenBrace();
                    OutNullGuards();
                    OutLn("_ = builder.ConfigureHostConfiguration(configBuilder => configBuilder.AddBuildMetadata(sectionName))");
                    Indent();
                        OutLn(".ConfigureServices((hostBuilderContext, serviceCollection) =>");
                        Indent();
                            OutLn("serviceCollection.AddBuildMetadata(hostBuilderContext.Configuration.GetSection(sectionName)));");
                        Unindent();
                    Unindent();
                    OutLn();

                    OutLn("return builder;");
                OutCloseBrace();
                OutLn();

                OutLn("public static TBuilder UseBuildMetadata<TBuilder>(this TBuilder builder, string sectionName = DefaultSectionName)");
                Indent();
                    OutLn("where TBuilder : IHostApplicationBuilder");
                Unindent();
                OutOpenBrace();
                    OutNullGuards();
                    OutLn("_ = builder.Configuration.AddBuildMetadata(sectionName);");
                    OutLn("_ = builder.Services.AddBuildMetadata(builder.Configuration.GetSection(sectionName));");
                    OutLn();

                    OutLn("return builder;");
                OutCloseBrace();
                OutLn();

                OutLn("public static IConfigurationBuilder AddBuildMetadata(this IConfigurationBuilder builder, string sectionName = DefaultSectionName)");
                OutOpenBrace();
                    OutNullGuards();
                    OutLn("return builder.Add(new BuildMetadataSource(sectionName));");
                OutCloseBrace();
            OutCloseBrace();
        OutCloseBrace();
    }

    [SuppressMessage("Format", "IDE0055", Justification = "For better visualization of how the generated code will look like.")]
    private void OutNullGuards(bool checkBuilder = true)
    {
        OutPP("#if !NET");

        if (checkBuilder)
        {
            OutLn("if (builder is null)");
            OutOpenBrace();
            OutLn("throw new ArgumentNullException(nameof(builder));");
            OutCloseBrace();
            OutLn();
        }

            OutLn("if (string.IsNullOrWhiteSpace(sectionName))");
            OutOpenBrace();
                OutLn("if (sectionName is null)");
                OutOpenBrace();
                    OutLn("throw new ArgumentNullException(nameof(sectionName));");
                OutCloseBrace();
                OutLn();
                OutLn("throw new ArgumentException(\"The value cannot be an empty string or composed entirely of whitespace.\", nameof(sectionName));");
            OutCloseBrace();

        OutPP("#else");

        if (checkBuilder)
        {
            OutLn("ArgumentNullException.ThrowIfNull(builder);");
        }

            OutLn("ArgumentException.ThrowIfNullOrWhiteSpace(sectionName);");

        OutPP("#endif");
        OutLn();
    }
}
