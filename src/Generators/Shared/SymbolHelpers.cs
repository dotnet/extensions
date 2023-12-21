// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

#pragma warning disable CA1716
namespace Microsoft.Gen.Shared;
#pragma warning restore CA1716

#if !SHARED_PROJECT
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
internal static class SymbolHelpers
{
    public static string GetFullNamespace(ISymbol symbol)
    {
        return symbol.ContainingNamespace.IsGlobalNamespace ? string.Empty : symbol.ContainingNamespace.ToString();
    }

    /// <summary>
    /// Can code in a given type access a given member?
    /// </summary>
    /// <remarks>
    /// Note that this implementation assumes that the target member is within the origin type
    /// or a base class of the origin type.
    /// </remarks>
    public static bool CanAccess(this INamedTypeSymbol originType, ISymbol targetMember)
    {
        if (SymbolEqualityComparer.Default.Equals(originType, targetMember.ContainingType))
        {
            // target member is from the origin type, we're good
            return true;
        }

        if (targetMember.DeclaredAccessibility == Accessibility.Private)
        {
            // can't access a private member from a different type
            return false;
        }

        if (SymbolEqualityComparer.Default.Equals(originType.ContainingAssembly, targetMember.ContainingAssembly))
        {
            // target member is in the same assembly as the origin type, so we're good
            return true;
        }

        if (targetMember.DeclaredAccessibility is Accessibility.Internal or Accessibility.ProtectedAndInternal)
        {
            // can't access internal members of other assemblies (sorry, we don't support IVT right now)
            return false;
        }

        return true;
    }
}
