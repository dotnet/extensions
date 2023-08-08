// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.Gen.Shared;

namespace Microsoft.Gen.Logging.Parsing;

internal sealed class DiagDescriptors : DiagDescriptorsBase
{
    private const string Category = "LogMethod";

    public static DiagnosticDescriptor ShouldntMentionLogLevelInMessage { get; } = Make(
        id: "LOGGEN000",
        title: Resources.ShouldntMentionLogLevelInMessageTitle,
        messageFormat: Resources.ShouldntMentionLogLevelInMessageMessage,
        category: Category,
        DiagnosticSeverity.Warning);

    public static DiagnosticDescriptor MissingRequiredType { get; } = Make(
        id: "LOGGEN001",
        title: Resources.MissingRequiredTypeTitle,
        messageFormat: Resources.MissingRequiredTypeMessage,
        category: Category);

    public static DiagnosticDescriptor ShouldntReuseEventIds { get; } = Make(
        id: "LOGGEN002",
        title: Resources.ShouldntReuseEventIdsTitle,
        messageFormat: Resources.ShouldntReuseEventIdsMessage,
        category: Category,
        DiagnosticSeverity.Warning);

    public static DiagnosticDescriptor LoggingMethodMustReturnVoid { get; } = Make(
        id: "LOGGEN003",
        title: Resources.LoggingMethodMustReturnVoidTitle,
        messageFormat: Resources.LoggingMethodMustReturnVoidMessage,
        category: Category);

    public static DiagnosticDescriptor MissingLoggerParameter { get; } = Make(
        id: "LOGGEN004",
        title: Resources.MissingLoggerParameterTitle,
        messageFormat: Resources.MissingLoggerParameterMessage,
        category: Category);

    public static DiagnosticDescriptor LoggingMethodShouldBeStatic { get; } = Make(
        id: "LOGGEN005",
        title: Resources.LoggingMethodShouldBeStaticTitle,
        messageFormat: Resources.LoggingMethodShouldBeStaticMessage,
        category: Category,
        DiagnosticSeverity.Warning);

    public static DiagnosticDescriptor LoggingMethodMustBePartial { get; } = Make(
        id: "LOGGEN006",
        title: Resources.LoggingMethodMustBePartialTitle,
        messageFormat: Resources.LoggingMethodMustBePartialMessage,
        category: Category);

    public static DiagnosticDescriptor LoggingMethodIsGeneric { get; } = Make(
        id: "LOGGEN007",
        title: Resources.LoggingMethodIsGenericTitle,
        messageFormat: Resources.LoggingMethodIsGenericMessage,
        category: Category);

    public static DiagnosticDescriptor RedundantQualifierInMessage { get; } = Make(
        id: "LOGGEN008",
        title: Resources.RedundantQualifierInMessageTitle,
        messageFormat: Resources.RedundantQualifierInMessageMessage,
        category: Category,
        DiagnosticSeverity.Warning);

    public static DiagnosticDescriptor ShouldntMentionExceptionInMessage { get; } = Make(
        id: "LOGGEN009",
        title: Resources.ShouldntMentionExceptionInMessageTitle,
        messageFormat: Resources.ShouldntMentionExceptionInMessageMessage,
        category: Category,
        DiagnosticSeverity.Warning);

    public static DiagnosticDescriptor TemplateHasNoCorrespondingParameter { get; } = Make(
        id: "LOGGEN010",
        title: Resources.TemplateHasNoCorrespondingParameterTitle,
        messageFormat: Resources.TemplateHasNoCorrespondingParameterMessage,
        category: Category);

    public static DiagnosticDescriptor ParameterHasNoCorrespondingTemplate { get; } = Make(
        id: "LOGGEN011",
        title: Resources.ParameterHasNoCorrespondingTemplateTitle,
        messageFormat: Resources.ParameterHasNoCorrespondingTemplateMessage,
        category: Category,
        DiagnosticSeverity.Info);

    public static DiagnosticDescriptor LoggingMethodHasBody { get; } = Make(
        id: "LOGGEN012",
        title: Resources.LoggingMethodHasBodyTitle,
        messageFormat: Resources.LoggingMethodHasBodyMessage,
        category: Category);

    public static DiagnosticDescriptor MissingLogLevel { get; } = Make(
        id: "LOGGEN013",
        title: Resources.MissingLogLevelTitle,
        messageFormat: Resources.MissingLogLevelMessage,
        category: Category);

    public static DiagnosticDescriptor ShouldntMentionLoggerInMessage { get; } = Make(
        id: "LOGGEN014",
        title: Resources.ShouldntMentionLoggerInMessageTitle,
        messageFormat: Resources.ShouldntMentionLoggerInMessageMessage,
        category: Category,
        DiagnosticSeverity.Warning);

