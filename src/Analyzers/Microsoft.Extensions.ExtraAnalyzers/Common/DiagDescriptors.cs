// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

[assembly: System.Resources.NeutralResourcesLanguage("en-us")]

namespace Microsoft.Extensions.ExtraAnalyzers;

internal static class DiagDescriptors
{
    /// <summary>
    /// Category for analyzers that will improve performance of the application.
    /// </summary>
    private const string Performance = nameof(Performance);

    /// <summary>
    /// Category for analyzers that will make code more readable.
    /// </summary>
    private const string Readability = nameof(Readability);

    /// <summary>
    /// Category for analyzers that will improve reliability of the application.
    /// </summary>
    private const string Reliability = nameof(Reliability);

    /// <summary>
    /// Category for analyzers that will improve resiliency of the application.
    /// </summary>
    private const string Resilience = nameof(Resilience);

    /// <summary>
    /// Category for analyzers that will make code more correct.
    /// </summary>
    private const string Correctness = nameof(Correctness);

    /// <summary>
    /// Category for analyzers that will improve the privacy posture of code.
    /// </summary>
    private const string Privacy = nameof(Privacy);

    public static DiagnosticDescriptor LegacyLogging { get; } = new(
        id: "R9A000",
        messageFormat: Resources.LegacyLoggingMessage,
        title: Resources.LegacyLoggingTitle,
        category: Performance,
        description: Resources.LegacyLoggingDescription,
        defaultSeverity: DiagnosticSeverity.Warning,
        helpLinkUri: "https://TODO/r9a000",
        isEnabledByDefault: true);

    // R9A001..R9A020 are retired

    public static DiagnosticDescriptor UsingToStringInLoggers { get; } = new(
        id: "R9A021",
        messageFormat: Resources.UsingToStringInLoggersMessage,
        title: Resources.UsingToStringInLoggersTitle,
        category: "Performance",
        description: Resources.UsingToStringInLoggersDescription,
        defaultSeverity: DiagnosticSeverity.Warning,
        helpLinkUri: "https://TODO/r9a021",
        isEnabledByDefault: true);

    public static DiagnosticDescriptor StaticTime { get; } = new(
        id: "R9A022",
        messageFormat: Resources.StaticTimeMessage,
        title: Resources.StaticTimeTitle,
        category: Reliability,
        description: Resources.StaticTimeDescription,
        defaultSeverity: DiagnosticSeverity.Warning,
        helpLinkUri: "https://TODO/r9a022",
        isEnabledByDefault: true);

    // R9A023..R9A029 retired

    public static DiagnosticDescriptor StartsEndsWith { get; } = new(
        id: "R9A030",
        messageFormat: Resources.StartsEndsWithMessage,
        title: Resources.StartsEndsWithTitle,
        category: Performance,
        description: Resources.StartsEndsWithDescription,
        defaultSeverity: DiagnosticSeverity.Warning,
        helpLinkUri: "https://TODO/r9a030",
        isEnabledByDefault: true);

    public static DiagnosticDescriptor MakeExeTypesInternal { get; } = new(
        id: "R9A031",
        messageFormat: Resources.MakeExeTypesInternalMessage,
        title: Resources.MakeExeTypesInternalTitle,
        category: Performance,
        description: Resources.MakeExeTypesInternalDescription,
        defaultSeverity: DiagnosticSeverity.Warning,
        helpLinkUri: "https://TODO/r9a031",
        isEnabledByDefault: true);

    public static DiagnosticDescriptor Arrays { get; } = new(
        id: "R9A032",
        messageFormat: Resources.ArraysMessage,
        title: Resources.ArraysTitle,
        category: Performance,
        description: Resources.ArraysDescription,
        defaultSeverity: DiagnosticSeverity.Warning,
        helpLinkUri: "https://TODO/r9a032",
        isEnabledByDefault: true);

