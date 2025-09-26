// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.Gen.Shared;

namespace Microsoft.Gen.BuildMetadata;

[SuppressMessage("Format", "S1199", Justification = "For better visualization of how the generated code will look like.")]
internal sealed class Emitter : EmitterBase
{
    private const string DependencyInjectionNamespace = "global::Microsoft.Extensions.DependencyInjection.";
    private const string ConfigurationNamespace = "global::Microsoft.Extensions.Configuration.";
    private const string HostingNamespace = "global::Microsoft.Extensions.Hosting.";
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

    private void GenerateBuildMetadataSource()
    {
        OutGeneratedCodeAttribute();
        OutLn("[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]");
        OutLn($"private sealed class BuildMetadataSource : {ConfigurationNamespace}IConfigurationSource");
        OutOpenBrace();
        {
            OutLn("public string SectionName { get; }");
            OutLn();

            OutLn("public BuildMetadataSource(string sectionName)");
            OutOpenBrace();
            {
                OutNullGuards(checkBuilder: false);
                OutLn("SectionName = sectionName;");
            }

            OutCloseBrace();
            OutLn();
            OutLn($"public {ConfigurationNamespace}IConfigurationProvider Build({ConfigurationNamespace}IConfigurationBuilder builder)");
            OutOpenBrace();
            {
                OutLn($"return new {ConfigurationNamespace}Memory.MemoryConfigurationProvider(new {ConfigurationNamespace}Memory.MemoryConfigurationSource())");
                OutOpenBrace();
                {
                    OutLn($$"""{ $"{SectionName}:buildid", "{{_buildId}}" },""");
                    OutLn($$"""{ $"{SectionName}:buildnumber", "{{_buildNumber}}" },""");
                    OutLn($$"""{ $"{SectionName}:sourcebranchname", "{{_sourceBranchName}}" },""");
                    OutLn($$"""{ $"{SectionName}:sourceversion", "{{_sourceVersion}}" },""");
                }

                OutCloseBraceWithExtra(";");
            }

            OutCloseBrace();
        }

        OutCloseBrace();
    }

    private void GenerateBuildMetadataExtensions()
    {
        OutLn("namespace Microsoft.Extensions.AmbientMetadata");
        OutOpenBrace();
        {
            OutGeneratedCodeAttribute();
            OutLn("internal static class BuildMetadataGeneratedExtensions");
            OutOpenBrace();
            {
                OutLn("private const string DefaultSectionName = \"ambientmetadata:build\";");
                OutLn();

                GenerateBuildMetadataSource();
                OutLn();

                OutLn($"public static {HostingNamespace}IHostBuilder UseBuildMetadata(this {HostingNamespace}IHostBuilder builder, string sectionName = DefaultSectionName)");
                OutOpenBrace();
                {
                    OutNullGuards();
                    OutLn("_ = builder.ConfigureHostConfiguration(configBuilder => configBuilder.AddBuildMetadata(sectionName))");
                    Indent();
                    OutLn(".ConfigureServices((hostBuilderContext, serviceCollection) =>");
                    Indent();
                    OutLn($"{DependencyInjectionNamespace}BuildMetadataServiceCollectionExtensions.AddBuildMetadata(serviceCollection, hostBuilderContext.Configuration.GetSection(sectionName)));");
                    Unindent();
                    Unindent();
                    OutLn();

                    OutLn("return builder;");
                }

                OutCloseBrace();
                OutLn();

                OutLn("public static TBuilder UseBuildMetadata<TBuilder>(this TBuilder builder, string sectionName = DefaultSectionName)");
                Indent();
                OutLn($"where TBuilder : {HostingNamespace}IHostApplicationBuilder");
                Unindent();
                OutOpenBrace();
                {
                    OutNullGuards();
                    OutLn("_ = builder.Configuration.AddBuildMetadata(sectionName);");
                    OutLn($"{DependencyInjectionNamespace}BuildMetadataServiceCollectionExtensions.AddBuildMetadata(builder.Services, builder.Configuration.GetSection(sectionName));");
                    OutLn();

                    OutLn("return builder;");
                }

                OutCloseBrace();
                OutLn();

#pragma warning disable S103 // Lines should not be too long
                OutLn($"public static {ConfigurationNamespace}IConfigurationBuilder AddBuildMetadata(this {ConfigurationNamespace}IConfigurationBuilder builder, string sectionName = DefaultSectionName)");
#pragma warning restore S103 // Lines should not be too long
                OutOpenBrace();
                {
                    OutNullGuards();
                    OutLn("return builder.Add(new BuildMetadataSource(sectionName));");
                }

                OutCloseBrace();
            }

            OutCloseBrace();
        }

        OutCloseBrace();
    }

    private void OutNullGuards(bool checkBuilder = true)
    {
        OutPP("#if !NET");

        if (checkBuilder)
        {
            OutLn("if (builder is null)");
            OutOpenBrace();
            OutLn("throw new global::System.ArgumentNullException(nameof(builder));");
            OutCloseBrace();
            OutLn();
        }

        OutLn("if (global::System.string.IsNullOrWhiteSpace(sectionName))");
        OutOpenBrace();
        {
            OutLn("if (sectionName is null)");
            OutOpenBrace();
            {
                OutLn("throw new global::System.ArgumentNullException(nameof(sectionName));");
            }

            OutCloseBrace();
            OutLn();
            OutLn("throw new global::System.ArgumentException(\"The value cannot be an empty string or composed entirely of whitespace.\", nameof(sectionName));");
        }

        OutCloseBrace();

        OutPP("#else");

        if (checkBuilder)
        {
            OutLn("global::System.ArgumentNullException.ThrowIfNull(builder);");
        }

        OutLn("global::System.ArgumentException.ThrowIfNullOrWhiteSpace(sectionName);");

        OutPP("#endif");
        OutLn();
    }
}
