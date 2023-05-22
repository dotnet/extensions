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

    public static DiagnosticDescriptor LegacyLogging { get; } = new(
        id: "R9A000",
        messageFormat: Resources.LegacyLoggingMessage,
        title: Resources.LegacyLoggingTitle,
        category: Performance,
        description: Resources.LegacyLoggingDescription,
        defaultSeverity: DiagnosticSeverity.Warning,
        helpLinkUri: "https://eng.ms/docs/experiences-devices/r9-sdk/docs/static-analysis/analyzers/r9a000",
        isEnabledByDefault: true);

    public static DiagnosticDescriptor MemoryStream { get; } = new(
        id: "R9A001",
        messageFormat: Resources.MemoryStreamMessage,
        title: Resources.MemoryStreamTitle,
        category: Performance,
        description: Resources.MemoryStreamDescription,
        defaultSeverity: DiagnosticSeverity.Warning,
        helpLinkUri: "https://eng.ms/docs/experiences-devices/r9-sdk/docs/static-analysis/analyzers/r9a001",
        isEnabledByDefault: true);

    // R9A002 is no longer in use

    public static DiagnosticDescriptor DistributedCache { get; } = new(
        id: "R9A003",
        messageFormat: Resources.DistributedCacheMessage,
        title: Resources.DistributedCacheTitle,
        category: Performance,
        description: Resources.DistributedCacheDescription,
        defaultSeverity: DiagnosticSeverity.Warning,
        helpLinkUri: "https://eng.ms/docs/experiences-devices/r9-sdk/docs/static-analysis/analyzers/r9a003",
        isEnabledByDefault: true);

    // R9A004: retired

    public static DiagnosticDescriptor UsingMicrosoftExtensionsCachingRedis { get; } = new(
        id: "R9A005",
        messageFormat: Resources.UsingMicrosoftExtensionsCachingRedisMessage,
        title: Resources.UsingMicrosoftExtensionsCachingRedisTitle,
        category: Resilience,
        description: Resources.UsingMicrosoftExtensionsCachingRedisDescription,
        defaultSeverity: DiagnosticSeverity.Warning,
        helpLinkUri: "https://eng.ms/docs/experiences-devices/r9-sdk/docs/static-analysis/analyzers/r9a005",
        isEnabledByDefault: true);

    public static DiagnosticDescriptor UpdateReturnType { get; } = new(
        id: "R9A006",
        messageFormat: Resources.UpdateReturnTypeMessage,
        title: Resources.UpdateReturnTypeTitle,
        category: Correctness,
        description: Resources.UsingMetricMethodDescription,
        defaultSeverity: DiagnosticSeverity.Error,
        helpLinkUri: "https://eng.ms/docs/experiences-devices/r9-sdk/docs/static-analysis/analyzers/r9a006",
        isEnabledByDefault: true);

    public static DiagnosticDescriptor RemoveMethodBody { get; } = new(
        id: "R9A007",
        messageFormat: Resources.RemoveMethodBodyMessage,
        title: Resources.RemoveMethodBodyTitle,
        category: Correctness,
        description: Resources.UsingMetricMethodDescription,
        defaultSeverity: DiagnosticSeverity.Error,
        helpLinkUri: "https://eng.ms/docs/experiences-devices/r9-sdk/docs/static-analysis/analyzers/r9a007",
        isEnabledByDefault: true);

    public static DiagnosticDescriptor AddMissingMeter { get; } = new(
        id: "R9A008",
        messageFormat: Resources.AddMissingMeterMessage,
        title: Resources.AddMissingMeterTitle,
        category: Correctness,
        description: Resources.UsingMetricMethodDescription,
        defaultSeverity: DiagnosticSeverity.Error,
        helpLinkUri: "https://eng.ms/docs/experiences-devices/r9-sdk/docs/static-analysis/analyzers/r9a008",
        isEnabledByDefault: true);

    public static DiagnosticDescriptor UpdateDimensionParamTypes { get; } = new(
        id: "R9A009",
        messageFormat: Resources.UpdateDimensionParamTypesMessage,
        title: Resources.UpdateDimensionParamTypesTitle,
        category: Correctness,
        description: Resources.UsingMetricMethodDescription,
        defaultSeverity: DiagnosticSeverity.Error,
        helpLinkUri: "https://eng.ms/docs/experiences-devices/r9-sdk/docs/static-analysis/analyzers/r9a009",
        isEnabledByDefault: true);

    public static DiagnosticDescriptor StaticMethodDeclaration { get; } = new(
        id: "R9A010",
        messageFormat: Resources.StaticMethodDeclarationMessage,
        title: Resources.StaticMethodDeclarationTitle,
        category: Correctness,
        description: Resources.UsingMetricMethodDescription,
        defaultSeverity: DiagnosticSeverity.Error,
        helpLinkUri: "https://eng.ms/docs/experiences-devices/r9-sdk/docs/static-analysis/analyzers/r9a010",
        isEnabledByDefault: true);

    public static DiagnosticDescriptor PartialMethodDeclaration { get; } = new(
        id: "R9A011",
        messageFormat: Resources.PartialMethodDeclarationMessage,
        title: Resources.PartialMethodDeclarationTitle,
        category: Correctness,
        description: Resources.UsingMetricMethodDescription,
        defaultSeverity: DiagnosticSeverity.Error,
        helpLinkUri: "https://eng.ms/docs/experiences-devices/r9-sdk/docs/static-analysis/analyzers/r9a011",
        isEnabledByDefault: true);

    public static DiagnosticDescriptor PublicMethodDeclaration { get; } = new(
        id: "R9A012",
        messageFormat: Resources.PublicMethodDeclarationMessage,
        title: Resources.PublicMethodDeclarationTitle,
        category: Correctness,
        description: Resources.UsingMetricMethodDescription,
        defaultSeverity: DiagnosticSeverity.Warning,
        helpLinkUri: "https://eng.ms/docs/experiences-devices/r9-sdk/docs/static-analysis/analyzers/r9a012",
        isEnabledByDefault: true);

    public static DiagnosticDescriptor SealInternalClass { get; } = new(
        id: "R9A013",
        messageFormat: Resources.SealInternalClassMessage,
        title: Resources.SealInternalClassTitle,
        category: Performance,
        description: Resources.SealInternalClassDescription,
        defaultSeverity: DiagnosticSeverity.Warning,
        helpLinkUri: "https://eng.ms/docs/experiences-devices/r9-sdk/docs/static-analysis/analyzers/r9a013",
        isEnabledByDefault: true);

    public static DiagnosticDescriptor ThrowsExpression { get; } = new(
        id: "R9A014",
        messageFormat: Resources.ThrowsExpressionMessage,
        title: Resources.ThrowsExpressionTitle,
        category: Performance,
        description: Resources.ThrowsExpressionDescription,
        defaultSeverity: DiagnosticSeverity.Warning,
        helpLinkUri: "https://eng.ms/docs/experiences-devices/r9-sdk/docs/static-analysis/analyzers/r9a014",
        isEnabledByDefault: true);

    public static DiagnosticDescriptor ThrowsStatement { get; } = new(
        id: "R9A015",
        messageFormat: Resources.ThrowsStatementMessage,
        title: Resources.ThrowsStatementTitle,
        category: Performance,
        description: Resources.ThrowsStatementDescription,
        defaultSeverity: DiagnosticSeverity.Warning,
        helpLinkUri: "https://eng.ms/docs/experiences-devices/r9-sdk/docs/static-analysis/analyzers/r9a015",
        isEnabledByDefault: true);

    // R9A016 has been retired

    public static DiagnosticDescriptor BlockingCall { get; } = new(
        id: "R9A017",
        messageFormat: Resources.BlockingCallMessage,
        title: Resources.BlockingCallTitle,
        category: Performance,
        description: Resources.BlockingCallDescription,
        defaultSeverity: DiagnosticSeverity.Info,
        helpLinkUri: "https://eng.ms/docs/experiences-devices/r9-sdk/docs/static-analysis/analyzers/r9a017",
        isEnabledByDefault: true);

    public static DiagnosticDescriptor StringFormat { get; } = new(
        id: "R9A018",
        messageFormat: Resources.StringFormatMessage,
        title: Resources.StringFormatTitle,
        category: Performance,
        description: Resources.StringFormatDescription,
        defaultSeverity: DiagnosticSeverity.Warning,
        helpLinkUri: "https://eng.ms/docs/experiences-devices/r9-sdk/docs/static-analysis/analyzers/r9a018",
        isEnabledByDefault: true);

    public static DiagnosticDescriptor UsingExcessiveDictionaryLookup { get; } = new(
        id: "R9A019",
        messageFormat: Resources.UsingExcessiveDictionaryLookupMessage,
        title: Resources.UsingExcessiveDictionaryLookupTitle,
        category: Performance,
        description: Resources.UsingExcessiveDictionaryLookupDescription,
        defaultSeverity: DiagnosticSeverity.Warning,
        helpLinkUri: "https://eng.ms/docs/experiences-devices/r9-sdk/docs/static-analysis/analyzers/r9a019",
        isEnabledByDefault: true);

    public static DiagnosticDescriptor UsingExcessiveSetLookup { get; } = new(
        id: "R9A020",
        messageFormat: Resources.UsingExcessiveSetLookupMessage,
        title: Resources.UsingExcessiveSetLookupTitle,
        category: Performance,
        description: Resources.UsingExcessiveSetLookupDescription,
        defaultSeverity: DiagnosticSeverity.Warning,
        helpLinkUri: "https://eng.ms/docs/experiences-devices/r9-sdk/docs/static-analysis/analyzers/r9a020",
        isEnabledByDefault: true);

    public static DiagnosticDescriptor UsingToStringInLoggers { get; } = new(
        id: "R9A021",
        messageFormat: Resources.UsingToStringInLoggersMessage,
        title: Resources.UsingToStringInLoggersTitle,
        category: "Performance",
        description: Resources.UsingToStringInLoggersDescription,
        defaultSeverity: DiagnosticSeverity.Warning,
        helpLinkUri: "https://eng.ms/docs/experiences-devices/r9-sdk/docs/static-analysis/analyzers/r9a021",
        isEnabledByDefault: true);

    public static DiagnosticDescriptor StaticTime { get; } = new(
        id: "R9A022",
        messageFormat: Resources.StaticTimeMessage,
        title: Resources.StaticTimeTitle,
        category: Reliability,
        description: Resources.StaticTimeDescription,
        defaultSeverity: DiagnosticSeverity.Warning,
        helpLinkUri: "https://eng.ms/docs/experiences-devices/r9-sdk/docs/static-analysis/analyzers/r9a022",
        isEnabledByDefault: true);

    public static DiagnosticDescriptor Stopwatch { get; } = new(
        id: "R9A023",
        messageFormat: Resources.StopwatchMessage,
        title: Resources.StopwatchTitle,
        category: Performance,
        description: Resources.StopwatchDescription,
        defaultSeverity: DiagnosticSeverity.Warning,
        helpLinkUri: "https://eng.ms/docs/experiences-devices/r9-sdk/docs/static-analysis/analyzers/r9a023",
        isEnabledByDefault: true);

    public static DiagnosticDescriptor SensitiveDataClassifierPropagation { get; } = new(
        id: "R9A024",
        messageFormat: Resources.SensitiveDataClassifierPropagationMessage,
        title: Resources.SensitiveDataClassifierPropagationTitle,
        category: Privacy,
        description: Resources.SensitiveDataClassifierPropagationDescription,
        defaultSeverity: DiagnosticSeverity.Warning,
        helpLinkUri: "https://eng.ms/docs/experiences-devices/r9-sdk/docs/static-analysis/analyzers/r9a024",
        isEnabledByDefault: true);

    // R9A025..R9A027 unused

    public static DiagnosticDescriptor UserInputFromRequestAnalyzer { get; } = new(
        id: "R9A028",
        messageFormat: Resources.UserInputFromRequestAnalyzerMessage,
        title: Resources.UserInputFromRequestAnalyzerTitle,
        category: Privacy,
        description: Resources.UserInputFromRequestAnalyzerDescription,
        defaultSeverity: DiagnosticSeverity.Warning,
        helpLinkUri: "https://eng.ms/docs/experiences-devices/r9-sdk/docs/static-analysis/analyzers/r9a028",
        isEnabledByDefault: true);

    public static DiagnosticDescriptor UsingExperimentalApi { get; } = new(
        id: "R9A029",
        messageFormat: Resources.UsingExperimentalApiMessage,
        title: Resources.UsingExperimentalApiTitle,
        category: Reliability,
        description: Resources.UsingExperimentalApiDescription,
        defaultSeverity: DiagnosticSeverity.Warning,
        helpLinkUri: "https://eng.ms/docs/experiences-devices/r9-sdk/docs/static-analysis/analyzers/r9a029",
        isEnabledByDefault: true);

    public static DiagnosticDescriptor StartsEndsWith { get; } = new(
        id: "R9A030",
        messageFormat: Resources.StartsEndsWithMessage,
        title: Resources.StartsEndsWithTitle,
        category: Performance,
        description: Resources.StartsEndsWithDescription,
        defaultSeverity: DiagnosticSeverity.Warning,
        helpLinkUri: "https://eng.ms/docs/experiences-devices/r9-sdk/docs/static-analysis/analyzers/r9a030",
        isEnabledByDefault: true);

    public static DiagnosticDescriptor MakeExeTypesInternal { get; } = new(
        id: "R9A031",
        messageFormat: Resources.MakeExeTypesInternalMessage,
        title: Resources.MakeExeTypesInternalTitle,
        category: Performance,
        description: Resources.MakeExeTypesInternalDescription,
        defaultSeverity: DiagnosticSeverity.Warning,
        helpLinkUri: "https://eng.ms/docs/experiences-devices/r9-sdk/docs/static-analysis/analyzers/r9a031",
        isEnabledByDefault: true);

    public static DiagnosticDescriptor Arrays { get; } = new(
        id: "R9A032",
        messageFormat: Resources.ArraysMessage,
        title: Resources.ArraysTitle,
        category: Performance,
        description: Resources.ArraysDescription,
        defaultSeverity: DiagnosticSeverity.Warning,
        helpLinkUri: "https://eng.ms/docs/experiences-devices/r9-sdk/docs/static-analysis/analyzers/r9a032",
        isEnabledByDefault: true);

    public static DiagnosticDescriptor EnumStrings { get; } = new(
        id: "R9A033",
        messageFormat: Resources.EnumStringsMessage,
        title: Resources.EnumStringsTitle,
        category: Performance,
        description: Resources.EnumStringsDescription,
        defaultSeverity: DiagnosticSeverity.Warning,
        helpLinkUri: "https://eng.ms/docs/experiences-devices/r9-sdk/docs/static-analysis/analyzers/r9a033",
        isEnabledByDefault: true);

    // R9A034 deprecated
    // R9A035 deprecated

    public static DiagnosticDescriptor ToInvariantString { get; } = new(
        id: "R9A036",
        messageFormat: Resources.ToInvariantStringMessage,
        title: Resources.ToInvariantStringTitle,
        category: Performance,
        description: Resources.ToInvariantStringDescription,
        defaultSeverity: DiagnosticSeverity.Warning,
        helpLinkUri: "https://eng.ms/docs/experiences-devices/r9-sdk/docs/static-analysis/analyzers/r9a036",
        isEnabledByDefault: true);

    public static DiagnosticDescriptor ValueTuple { get; } = new(
        id: "R9A037",
        messageFormat: Resources.ValueTupleMessage,
        title: Resources.ValueTupleTitle,
        category: Performance,
        description: Resources.ValueTupleDescription,
        defaultSeverity: DiagnosticSeverity.Warning,
        helpLinkUri: "https://eng.ms/docs/experiences-devices/r9-sdk/docs/static-analysis/analyzers/r9a037",
        isEnabledByDefault: true);

    public static DiagnosticDescriptor ObjectPool { get; } = new(
        id: "R9A038",
        messageFormat: Resources.ObjectPoolMessage,
        title: Resources.ObjectPoolTitle,
        category: Performance,
        description: Resources.ObjectPoolDescription,
        defaultSeverity: DiagnosticSeverity.Warning,
        helpLinkUri: "https://eng.ms/docs/experiences-devices/r9-sdk/docs/static-analysis/analyzers/r9a038",
        isEnabledByDefault: true);

    public static DiagnosticDescriptor NullCheck { get; } = new(
        id: "R9A039",
        messageFormat: Resources.NullCheckMessage,
        title: Resources.NullCheckTitle,
        category: Performance,
        description: Resources.NullCheckDescription,
        defaultSeverity: DiagnosticSeverity.Warning,
        helpLinkUri: "https://eng.ms/docs/experiences-devices/r9-sdk/docs/static-analysis/analyzers/r9a039",
        isEnabledByDefault: true);

    public static DiagnosticDescriptor LegacyCollection { get; } = new(
        id: "R9A040",
        messageFormat: Resources.LegacyCollectionMessage,
        title: Resources.LegacyCollectionTitle,
        category: Performance,
        description: Resources.LegacyCollectionDescription,
        defaultSeverity: DiagnosticSeverity.Warning,
        helpLinkUri: "https://eng.ms/docs/experiences-devices/r9-sdk/docs/static-analysis/analyzers/r9a040",
        isEnabledByDefault: true);

    public static DiagnosticDescriptor UseConcreteTypeForField { get; } = new(
        id: "R9A041",
        messageFormat: Resources.UseConcreteTypeForFieldMessage,
        title: Resources.UseConcreteTypeTitle,
        category: Performance,
        description: Resources.UseConcreteTypeDescription,
        defaultSeverity: DiagnosticSeverity.Warning,
        helpLinkUri: "https://eng.ms/docs/experiences-devices/r9-sdk/docs/static-analysis/analyzers/r9a041",
        isEnabledByDefault: true);

    public static DiagnosticDescriptor UseConcreteTypeForParameter { get; } = new(
        id: "R9A041",
        messageFormat: Resources.UseConcreteTypeForParameterMessage,
        title: Resources.UseConcreteTypeTitle,
        category: Performance,
        description: Resources.UseConcreteTypeDescription,
        defaultSeverity: DiagnosticSeverity.Warning,
        helpLinkUri: "https://eng.ms/docs/experiences-devices/r9-sdk/docs/static-analysis/analyzers/r9a041",
        isEnabledByDefault: true);

    public static DiagnosticDescriptor UseConcreteTypeForLocal { get; } = new(
        id: "R9A041",
        messageFormat: Resources.UseConcreteTypeForLocalMessage,
        title: Resources.UseConcreteTypeTitle,
        category: Performance,
        description: Resources.UseConcreteTypeDescription,
        defaultSeverity: DiagnosticSeverity.Warning,
        helpLinkUri: "https://eng.ms/docs/experiences-devices/r9-sdk/docs/static-analysis/analyzers/r9a041",
        isEnabledByDefault: true);

    public static DiagnosticDescriptor UseConcreteTypeForMethodReturn { get; } = new(
        id: "R9A041",
        messageFormat: Resources.UseConcreteTypeForMethodReturnMessage,
        title: Resources.UseConcreteTypeTitle,
        category: Performance,
        description: Resources.UseConcreteTypeDescription,
        defaultSeverity: DiagnosticSeverity.Warning,
        helpLinkUri: "https://eng.ms/docs/experiences-devices/r9-sdk/docs/static-analysis/analyzers/r9a041",
        isEnabledByDefault: true);

    public static DiagnosticDescriptor UserDataAPIAllParametersAnnotated { get; } = new(
        id: "R9A042",
        messageFormat: Resources.UserDataAPIAllParametersAnnotatedMessage,
        title: Resources.UserDataAPIAllParametersAnnotatedTitle,
        category: Privacy,
        description: Resources.UserDataAPIAllParametersAnnotatedDescription,
        defaultSeverity: DiagnosticSeverity.Warning,
        helpLinkUri: "https://eng.ms/docs/experiences-devices/r9-sdk/docs/static-analysis/analyzers/r9a042",
        isEnabledByDefault: true);

    public static DiagnosticDescriptor Split { get; } = new(
        id: "R9A043",
        messageFormat: Resources.SplitMessage,
        title: Resources.SplitTitle,
        category: Performance,
        description: Resources.SplitDescription,
        defaultSeverity: DiagnosticSeverity.Warning,
        helpLinkUri: "https://eng.ms/docs/experiences-devices/r9-sdk/docs/static-analysis/analyzers/r9a043",
        isEnabledByDefault: true);

    public static DiagnosticDescriptor MakeArrayStatic { get; } = new(
        id: "R9A044",
        messageFormat: Resources.MakeArrayStaticMessage,
        title: Resources.MakeArrayStaticTitle,
        category: Performance,
        description: Resources.MakeArrayStaticDescription,
        defaultSeverity: DiagnosticSeverity.Warning,
        helpLinkUri: "https://eng.ms/docs/experiences-devices/r9-sdk/docs/static-analysis/analyzers/r9a044",
        isEnabledByDefault: true);

    // R9A045..R9A047 were retired

    public static DiagnosticDescriptor Any { get; } = new(
        id: "R9A048",
        messageFormat: Resources.AnyMessage,
        title: Resources.AnyTitle,
        category: Performance,
        description: Resources.AnyDescription,
        defaultSeverity: DiagnosticSeverity.Warning,
        helpLinkUri: "https://eng.ms/docs/experiences-devices/r9-sdk/docs/static-analysis/analyzers/r9a048",
        isEnabledByDefault: true);

    public static DiagnosticDescriptor NewSymbolsMustBeMarkedExperimental { get; } = new(
        id: "R9A049",
        messageFormat: Resources.NewSymbolsMustBeMarkedExperimentalMessage,
        title: Resources.NewSymbolsMustBeMarkedExperimentalTitle,
        category: Correctness,
        description: Resources.NewSymbolsMustBeMarkedExperimentalDescription,
        defaultSeverity: DiagnosticSeverity.Hidden,
        helpLinkUri: "https://eng.ms/docs/experiences-devices/r9-sdk/docs/static-analysis/analyzers/r9a049",
        isEnabledByDefault: true);

    public static DiagnosticDescriptor ExperimentalSymbolsCantBeMarkedObsolete { get; } = new(
        id: "R9A050",
        messageFormat: Resources.ExperimentalSymbolsCantBeMarkedObsoleteMessage,
        title: Resources.ExperimentalSymbolsCantBeMarkedObsoleteTitle,
        category: Correctness,
        description: Resources.ExperimentalSymbolsCantBeMarkedObsoleteDescription,
        defaultSeverity: DiagnosticSeverity.Hidden,
        helpLinkUri: "https://eng.ms/docs/experiences-devices/r9-sdk/docs/static-analysis/analyzers/r9a050",
        isEnabledByDefault: true);

    public static DiagnosticDescriptor PublishedSymbolsCantBeMarkedExperimental { get; } = new(
        id: "R9A051",
        messageFormat: Resources.PublishedSymbolsCantBeMarkedExperimentalMessage,
        title: Resources.PublishedSymbolsCantBeMarkedExperimentalTitle,
        category: Correctness,
        description: Resources.PublishedSymbolsCantBeMarkedExperimentalDescription,
        defaultSeverity: DiagnosticSeverity.Hidden,
        helpLinkUri: "https://eng.ms/docs/experiences-devices/r9-sdk/docs/static-analysis/analyzers/r9a051",
        isEnabledByDefault: true);

    public static DiagnosticDescriptor PublishedSymbolsCantBeDeleted { get; } = new(
        id: "R9A052",
        messageFormat: Resources.PublishedSymbolsCantBeDeletedMessage,
        title: Resources.PublishedSymbolsCantBeDeletedTitle,
        category: Correctness,
        description: Resources.PublishedSymbolsCantBeDeletedDescription,
        defaultSeverity: DiagnosticSeverity.Hidden,
        helpLinkUri: "https://eng.ms/docs/experiences-devices/r9-sdk/docs/static-analysis/analyzers/r9a052",
        isEnabledByDefault: true);

    // R9A053 and R9A054 were retired

    public static DiagnosticDescriptor PublishedSymbolsCantChange { get; } = new(
        id: "R9A055",
        messageFormat: Resources.PublishedSymbolsCantChangeMessage,
        title: Resources.PublishedSymbolsCantChangedTitle,
        category: Correctness,
        description: Resources.PublishedSymbolsCantChangeDescription,
        defaultSeverity: DiagnosticSeverity.Hidden,
        helpLinkUri: "https://eng.ms/docs/experiences-devices/r9-sdk/docs/static-analysis/analyzers/r9a055",
        isEnabledByDefault: true);

    public static DiagnosticDescriptor AsyncCallInsideUsingBlock { get; } = new(
        id: "R9A056",
        messageFormat: Resources.AsyncCallInsideUsingBlockMessage,
        title: Resources.AsyncCallInsideUsingBlockTitle,
        category: Correctness,
        description: Resources.AsyncCallInsideUsingBlockDescription,
        defaultSeverity: DiagnosticSeverity.Warning,
        helpLinkUri: "https://eng.ms/docs/experiences-devices/r9-sdk/docs/static-analysis/analyzers/r9a056",
        isEnabledByDefault: true);

    // R9A057 retired

    public static DiagnosticDescriptor ConditionalAccess { get; } = new(
        id: "R9A058",
        messageFormat: Resources.ConditionalAccessMessage,
        title: Resources.ConditionalAccessTitle,
        category: Performance,
        description: Resources.ConditionalAccessDescription,
        defaultSeverity: DiagnosticSeverity.Warning,
        helpLinkUri: "https://eng.ms/docs/experiences-devices/r9-sdk/docs/static-analysis/analyzers/r9a058",
        isEnabledByDefault: true);

    public static DiagnosticDescriptor CoalesceAssignment { get; } = new(
        id: "R9A059",
        messageFormat: Resources.CoalesceAssignmentMessage,
        title: Resources.CoalesceAssignmentTitle,
        category: Performance,
        description: Resources.CoalesceAssignmentDescription,
        defaultSeverity: DiagnosticSeverity.Warning,
        helpLinkUri: "https://eng.ms/docs/experiences-devices/r9-sdk/docs/static-analysis/analyzers/r9a059",
        isEnabledByDefault: true);

    public static DiagnosticDescriptor Coalesce { get; } = new(
        id: "R9A060",
        messageFormat: Resources.CoalesceMessage,
        title: Resources.CoalesceTitle,
        category: Performance,
        description: Resources.CoalesceDescription,
        defaultSeverity: DiagnosticSeverity.Warning,
        helpLinkUri: "https://eng.ms/docs/experiences-devices/r9-sdk/docs/static-analysis/analyzers/r9a060",
        isEnabledByDefault: true);

    public static DiagnosticDescriptor AsyncMethodWithoutCancellation { get; } = new(
        id: "R9A061",
        messageFormat: Resources.AsyncMethodWithoutCancellationMessage,
        title: Resources.AsyncMethodWithoutCancellationTitle,
        category: Resilience,
        description: Resources.AsyncMethodWithoutCancellationDescription,
        defaultSeverity: DiagnosticSeverity.Warning,
        helpLinkUri: "https://eng.ms/docs/experiences-devices/r9-sdk/docs/static-analysis/analyzers/r9a061",
        isEnabledByDefault: true);
}