    public static DiagnosticDescriptor EnumStrings { get; } = new(
        id: "R9A033",
        messageFormat: Resources.EnumStringsMessage,
        title: Resources.EnumStringsTitle,
        category: Performance,
        description: Resources.EnumStringsDescription,
        defaultSeverity: DiagnosticSeverity.Warning,
        helpLinkUri: "https://TODO/r9a033",
        isEnabledByDefault: true);

    // R9A034..R9A036 retired

    public static DiagnosticDescriptor ValueTuple { get; } = new(
        id: "R9A037",
        messageFormat: Resources.ValueTupleMessage,
        title: Resources.ValueTupleTitle,
        category: Performance,
        description: Resources.ValueTupleDescription,
        defaultSeverity: DiagnosticSeverity.Warning,
        helpLinkUri: "https://TODO/r9a037",
        isEnabledByDefault: true);

    // R9A038..R9A039 retired

    public static DiagnosticDescriptor LegacyCollection { get; } = new(
        id: "R9A040",
        messageFormat: Resources.LegacyCollectionMessage,
        title: Resources.LegacyCollectionTitle,
        category: Performance,
        description: Resources.LegacyCollectionDescription,
        defaultSeverity: DiagnosticSeverity.Warning,
        helpLinkUri: "https://TODO/r9a040",
        isEnabledByDefault: true);

    // R9A041..R9A042 retired

    public static DiagnosticDescriptor Split { get; } = new(
        id: "R9A043",
        messageFormat: Resources.SplitMessage,
        title: Resources.SplitTitle,
        category: Performance,
        description: Resources.SplitDescription,
        defaultSeverity: DiagnosticSeverity.Warning,
        helpLinkUri: "https://TODO/r9a043",
        isEnabledByDefault: true);

    // R9A044..R9A055 retired

    public static DiagnosticDescriptor AsyncCallInsideUsingBlock { get; } = new(
        id: "R9A056",
        messageFormat: Resources.AsyncCallInsideUsingBlockMessage,
        title: Resources.AsyncCallInsideUsingBlockTitle,
        category: Correctness,
        description: Resources.AsyncCallInsideUsingBlockDescription,
        defaultSeverity: DiagnosticSeverity.Warning,
        helpLinkUri: "https://TODO/r9a056",
        isEnabledByDefault: true);

    // R9A057 retired

    public static DiagnosticDescriptor ConditionalAccess { get; } = new(
        id: "R9A058",
        messageFormat: Resources.ConditionalAccessMessage,
        title: Resources.ConditionalAccessTitle,
        category: Performance,
        description: Resources.ConditionalAccessDescription,
        defaultSeverity: DiagnosticSeverity.Warning,
        helpLinkUri: "https://TODO/r9a058",
        isEnabledByDefault: true);

    public static DiagnosticDescriptor CoalesceAssignment { get; } = new(
        id: "R9A059",
        messageFormat: Resources.CoalesceAssignmentMessage,
        title: Resources.CoalesceAssignmentTitle,
        category: Performance,
        description: Resources.CoalesceAssignmentDescription,
        defaultSeverity: DiagnosticSeverity.Warning,
        helpLinkUri: "https://TODO/r9a059",
        isEnabledByDefault: true);

    public static DiagnosticDescriptor Coalesce { get; } = new(
        id: "R9A060",
        messageFormat: Resources.CoalesceMessage,
        title: Resources.CoalesceTitle,
        category: Performance,
        description: Resources.CoalesceDescription,
        defaultSeverity: DiagnosticSeverity.Warning,
        helpLinkUri: "https://TODO/r9a060",
        isEnabledByDefault: true);

    public static DiagnosticDescriptor AsyncMethodWithoutCancellation { get; } = new(
        id: "R9A061",
        messageFormat: Resources.AsyncMethodWithoutCancellationMessage,
        title: Resources.AsyncMethodWithoutCancellationTitle,
        category: Resilience,
        description: Resources.AsyncMethodWithoutCancellationDescription,
        defaultSeverity: DiagnosticSeverity.Warning,
        helpLinkUri: "https://TODO/r9a061",
        isEnabledByDefault: true);
}
