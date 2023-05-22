// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Gen.ComplianceReports;

internal sealed class Parser
{
    private readonly Compilation _compilation;
    private readonly SymbolHolder _symbolHolder;
    private readonly CancellationToken _cancellationToken;

    public Parser(Compilation compilation, SymbolHolder symbolHolder, CancellationToken cancellationToken)
    {
        _compilation = compilation;
        _symbolHolder = symbolHolder;
        _cancellationToken = cancellationToken;
    }

    /// <summary>
    /// Gets the set of data classification classes containing properties and method parameters to output.
    /// </summary>
    public IReadOnlyList<ClassifiedType> GetClassifiedTypes(IEnumerable<TypeDeclarationSyntax> classes)
    {
        var result = new List<ClassifiedType>();

        // We enumerate by syntax tree, to minimize the need to instantiate semantic models (since they're expensive)
        IEnumerable<IGrouping<SyntaxTree, TypeDeclarationSyntax>> typesBySyntaxTree = classes.GroupBy(x => x.SyntaxTree);
        foreach (var typeForSyntaxTree in typesBySyntaxTree)
        {
            SemanticModel? sm = null;
            foreach (TypeDeclarationSyntax typeSyntax in typeForSyntaxTree.Where(n => !n.IsKind(SyntaxKind.InterfaceDeclaration)))
            {
                _cancellationToken.ThrowIfCancellationRequested();

                sm ??= _compilation.GetSemanticModel(typeSyntax.SyntaxTree);

                INamedTypeSymbol? typeSymbol = sm.GetDeclaredSymbol(typeSyntax, _cancellationToken);
                if (typeSymbol != null)
                {
                    Dictionary<string, ClassifiedItem>? classifiedMembers = null;

                    // grab the annotated members
                    classifiedMembers = GetClassifiedMembers(typeSymbol, classifiedMembers);

                    // include annotations applied via an implemented interface
                    foreach (var iface in typeSymbol.AllInterfaces)
                    {
                        classifiedMembers = GetClassifiedMembers(iface, classifiedMembers);
                    }

                    // include annotations from base classes
                    var parent = typeSymbol.BaseType;
                    while (parent != null)
                    {
                        classifiedMembers = GetClassifiedMembers(parent, classifiedMembers);
                        parent = parent.BaseType;
                    }

                    // grab the logging methods
                    var classifiedLogMethods = GetClassifiedLogMethods(typeSymbol);

                    if (classifiedMembers != null || classifiedLogMethods != null)
                    {
                        result.Add(new ClassifiedType
                        {
                            TypeName = FormatType(typeSymbol),
                            Members = classifiedMembers?.Values.ToList(),
                            LogMethods = classifiedLogMethods,
                        });
                    }
                }
            }
        }

        return result;
    }

    private static string FormatType(ITypeSymbol typeSymbol)
    {
        var result = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        if (result.StartsWith("global::", StringComparison.Ordinal))
        {
            result = result.Substring("global::".Length);
        }

        return result;
    }

