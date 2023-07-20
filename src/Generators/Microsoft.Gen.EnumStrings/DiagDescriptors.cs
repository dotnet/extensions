// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.Gen.Shared;

namespace Microsoft.Gen.EnumStrings;

internal sealed class DiagDescriptors : DiagDescriptorsBase
{
    private const string Category = "EnumStrings";

    public static DiagnosticDescriptor InvalidExtensionNamespace { get; } = Make(
        id: "R9G250",
        title: Resources.InvalidExtensionNamespaceTitle,
        messageFormat: Resources.InvalidExtensionNamespaceMessage,
        category: Category);

    public static DiagnosticDescriptor IncorrectOverload { get; } = Make(
        id: "R9G251",
        title: Resources.IncorrectOverloadTitle,
        messageFormat: Resources.IncorrectOverloadMessage,
        category: Category);

    public static DiagnosticDescriptor InvalidExtensionClassName { get; } = Make(
        id: "R9G252",
        title: Resources.InvalidExtensionClassNameTitle,
        messageFormat: Resources.InvalidExtensionClassNameMessage,
        category: Category);

    public static DiagnosticDescriptor InvalidExtensionMethodName { get; } = Make(
        id: "R9G253",
        title: Resources.InvalidExtensionMethodNameTitle,
        messageFormat: Resources.InvalidExtensionMethodNameMessage,
        category: Category);

    public static DiagnosticDescriptor InvalidEnumType { get; } = Make(
        id: "R9G254",
        title: Resources.InvalidEnumTypeTitle,
        messageFormat: Resources.InvalidEnumTypeMessage,
        category: Category);
}
