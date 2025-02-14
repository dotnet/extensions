// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Threading;

namespace Microsoft.Extensions.AI;

/// <summary>Provides additional context to the invocation of an <see cref="AIFunction"/> created by <see cref="AIFunctionFactory"/>.</summary>
/// <remarks>
/// A delegate or <see cref="MethodInfo"/> passed to <see cref="AIFunctionFactory"/> methods can represent a method that has a parameter
/// of type <see cref="AIFunctionContext"/>. Whereas all other parameters are passed by name from the supplied collection of arguments,
/// an <see cref="AIFunctionContext"/> parameter is passed specially by the <see cref="AIFunction"/> implementation to pass relevant
/// context into the method's invocation. For example, any <see cref="CancellationToken"/> passed to the <see cref="AIFunction.InvokeAsync"/>
/// method is available from the <see cref="AIFunctionContext.CancellationToken"/> property.
/// </remarks>
public class AIFunctionContext
{
    /// <summary>Initializes a new instance of the <see cref="AIFunctionContext"/> class.</summary>
    public AIFunctionContext()
    {
    }

    /// <summary>Gets or sets a <see cref="CancellationToken"/> related to the operation.</summary>
    public CancellationToken CancellationToken { get; set; }
}
