// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.Extensions.ExtraAnalyzers.CallAnalysis;

/// <summary>
/// Recommends replacing dictionaries and sets indexed by [enum|byte|sbyte] with simple arrays instead.
/// </summary>
internal sealed class Arrays
{
    private static readonly string[] _collectionTypes = new[]
    {
        "System.Collections.Generic.Dictionary`2",
        "System.Collections.Generic.HashSet`1",
        "System.Collections.Generic.SortedDictionary`2",
        "System.Collections.Generic.SortedSet`1",
        "System.Collections.Immutable.ImmutableDictionary`2",
        "System.Collections.Immutable.ImmutableHashSet`1",
        "System.Collections.Immutable.ImmutableSortedDictionary`2",
        "System.Collections.Immutable.ImmutableSortedSet`1",
        "System.Collections.Frozen.FrozenDictionary`2",
        "System.Collections.Frozen.FrozenSet`1",
    };

    private static readonly Dictionary<string, string[]> _collectionFactories = new()
    {
        ["System.Collections.Immutable.ImmutableDictionary"] = new[]
        {
            "Create",
            "CreateRange",
        },

        ["System.Collections.Immutable.ImmutableHashSet"] = new[]
        {
            "Create",
            "CreateRange",
        },

        ["System.Collections.Immutable.ImmutableSortedDictionary"] = new[]
        {
            "Create",
            "CreateRange",
        },

        ["System.Collections.Immutable.ImmutableSortedSet"] = new[]
        {
            "Create",
            "CreateRange",
        },

        ["System.Collections.Immutable.ImmutableDictionary`2+Builder"] = new[]
        {
            "ToImmutable",
        },

        ["System.Collections.Immutable.ImmutableHashSet`1+Builder"] = new[]
        {
            "ToImmutable",
        },

        ["System.Collections.Immutable.ImmutableSortedDictionary`2+Builder"] = new[]
        {
            "ToImmutable",
        },

        ["System.Collections.Immutable.ImmutableSortedSet`1+Builder"] = new[]
        {
            "ToImmutable",
        },
    };

    public Arrays(CallAnalyzer.Registrar reg)
    {
        reg.RegisterConstructors(_collectionTypes, HandleConstructor);
        reg.RegisterMethods(_collectionFactories, HandleMethod);

        var freezer = reg.Compilation.GetTypeByMetadataName("System.Collections.Frozen.FrozenDictionary");
        if (freezer != null)
        {
            foreach (var method in freezer.GetMembers("ToFrozenDictionary").OfType<IMethodSymbol>().Where(m => m.TypeParameters.Length == 2))
            {
                reg.RegisterMethod(method, HandleMethod);
            }
        }

        freezer = reg.Compilation.GetTypeByMetadataName("System.Collections.Frozen.FrozenSet");
        if (freezer != null)
        {
            foreach (var method in freezer.GetMembers("ToFrozenSet").OfType<IMethodSymbol>().Where(m => m.TypeParameters.Length == 1))
            {
                reg.RegisterMethod(method, HandleMethod);
            }
        }

        static void HandleMethod(OperationAnalysisContext context, IInvocationOperation op) => HandleSuspectType(context, (INamedTypeSymbol)op.TargetMethod.ReturnType, op.Syntax.GetLocation());

        static void HandleConstructor(OperationAnalysisContext context, IObjectCreationOperation op) => HandleSuspectType(context, (INamedTypeSymbol)op.Type!, op.Syntax.GetLocation());

        static void HandleSuspectType(OperationAnalysisContext context, INamedTypeSymbol type, Location loc)
        {
            var keyType = type.TypeArguments[0];
            if (keyType.TypeKind == TypeKind.Enum
             || keyType.SpecialType == SpecialType.System_Byte
             || keyType.SpecialType == SpecialType.System_SByte)
            {
                if (keyType.TypeKind == TypeKind.Enum)
                {
                    var flagsAttr = context.Compilation.GetTypeByMetadataName("System.FlagsAttribute");
                    if (keyType.GetAttributes().Any(a => a.AttributeClass != null && SymbolEqualityComparer.Default.Equals(a.AttributeClass, flagsAttr)))
                    {
                        // not for [Flags] enums
                        return;
                    }
                }

                var valueType = keyType;
                if (type.TypeArguments.Length == 2)
                {
                    valueType = type.TypeArguments[1];
                }

                var diagnostic = Diagnostic.Create(DiagDescriptors.Arrays, loc, valueType.ToDisplayString(), type.ToDisplayString());
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
