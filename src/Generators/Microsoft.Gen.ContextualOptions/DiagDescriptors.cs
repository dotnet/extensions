// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.Gen.Shared;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Gen.ContextualOptions;

internal sealed class DiagDescriptors : DiagDescriptorsBase
{
    private const string Category = nameof(DiagnosticIds.ContextualOptions);

    public static DiagnosticDescriptor ContextCannotBeStatic { get; } = Make(
        id: DiagnosticIds.ContextualOptions.CTXOPTGEN000,
        title: Resources.ContextCannotBeStaticTitle,
        messageFormat: Resources.ContextCannotBeStaticMessage,
        category: Category);

    public static DiagnosticDescriptor ContextMustBePartial { get; } = Make(
        id: DiagnosticIds.ContextualOptions.CTXOPTGEN001,
        title: Resources.ContextMustBePartialTitle,
        messageFormat: Resources.ContextMustBePartialMessage,
        category: Category);

    public static DiagnosticDescriptor ContextDoesNotHaveValidProperties { get; } = Make(
        id: DiagnosticIds.ContextualOptions.CTXOPTGEN002,
        title: Resources.ContextDoesNotHaveValidPropertiesTitle,
        messageFormat: Resources.ContextDoesNotHaveValidPropertiesMessage,
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning);

    public static DiagnosticDescriptor ContextCannotBeRefLike { get; } = Make(
        id: DiagnosticIds.ContextualOptions.CTXOPTGEN003,
        title: Resources.ContextCannotBeRefLikeTitle,
        messageFormat: Resources.ContextCannotBeRefLikeMessage,
        category: Category);
}
