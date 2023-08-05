// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.Gen.Shared;

namespace Microsoft.Gen.Metering;

internal sealed class DiagDescriptors : DiagDescriptorsBase
{
    private const string Category = "Metering";

    public static DiagnosticDescriptor ErrorInvalidMethodName { get; } = Make(
        id: "METGEN000",
        title: Resources.ErrorInvalidMethodNameTitle,
        messageFormat: Resources.ErrorInvalidMethodNameMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorInvalidParameterName { get; } = Make(
        id: "METGEN001",
        title: Resources.ErrorInvalidParameterNameTitle,
        messageFormat: Resources.ErrorInvalidParameterNameMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorInvalidMetricName { get; } = Make(
        id: "METGEN002",
        title: Resources.ErrorInvalidMetricNameTitle,
        messageFormat: Resources.ErrorInvalidMetricNameMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorMetricNameReuse { get; } = Make(
        id: "METGEN003",
        title: Resources.ErrorMetricNameReuseTitle,
        messageFormat: Resources.ErrorMetricNameReuseMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorInvalidMethodReturnType { get; } = Make(
        id: "METGEN004",
        title: Resources.ErrorInvalidMethodReturnTypeTitle,
        messageFormat: Resources.ErrorInvalidMethodReturnTypeMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorMissingMeter { get; } = Make(
        id: "METGEN005",
        title: Resources.ErrorMissingMeterTitle,
        messageFormat: Resources.ErrorMissingMeterMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorNotPartialMethod { get; } = Make(
        id: "METGEN006",
        title: Resources.ErrorNotPartialMethodTitle,
        messageFormat: Resources.ErrorNotPartialMethodMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorMethodIsGeneric { get; } = Make(
        id: "METGEN007",
        title: Resources.ErrorMethodIsGenericTitle,
        messageFormat: Resources.ErrorMethodIsGenericMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorMethodHasBody { get; } = Make(
        id: "METGEN008",
        title: Resources.ErrorMethodHasBodyTitle,
        messageFormat: Resources.ErrorMethodHasBodyMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorInvalidTagNames { get; } = Make(
        id: "METGEN009",
        title: Resources.ErrorInvalidTagNamesMessage,
        messageFormat: Resources.ErrorInvalidTagNamesTitle,
        category: Category);

    public static DiagnosticDescriptor ErrorNotStaticMethod { get; } = Make(
        id: "METGEN010",
        title: Resources.ErrorNotStaticMethodTitle,
        messageFormat: Resources.ErrorNotStaticMethodMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorDuplicateTagName { get; } = Make(
        id: "METGEN011",
        title: Resources.ErrorDuplicateTagNameTitle,
        messageFormat: Resources.ErrorDuplicateTagNameMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorInvalidTagNameType { get; } = Make(
        id: "METGEN012",
        title: Resources.ErrorInvalidTagTypeTitle,
        messageFormat: Resources.ErrorInvalidTagTypeMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorTooManyTagNames { get; } = Make(
        id: "METGEN013",
        title: Resources.ErrorTooManyTagNamesTitle,
        messageFormat: Resources.ErrorTooManyTagNamesMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorInvalidAttributeGenericType { get; } = Make(
        id: "METGEN014",
        title: Resources.ErrorInvalidAttributeGenericTypeTitle,
        messageFormat: Resources.ErrorInvalidAttributeGenericTypeMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorInvalidMethodReturnTypeLocation { get; } = Make(
        id: "METGEN015",
        title: Resources.ErrorInvalidMethodReturnTypeLocationTitle,
        messageFormat: Resources.ErrorInvalidMethodReturnTypeLocationMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorInvalidMethodReturnTypeArity { get; } = Make(
        id: "METGEN016",
        title: Resources.ErrorInvalidMethodReturnTypeArityTitle,
        messageFormat: Resources.ErrorInvalidMethodReturnTypeArityMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorGaugeNotSupported { get; } = Make(
        id: "METGEN017",
        title: Resources.ErrorGaugeNotSupportedTitle,
        messageFormat: Resources.ErrorGaugeNotSupportedMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorXmlNotLoadedCorrectly { get; } = Make(
        id: "METGEN018",
        title: Resources.ErrorXmlNotLoadedCorrectlyTitle,
        messageFormat: Resources.ErrorXmlNotLoadedCorrectlyMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorTagTypeCycleDetected { get; } = Make(
        id: "METGEN019",
        title: Resources.ErrorTagTypeCycleDetectedTitle,
        messageFormat: Resources.ErrorTagTypeCycleDetectedMessage,
        category: Category);
}
