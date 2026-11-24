// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;

namespace Microsoft.Extensions.AI;

internal sealed class DecisionDefinitionQuestion<TResult>(
    PropertyInfo property,
    string propertyName,
    DecisionQuestion question,
    object? enumDefinition,
    bool usesExpectedScore,
    Func<TResult, object?> valueAccessor,
    Func<object?, string, bool>? choiceValueMatches)
{
    public PropertyInfo Property { get; } = property;

    public string PropertyName { get; } = propertyName;

    public DecisionQuestion Question { get; } = question;

    public object? EnumDefinition { get; } = enumDefinition;

    public bool UsesExpectedScore { get; } = usesExpectedScore;

    public Func<TResult, object?> ValueAccessor { get; } = valueAccessor;

    public Func<object?, string, bool>? ChoiceValueMatches { get; } = choiceValueMatches;
}
