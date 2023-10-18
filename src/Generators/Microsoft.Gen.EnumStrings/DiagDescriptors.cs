// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.Gen.Shared;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Gen.EnumStrings;

internal sealed class DiagDescriptors : DiagDescriptorsBase
{
    private const string Category = nameof(DiagnosticIds.EnumStrings);

    public static DiagnosticDescriptor InvalidExtensionNamespace { get; } = Make(
        id: DiagnosticIds.EnumStrings.ENUMSTRGEN000,
        title: Resources.InvalidExtensionNamespaceTitle,
        messageFormat: Resources.InvalidExtensionNamespaceMessage,
        category: Category);

    public static DiagnosticDescriptor IncorrectOverload { get; } = Make(
        id: DiagnosticIds.EnumStrings.ENUMSTRGEN001,
        title: Resources.IncorrectOverloadTitle,
        messageFormat: Resources.IncorrectOverloadMessage,
        category: Category);

    public static DiagnosticDescriptor InvalidExtensionClassName { get; } = Make(
        id: DiagnosticIds.EnumStrings.ENUMSTRGEN002,
        title: Resources.InvalidExtensionClassNameTitle,
        messageFormat: Resources.InvalidExtensionClassNameMessage,
        category: Category);

    public static DiagnosticDescriptor InvalidExtensionMethodName { get; } = Make(
        id: DiagnosticIds.EnumStrings.ENUMSTRGEN003,
        title: Resources.InvalidExtensionMethodNameTitle,
        messageFormat: Resources.InvalidExtensionMethodNameMessage,
        category: Category);

    public static DiagnosticDescriptor InvalidEnumType { get; } = Make(
        id: DiagnosticIds.EnumStrings.ENUMSTRGEN004,
        title: Resources.InvalidEnumTypeTitle,
        messageFormat: Resources.InvalidEnumTypeMessage,
        category: Category);
}
