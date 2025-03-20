// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using System.Threading;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents options that can be provided when creating an <see cref="AIFunction"/> from a method.
/// </summary>
public sealed class AIFunctionFactoryOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIFunctionFactoryOptions"/> class.
    /// </summary>
    public AIFunctionFactoryOptions()
    {
    }

    /// <summary>Gets or sets the <see cref="JsonSerializerOptions"/> used to marshal .NET values being passed to the underlying delegate.</summary>
    /// <remarks>
    /// If no value has been specified, the <see cref="AIJsonUtilities.DefaultOptions"/> instance will be used.
    /// </remarks>
    public JsonSerializerOptions? SerializerOptions { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="AIJsonSchemaCreateOptions"/> governing the generation of JSON schemas for the function.
    /// </summary>
    /// <remarks>
    /// If no value has been specified, the <see cref="AIJsonSchemaCreateOptions.Default"/> instance will be used.
    /// </remarks>
    public AIJsonSchemaCreateOptions? JsonSchemaCreateOptions { get; set; }

    /// <summary>Gets or sets the name to use for the function.</summary>
    /// <value>
    /// The name to use for the function. The default value is a name derived from the method represented by the passed <see cref="Delegate"/> or <see cref="MethodInfo"/>.
    /// </value>
    public string? Name { get; set; }

    /// <summary>Gets or sets the description to use for the function.</summary>
    /// <value>
    /// The description for the function. The default value is a description derived from the passed <see cref="Delegate"/> or <see cref="MethodInfo"/>, if possible
    /// (for example, via a <see cref="DescriptionAttribute"/> on the method).
    /// </value>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets additional values to store on the resulting <see cref="AITool.AdditionalProperties" /> property.
    /// </summary>
    /// <remarks>
    /// This property can be used to provide arbitrary information about the function.
    /// </remarks>
    public IReadOnlyDictionary<string, object?>? AdditionalProperties { get; set; }

    /// <summary>
    /// Gets or sets a delegate that binds parameters to function arguments.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When set to a non-<see langword="null"/> value, this delegate will be invoked for each parameter
    /// in a function each time that function is invoked, allowing the caller to provide custom logic for binding
    /// the parameter to an argument. This delegate is in complete control of the process, including
    /// whether to throw an exception if the parameter cannot be bound (if the delegate returns
    /// <see langword="null"/>), the default value for the parameter's type will be used.
    /// </para>
    /// <para>
    /// When setting this property, the caller should ensure that <see cref="JsonSchemaCreateOptions"/>
    /// is configured in a consistent manner. In particular, for any parameter that <see cref="ArgumentBinder" />
    /// binds to something other than the corresponding value from the arguments dictionary, the
    /// <see cref="AIJsonSchemaCreateOptions.IncludeParameter"/> delegate typically should be set to
    /// exclude that parameter from the generated schema.
    /// </para>
    /// <para>
    /// This delegate is not invoked for parameters of type <see cref="CancellationToken"/>,
    /// which are always handled by <see cref="AIFunctionFactory"/>, invariably binding them to the
    /// <see cref="CancellationToken" /> provided to the <see cref="AIFunction.InvokeAsync"/> call.
    /// </para>
    /// </remarks>
    public ArgumentBinderFunc? ArgumentBinder { get; set; }

    /// <summary>Delegate type used with <see cref="ArgumentBinder"/>.</summary>
    /// <param name="parameter">The information about the parameter to bind.</param>
    /// <param name="arguments">The <see cref="AIFunctionArguments"/> provided to the function's invocation.</param>
    /// <param name="value">The argument value selected by the binder to be used as the parameter's value.</param>
    /// <returns>
    /// <see langword="true"/> if the binder chooses to provide an argument value for the parameter,
    /// in which case <paramref name="value"/> stores the selected value; otherwise, <see langword="false"/>,
    /// in which case the default binding will be used for the parameter.
    /// </returns>
    public delegate bool ArgumentBinderFunc(ParameterInfo parameter, AIFunctionArguments arguments, out object? value);
}
