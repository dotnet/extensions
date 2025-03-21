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

    /// <summary>Gets or sets a delegate used to determine how a particular parameter to the function should be bound.</summary>
    /// <remarks>
    /// <para>
    /// If <see langword="null"/>, the default parameter binding logic will be used. If non-<see langword="null"/> value,
    /// this delegate will be invoked once for each parameter in the function as part of creating the <see cref="AIFunction"/> instance.
    /// It is not invoked for parameters of type <see cref="CancellationToken"/>, which are invariably bound to the token
    /// provided to the <see cref="AIFunction.InvokeAsync"/> invocation.
    /// </para>
    /// <para>
    /// Returning a default <see cref="ParameterBindingOptions"/> results in the same behavior as if
    /// <see cref="ConfigureParameterBinding"/> is <see langword="null"/>.
    /// </para>
    /// </remarks>
    public Func<ParameterInfo, ParameterBindingOptions>? ConfigureParameterBinding { get; set; }

    /// <summary>Provides configuration options produced by the <see cref="ConfigureParameterBinding"/> delegate.</summary>
    public readonly record struct ParameterBindingOptions
    {
        /// <summary>Gets a delegate used to determine the value for a bound parameter.</summary>
        /// <remarks>
        /// <para>
        /// The default value is <see langword="null"/>.
        /// </para>
        /// <para>
        /// If <see langword="null"/>, the default binding semantics are used for the parameter.
        /// If non- <see langword="null"/>, each time the <see cref="AIFunction"/> is invoked, this delegate will be invoked
        /// to select the argument value to use for the parameter. The return value of the delegate will be used for the parameter's
        /// value.
        /// </para>
        /// </remarks>
        public Func<ParameterInfo, AIFunctionArguments, object?>? BindParameter { get; init; }

        /// <summary>Gets a value indicating whether the parameter should be excluded from the generated schema.</summary>
        /// <remarks>
        /// <para>
        /// The default value is <see langword="false"/>.
        /// </para>
        /// <para>
        /// Typically, this property is set to <see langword="true"/> if and only if <see cref="BindParameter"/> is also set to
        /// non-<see langword="null"/>. While it's possible to exclude the schema when <see cref="BindParameter"/> is <see langword="null"/>,
        /// doing so means that default marshaling will be used but the AI service won't be aware of the parameter or able to generate
        /// an argument for it. This is likely to result in invocation errors, as the parameter information is unlikely to be available.
        /// It, however, is permissible for cases where invocation of the <see cref="AIFunction"/> is tightly controlled, and the caller
        /// is expected to augment the argument dictionary with the parameter value.
        /// </para>
        /// </remarks>
        public bool ExcludeFromSchema { get; init; }
    }
}
