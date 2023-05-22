// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.Gen.Shared;

namespace Microsoft.Gen.OptionsValidation;

internal sealed class DiagDescriptors : DiagDescriptorsBase
{
    private const string Category = "OptionsValidation";

    // Skipping R9G100

    public static DiagnosticDescriptor CantUseWithGenericTypes { get; } = Make(
        id: "R9G101",
        title: Resources.CantUseWithGenericTypesTitle,
        messageFormat: Resources.CantUseWithGenericTypesMessage,
        category: Category);

    public static DiagnosticDescriptor NoEligibleMember { get; } = Make(
        id: "R9G102",
        title: Resources.NoEligibleMemberTitle,
        messageFormat: Resources.NoEligibleMemberMessage,
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning);

    public static DiagnosticDescriptor NoEligibleMembersFromValidator { get; } = Make(
        id: "R9G103",
        title: Resources.NoEligibleMembersFromValidatorTitle,
        messageFormat: Resources.NoEligibleMembersFromValidatorMessage,
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning);

    public static DiagnosticDescriptor DoesntImplementIValidateOptions { get; } = Make(
        id: "R9G104",
        title: Resources.DoesntImplementIValidateOptionsTitle,
        messageFormat: Resources.DoesntImplementIValidateOptionsMessage,
        category: Category);

    public static DiagnosticDescriptor AlreadyImplementsValidateMethod { get; } = Make(
        id: "R9G105",
        title: Resources.AlreadyImplementsValidateMethodTitle,
        messageFormat: Resources.AlreadyImplementsValidateMethodMessage,
        category: Category);

    public static DiagnosticDescriptor MemberIsInaccessible { get; } = Make(
        id: "R9G106",
        title: Resources.MemberIsInaccessibleTitle,
        messageFormat: Resources.MemberIsInaccessibleMessage,
        category: Category);

    public static DiagnosticDescriptor NotEnumerableType { get; } = Make(
        id: "R9G107",
        title: Resources.NotEnumerableTypeTitle,
        messageFormat: Resources.NotEnumerableTypeMessage,
        category: Category);

    public static DiagnosticDescriptor ValidatorsNeedSimpleConstructor { get; } = Make(
        id: "R9G108",
        title: Resources.ValidatorsNeedSimpleConstructorTitle,
        messageFormat: Resources.ValidatorsNeedSimpleConstructorMessage,
        category: Category);

    public static DiagnosticDescriptor CantBeStaticClass { get; } = Make(
        id: "R9G109",
        title: Resources.CantBeStaticClassTitle,
        messageFormat: Resources.CantBeStaticClassMessage,
        category: Category);

    public static DiagnosticDescriptor NullValidatorType { get; } = Make(
        id: "R9G110",
        title: Resources.NullValidatorTypeTitle,
        messageFormat: Resources.NullValidatorTypeMessage,
        category: Category);

    public static DiagnosticDescriptor CircularTypeReferences { get; } = Make(
        id: "R9G111",
        title: Resources.CircularTypeReferencesTitle,
        messageFormat: Resources.CircularTypeReferencesMessage,
        category: Category);

    // 112 is available for reuse

    public static DiagnosticDescriptor PotentiallyMissingTransitiveValidation { get; } = Make(
        id: "R9G113",
        title: Resources.PotentiallyMissingTransitiveValidationTitle,
        messageFormat: Resources.PotentiallyMissingTransitiveValidationMessage,
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning);

    public static DiagnosticDescriptor PotentiallyMissingEnumerableValidation { get; } = Make(
        id: "R9G114",
        title: Resources.PotentiallyMissingEnumerableValidationTitle,
        messageFormat: Resources.PotentiallyMissingEnumerableValidationMessage,
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning);
}
