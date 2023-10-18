// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.Gen.Shared;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Gen.AutoClient;

internal sealed class DiagDescriptors : DiagDescriptorsBase
{
    private const string Category = nameof(DiagnosticIds.Design);

    public static DiagnosticDescriptor ErrorClientMustNotBeNested { get; } = Make(
        id: DiagnosticIds.Design.AUTOCLIENTGEN001,
        title: Resources.ErrorClientMustNotBeNestedTitle,
        messageFormat: Resources.ErrorClientMustNotBeNestedMessage,
        category: Category);

    public static DiagnosticDescriptor WarningRestClientWithoutRestMethods { get; } = Make(
        id: DiagnosticIds.Design.AUTOCLIENTGEN002,
        title: Resources.WarningRestClientWithoutRestMethodsTitle,
        messageFormat: Resources.WarningRestClientWithoutRestMethodsMessage,
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning);

    public static DiagnosticDescriptor ErrorApiMethodMoreThanOneAttribute { get; } = Make(
        id: DiagnosticIds.Design.AUTOCLIENTGEN003,
        title: Resources.ErrorApiMethodMoreThanOneAttributeTitle,
        messageFormat: Resources.ErrorApiMethodMoreThanOneAttributeMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorInvalidReturnType { get; } = Make(
        id: DiagnosticIds.Design.AUTOCLIENTGEN004,
        title: Resources.ErrorInvalidReturnTypeTitle,
        messageFormat: Resources.ErrorInvalidReturnTypeMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorMethodIsGeneric { get; } = Make(
        id: DiagnosticIds.Design.AUTOCLIENTGEN005,
        title: Resources.ErrorMethodIsGenericTitle,
        messageFormat: Resources.ErrorMethodIsGenericMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorUnsupportedMethodBody { get; } = Make(
        id: DiagnosticIds.Design.AUTOCLIENTGEN006,
        title: Resources.ErrorUnsupportedMethodBodyTitle,
        messageFormat: Resources.ErrorUnsupportedMethodBodyMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorStaticMethod { get; } = Make(
        id: DiagnosticIds.Design.AUTOCLIENTGEN007,
        title: Resources.ErrorStaticMethodTitle,
        messageFormat: Resources.ErrorStaticMethodMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorMissingMethodAttribute { get; } = Make(
        id: DiagnosticIds.Design.AUTOCLIENTGEN008,
        title: Resources.ErrorMissingMethodAttributeTitle,
        messageFormat: Resources.ErrorMissingMethodAttributeMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorInterfaceIsGeneric { get; } = Make(
        id: DiagnosticIds.Design.AUTOCLIENTGEN009,
        title: Resources.ErrorInterfaceIsGenericTitle,
        messageFormat: Resources.ErrorInterfaceIsGenericMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorInterfaceName { get; } = Make(
        id: DiagnosticIds.Design.AUTOCLIENTGEN010,
        title: Resources.ErrorInterfaceNameTitle,
        messageFormat: Resources.ErrorInterfaceNameMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorDuplicateBody { get; } = Make(
        id: DiagnosticIds.Design.AUTOCLIENTGEN011,
        title: Resources.ErrorDuplicateBodyTitle,
        messageFormat: Resources.ErrorDuplicateBodyMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorMissingParameterUrl { get; } = Make(
        id: DiagnosticIds.Design.AUTOCLIENTGEN012,
        title: Resources.ErrorMissingParameterUrlTitle,
        messageFormat: Resources.ErrorMissingParameterUrlMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorDuplicateCancellationToken { get; } = Make(
        id: DiagnosticIds.Design.AUTOCLIENTGEN013,
        title: Resources.ErrorDuplicateCancellationTokenTitle,
        messageFormat: Resources.ErrorDuplicateCancellationTokenMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorMissingCancellationToken { get; } = Make(
        id: DiagnosticIds.Design.AUTOCLIENTGEN014,
        title: Resources.ErrorMissingCancellationTokenTitle,
        messageFormat: Resources.ErrorMissingCancellationTokenMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorPathWithQuery { get; } = Make(
        id: DiagnosticIds.Design.AUTOCLIENTGEN015,
        title: Resources.ErrorPathWithQueryTitle,
        messageFormat: Resources.ErrorPathWithQueryMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorDuplicateRequestName { get; } = Make(
        id: DiagnosticIds.Design.AUTOCLIENTGEN016,
        title: Resources.ErrorDuplicateRequestNameTitle,
        messageFormat: Resources.ErrorDuplicateRequestNameMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorInvalidHttpClientName { get; } = Make(
        id: DiagnosticIds.Design.AUTOCLIENTGEN017,
        title: Resources.ErrorInvalidHttpClientNameTitle,
        messageFormat: Resources.ErrorInvalidHttpClientNameMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorInvalidDependencyName { get; } = Make(
        id: DiagnosticIds.Design.AUTOCLIENTGEN018,
        title: Resources.ErrorInvalidDependencyNameTitle,
        messageFormat: Resources.ErrorInvalidDependencyNameMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorInvalidHeaderName { get; } = Make(
        id: DiagnosticIds.Design.AUTOCLIENTGEN019,
        title: Resources.ErrorInvalidHeaderNameTitle,
        messageFormat: Resources.ErrorInvalidHeaderNameMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorInvalidHeaderValue { get; } = Make(
        id: DiagnosticIds.Design.AUTOCLIENTGEN020,
        title: Resources.ErrorInvalidHeaderValueTitle,
        messageFormat: Resources.ErrorInvalidHeaderValueMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorInvalidPath { get; } = Make(
        id: DiagnosticIds.Design.AUTOCLIENTGEN021,
        title: Resources.ErrorInvalidPathTitle,
        messageFormat: Resources.ErrorInvalidPathMessage,
        category: Category);

    public static DiagnosticDescriptor ErrorInvalidRequestName { get; } = Make(
        id: DiagnosticIds.Design.AUTOCLIENTGEN022,
        title: Resources.ErrorInvalidRequestNameTitle,
        messageFormat: Resources.ErrorInvalidRequestNameMessage,
        category: Category);
}
