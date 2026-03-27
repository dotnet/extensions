// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace Microsoft.Extensions.VectorData.ProviderServices.Filter;

/// <summary>
/// An expression representation a query parameter (captured variable) in the filter expression.
/// </summary>
[Experimental("MEVD9001")]
public class QueryParameterExpression(string name, object? value, Type type) : Expression
{
    /// <summary>
    /// Gets the name of the parameter.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// Gets the value of the parameter.
    /// </summary>
    public object? Value { get; } = value;

    /// <inheritdoc />
    public override ExpressionType NodeType => ExpressionType.Extension;

    /// <inheritdoc />
    public override Type Type => type;

    /// <inheritdoc />
    protected override Expression VisitChildren(ExpressionVisitor visitor) => this;
}
