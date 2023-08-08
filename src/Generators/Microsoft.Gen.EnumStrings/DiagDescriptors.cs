// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.Gen.Shared;

namespace Microsoft.Gen.EnumStrings;

internal sealed class DiagDescriptors : DiagDescriptorsBase
{
    private const string Category = "EnumStrings";

    public static DiagnosticDescriptor InvalidExtensionNamespace { get; } = Make(
        id: "ENUMSTRGEN000",
        title: Resources.InvalidExtensionNamespaceTitle,
        messageFormat: Resources.InvalidExtensionNamespaceMessage,
        category: Category);

    public static DiagnosticDescriptor IncorrectOverload { get; } = Make(
        id: "ENUMSTRGEN001",
        title: Resources.IncorrectOverloadTitle,
        messageFormat: Resources.IncorrectOverloadMessage,
        category: Category);

    public static DiagnosticDescriptor InvalidExtensionClassName { get; } = Make(
        id: "ENUMSTRGEN002",
        title: Resources.InvalidExtensionClassNameTitle,
        messageFormat: Resources.InvalidExtensionClassNameMessage,
        category: Category);

    public static DiagnosticDescriptor InvalidExtensionMethodName { get; } = Make(
        id: "ENUMSTRGEN003",
        title: Resources.InvalidExtensionMethodNameTitle,
        messageFormat: Resources.InvalidExtensionMethodNameMessage,
        category: Category);

    public static DiagnosticDescriptor InvalidEnumType { get; } = Make(
        id: "ENUMSTRGEN004",
        title: Resources.InvalidEnumTypeTitle,
        messageFormat: Resources.InvalidEnumTypeMessage,
        category: Category);
}
