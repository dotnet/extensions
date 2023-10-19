// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

[assembly: System.Resources.NeutralResourcesLanguage("en-us")]

namespace Microsoft.Extensions.ExtraAnalyzers;

internal static class DiagDescriptors
{
    private const string Performance = nameof(Performance);
    private const string Reliability = nameof(Reliability);
    private const string Resilience = nameof(Resilience);
    private const string Correctness = nameof(Correctness);

    public static DiagnosticDescriptor LegacyLogging { get; } = Make(
        id: "EA0000",
        messageFormat: Resources.LegacyLoggingMessage,
        title: Resources.LegacyLoggingTitle,
        category: Performance,
        description: Resources.LegacyLoggingDescription,
        defaultSeverity: DiagnosticSeverity.Warning);

    public static DiagnosticDescriptor UsingToStringInLoggers { get; } = Make(
        id: "EA0001",
        messageFormat: Resources.UsingToStringInLoggersMessage,
        title: Resources.UsingToStringInLoggersTitle,
        category: Performance,
        description: Resources.UsingToStringInLoggersDescription,
        defaultSeverity: DiagnosticSeverity.Warning);

    public static DiagnosticDescriptor StaticTime { get; } = Make(
        id: "EA0002",
        messageFormat: Resources.StaticTimeMessage,
        title: Resources.StaticTimeTitle,
        category: Reliability,
        description: Resources.StaticTimeDescription,
        defaultSeverity: DiagnosticSeverity.Warning);

    public static DiagnosticDescriptor StartsEndsWith { get; } = Make(
        id: "EA0003",
        messageFormat: Resources.StartsEndsWithMessage,
        title: Resources.StartsEndsWithTitle,
        category: Performance,
        description: Resources.StartsEndsWithDescription,
        defaultSeverity: DiagnosticSeverity.Warning);

    public static DiagnosticDescriptor MakeExeTypesInternal { get; } = Make(
        id: "EA0004",
        messageFormat: Resources.MakeExeTypesInternalMessage,
        title: Resources.MakeExeTypesInternalTitle,
        category: Performance,
        description: Resources.MakeExeTypesInternalDescription,
        defaultSeverity: DiagnosticSeverity.Warning);

    public static DiagnosticDescriptor Arrays { get; } = Make(
        id: "EA0005",
        messageFormat: Resources.ArraysMessage,
        title: Resources.ArraysTitle,
        category: Performance,
        description: Resources.ArraysDescription,
        defaultSeverity: DiagnosticSeverity.Warning);

    public static DiagnosticDescriptor EnumStrings { get; } = Make(
        id: "EA0006",
        messageFormat: Resources.EnumStringsMessage,
        title: Resources.EnumStringsTitle,
        category: Performance,
        description: Resources.EnumStringsDescription,
        defaultSeverity: DiagnosticSeverity.Warning);

    public static DiagnosticDescriptor ValueTuple { get; } = Make(
        id: "EA0007",
        messageFormat: Resources.ValueTupleMessage,
        title: Resources.ValueTupleTitle,
        category: Performance,
        description: Resources.ValueTupleDescription,
        defaultSeverity: DiagnosticSeverity.Warning);

    public static DiagnosticDescriptor LegacyCollection { get; } = Make(
        id: "EA0008",
        messageFormat: Resources.LegacyCollectionMessage,
        title: Resources.LegacyCollectionTitle,
        category: Performance,
        description: Resources.LegacyCollectionDescription,
        defaultSeverity: DiagnosticSeverity.Warning);

    public static DiagnosticDescriptor Split { get; } = Make(
        id: "EA0009",
        messageFormat: Resources.SplitMessage,
        title: Resources.SplitTitle,
        category: Performance,
        description: Resources.SplitDescription,
        defaultSeverity: DiagnosticSeverity.Warning);

    public static DiagnosticDescriptor AsyncCallInsideUsingBlock { get; } = Make(
        id: "EA0010",
        messageFormat: Resources.AsyncCallInsideUsingBlockMessage,
        title: Resources.AsyncCallInsideUsingBlockTitle,
        category: Correctness,
        description: Resources.AsyncCallInsideUsingBlockDescription,
        defaultSeverity: DiagnosticSeverity.Warning);

    public static DiagnosticDescriptor ConditionalAccess { get; } = Make(
        id: "EA0011",
        messageFormat: Resources.ConditionalAccessMessage,
        title: Resources.ConditionalAccessTitle,
        category: Performance,
        description: Resources.ConditionalAccessDescription,
        defaultSeverity: DiagnosticSeverity.Warning);

    public static DiagnosticDescriptor CoalesceAssignment { get; } = Make(
        id: "EA0012",
        messageFormat: Resources.CoalesceAssignmentMessage,
        title: Resources.CoalesceAssignmentTitle,
        category: Performance,
        description: Resources.CoalesceAssignmentDescription,
        defaultSeverity: DiagnosticSeverity.Warning);

    public static DiagnosticDescriptor Coalesce { get; } = Make(
        id: "EA0013",
        messageFormat: Resources.CoalesceMessage,
        title: Resources.CoalesceTitle,
        category: Performance,
        description: Resources.CoalesceDescription,
        defaultSeverity: DiagnosticSeverity.Warning);

    public static DiagnosticDescriptor AsyncMethodWithoutCancellation { get; } = Make(
        id: "EA0014",
        messageFormat: Resources.AsyncMethodWithoutCancellationMessage,
        title: Resources.AsyncMethodWithoutCancellationTitle,
        category: Resilience,
        description: Resources.AsyncMethodWithoutCancellationDescription,
        defaultSeverity: DiagnosticSeverity.Warning);

    private static DiagnosticDescriptor Make(string id, string title, string description, string messageFormat, string category, DiagnosticSeverity defaultSeverity)
    {
#pragma warning disable CA1305 // Specify IFormatProvider
        return new(
            id,
            title,
            messageFormat,
            category,
            defaultSeverity,
            true,
            description,
            string.Format(DiagnosticIds.UrlFormat, id),
            Array.Empty<string>());
#pragma warning restore CA1305 // Specify IFormatProvider
    }
}
