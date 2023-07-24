// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Extensions.ExtraAnalyzers.Utilities;

/// <summary>
/// Class contains <see cref="SyntaxNode"/> extensions.
/// </summary>
internal static class SyntaxNodeExtensions
{
    /// <summary>
    /// Finds the closest ancestor by syntax kind.
    /// </summary>
    /// <param name="node">The start node.</param>
    /// <param name="kind">The kind to search by.</param>
    /// <returns>The found node or <see langword="null" />.</returns>
    public static SyntaxNode? GetFirstAncestorOfSyntaxKind(this SyntaxNode node, SyntaxKind kind)
    {
        var n = node.Parent;
        while (n != null && !n.IsKind(kind))
        {
            n = n.Parent;
        }

        return n;
    }

    /// <summary>
    /// Checks node is invocation expression with specified name.
    /// </summary>
    /// <param name="node">Node to check.</param>
    /// <param name="semanticModel">Semantic model.</param>
    /// <param name="expectedFullMethodNames">Expected full method names.</param>
    /// <returns>Check result.</returns>
    public static bool NodeHasSpecifiedMethod(
        this SyntaxNode? node,
        SemanticModel semanticModel,
        ICollection<string> expectedFullMethodNames)
    {
        if (node is InvocationExpressionSyntax invocationExpression)
        {
            var memberSymbol = semanticModel.GetSymbolInfo(invocationExpression.Expression).Symbol as IMethodSymbol;
            if (memberSymbol == null)
            {
                return false;
            }

            var result = false;
            if (memberSymbol.ReducedFrom != null)
            {
                var fullMethodName = memberSymbol.ReducedFrom.ToString();
                result = expectedFullMethodNames.Contains(fullMethodName);
            }

            if (!result)
            {
                var fullMethodName = memberSymbol.OriginalDefinition.ToString();
                return expectedFullMethodNames.Contains(fullMethodName);
            }

            return result;
        }

        return false;
    }

    /// <summary>
    /// Returns invocation expression name.
    /// </summary>
    /// <param name="invocationExpression">The invocation expression.</param>
    /// <returns>The expression syntax name.</returns>
    public static SimpleNameSyntax? GetExpressionName(this InvocationExpressionSyntax invocationExpression)
    {
        if (invocationExpression.Expression is MemberAccessExpressionSyntax memberExpression)
        {
            return memberExpression.Name;
        }

        if (invocationExpression.Expression is MemberBindingExpressionSyntax memberBindingExpression)
        {
            return memberBindingExpression.Name;
        }

        return null;
    }

    /// <summary>
    /// Looks for a invocation node in a tree with a specified root type.
    /// </summary>
    /// <param name="nodeToStart">Node to start traversing.</param>
    /// <param name="semanticModel">The semantic model.</param>
    /// <param name="expectedFullMethodNames">Expected full method names.</param>
    /// <param name="typesToStopTraversing">Root node types.</param>
    /// <returns>Found invocation node or <see langword="null" />.</returns>
    public static SyntaxNode? FindNodeInTreeUpToSpecifiedParentByMethodName(
        this SyntaxNode nodeToStart,
        SemanticModel semanticModel,
        ICollection<string> expectedFullMethodNames,
        ICollection<Type> typesToStopTraversing)
    {
        var currentNode = nodeToStart;
        do
        {
            var foundNode = currentNode.DescendantNodesAndSelf()
                .FirstOrDefault(n => n.NodeHasSpecifiedMethod(semanticModel, expectedFullMethodNames));
            if (foundNode != null)
            {
                return foundNode;
            }

            currentNode = currentNode.Parent;
        }
        while (currentNode != null && !typesToStopTraversing.Contains(currentNode.GetType()));

        return currentNode?
                .DescendantNodesAndSelf()
                .FirstOrDefault(n => n.NodeHasSpecifiedMethod(semanticModel, expectedFullMethodNames));
    }

    /// <summary>
    /// Checks <see cref="IdentifierNameSyntax"/> has expected name.
    /// </summary>
    /// <param name="expression">Expression syntax to check.</param>
    /// <param name="expectedName">Expected name.</param>
    /// <returns><see langword="true" /> if the identifier text is equal to expected name; otherwise, <see langword="false" />.</returns>
    public static bool IdentifierNameEquals(this ExpressionSyntax expression, string expectedName)
    {
        return expression is IdentifierNameSyntax id && id.Identifier.Text == expectedName;
    }
}
