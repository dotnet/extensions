// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.Gen.Shared;

namespace Microsoft.Gen.AutoClient;

internal sealed class DiagDescriptors : DiagDescriptorsBase
{
    private const string Category = "Design";

    public static DiagnosticDescriptor ErrorClientMustNotBeNested { get; } = Make(
        id: "R9G301",
        title: Resources.ErrorClientMustNotBeNestedTitle,
        messageFormat: Resources.ErrorClientMustNotBeNestedMessage,
        category: Category);

    public static DiagnosticDescriptor WarningRestClientWithoutRestMethods { get; } = Make(
        id: "R9G302",
        title: Resources.WarningRestClientWithoutRestMethodsTitle,
        messageFormat: Resources.WarningRestClientWithoutRestMethodsMessage,
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning);

    public static DiagnosticDescriptor ErrorApiMethodMoreThanOneAttribute { get; } = Make(
        id: "R9G303",
        title: Resources.ErrorApiMethodMoreThanOneAttributeTitle,
        messageFormat: Resources.ErrorApiMethodMoreThanOneAttributeMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorInvalidReturnType { get; } = Make(
        id: "R9G304",
        title: Resources.ErrorInvalidReturnTypeTitle,
        messageFormat: Resources.ErrorInvalidReturnTypeMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorMethodIsGeneric { get; } = Make(
        id: "R9G305",
        title: Resources.ErrorMethodIsGenericTitle,
        messageFormat: Resources.ErrorMethodIsGenericMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorUnsupportedMethodBody { get; } = Make(
        id: "R9G306",
        title: Resources.ErrorUnsupportedMethodBodyTitle,
        messageFormat: Resources.ErrorUnsupportedMethodBodyMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorStaticMethod { get; } = Make(
        id: "R9G307",
        title: Resources.ErrorStaticMethodTitle,
        messageFormat: Resources.ErrorStaticMethodMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorInvalidMethodName { get; } = Make(
        id: "R9G308",
        title: Resources.ErrorInvalidMethodNameTitle,
        messageFormat: Resources.ErrorInvalidMethodNameMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorInvalidParameterName { get; } = Make(
        id: "R9G309",
        title: Resources.ErrorInvalidParameterNameTitle,
        messageFormat: Resources.ErrorInvalidParameterNameMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorMissingMethodAttribute { get; } = Make(
        id: "R9G310",
        title: Resources.ErrorMissingMethodAttributeTitle,
        messageFormat: Resources.ErrorMissingMethodAttributeMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorInterfaceIsGeneric { get; } = Make(
        id: "R9G311",
        title: Resources.ErrorInterfaceIsGenericTitle,
        messageFormat: Resources.ErrorInterfaceIsGenericMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorInterfaceName { get; } = Make(
        id: "R9G312",
        title: Resources.ErrorInterfaceNameTitle,
        messageFormat: Resources.ErrorInterfaceNameMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorDuplicateBody { get; } = Make(
        id: "R9G313",
        title: Resources.ErrorDuplicateBodyTitle,
        messageFormat: Resources.ErrorDuplicateBodyMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorMissingParameterUrl { get; } = Make(
        id: "R9G314",
        title: Resources.ErrorMissingParameterUrlTitle,
        messageFormat: Resources.ErrorMissingParameterUrlMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorDuplicateCancellationToken { get; } = Make(
        id: "R9G315",
        title: Resources.ErrorDuplicateCancellationTokenTitle,
        messageFormat: Resources.ErrorDuplicateCancellationTokenMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorMissingCancellationToken { get; } = Make(
        id: "R9G315",
        title: Resources.ErrorMissingCancellationTokenTitle,
        messageFormat: Resources.ErrorMissingCancellationTokenMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorPathWithQuery { get; } = Make(
        id: "R9G316",
        title: Resources.ErrorPathWithQueryTitle,
        messageFormat: Resources.ErrorPathWithQueryMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorDuplicateRequestName { get; } = Make(
        id: "R9G317",
        title: Resources.ErrorDuplicateRequestNameTitle,
        messageFormat: Resources.ErrorDuplicateRequestNameMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorInvalidHttpClientName { get; } = Make(
        id: "R9G318",
        title: Resources.ErrorInvalidHttpClientNameTitle,
        messageFormat: Resources.ErrorInvalidHttpClientNameMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorInvalidDependencyName { get; } = Make(
        id: "R9G319",
        title: Resources.ErrorInvalidDependencyNameTitle,
        messageFormat: Resources.ErrorInvalidDependencyNameMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorInvalidHeaderName { get; } = Make(
        id: "R9G320",
        title: Resources.ErrorInvalidHeaderNameTitle,
        messageFormat: Resources.ErrorInvalidHeaderNameMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorInvalidHeaderValue { get; } = Make(
        id: "R9G321",
        title: Resources.ErrorInvalidHeaderValueTitle,
        messageFormat: Resources.ErrorInvalidHeaderValueMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorInvalidPath { get; } = Make(
        id: "R9G322",
        title: Resources.ErrorInvalidPathTitle,
        messageFormat: Resources.ErrorInvalidPathMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorInvalidRequestName { get; } = Make(
        id: "R9G323",
        title: Resources.ErrorInvalidRequestNameTitle,
        messageFormat: Resources.ErrorInvalidRequestNameMessage,
        category: Category);
}
