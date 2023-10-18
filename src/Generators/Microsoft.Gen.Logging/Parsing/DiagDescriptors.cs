// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.Gen.Shared;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Gen.Logging.Parsing;

internal sealed class DiagDescriptors : DiagDescriptorsBase
{
    private const string Category = nameof(DiagnosticIds.LoggerMessage);

    public static DiagnosticDescriptor ShouldntMentionLogLevelInMessage { get; } = Make(
        id: DiagnosticIds.LoggerMessage.LOGGEN000,
        title: Resources.ShouldntMentionLogLevelInMessageTitle,
        messageFormat: Resources.ShouldntMentionLogLevelInMessageMessage,
        category: Category,
        DiagnosticSeverity.Warning);

    public static DiagnosticDescriptor MissingRequiredType { get; } = Make(
        id: DiagnosticIds.LoggerMessage.LOGGEN001,
        title: Resources.MissingRequiredTypeTitle,
        messageFormat: Resources.MissingRequiredTypeMessage,
        category: Category);

    public static DiagnosticDescriptor ShouldntReuseEventIds { get; } = Make(
        id: DiagnosticIds.LoggerMessage.LOGGEN002,
        title: Resources.ShouldntReuseEventIdsTitle,
        messageFormat: Resources.ShouldntReuseEventIdsMessage,
        category: Category,
        DiagnosticSeverity.Warning);

    public static DiagnosticDescriptor LoggingMethodMustReturnVoid { get; } = Make(
        id: DiagnosticIds.LoggerMessage.LOGGEN003,
        title: Resources.LoggingMethodMustReturnVoidTitle,
        messageFormat: Resources.LoggingMethodMustReturnVoidMessage,
        category: Category);

    public static DiagnosticDescriptor MissingLoggerParameter { get; } = Make(
        id: DiagnosticIds.LoggerMessage.LOGGEN004,
        title: Resources.MissingLoggerParameterTitle,
        messageFormat: Resources.MissingLoggerParameterMessage,
        category: Category);

    public static DiagnosticDescriptor LoggingMethodShouldBeStatic { get; } = Make(
        id: DiagnosticIds.LoggerMessage.LOGGEN005,
        title: Resources.LoggingMethodShouldBeStaticTitle,
        messageFormat: Resources.LoggingMethodShouldBeStaticMessage,
        category: Category,
        DiagnosticSeverity.Warning);

    public static DiagnosticDescriptor LoggingMethodMustBePartial { get; } = Make(
        id: DiagnosticIds.LoggerMessage.LOGGEN006,
        title: Resources.LoggingMethodMustBePartialTitle,
        messageFormat: Resources.LoggingMethodMustBePartialMessage,
        category: Category);

    public static DiagnosticDescriptor LoggingMethodIsGeneric { get; } = Make(
        id: DiagnosticIds.LoggerMessage.LOGGEN007,
        title: Resources.LoggingMethodIsGenericTitle,
        messageFormat: Resources.LoggingMethodIsGenericMessage,
        category: Category);

    public static DiagnosticDescriptor RedundantQualifierInMessage { get; } = Make(
        id: DiagnosticIds.LoggerMessage.LOGGEN008,
        title: Resources.RedundantQualifierInMessageTitle,
        messageFormat: Resources.RedundantQualifierInMessageMessage,
        category: Category,
        DiagnosticSeverity.Warning);

    public static DiagnosticDescriptor ShouldntMentionExceptionInMessage { get; } = Make(
        id: DiagnosticIds.LoggerMessage.LOGGEN009,
        title: Resources.ShouldntMentionExceptionInMessageTitle,
        messageFormat: Resources.ShouldntMentionExceptionInMessageMessage,
        category: Category,
        DiagnosticSeverity.Warning);

    public static DiagnosticDescriptor TemplateHasNoCorrespondingParameter { get; } = Make(
        id: DiagnosticIds.LoggerMessage.LOGGEN010,
        title: Resources.TemplateHasNoCorrespondingParameterTitle,
        messageFormat: Resources.TemplateHasNoCorrespondingParameterMessage,
        category: Category);

