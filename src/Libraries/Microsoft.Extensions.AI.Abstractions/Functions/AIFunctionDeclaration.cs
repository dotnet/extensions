// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Threading.Tasks;

namespace Microsoft.Extensions.AI;

/// <summary>Represents a function that can be described to an AI service.</summary>
/// <remarks>
/// <see cref="AIFunctionDeclaration"/> is the base class for <see cref="AIFunction"/>, which
/// adds the ability to invoke the function. Components can type test <see cref="AITool"/> instances
/// for <see cref="AIFunctionDeclaration"/> to determine whether they can be described as functions,
/// and can type test for <see cref="AIFunction"/> to determine whether they can be invoked.
/// </remarks>
public abstract class AIFunctionDeclaration : AITool
{
    /// <summary>Initializes a new instance of the <see cref="AIFunctionDeclaration"/> class.</summary>
    protected AIFunctionDeclaration()
    {
    }

    /// <summary>Gets a JSON Schema describing the function and its input parameters.</summary>
    /// <remarks>
    /// <para>
    /// When specified, declares a self-contained JSON schema document that describes the function and its input parameters.
    /// A simple example of a JSON schema for a function that adds two numbers together is shown below:
    /// </para>
    /// <code>
    /// {
    ///   "type": "object",
    ///   "properties": {
    ///     "a" : { "type": "number" },
    ///     "b" : { "type": ["number","null"], "default": 1 }
    ///   },
    ///   "required" : ["a"]
    /// }
    /// </code>
    /// <para>
    /// The metadata present in the schema document plays an important role in guiding AI function invocation.
    /// </para>
    /// <para>
    /// When an <see cref="AIFunction"/> is created via <see cref="AIFunctionFactory"/>, this schema is automatically derived from the
    /// method's parameters using the configured <see cref="JsonSerializerOptions"/> and <see cref="AIJsonSchemaCreateOptions"/>.
    /// </para>
    /// <para>
    /// When no schema is specified, consuming chat clients should assume the "{}" or "true" schema, indicating that any JSON input is admissible.
    /// </para>
    /// </remarks>
    public virtual JsonElement JsonSchema => AIJsonUtilities.DefaultJsonSchema;

    /// <summary>Gets a JSON Schema describing the function's return value.</summary>
    /// <remarks>
    /// <para>
    /// When an <see cref="AIFunction"/> is created via <see cref="AIFunctionFactory"/>, this schema is automatically derived from the
    /// method's return type using the configured <see cref="JsonSerializerOptions"/> and <see cref="AIJsonSchemaCreateOptions"/>.
    /// For methods returning <see cref="Task{TResult}"/> or <see cref="ValueTask{TResult}"/>, the schema is based on the
    /// unwrapped result type. Return schema generation can be suppressed by setting
    /// <see cref="AIFunctionFactoryOptions.ExcludeResultSchema"/> to <see langword="true"/>.
    /// </para>
    /// <para>
    /// A <see langword="null"/> value typically reflects a function that doesn't specify a return schema,
    /// a function that returns <see cref="void"/>, <see cref="Task"/>, or <see cref="ValueTask"/>,
    /// or a function for which <see cref="AIFunctionFactoryOptions.ExcludeResultSchema"/> was set to <see langword="true"/>.
    /// </para>
    /// </remarks>
    public virtual JsonElement? ReturnJsonSchema => null;
}
