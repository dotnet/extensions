// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

[assembly: System.Resources.NeutralResourcesLanguage("en-us")]

namespace Microsoft.Extensions.LocalAnalyzers;

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

    public static DiagnosticDescriptor ThrowsExpression { get; } = new(
        id: "R9A014",
        messageFormat: Resources.ThrowsExpressionMessage,
        title: Resources.ThrowsExpressionTitle,
        category: Performance,
        description: Resources.ThrowsExpressionDescription,
        defaultSeverity: DiagnosticSeverity.Warning,
        helpLinkUri: "https://TODO/r9a014",
        isEnabledByDefault: true);

    public static DiagnosticDescriptor ThrowsStatement { get; } = new(
        id: "R9A015",
        messageFormat: Resources.ThrowsStatementMessage,
        title: Resources.ThrowsStatementTitle,
        category: Performance,
        description: Resources.ThrowsStatementDescription,
        defaultSeverity: DiagnosticSeverity.Warning,
        helpLinkUri: "https://TODO/r9a015",
        isEnabledByDefault: true);

    public static DiagnosticDescriptor ToInvariantString { get; } = new(
        id: "R9A036",
        messageFormat: Resources.ToInvariantStringMessage,
        title: Resources.ToInvariantStringTitle,
        category: Performance,
        description: Resources.ToInvariantStringDescription,
        defaultSeverity: DiagnosticSeverity.Warning,
        helpLinkUri: "https://TODO/r9a036",
        isEnabledByDefault: true);

    public static DiagnosticDescriptor NewSymbolsMustBeMarkedExperimental { get; } = new(
        id: "R9A049",
        messageFormat: Resources.NewSymbolsMustBeMarkedExperimentalMessage,
        title: Resources.NewSymbolsMustBeMarkedExperimentalTitle,
        category: Correctness,
        description: Resources.NewSymbolsMustBeMarkedExperimentalDescription,
        defaultSeverity: DiagnosticSeverity.Hidden,
        helpLinkUri: "https://TODO/r9a049",
        isEnabledByDefault: true);

    public static DiagnosticDescriptor ExperimentalSymbolsCantBeMarkedObsolete { get; } = new(
        id: "R9A050",
        messageFormat: Resources.ExperimentalSymbolsCantBeMarkedObsoleteMessage,
        title: Resources.ExperimentalSymbolsCantBeMarkedObsoleteTitle,
        category: Correctness,
        description: Resources.ExperimentalSymbolsCantBeMarkedObsoleteDescription,
        defaultSeverity: DiagnosticSeverity.Hidden,
        helpLinkUri: "https://TODO/r9a050",
        isEnabledByDefault: true);

    public static DiagnosticDescriptor PublishedSymbolsCantBeMarkedExperimental { get; } = new(
        id: "R9A051",
        messageFormat: Resources.PublishedSymbolsCantBeMarkedExperimentalMessage,
        title: Resources.PublishedSymbolsCantBeMarkedExperimentalTitle,
        category: Correctness,
        description: Resources.PublishedSymbolsCantBeMarkedExperimentalDescription,
        defaultSeverity: DiagnosticSeverity.Hidden,
        helpLinkUri: "https://TODO/r9a051",
        isEnabledByDefault: true);

    public static DiagnosticDescriptor PublishedSymbolsCantBeDeleted { get; } = new(
        id: "R9A052",
        messageFormat: Resources.PublishedSymbolsCantBeDeletedMessage,
        title: Resources.PublishedSymbolsCantBeDeletedTitle,
        category: Correctness,
        description: Resources.PublishedSymbolsCantBeDeletedDescription,
        defaultSeverity: DiagnosticSeverity.Hidden,
        helpLinkUri: "https://TODO/r9a052",
        isEnabledByDefault: true);

    public static DiagnosticDescriptor PublishedSymbolsCantChange { get; } = new(
        id: "R9A055",
        messageFormat: Resources.PublishedSymbolsCantChangeMessage,
        title: Resources.PublishedSymbolsCantChangedTitle,
        category: Correctness,
        description: Resources.PublishedSymbolsCantChangeDescription,
        defaultSeverity: DiagnosticSeverity.Hidden,
        helpLinkUri: "https://TODO/r9a055",
        isEnabledByDefault: true);

}