    public static DiagnosticDescriptor MissingLoggerField { get; } = Make(
        id: "LOGGEN015",
        title: Resources.MissingLoggerFieldTitle,
        messageFormat: Resources.MissingLoggerFieldMessage,
        category: Category);

    public static DiagnosticDescriptor MultipleLoggerFields { get; } = Make(
        id: "LOGGEN016",
        title: Resources.MultipleLoggerFieldsTitle,
        messageFormat: Resources.MultipleLoggerFieldsMessage,
        category: Category);

    public static DiagnosticDescriptor MultipleDataClassificationAttributes { get; } = Make(
        id: "LOGGEN017",
        title: Resources.MultipleDataClassificationAttributesTitle,
        messageFormat: Resources.MultipleDataClassificationAttributesMessage,
        category: Category);

    public static DiagnosticDescriptor InvalidTypeToLogProperties { get; } = Make(
        id: "LOGGEN018",
        title: Resources.InvalidTypeToLogPropertiesTitle,
        messageFormat: Resources.InvalidTypeToLogPropertiesMessage,
        category: Category,
        DiagnosticSeverity.Warning);

    public static DiagnosticDescriptor LogPropertiesInvalidUsage { get; } = Make(
        id: "LOGGEN019",
        title: Resources.LogPropertiesInvalidUsageTitle,
        messageFormat: Resources.LogPropertiesInvalidUsageMessage,
        category: Category);

    public static DiagnosticDescriptor LogPropertiesParameterSkipped { get; } = Make(
        id: "LOGGEN020",
        title: Resources.LogPropertiesParameterSkippedTitle,
        messageFormat: Resources.LogPropertiesParameterSkippedMessage,
        category: Category,
        DiagnosticSeverity.Warning);

    public static DiagnosticDescriptor LogPropertiesCycleDetected { get; } = Make(
        id: "LOGGEN021",
        title: Resources.LogPropertiesCycleDetectedTitle,
        messageFormat: Resources.LogPropertiesCycleDetectedMessage,
        category: Category);

    public static DiagnosticDescriptor TagProviderMethodNotFound { get; } = Make(
        id: "LOGGEN022",
        title: Resources.TagProviderMethodNotFoundTitle,
        messageFormat: Resources.TagProviderMethodNotFoundMessage,
        category: Category);

    public static DiagnosticDescriptor TagProviderMethodInaccessible { get; } = Make(
        id: "LOGGEN023",
        title: Resources.TagProviderMethodInaccessibleTitle,
        messageFormat: Resources.TagProviderMethodInaccessibleMessage,
        category: Category);

    public static DiagnosticDescriptor TagProviderMethodInvalidSignature { get; } = Make(
        id: "LOGGEN024",
        title: Resources.TagProviderMethodInvalidSignatureTitle,
        messageFormat: Resources.TagProviderMethodInvalidSignatureMessage,
        category: Category);

    public static DiagnosticDescriptor LoggingMethodParameterRefKind { get; } = Make(
        id: "LOGGEN025",
        title: Resources.LoggingMethodParameterRefKindTitle,
        messageFormat: Resources.LoggingMethodParameterRefKindMessage,
        category: Category);

    public static DiagnosticDescriptor LogPropertiesProviderWithRedaction { get; } = Make(
        id: "LOGGEN026",
        title: Resources.TagProviderWithRedactionTitle,
        messageFormat: Resources.TagProviderWithRedactionMessage,
        category: Category,
        DiagnosticSeverity.Warning);

    public static DiagnosticDescriptor ShouldntReuseEventNames { get; } = Make(
        id: "LOGGEN027",
        title: Resources.ShouldntReuseEventNamesTitle,
        messageFormat: Resources.ShouldntReuseEventNamesMessage,
        category: Category,
        DiagnosticSeverity.Warning);

    public static DiagnosticDescriptor LogPropertiesHiddenPropertyDetected { get; } = Make(
        id: "LOGGEN028",
        title: Resources.LogPropertiesHiddenPropertyDetectedTitle,
        messageFormat: Resources.LogPropertiesHiddenPropertyDetectedMessage,
        category: Category);

    public static DiagnosticDescriptor LogPropertiesNameCollision { get; } = Make(
        id: "LOGGEN029",
        title: Resources.LogPropertiesNameCollisionTitle,
        messageFormat: Resources.LogPropertiesNameCollisionMessage,
        category: Category);

    public static DiagnosticDescriptor EmptyLoggingMethod { get; } = Make(
        id: "LOGGEN030",
        title: Resources.EmptyLoggingMethodTitle,
        messageFormat: Resources.EmptyLoggingMethodMessage,
        category: Category);

    public static DiagnosticDescriptor TemplateStartsWithAtSymbol { get; } = Make(
        id: "LOGGEN031",
        title: Resources.TemplateStartsWithAtSymbolTitle,
        messageFormat: Resources.TemplateStartsWithAtSymbolMessage,
        category: Category);
}
