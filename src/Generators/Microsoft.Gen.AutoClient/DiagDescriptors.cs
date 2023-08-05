// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.Gen.Shared;

namespace Microsoft.Gen.AutoClient;

internal sealed class DiagDescriptors : DiagDescriptorsBase
{
    private const string Category = "Design";

    public static DiagnosticDescriptor ErrorClientMustNotBeNested { get; } = Make(
        id: "AUTOCLIENTGEN001",
        title: Resources.ErrorClientMustNotBeNestedTitle,
        messageFormat: Resources.ErrorClientMustNotBeNestedMessage,
        category: Category);

    public static DiagnosticDescriptor WarningRestClientWithoutRestMethods { get; } = Make(
        id: "AUTOCLIENTGEN002",
        title: Resources.WarningRestClientWithoutRestMethodsTitle,
        messageFormat: Resources.WarningRestClientWithoutRestMethodsMessage,
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning);

    public static DiagnosticDescriptor ErrorApiMethodMoreThanOneAttribute { get; } = Make(
        id: "AUTOCLIENTGEN003",
        title: Resources.ErrorApiMethodMoreThanOneAttributeTitle,
        messageFormat: Resources.ErrorApiMethodMoreThanOneAttributeMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorInvalidReturnType { get; } = Make(
        id: "AUTOCLIENTGEN004",
        title: Resources.ErrorInvalidReturnTypeTitle,
        messageFormat: Resources.ErrorInvalidReturnTypeMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorMethodIsGeneric { get; } = Make(
        id: "AUTOCLIENTGEN005",
        title: Resources.ErrorMethodIsGenericTitle,
        messageFormat: Resources.ErrorMethodIsGenericMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorUnsupportedMethodBody { get; } = Make(
        id: "AUTOCLIENTGEN006",
        title: Resources.ErrorUnsupportedMethodBodyTitle,
        messageFormat: Resources.ErrorUnsupportedMethodBodyMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorStaticMethod { get; } = Make(
        id: "AUTOCLIENTGEN007",
        title: Resources.ErrorStaticMethodTitle,
        messageFormat: Resources.ErrorStaticMethodMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorMissingMethodAttribute { get; } = Make(
        id: "AUTOCLIENTGEN008",
        title: Resources.ErrorMissingMethodAttributeTitle,
        messageFormat: Resources.ErrorMissingMethodAttributeMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorInterfaceIsGeneric { get; } = Make(
        id: "AUTOCLIENTGEN009",
        title: Resources.ErrorInterfaceIsGenericTitle,
        messageFormat: Resources.ErrorInterfaceIsGenericMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorInterfaceName { get; } = Make(
        id: "AUTOCLIENTGEN010",
        title: Resources.ErrorInterfaceNameTitle,
        messageFormat: Resources.ErrorInterfaceNameMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorDuplicateBody { get; } = Make(
        id: "AUTOCLIENTGEN011",
        title: Resources.ErrorDuplicateBodyTitle,
        messageFormat: Resources.ErrorDuplicateBodyMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorMissingParameterUrl { get; } = Make(
        id: "AUTOCLIENTGEN012",
        title: Resources.ErrorMissingParameterUrlTitle,
        messageFormat: Resources.ErrorMissingParameterUrlMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorDuplicateCancellationToken { get; } = Make(
        id: "AUTOCLIENTGEN013",
        title: Resources.ErrorDuplicateCancellationTokenTitle,
        messageFormat: Resources.ErrorDuplicateCancellationTokenMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorMissingCancellationToken { get; } = Make(
        id: "AUTOCLIENTGEN014",
        title: Resources.ErrorMissingCancellationTokenTitle,
        messageFormat: Resources.ErrorMissingCancellationTokenMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorPathWithQuery { get; } = Make(
        id: "AUTOCLIENTGEN015",
        title: Resources.ErrorPathWithQueryTitle,
        messageFormat: Resources.ErrorPathWithQueryMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorDuplicateRequestName { get; } = Make(
        id: "AUTOCLIENTGEN016",
        title: Resources.ErrorDuplicateRequestNameTitle,
        messageFormat: Resources.ErrorDuplicateRequestNameMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorInvalidHttpClientName { get; } = Make(
        id: "AUTOCLIENTGEN017",
        title: Resources.ErrorInvalidHttpClientNameTitle,
        messageFormat: Resources.ErrorInvalidHttpClientNameMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorInvalidDependencyName { get; } = Make(
        id: "AUTOCLIENTGEN018",
        title: Resources.ErrorInvalidDependencyNameTitle,
        messageFormat: Resources.ErrorInvalidDependencyNameMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorInvalidHeaderName { get; } = Make(
        id: "AUTOCLIENTGEN019",
        title: Resources.ErrorInvalidHeaderNameTitle,
        messageFormat: Resources.ErrorInvalidHeaderNameMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorInvalidHeaderValue { get; } = Make(
        id: "AUTOCLIENTGEN020",
        title: Resources.ErrorInvalidHeaderValueTitle,
        messageFormat: Resources.ErrorInvalidHeaderValueMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorInvalidPath { get; } = Make(
        id: "AUTOCLIENTGEN021",
        title: Resources.ErrorInvalidPathTitle,
        messageFormat: Resources.ErrorInvalidPathMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorInvalidRequestName { get; } = Make(
        id: "AUTOCLIENTGEN022",
        title: Resources.ErrorInvalidRequestNameTitle,
        messageFormat: Resources.ErrorInvalidRequestNameMessage,
        category: Category);
}
