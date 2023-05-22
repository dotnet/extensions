// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.Extensions.LocalAnalyzers.Utilities;

internal static class SymbolExtensions
{
    /// <summary>
    /// Determines whether the current instance is an ancestor type of the parameter.
    /// </summary>
    /// <param name="potentialAncestor">The potential ancestor being inspected.</param>
    /// <param name="potentialDescendant">The type to test.</param>
    /// <returns><see langword="true"/> if <paramref name="potentialDescendant"/> derives directly or indirectly from <paramref name="potentialAncestor"/>.</returns>
    public static bool IsAncestorOf(this ITypeSymbol potentialAncestor, ITypeSymbol potentialDescendant)
    {
        ITypeSymbol? t = potentialDescendant;
        while (true)
        {
            t = t.BaseType;
            if (t == null)
            {
                return false;
            }

            if (SymbolEqualityComparer.Default.Equals(t, potentialAncestor))
            {
                return true;
            }
        }
    }

    /// <summary>
    /// True if the symbol is externally visible outside this assembly.
    /// </summary>
    public static bool IsExternallyVisible(this ISymbol symbol)
    {
        while (symbol.Kind != SymbolKind.Namespace)
        {
            switch (symbol.DeclaredAccessibility)
            {
                // If we see anything private, then the symbol is private.
                case Accessibility.NotApplicable:
                case Accessibility.Private:
                    return false;

                // If we see anything internal, then knock it down from public to
                // internal.
                case Accessibility.Internal:
                case Accessibility.ProtectedAndInternal:
                    return false;
            }

            symbol = symbol.ContainingSymbol;
        }

        return true;
    }

    public static bool ImplementsPublicInterface(this IMethodSymbol method)
    {
        foreach (var iface in method.ContainingType.AllInterfaces)
        {
            if (iface.IsExternallyVisible())
            {
                foreach (var member in iface.GetMembers().OfType<IMethodSymbol>())
                {
                    var impl = method.ContainingType.FindImplementationForInterfaceMember(member);
                    if (SymbolEqualityComparer.Default.Equals(impl, method))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if a symbol has the queried fully qualified name.
    /// </summary>
    /// <param name="symbol">The symbol to check.</param>
    /// <param name="fullyQualifiedName">The fully qualified name to check against.</param>
    /// <returns>True if the symbol has the provided fully qualified name, false otherwise.</returns>
    public static bool HasFullyQualifiedName(this ISymbol symbol, string fullyQualifiedName)
    {
        if (symbol is not null)
        {
            var actualSymbolFullName = symbol.ToDisplayString();
            return actualSymbolFullName.Equals(fullyQualifiedName, System.StringComparison.Ordinal);
        }

        return false;
    }

    /// <summary>
    /// Checks if a type has the specified base type.
    /// </summary>
    /// <param name="type">The type being checked.</param>
    /// <param name="baseTypeFullName">The fully qualified name of the base type to look for.</param>
    /// <returns>True if the type has the specified base type, false otherwise.</returns>
    public static bool InheritsFromType(this ITypeSymbol type, string baseTypeFullName)
    {
        if (type is not null)
        {
            while (type.BaseType != null)
            {
                var actualBaseTypeFullName = string.Concat(type.BaseType.ContainingNamespace, ".", type.BaseType.Name);
                if (actualBaseTypeFullName.Equals(baseTypeFullName, System.StringComparison.Ordinal))
                {
                    return true;
                }

                type = type.BaseType;
            }
        }

        return false;
    }

    public static bool HasAttribute(this ISymbol sym, INamedTypeSymbol attribute)
    {
        foreach (var a in sym.GetAttributes())
        {
            if (SymbolEqualityComparer.Default.Equals(a.AttributeClass, attribute))
            {
                return true;
            }
        }

        return false;
    }

    public static bool IsTopLevelStatementsEntryPointMethod(this IMethodSymbol? methodSymbol)
        => methodSymbol?.IsStatic == true && methodSymbol.Name switch
        {
            "$Main" => true,
            "<Main>$" => true,
            _ => false
        };

    public static bool IsContaminated(this ISymbol symbol, INamedTypeSymbol? contaminationAttribute)
    {
        return (contaminationAttribute != null) && IsContaminated(symbol);

        bool IsContaminated(ISymbol symbol)
        {
            if (symbol.HasAttribute(contaminationAttribute))
            {
                // symbol is annotated
                return true;
            }

            if (symbol.ContainingAssembly != null
                && symbol.ContainingAssembly.HasAttribute(contaminationAttribute))
            {
                // symbol's assembly is annotated
                return true;
            }

            var container = symbol.ContainingType;
            while (container != null)
            {
                if (IsContaminated(container))
                {
                    // symbol's container is annotated
                    return true;
                }

                container = container.ContainingType;
            }

            if (symbol is INamedTypeSymbol type)
            {
                var baseType = type.BaseType;
                while (baseType != null)
                {
                    if (IsContaminated(baseType))
                    {
                        // symbol's base type is annotated
                        return true;
                    }

                    baseType = baseType.BaseType;
                }
            }

            return false;
        }
    }

    internal static ITypeSymbol? GetFieldOrPropertyType(this ISymbol symbol)
    {
        if (symbol is IFieldSymbol fieldSymbol)
        {
            return fieldSymbol.Type;
        }
        else if (symbol is IPropertySymbol propertySymbol)
        {
            return propertySymbol.Type;
        }
        else
        {
            return null;
        }
    }
}
