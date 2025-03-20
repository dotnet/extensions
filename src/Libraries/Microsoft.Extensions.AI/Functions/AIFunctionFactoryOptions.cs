// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;

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
    /// If <see langword="null"/>, the default parameter binding logic will be used, and <see cref="BindParameter"/> will be ignored.
    /// If set to a non-<see langword="null"/> value, this delegate will be invoked once for each parameter in the function as part of
    /// creating the <see cref="AIFunction"/> instance.
    /// </para>
    /// </remarks>
    public Func<ParameterInfo, ParameterBindingOptions>? ConfigureParameterBinding { get; set; }

    /// <summary>Gets or sets a delegate used to determine the value for a bound parameter.</summary>
    /// <remarks>
    /// For any parameter for which <see cref="ConfigureParameterBinding"/> returns a result including
    /// <see cref="ParameterBindingOptions.UseBindParameter"/> set to true, <see cref="BindParameter"/>
    /// will be invoked for that parameter each time the <see cref="AIFunction"/> is invoked. The <see cref="BindParameter"/>
    /// delegate will be passed the <see cref="ParameterInfo" /> for the parameter, the <see cref="AIFunctionArguments"/> used
    /// with the <see cref="AIFunction.InvokeAsync"/> call, and the <see cref="ParameterBindingOptions"/> result produced by
    /// the <see cref="ConfigureParameterBinding"/> delegate. The return value of the delegate will be used for the parameter's
    /// value. If <see cref="ParameterBindingOptions"/> is <see langword="null"/> or if the <see cref="ParameterBindingOptions"/>
    /// it produces has <see cref="ParameterBindingOptions.UseBindParameter"/> as <see langword="false"/>, this delegate will be
    /// ignored for that parameter.
    /// </remarks>
    public Func<ParameterBindingOptions, ParameterInfo, AIFunctionArguments, object?>? BindParameter { get; set; }

    /// <summary>Provides configuration options produced by the <see cref="ConfigureParameterBinding"/> delegate.</summary>
    [StructLayout(LayoutKind.Auto)]
    public readonly record struct ParameterBindingOptions
    {
        /// <summary>Gets a value indicating whether the <see cref="BindParameter"/> delegate should be used for this parameter.</summary>
        /// <remarks>
        /// <para>
        /// The default value is <see langword="false"/>.
        /// </para>
        /// <para>
        /// If this property is <see langword="false"/>, the associated parameter will employ the default binding.
        /// If this property is <see langword="true"/>, the <see cref="BindParameter"/> delegate will be invoked to be
        /// solely responsible for the value to use for the property.
        /// </para>
        /// <para>
        /// Typically, this value is used in combination with <see cref="ExcludeFromSchema"/> also set to <see langword="true"/>,
        /// for cases where <see cref="BindParameter"/> will source the argument value for the parameter from somewhere other than
        /// the argument dictionary (for example, from its <see cref="AIFunctionArguments.Services"/> or
        /// <see cref="AIFunctionArguments.Context"/>. However, it may be used even when <see cref="ExcludeFromSchema"/> is <see langword="false"/>,
        /// in a situation where it's desirable for the AI service to generate a value for the parameter, which <see cref="BindParameter"/>
        /// may then use as part of its logic.
        /// </para>
        /// <para>
        /// It is an error for <see cref="ConfigureParameterBinding"/> to return a <see cref="ParameterBindingOptions"/> with
        /// <see cref="UseBindParameter"/> set to <see langword="true"/> when <see cref="BindParameter"/> is <see langword="null"/>.
        /// </para>
        /// </remarks>
        public bool UseBindParameter { get; init; }

        /// <summary>Gets a value indicating whether the parameter should be excluded from the generated schema.</summary>
        /// <remarks>
        /// <para>
        /// The default value is <see langword="false"/>.
        /// </para>
        /// <para>
        /// Typically, this property is set to <see langword="true"/> when <see cref="UseBindParameter"/> is also set to
        /// <see langword="true"/>. While it's possible to exclude the schema when <see cref="UseBindParameter"/> is <see langword="false"/>,
        /// doing so means that default marshaling will be used but the AI service won't be aware of the parameter or able to generate
        /// an argument for it. This is likely to result in invocation errors, as the parameter information is unlikely to be available.
        /// It, however, is permissible for cases where invocation of the <see cref="AIFunction"/> is tightly controlled, and the caller
        /// is expected to augment the argument dictionary with the parameter value.
        /// </para>
        /// </remarks>
        public bool ExcludeFromSchema { get; init; }

        /// <summary>
        /// Gets additional context information that can be passed from <see cref="ConfigureParameterBinding"/> into
        /// <see cref="BindParameter"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The default value is <see langword="null"/>.
        /// </para>
        /// <para>
        /// In situations where <see cref="ConfigureParameterBinding"/> retrieves metadata about a parameter in order
        /// to decide how it should be handled, that metadata is often also useful in <see cref="BindParameter"/>.
        /// <see cref="ConfigureParameterBinding"/> can pass that information into <see cref="BindParameter"/> via
        /// <see cref="Context"/>, rather than <see cref="BindParameter"/> needing to rediscover it.
        /// </para>
        /// </remarks>
        public object? Context { get; init; }
    }
}
