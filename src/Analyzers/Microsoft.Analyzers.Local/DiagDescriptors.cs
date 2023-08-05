// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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

    public static DiagnosticDescriptor ThrowsExpression { get; } = Make(
        id: "LA0000",
        messageFormat: Resources.ThrowsExpressionMessage,
        title: Resources.ThrowsExpressionTitle,
        category: Performance,
        description: Resources.ThrowsExpressionDescription,
        defaultSeverity: DiagnosticSeverity.Warning);

    public static DiagnosticDescriptor ThrowsStatement { get; } = Make(
        id: "LA0001",
        messageFormat: Resources.ThrowsStatementMessage,
        title: Resources.ThrowsStatementTitle,
        category: Performance,
        description: Resources.ThrowsStatementDescription,
        defaultSeverity: DiagnosticSeverity.Warning);

    public static DiagnosticDescriptor ToInvariantString { get; } = Make(
        id: "LA0002",
        messageFormat: Resources.ToInvariantStringMessage,
        title: Resources.ToInvariantStringTitle,
        category: Performance,
        description: Resources.ToInvariantStringDescription,
        defaultSeverity: DiagnosticSeverity.Warning);

    public static DiagnosticDescriptor NewSymbolsMustBeMarkedExperimental { get; } = Make(
        id: "LA0003",
        messageFormat: Resources.NewSymbolsMustBeMarkedExperimentalMessage,
        title: Resources.NewSymbolsMustBeMarkedExperimentalTitle,
        category: Correctness,
        description: Resources.NewSymbolsMustBeMarkedExperimentalDescription,
        defaultSeverity: DiagnosticSeverity.Warning);

    public static DiagnosticDescriptor ExperimentalSymbolsCantBeMarkedObsolete { get; } = Make(
        id: "LA0004",
        messageFormat: Resources.ExperimentalSymbolsCantBeMarkedObsoleteMessage,
        title: Resources.ExperimentalSymbolsCantBeMarkedObsoleteTitle,
        category: Correctness,
        description: Resources.ExperimentalSymbolsCantBeMarkedObsoleteDescription,
        defaultSeverity: DiagnosticSeverity.Warning);

    public static DiagnosticDescriptor PublishedSymbolsCantBeMarkedExperimental { get; } = Make(
        id: "LA0005",
        messageFormat: Resources.PublishedSymbolsCantBeMarkedExperimentalMessage,
        title: Resources.PublishedSymbolsCantBeMarkedExperimentalTitle,
        category: Correctness,
        description: Resources.PublishedSymbolsCantBeMarkedExperimentalDescription,
        defaultSeverity: DiagnosticSeverity.Warning);

    public static DiagnosticDescriptor PublishedSymbolsCantBeDeleted { get; } = Make(
        id: "LA0006",
        messageFormat: Resources.PublishedSymbolsCantBeDeletedMessage,
        title: Resources.PublishedSymbolsCantBeDeletedTitle,
        category: Correctness,
        description: Resources.PublishedSymbolsCantBeDeletedDescription,
        defaultSeverity: DiagnosticSeverity.Warning);

    public static DiagnosticDescriptor PublishedSymbolsCantChange { get; } = Make(
        id: "LA0007",
        messageFormat: Resources.PublishedSymbolsCantChangeMessage,
        title: Resources.PublishedSymbolsCantChangedTitle,
        category: Correctness,
        description: Resources.PublishedSymbolsCantChangeDescription,
        defaultSeverity: DiagnosticSeverity.Warning);

    private static DiagnosticDescriptor Make(string id, string title, string description, string messageFormat, string category, DiagnosticSeverity defaultSeverity)
        => new(id, title, messageFormat, category, defaultSeverity, true, description);
}
