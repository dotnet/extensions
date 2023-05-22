// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Gen.EnumStrings.Model;

namespace Microsoft.Gen.EnumStrings;

/// <summary>
/// Holds an internal parser class that extracts necessary information for generating IValidateOptions.
/// </summary>
internal sealed class Parser
{
    private readonly CancellationToken _cancellationToken;
    private readonly Compilation _compilation;
    private readonly Action<Diagnostic> _reportDiagnostic;
    private readonly SymbolHolder _symbolHolder;

    public Parser(
        Compilation compilation,
        Action<Diagnostic> reportDiagnostic,
        SymbolHolder symbolHolder,
        CancellationToken cancellationToken)
    {
        _compilation = compilation;
        _cancellationToken = cancellationToken;
        _reportDiagnostic = reportDiagnostic;
        _symbolHolder = symbolHolder;
    }

    public IReadOnlyList<ToStringMethod> GetToStringMethods(IEnumerable<SyntaxNode> nodes)
    {
        var results = new List<ToStringMethod>();

        foreach (var group in nodes.GroupBy(x => x.SyntaxTree))
        {
            SemanticModel? sm = null;
            foreach (var node in group)
            {
                _cancellationToken.ThrowIfCancellationRequested();
                sm ??= _compilation.GetSemanticModel(node.SyntaxTree);

                if (node.IsKind(SyntaxKind.EnumDeclaration))
                {
                    // enum-level attribute usage
                    var enumDecl = (EnumDeclarationSyntax)node;
                    var enumSym = sm.GetDeclaredSymbol(node) as INamedTypeSymbol;
                    if (enumSym != null)
                    {
                        ParseAttributeList(
                            enumSym,
                            enumSym!.GetAttributes(),
                            results);
                    }
                }
                else if (node.IsKind(SyntaxKind.CompilationUnit))
                {
                    // assembly-level attribute usage
                    var compUnitDecl = (CompilationUnitSyntax)node;
                    ParseAttributeList(
                        null,
                        sm.Compilation.Assembly.GetAttributes().Where(ad => ad.ApplicationSyntaxReference!.SyntaxTree == node.SyntaxTree),
                        results);
                }
            }
        }

        return results;
    }

    private static (INamedTypeSymbol? explicitEnumType, string? nspace, string? className, string? methodName, string? classModifiers)
        ExtractAttributeValues(AttributeData args)
    {
        INamedTypeSymbol? explicitEnumType = null;
        string? nspace = null;
        string? className = null;
        string? methodName = null;
        string? classModifiers = null;

        // Two constructor shapes:
        //
        //   ()
        //   (Type enumType)
        if (args.ConstructorArguments.Length > 0)
        {
            explicitEnumType = args.ConstructorArguments[0].Value as INamedTypeSymbol;
        }

        foreach (var a in args.NamedArguments)
        {
            switch (a.Key)
            {
                case "ExtensionClassModifiers":
                    classModifiers = a.Value.Value as string;
                    break;

                case "ExtensionNamespace":
                    nspace = a.Value.Value as string;
                    break;

                case "ExtensionClassName":
                    className = a.Value.Value as string;
                    break;

                case "ExtensionMethodName":
                    methodName = a.Value.Value as string;
                    break;
            }
        }

        return (explicitEnumType, nspace, className, methodName, classModifiers);
    }

    private static ulong ConvertValue(object obj) =>
        obj switch
        {
            sbyte or short or int or long => (ulong)Convert.ToInt64(obj, CultureInfo.InvariantCulture),
            byte or ushort or uint or ulong => Convert.ToUInt64(obj, CultureInfo.InvariantCulture),
            _ => 0,
        };

    private static bool IsValidNamespace(string nspace)
    {
        var source = $"namespace {nspace} {{ }}";
        var st = CSharpSyntaxTree.ParseText(source);
        return !st.GetDiagnostics().Any();
    }

    private static bool IsValidClassName(string className)
    {
        var source = $"class {className} {{ }}";
        var st = CSharpSyntaxTree.ParseText(source);
        return !st.GetDiagnostics().Any();
    }

