// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Gen.AutoClient.Model;
using Microsoft.Gen.Shared;

namespace Microsoft.Gen.AutoClient;

internal sealed class Parser
{
    private const string ReturnTypePrefix = "System.Threading.Tasks.Task<";

    private static readonly string[] _dependencyNameTrimEndings = new[] { "Api", "Client" };
    private static readonly string[] _requestNameTrimEndings = new[] { "Async" };

    private readonly CancellationToken _cancellationToken;
    private readonly Compilation _compilation;
    private readonly Action<Diagnostic> _reportDiagnostic;

    private readonly SymbolDisplayFormat _globalDisplayFormat = SymbolDisplayFormat
        .FullyQualifiedFormat
        .WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Included);

    public Parser(Compilation compilation, Action<Diagnostic> reportDiagnostic, CancellationToken cancellationToken)
    {
        _compilation = compilation;
        _cancellationToken = cancellationToken;
        _reportDiagnostic = reportDiagnostic;
    }

    public IReadOnlyList<RestApiType> GetRestApiClasses(IEnumerable<InterfaceDeclarationSyntax> types)
    {
        var symbols = SymbolLoader.LoadSymbols(_compilation);
        if (symbols == null)
        {
            return Array.Empty<RestApiType>();
        }

        var results = new List<RestApiType>();

        foreach (var typeDeclarationGroup in types.GroupBy(x => x.SyntaxTree))
        {
            SemanticModel? semanticModel = null;
            foreach (var typeDeclaration in typeDeclarationGroup)
            {
                // stop if we're asked to
                _cancellationToken.ThrowIfCancellationRequested();
                semanticModel ??= _compilation.GetSemanticModel(typeDeclaration.SyntaxTree);

                var classSymbol = semanticModel.GetDeclaredSymbol(typeDeclaration, _cancellationToken);
                if (classSymbol == null)
                {
                    continue;
                }

                var classAttributes = classSymbol.GetAttributes();
                if (classAttributes.Length == 0)
                {
                    continue;
                }

                var attrResult = ParseInterfaceAttributes(classAttributes, symbols);
                if (attrResult.HttpClientName == null)
                {
                    continue;
                }

                if (typeDeclaration.Arity > 0)
                {
                    Diag(DiagDescriptors.ErrorInterfaceIsGeneric, typeDeclaration.GetLocation());
                    continue;
                }

                if (typeDeclaration.Identifier.ToString()[0] != 'I')
                {
                    Diag(DiagDescriptors.ErrorInterfaceName, typeDeclaration.GetLocation());
                    continue;
                }

                if (typeDeclaration.Parent != null &&
                    (typeDeclaration.Parent!.IsKind(SyntaxKind.ClassDeclaration) ||
                    typeDeclaration.Parent!.IsKind(SyntaxKind.StructDeclaration) ||
                    typeDeclaration.Parent!.IsKind(SyntaxKind.RecordDeclaration)))
                {
                    Diag(DiagDescriptors.ErrorClientMustNotBeNested, typeDeclaration.Parent.GetLocation());
                    continue;
                }

                var nspace = GetNamespace(typeDeclaration);
                var className = typeDeclaration.Identifier.ToString().Substring(1);
                var restApiType = new RestApiType
                {
                    Namespace = nspace,
                    Name = className,
                    Constraints = typeDeclaration.ConstraintClauses.ToString(),
                    Modifiers = typeDeclaration.Modifiers.ToString(),
                    Keyword = "class",
                    HttpClientName = attrResult.HttpClientName,
                    StaticHeaders = attrResult.StaticHeaders,
                    DependencyName = attrResult.CustomDependencyName ?? GetDependencyName(className),
                };

                var requestNames = new HashSet<string>();

                foreach (var memberSyntax in typeDeclaration.Members.Where(x => x.IsKind(SyntaxKind.MethodDeclaration)))
                {
                    var methodSyntax = (MethodDeclarationSyntax)memberSyntax;
                    var methodSymbol = semanticModel.GetDeclaredSymbol(methodSyntax, _cancellationToken);
                    if (methodSymbol == null)
                    {
                        continue;
                    }

                    var clientMethod = ProcessMethod(methodSymbol, symbols, requestNames);
                    if (clientMethod == null)
                    {
                        continue;
                    }

                    restApiType.Methods.Add(clientMethod);
                }

                if (restApiType.Methods.Count == 0)
                {
                    Diag(DiagDescriptors.WarningRestClientWithoutRestMethods, typeDeclaration.GetLocation());
                }

                results.Add(restApiType);
            }
        }

        return results;
    }

    private static string GetDependencyName(string className)
    {
        return TryRemoveFromEnd(className, _dependencyNameTrimEndings);
    }

    private static string GetRequestName(string methodName)
    {
        return TryRemoveFromEnd(methodName, _requestNameTrimEndings);
    }

    private static string TryRemoveFromEnd(string value, string[] endings)
    {
        foreach (var ending in endings)
        {
            if (value.EndsWith(ending, StringComparison.Ordinal))
            {
                return value.Substring(0, value.Length - ending.Length);
            }
        }

        return value;
    }

    private static ParseParameterAttributesResult ParseParameterAttributes(ImmutableArray<AttributeData> attributes, SymbolHolder symbols, string paramName)
    {
        string? headerName = null;
        string? queryKey = null;
        BodyContentTypeParam? bodyType = null;

        foreach (var attribute in attributes)
        {
            var attributeSymbol = attribute.AttributeClass;
            if (attributeSymbol == null)
            {
                continue;
            }

            if (attributeSymbol.Equals(symbols.RestHeaderAttribute, SymbolEqualityComparer.Default))
            {
                if (attribute.ConstructorArguments.Length != 1)
                {
                    continue;
                }

                headerName = attribute.ConstructorArguments[0].Value as string;
            }
            else if (attributeSymbol.Equals(symbols.RestQueryAttribute, SymbolEqualityComparer.Default))
            {
                int argLength = attribute.ConstructorArguments.Length;
                if (argLength == 0)
                {
                    queryKey = paramName;
                }
                else if (argLength == 1)
                {
                    queryKey = attribute.ConstructorArguments[0].Value as string;
                }
            }
            else if (attributeSymbol.Equals(symbols.RestBodyAttribute, SymbolEqualityComparer.Default))
            {
                int argLength = attribute.ConstructorArguments.Length;
                if (argLength == 0)
                {
                    bodyType = BodyContentTypeParam.ApplicationJson;
                }
                else if (argLength == 1)
                {
                    var intValue = attribute.ConstructorArguments[0].Value as int?;
                    if (intValue != null)
                    {
                        bodyType = (BodyContentTypeParam)intValue;
                    }
                }
            }
        }

        return new ParseParameterAttributesResult(headerName, queryKey, bodyType);
    }

    private static ParseInterfaceAttributesResult ParseInterfaceAttributes(ImmutableArray<AttributeData> classAttributes, SymbolHolder symbols)
    {
        string? httpClientName = null;
        string? customDependencyName = null;
        Dictionary<string, string> staticHeaders = new();

        foreach (var classAttribute in classAttributes)
        {
            var attributeSymbol = classAttribute.AttributeClass;
            if (attributeSymbol == null)
            {
                continue;
            }

            if (attributeSymbol.Equals(symbols.RestApiAttribute, SymbolEqualityComparer.Default))
            {
                if (classAttribute.ConstructorArguments.Length == 1)
                {
                    httpClientName = classAttribute.ConstructorArguments[0].Value as string;
                }
                else if (classAttribute.ConstructorArguments.Length == 2)
                {
                    httpClientName = classAttribute.ConstructorArguments[0].Value as string;
                    customDependencyName = classAttribute.ConstructorArguments[1].Value as string;
                }
            }
            else if (attributeSymbol.Equals(symbols.RestStaticHeaderAttribute, SymbolEqualityComparer.Default))
            {
                if (classAttribute.ConstructorArguments.Length != 2)
                {
                    continue;
                }

                var key = classAttribute.ConstructorArguments[0].Value as string;
                var value = classAttribute.ConstructorArguments[1].Value as string;

                if (key == null || value == null)
                {
                    continue;
                }

                staticHeaders.Add(key, value);
            }
        }

        return new(httpClientName, customDependencyName, staticHeaders);
    }

    private static string GetNamespace(TypeDeclarationSyntax typeDeclaration)
    {
        var result = string.Empty;

        // determine the namespace the class is declared in, if any
        SyntaxNode? potentialNamespaceParent = typeDeclaration.Parent;
        while (potentialNamespaceParent != null &&
#if ROSLYN_4_0_OR_GREATER
            potentialNamespaceParent is not NamespaceDeclarationSyntax &&
            potentialNamespaceParent is not FileScopedNamespaceDeclarationSyntax)
#else
            potentialNamespaceParent is not NamespaceDeclarationSyntax)