    public static DiagnosticDescriptor ParameterHasNoCorrespondingTemplate { get; } = Make(
        id: DiagnosticIds.LoggerMessage.LOGGEN011,
        title: Resources.ParameterHasNoCorrespondingTemplateTitle,
        messageFormat: Resources.ParameterHasNoCorrespondingTemplateMessage,
        category: Category,
        DiagnosticSeverity.Info);

    public static DiagnosticDescriptor LoggingMethodHasBody { get; } = Make(
        id: DiagnosticIds.LoggerMessage.LOGGEN012,
        title: Resources.LoggingMethodHasBodyTitle,
        messageFormat: Resources.LoggingMethodHasBodyMessage,
        category: Category);

    public static DiagnosticDescriptor MissingLogLevel { get; } = Make(
        id: DiagnosticIds.LoggerMessage.LOGGEN013,
        title: Resources.MissingLogLevelTitle,
        messageFormat: Resources.MissingLogLevelMessage,
        category: Category);

    public static DiagnosticDescriptor ShouldntMentionLoggerInMessage { get; } = Make(
        id: DiagnosticIds.LoggerMessage.LOGGEN014,
        title: Resources.ShouldntMentionLoggerInMessageTitle,
        messageFormat: Resources.ShouldntMentionLoggerInMessageMessage,
        category: Category,
        DiagnosticSeverity.Warning);

    public static DiagnosticDescriptor MissingLoggerField { get; } = Make(
        id: DiagnosticIds.LoggerMessage.LOGGEN015,
        title: Resources.MissingLoggerFieldTitle,
        messageFormat: Resources.MissingLoggerFieldMessage,
        category: Category);

    public static DiagnosticDescriptor MultipleLoggerFields { get; } = Make(
        id: DiagnosticIds.LoggerMessage.LOGGEN016,
        title: Resources.MultipleLoggerFieldsTitle,
        messageFormat: Resources.MultipleLoggerFieldsMessage,
        category: Category);

    public static DiagnosticDescriptor CantUseDataClassificationWithLogPropertiesOrTagProvider { get; } = Make(
        id: DiagnosticIds.LoggerMessage.LOGGEN017,
        title: Resources.CantUseDataClassificationWithLogPropertiesOrTagProviderTitle,
        messageFormat: Resources.CantUseDataClassificationWithLogPropertiesOrTagProviderMessage,
        category: Category);

    public static DiagnosticDescriptor InvalidTypeToLogProperties { get; } = Make(
        id: DiagnosticIds.LoggerMessage.LOGGEN018,
        title: Resources.InvalidTypeToLogPropertiesTitle,
        messageFormat: Resources.InvalidTypeToLogPropertiesMessage,
        category: Category,
        DiagnosticSeverity.Warning);

    public static DiagnosticDescriptor LogPropertiesInvalidUsage { get; } = Make(
        id: DiagnosticIds.LoggerMessage.LOGGEN019,
        title: Resources.LogPropertiesInvalidUsageTitle,
        messageFormat: Resources.LogPropertiesInvalidUsageMessage,
        category: Category);

    public static DiagnosticDescriptor LogPropertiesParameterSkipped { get; } = Make(
        id: DiagnosticIds.LoggerMessage.LOGGEN020,
        title: Resources.LogPropertiesParameterSkippedTitle,
        messageFormat: Resources.LogPropertiesParameterSkippedMessage,
        category: Category,
        DiagnosticSeverity.Warning);

    public static DiagnosticDescriptor LogPropertiesCycleDetected { get; } = Make(
        id: DiagnosticIds.LoggerMessage.LOGGEN021,
        title: Resources.LogPropertiesCycleDetectedTitle,
        messageFormat: Resources.LogPropertiesCycleDetectedMessage,
        category: Category);

    public static DiagnosticDescriptor TagProviderMethodNotFound { get; } = Make(
        id: DiagnosticIds.LoggerMessage.LOGGEN022,
        title: Resources.TagProviderMethodNotFoundTitle,
        messageFormat: Resources.TagProviderMethodNotFoundMessage,
        category: Category);