    private static bool IsValidMethodName(string methodName)
    {
        var source = $"class ___XYZ {{ public void {methodName}() {{ }} }}";
        var st = CSharpSyntaxTree.ParseText(source);
        return !(st.GetDiagnostics().Any() || methodName.Contains('.'));
    }

    private void ParseAttributeList(INamedTypeSymbol? implicitEnumType, IEnumerable<AttributeData> attrDataList, List<ToStringMethod> results)
    {
        foreach (var ad in attrDataList)
        {
            if (SymbolEqualityComparer.Default.Equals(ad.AttributeClass, _symbolHolder.EnumStringsAttributeSymbol))
            {
                var attrData = ad;
                var attrSyntax = attrData.ApplicationSyntaxReference?.GetSyntax(_cancellationToken) as AttributeSyntax;

                if (attrData != null && attrSyntax != null)
                {
                    var (explicitEnumType, nspace, className, methodName, classModifiers) = ExtractAttributeValues(attrData);

                    if (nspace != null && !IsValidNamespace(nspace))
                    {
                        Diag(DiagDescriptors.InvalidExtensionNamespace, attrSyntax.GetLocation(), nspace);
                    }

                    if (className != null && !IsValidClassName(className))
                    {
                        Diag(DiagDescriptors.InvalidExtensionClassName, attrSyntax.GetLocation(), className);
                    }

                    if (methodName != null && !IsValidMethodName(methodName))
                    {
                        Diag(DiagDescriptors.InvalidExtensionMethodName, attrSyntax.GetLocation(), methodName);
                    }

                    var enumType = implicitEnumType ?? explicitEnumType;

                    if ((implicitEnumType != null && explicitEnumType != null) || enumType == null)
                    {
                        // must have one and only one enum type
                        Diag(DiagDescriptors.IncorrectOverload, attrSyntax.GetLocation());
                        return;
                    }

                    if (enumType.TypeKind != TypeKind.Enum)
                    {
                        Diag(DiagDescriptors.InvalidEnumType, attrSyntax.GetLocation(), enumType);
                        return;
                    }

                    var flags = enumType.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, _symbolHolder.FlagsAttributeSymbol));

                    // get a sorted list of enum members
                    IEnumerable<KeyValuePair<string, ulong>> members = enumType
                        .GetMembers()
                        .OfType<IFieldSymbol>()
                        .Where(f => f.IsConst)
                        .Select(f => new KeyValuePair<string, ulong>(f.Name, ConvertValue(f.ConstantValue!)))
                        .OrderBy(kvp => kvp.Value);

                    if (flags)
                    {
                        members = members
                            .Reverse() // flip it so Distinct keeps the last instance of duplicates instead of the first to match what Enum.ToString does
                            .Distinct(new EntryComparer())
                            .Reverse(); // flip it back to natural order
                    }
                    else
                    {
                        members = members.Distinct(new EntryComparer());
                    }

                    results.Add(new ToStringMethod(
                        enumType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                        members.Select(kvp => kvp.Key).ToList(),
                        members.Select(kvp => kvp.Value).ToList(),
                        flags,
                        nspace ?? enumType.ContainingNamespace.ToString(),
                        className ?? enumType.Name + "Extensions",
                        methodName ?? "ToInvariantString",
                        classModifiers ?? "internal static",
                        enumType.EnumUnderlyingType!.ToString()));
                }
            }
        }
    }

    private void Diag(DiagnosticDescriptor desc, Location? location)
    {
        _reportDiagnostic(Diagnostic.Create(desc, location, Array.Empty<object?>()));
    }

    private void Diag(DiagnosticDescriptor desc, Location? location, params object?[]? messageArgs)
    {
        _reportDiagnostic(Diagnostic.Create(desc, location, messageArgs));
    }

    private sealed class EntryComparer : IEqualityComparer<KeyValuePair<string, ulong>>
    {
        public bool Equals(KeyValuePair<string, ulong> x, KeyValuePair<string, ulong> y) => x.Value == y.Value;
        public int GetHashCode(KeyValuePair<string, ulong> obj) => obj.Value.GetHashCode();
    }
}
