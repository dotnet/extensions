// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;

namespace Microsoft.TestUtilities;

/// <summary>
/// Skips a test based on the value of an environment variable.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true)]
public class EnvironmentVariableConditionAttribute : Attribute, ITestCondition
{
    private string? _currentValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="EnvironmentVariableConditionAttribute"/> class.
    /// </summary>
    /// <param name="variableName">Name of the environment variable.</param>
    /// <param name="values">Value(s) of the environment variable to match for the condition.</param>
    /// <remarks>
    /// By default, the test will be run if the value of the variable matches any of the supplied values.
    /// Set <see cref="RunOnMatch"/> to <c>False</c> to run the test only if the value does not match.
    /// </remarks>
    public EnvironmentVariableConditionAttribute(string variableName, params string[] values)
    {
        if (string.IsNullOrEmpty(variableName))
        {
            throw new ArgumentException("Value cannot be null or empty.", nameof(variableName));
        }

        if (values == null || values.Length == 0)
        {
            throw new ArgumentException("You must supply at least one value to match.", nameof(values));
        }

        VariableName = variableName;
        Values = values;
    }

    /// <summary>
    /// Gets or sets a value indicating whether the test should run if the value of the variable matches any
    /// of the supplied values. If <c>False</c>, the test runs only if the value does not match any of the
    /// supplied values. Default is <c>True</c>.
    /// </summary>
    public bool RunOnMatch { get; set; } = true;

    /// <summary>
    /// Gets the name of the environment variable.
    /// </summary>
    public string VariableName { get; }

    /// <summary>
    /// Gets the value(s) of the environment variable to match for the condition.
    /// </summary>
    public string[] Values { get; }

    /// <summary>
    /// Gets a value indicating whether the condition is met for the configured environment variable and values.
    /// </summary>
    public bool IsMet
    {
        get
        {
            _currentValue ??= Environment.GetEnvironmentVariable(VariableName);
            var hasMatched = Values.Any(value => string.Equals(value, _currentValue, StringComparison.OrdinalIgnoreCase));

            return RunOnMatch ? hasMatched : !hasMatched;
        }
    }

    /// <summary>
    /// Gets a value indicating the reason the test was skipped.
    /// </summary>
    public string SkipReason
    {
        get
        {
            var value = _currentValue ?? "(null)";

            return $"Test skipped on environment variable with name '{VariableName}' and value '{value}' " +
                $"for the '{nameof(RunOnMatch)}' value of '{RunOnMatch}'.";
        }
    }
}
