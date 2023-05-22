// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.Gen.Shared;

namespace Microsoft.Gen.Logging.Parsing;

internal sealed class DiagDescriptors : DiagDescriptorsBase
{
    private const string Category = "LogMethod";

    public static DiagnosticDescriptor InvalidLoggingMethodName { get; } = Make(
        id: "R9G000",
        title: Resources.InvalidLoggingMethodNameTitle,
        messageFormat: Resources.InvalidLoggingMethodNameMessage,
        category: Category);

    public static DiagnosticDescriptor ShouldntMentionLogLevelInMessage { get; } = Make(
        id: "R9G001",
        title: Resources.ShouldntMentionLogLevelInMessageTitle,
        messageFormat: Resources.ShouldntMentionLogLevelInMessageMessage,
        category: Category,
        DiagnosticSeverity.Warning);

    public static DiagnosticDescriptor InvalidLoggingMethodParameterName { get; } = Make(
        id: "R9G002",
        title: Resources.InvalidLoggingMethodParameterNameTitle,
        messageFormat: Resources.InvalidLoggingMethodParameterNameMessage,
        category: Category);

    // R9G003 is no longer in use

    public static DiagnosticDescriptor MissingRequiredType { get; } = Make(
        id: "R9G004",
        title: Resources.MissingRequiredTypeTitle,
        messageFormat: Resources.MissingRequiredTypeMessage,
        category: Category);

    public static DiagnosticDescriptor ShouldntReuseEventIds { get; } = Make(
        id: "R9G005",
        title: Resources.ShouldntReuseEventIdsTitle,
        messageFormat: Resources.ShouldntReuseEventIdsMessage,
        category: Category,
        DiagnosticSeverity.Warning);

    public static DiagnosticDescriptor LoggingMethodMustReturnVoid { get; } = Make(
        id: "R9G006",
        title: Resources.LoggingMethodMustReturnVoidTitle,
        messageFormat: Resources.LoggingMethodMustReturnVoidMessage,
        category: Category);

    public static DiagnosticDescriptor MissingLoggerArgument { get; } = Make(
        id: "R9G007",
        title: Resources.MissingLoggerArgumentTitle,
        messageFormat: Resources.MissingLoggerArgumentMessage,
        category: Category);

    public static DiagnosticDescriptor LoggingMethodShouldBeStatic { get; } = Make(
        id: "R9G008",
        title: Resources.LoggingMethodShouldBeStaticTitle,
        messageFormat: Resources.LoggingMethodShouldBeStaticMessage,
        category: Category,
        DiagnosticSeverity.Warning);

    public static DiagnosticDescriptor LoggingMethodMustBePartial { get; } = Make(
        id: "R9G009",
        title: Resources.LoggingMethodMustBePartialTitle,
        messageFormat: Resources.LoggingMethodMustBePartialMessage,
        category: Category);

    public static DiagnosticDescriptor LoggingMethodIsGeneric { get; } = Make(
        id: "R9G010",
        title: Resources.LoggingMethodIsGenericTitle,
        messageFormat: Resources.LoggingMethodIsGenericMessage,
        category: Category);

    public static DiagnosticDescriptor RedundantQualifierInMessage { get; } = Make(
        id: "R9G011",
        title: Resources.RedundantQualifierInMessageTitle,
        messageFormat: Resources.RedundantQualifierInMessageMessage,
        category: Category,
        DiagnosticSeverity.Warning);

    public static DiagnosticDescriptor ShouldntMentionExceptionInMessage { get; } = Make(
        id: "R9G012",
        title: Resources.ShouldntMentionExceptionInMessageTitle,
        messageFormat: Resources.ShouldntMentionExceptionInMessageMessage,
        category: Category,
        DiagnosticSeverity.Warning);

    public static DiagnosticDescriptor TemplateHasNoCorrespondingArgument { get; } = Make(
        id: "R9G013",
        title: Resources.TemplateHasNoCorrespondingArgumentTitle,
        messageFormat: Resources.TemplateHasNoCorrespondingArgumentMessage,
        category: Category);

    public static DiagnosticDescriptor ArgumentHasNoCorrespondingTemplate { get; } = Make(
        id: "R9G014",
        title: Resources.ArgumentHasNoCorrespondingTemplateTitle,
        messageFormat: Resources.ArgumentHasNoCorrespondingTemplateMessage,
        category: Category,
        DiagnosticSeverity.Info);

    public static DiagnosticDescriptor LoggingMethodHasBody { get; } = Make(
        id: "R9G015",
        title: Resources.LoggingMethodHasBodyTitle,
        messageFormat: Resources.LoggingMethodHasBodyMessage,
        category: Category);

    public static DiagnosticDescriptor MissingLogLevel { get; } = Make(
        id: "R9G016",
        title: Resources.MissingLogLevelTitle,
        messageFormat: Resources.MissingLogLevelMessage,
        category: Category);

    public static DiagnosticDescriptor ShouldntMentionLoggerInMessage { get; } = Make(
        id: "R9G017",
        title: Resources.ShouldntMentionLoggerInMessageTitle,
        messageFormat: Resources.ShouldntMentionLoggerInMessageMessage,
        category: Category,
        DiagnosticSeverity.Warning);

    public static DiagnosticDescriptor MissingLoggerField { get; } = Make(
        id: "R9G018",
        title: Resources.MissingLoggerFieldTitle,
        messageFormat: Resources.MissingLoggerFieldMessage,
        category: Category);

    public static DiagnosticDescriptor MultipleLoggerFields { get; } = Make(
        id: "R9G019",
        title: Resources.MultipleLoggerFieldsTitle,
        messageFormat: Resources.MultipleLoggerFieldsMessage,
        category: Category);

