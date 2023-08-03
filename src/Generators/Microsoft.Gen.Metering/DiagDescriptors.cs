// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.Gen.Shared;

namespace Microsoft.Gen.Metering;

internal sealed class DiagDescriptors : DiagDescriptorsBase
{
    private const string Category = "Metering";

    public static DiagnosticDescriptor ErrorInvalidMethodName { get; } = Make(
        id: "R9G050",
        title: Resources.ErrorInvalidMethodNameTitle,
        messageFormat: Resources.ErrorInvalidMethodNameMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorInvalidParameterName { get; } = Make(
        id: "R9G051",
        title: Resources.ErrorInvalidParameterNameTitle,
        messageFormat: Resources.ErrorInvalidParameterNameMessage,
        category: Category);

    // R9G052 is not in use, but can be referenced by the old Metric generator

    public static DiagnosticDescriptor ErrorInvalidMetricName { get; } = Make(
        id: "R9G053",
        title: Resources.ErrorInvalidMetricNameTitle,
        messageFormat: Resources.ErrorInvalidMetricNameMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorMetricNameReuse { get; } = Make(
        id: "R9G054",
        title: Resources.ErrorMetricNameReuseTitle,
        messageFormat: Resources.ErrorMetricNameReuseMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorInvalidMethodReturnType { get; } = Make(
        id: "R9G055",
        title: Resources.ErrorInvalidMethodReturnTypeTitle,
        messageFormat: Resources.ErrorInvalidMethodReturnTypeMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorMissingMeter { get; } = Make(
        id: "R9G056",
        title: Resources.ErrorMissingMeterTitle,
        messageFormat: Resources.ErrorMissingMeterMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorNotPartialMethod { get; } = Make(
        id: "R9G057",
        title: Resources.ErrorNotPartialMethodTitle,
        messageFormat: Resources.ErrorNotPartialMethodMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorMethodIsGeneric { get; } = Make(
        id: "R9G058",
        title: Resources.ErrorMethodIsGenericTitle,
        messageFormat: Resources.ErrorMethodIsGenericMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorMethodHasBody { get; } = Make(
        id: "R9G059",
        title: Resources.ErrorMethodHasBodyTitle,
        messageFormat: Resources.ErrorMethodHasBodyMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorInvalidDimensionNames { get; } = Make(
        id: "R9G060",
        title: Resources.ErrorInvalidDimensionNamesMessage,
        messageFormat: Resources.ErrorInvalidDimensionNamesTitle,
        category: Category);

    public static DiagnosticDescriptor ErrorNotStaticMethod { get; } = Make(
        id: "R9G062",
        title: Resources.ErrorNotStaticMethodTitle,
        messageFormat: Resources.ErrorNotStaticMethodMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorDuplicateDimensionName { get; } = Make(
        id: "R9G063",
        title: Resources.ErrorDuplicateDimensionNameTitle,
        messageFormat: Resources.ErrorDuplicateDimensionNameMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorInvalidDimensionType { get; } = Make(
        id: "R9G064",
        title: Resources.ErrorInvalidDimensionTypeTitle,
        messageFormat: Resources.ErrorInvalidDimensionTypeMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorTooManyDimensions { get; } = Make(
        id: "R9G065",
        title: Resources.ErrorTooManyDimensionsTitle,
        messageFormat: Resources.ErrorTooManyDimensionsMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorInvalidAttributeGenericType { get; } = Make(
        id: "R9G066",
        title: Resources.ErrorInvalidAttributeGenericTypeTitle,
        messageFormat: Resources.ErrorInvalidAttributeGenericTypeMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorInvalidMethodReturnTypeLocation { get; } = Make(
        id: "R9G067",
        title: Resources.ErrorInvalidMethodReturnTypeLocationTitle,
        messageFormat: Resources.ErrorInvalidMethodReturnTypeLocationMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorInvalidMethodReturnTypeArity { get; } = Make(
        id: "R9G068",
        title: Resources.ErrorInvalidMethodReturnTypeArityTitle,
        messageFormat: Resources.ErrorInvalidMethodReturnTypeArityMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorGaugeNotSupported { get; } = Make(
        id: "R9G069",
        title: Resources.ErrorGaugeNotSupportedTitle,
        messageFormat: Resources.ErrorGaugeNotSupportedMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorXmlNotLoadedCorrectly { get; } = Make(
        id: "R9G070",
        title: Resources.ErrorXmlNotLoadedCorrectlyTitle,
        messageFormat: Resources.ErrorXmlNotLoadedCorrectlyMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorDimensionTypeCycleDetected { get; } = Make(
        id: "R9G071",
        title: Resources.ErrorDimensionTypeCycleDetectedTitle,
        messageFormat: Resources.ErrorDimensionTypeCycleDetectedMessage,
        category: Category);
}