    public static DiagnosticDescriptor TagProviderMethodInaccessible { get; } = Make(
        id: DiagnosticIds.LoggerMessage.LOGGEN023,
        title: Resources.TagProviderMethodInaccessibleTitle,
        messageFormat: Resources.TagProviderMethodInaccessibleMessage,
        category: Category);

    public static DiagnosticDescriptor TagProviderMethodInvalidSignature { get; } = Make(
        id: DiagnosticIds.LoggerMessage.LOGGEN024,
        title: Resources.TagProviderMethodInvalidSignatureTitle,
        messageFormat: Resources.TagProviderMethodInvalidSignatureMessage,
        category: Category);

    public static DiagnosticDescriptor LoggingMethodParameterRefKind { get; } = Make(
        id: DiagnosticIds.LoggerMessage.LOGGEN025,
        title: Resources.LoggingMethodParameterRefKindTitle,
        messageFormat: Resources.LoggingMethodParameterRefKindMessage,
        category: Category);

    public static DiagnosticDescriptor LogPropertiesProviderWithRedaction { get; } = Make(
        id: DiagnosticIds.LoggerMessage.LOGGEN026,
        title: Resources.TagProviderWithRedactionTitle,
        messageFormat: Resources.TagProviderWithRedactionMessage,
        category: Category,
        DiagnosticSeverity.Warning);

    public static DiagnosticDescriptor ShouldntReuseEventNames { get; } = Make(
        id: DiagnosticIds.LoggerMessage.LOGGEN027,
        title: Resources.ShouldntReuseEventNamesTitle,
        messageFormat: Resources.ShouldntReuseEventNamesMessage,
        category: Category,
        DiagnosticSeverity.Warning);

    public static DiagnosticDescriptor LogPropertiesHiddenPropertyDetected { get; } = Make(
        id: DiagnosticIds.LoggerMessage.LOGGEN028,
        title: Resources.LogPropertiesHiddenPropertyDetectedTitle,
        messageFormat: Resources.LogPropertiesHiddenPropertyDetectedMessage,
        category: Category);

    public static DiagnosticDescriptor LogPropertiesNameCollision { get; } = Make(
        id: DiagnosticIds.LoggerMessage.LOGGEN029,
        title: Resources.LogPropertiesNameCollisionTitle,
        messageFormat: Resources.LogPropertiesNameCollisionMessage,
        category: Category);

    public static DiagnosticDescriptor EmptyLoggingMethod { get; } = Make(
        id: DiagnosticIds.LoggerMessage.LOGGEN030,
        title: Resources.EmptyLoggingMethodTitle,
        messageFormat: Resources.EmptyLoggingMethodMessage,
        category: Category);

    public static DiagnosticDescriptor TemplateStartsWithAtSymbol { get; } = Make(
        id: DiagnosticIds.LoggerMessage.LOGGEN031,
        title: Resources.TemplateStartsWithAtSymbolTitle,
        messageFormat: Resources.TemplateStartsWithAtSymbolMessage,
        category: Category);

    public static DiagnosticDescriptor CantMixAttributes { get; } = Make(
        id: DiagnosticIds.LoggerMessage.LOGGEN032,
        title: Resources.CantMixAttributesTitle,
        messageFormat: Resources.CantMixAttributesMessage,
        category: Category);

    public static DiagnosticDescriptor TagProviderInvalidUsage { get; } = Make(
        id: DiagnosticIds.LoggerMessage.LOGGEN033,
        title: Resources.TagProviderInvalidUsageTitle,
        messageFormat: Resources.TagProviderInvalidUsageMessage,
        category: Category);

    public static DiagnosticDescriptor InvalidAttributeUsage { get; } = Make(
        id: DiagnosticIds.LoggerMessage.LOGGEN034,
        title: Resources.InvalidAttributeUsageTitle,
        messageFormat: Resources.InvalidAttributeUsageMessage,
        category: Category);
}