    public static DiagnosticDescriptor MultipleDataClassificationAttributes { get; } = Make(
        id: "R9G020",
        title: Resources.MultipleDataClassificationAttributesTitle,
        messageFormat: Resources.MultipleDataClassificationAttributesMessage,
        category: Category);

    public static DiagnosticDescriptor MissingRedactorProviderArgument { get; } = Make(
        id: "R9G021",
        title: Resources.MissingRedactorProviderArgumentTitle,
        messageFormat: Resources.MissingRedactorProviderArgumentMessage,
        category: Category);

    public static DiagnosticDescriptor MissingDataClassificationArgument { get; } = Make(
        id: "R9G022",
        title: Resources.MissingDataClassificationArgumentTitle,
        messageFormat: Resources.MissingDataClassificationArgumentMessage,
        category: Category,
        DiagnosticSeverity.Warning);

    public static DiagnosticDescriptor MissingRedactorProviderField { get; } = Make(
        id: "R9G023",
        title: Resources.MissingRedactorProviderFieldTitle,
        messageFormat: Resources.MissingRedactorProviderFieldMessage,
        category: Category);

    public static DiagnosticDescriptor MultipleRedactorProviderFields { get; } = Make(
        id: "R9G024",
        title: Resources.MultipleRedactorProviderFieldsTitle,
        messageFormat: Resources.MultipleRedactorProviderFieldsMessage,
        category: Category);

    public static DiagnosticDescriptor InvalidTypeToLogProperties { get; } = Make(
        id: "R9G025",
        title: Resources.InvalidTypeToLogPropertiesTitle,
        messageFormat: Resources.InvalidTypeToLogPropertiesMessage,
        category: Category,
        DiagnosticSeverity.Warning);

    // Skipping R9G026

    public static DiagnosticDescriptor LogPropertiesInvalidUsage { get; } = Make(
        id: "R9G027",
        title: Resources.LogPropertiesInvalidUsageTitle,
        messageFormat: Resources.LogPropertiesInvalidUsageMessage,
        category: Category);

    public static DiagnosticDescriptor LogPropertiesParameterSkipped { get; } = Make(
        id: "R9G028",
        title: Resources.LogPropertiesParameterSkippedTitle,
        messageFormat: Resources.LogPropertiesParameterSkippedMessage,
        category: Category,
        DiagnosticSeverity.Warning);

    public static DiagnosticDescriptor LogPropertiesCycleDetected { get; } = Make(
        id: "R9G029",
        title: Resources.LogPropertiesCycleDetectedTitle,
        messageFormat: Resources.LogPropertiesCycleDetectedMessage,
        category: Category);

    // Skipping R9G030
    // Skipping R9G031

    public static DiagnosticDescriptor LogPropertiesProviderMethodNotFound { get; } = Make(
        id: "R9G032",
        title: Resources.LogPropertiesProviderMethodNotFoundTitle,
        messageFormat: Resources.LogPropertiesProviderMethodNotFoundMessage,
        category: Category);

    // Skipping R9G033

    public static DiagnosticDescriptor LogPropertiesProviderMethodInaccessible { get; } = Make(
        id: "R9G034",
        title: Resources.LogPropertiesProviderMethodInaccessibleTitle,
        messageFormat: Resources.LogPropertiesProviderMethodInaccessibleMessage,
        category: Category);

    public static DiagnosticDescriptor LogPropertiesProviderMethodInvalidSignature { get; } = Make(
        id: "R9G035",
        title: Resources.LogPropertiesProviderMethodInvalidSignatureTitle,
        messageFormat: Resources.LogPropertiesProviderMethodInvalidSignatureMessage,
        category: Category);

    // Skipping R9G036
    // Skipping R9G037

    public static DiagnosticDescriptor LoggingMethodParameterRefKind { get; } = Make(
        id: "R9G038",
        title: Resources.LoggingMethodParameterRefKindTitle,
        messageFormat: Resources.LoggingMethodParameterRefKindMessage,
        category: Category);

    public static DiagnosticDescriptor LogPropertiesProviderWithRedaction { get; } = Make(
        id: "R9G039",
        title: Resources.LogPropertiesProviderWithRedactionTitle,
        messageFormat: Resources.LogPropertiesProviderWithRedactionMessage,
        category: Category,
        DiagnosticSeverity.Warning);

    public static DiagnosticDescriptor ShouldntReuseEventNames { get; } = Make(
        id: "R9G040",
        title: Resources.ShouldntReuseEventNamesTitle,
        messageFormat: Resources.ShouldntReuseEventNamesMessage,
        category: Category,
        DiagnosticSeverity.Warning);

    // R9G041 is no longer in use

    public static DiagnosticDescriptor LogPropertiesHiddenPropertyDetected { get; } = Make(
        id: "R9G042",
        title: Resources.LogPropertiesHiddenPropertyDetectedTitle,
        messageFormat: Resources.LogPropertiesHiddenPropertyDetectedMessage,
        category: Category);

    public static DiagnosticDescriptor LogPropertiesNameCollision { get; } = Make(
        id: "R9G043",
        title: Resources.LogPropertiesNameCollisionTitle,
        messageFormat: Resources.LogPropertiesNameCollisionMessage,
        category: Category);

    public static DiagnosticDescriptor EmptyLoggingMethod { get; } = Make(
        id: "R9G044",
        title: Resources.EmptyLoggingMethodTitle,
        messageFormat: Resources.EmptyLoggingMethodMessage,
        category: Category);
}