#endif
        {
            potentialNamespaceParent = potentialNamespaceParent.Parent;
        }

#if ROSLYN_4_0_OR_GREATER
        var ns = potentialNamespaceParent as BaseNamespaceDeclarationSyntax;
#else
        var ns = potentialNamespaceParent as NamespaceDeclarationSyntax;
#endif

        if (ns != null)
        {
            result = ns.Name.ToString();
            while (true)
            {
                ns = ns.Parent as NamespaceDeclarationSyntax;
                if (ns == null)
                {
                    break;
                }

                result = $"{ns.Name}.{result}";
            }
        }

        return result;
    }

    private static ParseMethodAttributesResult ParseMethodAttributes(ImmutableArray<AttributeData> methodAttributes, SymbolHolder symbols)
    {
        List<string?> httpMethods = new();
        string? requestName = null;
        string? path = null;
        Dictionary<string, string> staticHeaders = new();

        foreach (var methodAttribute in methodAttributes)
        {
            var attributeSymbol = methodAttribute.AttributeClass;
            if (attributeSymbol == null)
            {
                continue;
            }

            if (attributeSymbol.Equals(symbols.RestGetAttribute, SymbolEqualityComparer.Default))
            {
                httpMethods.Add("Get");

                if (methodAttribute.ConstructorArguments.Length != 1)
                {
                    continue;
                }

                path = methodAttribute.ConstructorArguments[0].Value as string;
            }
            else if (attributeSymbol.Equals(symbols.RestPostAttribute, SymbolEqualityComparer.Default))
            {
                httpMethods.Add("Post");

                if (methodAttribute.ConstructorArguments.Length != 1)
                {
                    continue;
                }

                path = methodAttribute.ConstructorArguments[0].Value as string;
            }
            else if (attributeSymbol.Equals(symbols.RestPutAttribute, SymbolEqualityComparer.Default))
            {
                httpMethods.Add("Put");

                if (methodAttribute.ConstructorArguments.Length != 1)
                {
                    continue;
                }

                path = methodAttribute.ConstructorArguments[0].Value as string;
            }
            else if (attributeSymbol.Equals(symbols.RestDeleteAttribute, SymbolEqualityComparer.Default))
            {
                httpMethods.Add("Delete");

                if (methodAttribute.ConstructorArguments.Length != 1)
                {
                    continue;
                }

                path = methodAttribute.ConstructorArguments[0].Value as string;
            }
            else if (attributeSymbol.Equals(symbols.RestPatchAttribute, SymbolEqualityComparer.Default))
            {
                httpMethods.Add("Patch");

                if (methodAttribute.ConstructorArguments.Length != 1)
                {
                    continue;
                }

                path = methodAttribute.ConstructorArguments[0].Value as string;
            }
            else if (attributeSymbol.Equals(symbols.RestOptionsAttribute, SymbolEqualityComparer.Default))
            {
                httpMethods.Add("Options");

                if (methodAttribute.ConstructorArguments.Length != 1)
                {
                    continue;
                }

                path = methodAttribute.ConstructorArguments[0].Value as string;
            }
            else if (attributeSymbol.Equals(symbols.RestHeadAttribute, SymbolEqualityComparer.Default))
            {
                httpMethods.Add("Head");

                if (methodAttribute.ConstructorArguments.Length != 1)
                {
                    continue;
                }

                path = methodAttribute.ConstructorArguments[0].Value as string;
            }
            else if (attributeSymbol.Equals(symbols.RestRequestNameAttribute, SymbolEqualityComparer.Default))
            {
                if (methodAttribute.ConstructorArguments.Length != 1)
                {
                    continue;
                }

                requestName = methodAttribute.ConstructorArguments[0].Value as string;
            }
            else if (attributeSymbol.Equals(symbols.RestStaticHeaderAttribute, SymbolEqualityComparer.Default))
            {
                if (methodAttribute.ConstructorArguments.Length != 2)
                {
                    continue;
                }

                var key = methodAttribute.ConstructorArguments[0].Value as string;
                var value = methodAttribute.ConstructorArguments[1].Value as string;

                if (key == null || value == null)
                {
                    continue;
                }

                staticHeaders.Add(key, value);
            }
        }

        return new(httpMethods, path, requestName, staticHeaders);
    }

    private RestApiMethod? ProcessMethod(
        IMethodSymbol methodSymbol,
        SymbolHolder symbols,
        HashSet<string> requestNames)
    {
        var hasErrors = false;

        if (methodSymbol.Name[0] == '_')
        {
            // can't have method names that start with _ since that can lead to conflicting symbol names
            // because the generated symbols start with _
            Diag(DiagDescriptors.ErrorInvalidMethodName, methodSymbol.GetLocation());
            hasErrors = true;
        }

        if (methodSymbol.Arity > 0)
        {
            // we don't currently support generic methods
            Diag(DiagDescriptors.ErrorMethodIsGeneric, methodSymbol.GetLocation());
            hasErrors = true;
        }

        if (methodSymbol.IsStatic)
        {
            Diag(DiagDescriptors.ErrorStaticMethod, methodSymbol.GetLocation());
            hasErrors = true;
        }

        var methodAttrResult = ParseMethodAttributes(methodSymbol.GetAttributes(), symbols);
        if (methodAttrResult.HttpMethods.Count == 0 || methodAttrResult.Path == null)
        {
            Diag(DiagDescriptors.ErrorMissingMethodAttribute, methodSymbol.GetLocation());
            hasErrors = true;
        }

        if (methodAttrResult.HttpMethods.Count > 1)
        {
            Diag(DiagDescriptors.ErrorApiMethodMoreThanOneAttribute, methodSymbol.GetLocation());
            hasErrors = true;
        }

        var returnTypeSymbol = (INamedTypeSymbol)methodSymbol.ReturnType;
        ITypeSymbol? innerType = null;

        var returnType = methodSymbol.ReturnType.ToString();
        if (!returnType.StartsWith(ReturnTypePrefix, StringComparison.Ordinal))
        {
            Diag(DiagDescriptors.ErrorInvalidReturnType, methodSymbol.GetLocation());
            hasErrors = true;
        }
        else
        {
            if (returnTypeSymbol.TypeArguments.Length != 1)
            {
                Diag(DiagDescriptors.ErrorInvalidReturnType, methodSymbol.GetLocation());
                hasErrors = true;
            }

            innerType = returnTypeSymbol.TypeArguments[0];
            if (innerType.NullableAnnotation == NullableAnnotation.Annotated)
            {
                Diag(DiagDescriptors.ErrorInvalidReturnType, methodSymbol.GetLocation());
                hasErrors = true;
            }
        }

        if (methodAttrResult.Path != null && methodAttrResult.Path.Contains("?"))
        {
            Diag(DiagDescriptors.ErrorPathWithQuery, methodSymbol.GetLocation());
            hasErrors = true;
        }

        var requestName = methodAttrResult.RequestName ?? GetRequestName(methodSymbol.Name);

        if (!requestNames.Add(requestName))
        {
            Diag(DiagDescriptors.ErrorDuplicateRequestName, methodSymbol.GetLocation());
            hasErrors = true;
        }

        var restApiMethod = new RestApiMethod
        {
            HttpMethod = methodAttrResult.HttpMethods.FirstOrDefault(),
            MethodName = methodSymbol.Name,
            Path = methodAttrResult.Path,
            ReturnType = innerType?.ToDisplayString(_globalDisplayFormat),
            RequestName = requestName,
            StaticHeaders = methodAttrResult.StaticHeaders,
        };

        bool foundBody = false;
        bool foundCancellationToken = false;
        foreach (var paramSymbol in methodSymbol.Parameters)
        {
            var paramName = paramSymbol.Name;
            if (string.IsNullOrWhiteSpace(paramName))
            {
                // semantic problem, just bail quietly
                hasErrors = true;
            }

            var paramTypeSymbol = paramSymbol.Type;
            if (paramTypeSymbol is IErrorTypeSymbol)
            {
                // semantic problem, just bail quietly
                hasErrors = true;
            }

            if (paramName[0] == '_')
            {
                // can't have method parameter names that start with _ since that can lead to conflicting symbol names
                // because all generated symbols start with _
                Diag(DiagDescriptors.ErrorInvalidParameterName, paramSymbol.Locations[0]);
                hasErrors = true;
            }

            var paramAttributes = paramSymbol.GetAttributes();
            var isCancellationToken = paramTypeSymbol.ToString().Contains("System.Threading.CancellationToken");

            if (isCancellationToken)
            {
                if (foundCancellationToken)
                {
                    Diag(DiagDescriptors.ErrorDuplicateCancellationToken, paramSymbol.Locations[0], paramName);
                    hasErrors = true;
                }

                foundCancellationToken = true;
            }

            if (paramAttributes.IsEmpty && !isCancellationToken)
            {
                if (restApiMethod.Path == null || !restApiMethod.Path.Contains($"{{{paramName}}}"))
                {
                    Diag(DiagDescriptors.ErrorMissingParameterUrl, paramSymbol.Locations[0], paramName);
                    hasErrors = true;
                }
                else
                {
                    restApiMethod.FormatParameters.Add(paramName);
                }
            }

            var attrResult = ParseParameterAttributes(paramAttributes, symbols, paramName);

            if (attrResult.BodyType != null)
            {
                if (restApiMethod.HttpMethod == "Get" || restApiMethod.HttpMethod == "Head")
                {
                    Diag(DiagDescriptors.ErrorUnsupportedMethodBody, paramSymbol.Locations[0], restApiMethod.HttpMethod);
                    hasErrors = true;
                }

                if (foundBody)
                {
                    Diag(DiagDescriptors.ErrorDuplicateBody, paramSymbol.Locations[0]);
                    hasErrors = true;
                }

                foundBody = true;
            }

            var restApiMethodParameter = new RestApiMethodParameter
            {
                Name = paramName,
                Type = paramTypeSymbol.ToDisplayString(_globalDisplayFormat),
                HeaderName = attrResult.HeaderName,
                QueryKey = attrResult.QueryKey,
                BodyType = attrResult.BodyType,
                IsCancellationToken = isCancellationToken
            };

            restApiMethod.AllParameters.Add(restApiMethodParameter);
        }

        if (!foundCancellationToken)
        {
            Diag(DiagDescriptors.ErrorMissingCancellationToken, methodSymbol.Locations[0]);
            hasErrors = true;
        }

        return hasErrors ? null : restApiMethod;
    }

    private void Diag(DiagnosticDescriptor desc, Location? location)
    {
        _reportDiagnostic(Diagnostic.Create(desc, location, Array.Empty<object?>()));
    }

    private void Diag(DiagnosticDescriptor desc, Location? location, params object?[]? messageArgs)
    {
        _reportDiagnostic(Diagnostic.Create(desc, location, messageArgs));
    }

    private sealed record class ParseInterfaceAttributesResult(
        string? HttpClientName,
        string? CustomDependencyName,
        Dictionary<string, string> StaticHeaders);

    private sealed record class ParseParameterAttributesResult(
        string? HeaderName,
        string? QueryKey,
        BodyContentTypeParam? BodyType);

    private sealed record class ParseMethodAttributesResult(
        List<string?> HttpMethods,
        string? Path,
        string? RequestName,
        Dictionary<string, string> StaticHeaders);
}