    private Dictionary<string, ClassifiedItem>? GetClassifiedMembers(ITypeSymbol typeSymbol, Dictionary<string, ClassifiedItem>? classifiedMembers)
    {
        foreach (var property in typeSymbol.GetMembers().OfType<IPropertySymbol>())
        {
            classifiedMembers = ClassifyMember(classifiedMembers, property, property.Type);
        }

        foreach (var field in typeSymbol.GetMembers().OfType<IFieldSymbol>())
        {
            if (!field.IsImplicitlyDeclared)
            {
                classifiedMembers = ClassifyMember(classifiedMembers, field, field.Type);
            }
        }

        return classifiedMembers;

        Dictionary<string, ClassifiedItem>? ClassifyMember(Dictionary<string, ClassifiedItem>? classifiedMembers, ISymbol member, ITypeSymbol memberType)
        {
            ClassifiedItem? ci = null;
            if (classifiedMembers != null)
            {
                _ = classifiedMembers.TryGetValue(member.Name, out ci);
            }

            // classification coming from the member's container
            foreach (var attribute in typeSymbol.GetAttributes())
            {
                ci = AppendAttributeClassifications(ci, attribute);
            }

            // classification coming from the member's type
            foreach (var attribute in memberType.GetAttributes())
            {
                ci = AppendAttributeClassifications(ci, attribute);
            }

            // classificaiton coming from the member's attributes
            foreach (AttributeData attribute in member.GetAttributes())
            {
                ci = AppendAttributeClassifications(ci, attribute);
            }

            if (ci != null)
            {
                FileLinePositionSpan fileLine = member.Locations[0].GetLineSpan();
                ci.SourceFilePath = fileLine.Path;
                ci.SourceLine = fileLine.StartLinePosition.Line + 1;
                ci.Name = member.Name;
                ci.TypeName = FormatType(memberType);

                classifiedMembers ??= new();
                classifiedMembers[ci.Name] = ci;
            }

            return classifiedMembers;
        }
    }

    private List<ClassifiedLogMethod>? GetClassifiedLogMethods(ITypeSymbol typeSymbol)
    {
        List<ClassifiedLogMethod>? classifiedLogMethods = null;
        if (_symbolHolder.LogMethodAttribute != null)
        {
            var methods = typeSymbol.GetMembers().OfType<IMethodSymbol>();
            foreach (IMethodSymbol method in methods)
            {
                foreach (var a in method.GetAttributes())
                {
                    if (SymbolEqualityComparer.Default.Equals(_symbolHolder.LogMethodAttribute, a.AttributeClass))
                    {
                        var clm = new ClassifiedLogMethod
                        {
                            MethodName = method.Name,
                            LogMethodMessage = "Not Implemented",
                        };

                        foreach (var p in method.Parameters)
                        {
                            FileLinePositionSpan fileLine = p.Locations[0].GetLineSpan();
                            var ci = new ClassifiedItem
                            {
                                SourceFilePath = fileLine.Path,
                                SourceLine = fileLine.StartLinePosition.Line + 1,
                                Name = p.Name,
                                TypeName = FormatType(p.Type),
                            };

                            // classification coming from the parameter's type
                            foreach (var attribute in p.Type.GetAttributes())
                            {
                                ci = AppendAttributeClassifications(ci, attribute);
                            }

                            // classificaiton coming from the parameter's attributes
                            foreach (AttributeData attribute in p.GetAttributes())
                            {
                                ci = AppendAttributeClassifications(ci, attribute);
                            }

                            clm.Parameters.Add(ci);
                        }

                        classifiedLogMethods ??= new();
                        classifiedLogMethods.Add(clm);
                    }
                }
            }
        }

        return classifiedLogMethods;
    }

    private ClassifiedItem? AppendAttributeClassifications(ClassifiedItem? ci, AttributeData attribute)
    {
        if (DerivesFrom(attribute.AttributeClass!, _symbolHolder.DataClassificationAttributeSymbol))
        {
            string name = attribute.AttributeClass!.Name;
            if (name.EndsWith("Attribute", StringComparison.Ordinal))
            {
                name = name.Substring(0, name.Length - "Attribute".Length);
            }

            string? notes = null;
            foreach (var namedArg in attribute.NamedArguments)
            {
                if (namedArg.Key == "Notes")
                {
                    notes = namedArg.Value.Value?.ToString();
                    break;
                }
            }

            ci ??= new();

            ci.Classifications.Add(new Classification
            {
                Name = name,
                Notes = notes,
            });
        }

        return ci;
    }

    private bool DerivesFrom(ITypeSymbol source, ITypeSymbol dest)
    {
        var conversion = _compilation.ClassifyConversion(source, dest);
        return conversion.IsReference && conversion.IsImplicit;
    }
}
