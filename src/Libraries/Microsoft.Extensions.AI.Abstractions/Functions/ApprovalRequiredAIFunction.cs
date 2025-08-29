// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Marks an existing <see cref="AIFunction"/> with additional metadata to indicate that it requires approval.
/// </summary>
[Experimental("MEAI001")]
public sealed class ApprovalRequiredAIFunction : DelegatingAIFunction
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApprovalRequiredAIFunction"/> class.
    /// </summary>
    /// <param name="function">The <see cref="AIFunction"/> that requires approval.</param>
    public ApprovalRequiredAIFunction(AIFunction function)
        : base(function)
    {
        RequiresApprovalCallback = (_, _) => new(true);
    }

    /// <summary>
    /// Gets or sets an optional callback that can be used to determine if the function call requires approval, instead of the default behavior, which is to always require approval.
    /// </summary>
    public Func<ApprovalContext, CancellationToken, ValueTask<bool>> RequiresApprovalCallback { get; set; }

    /// <summary>
    /// Context object that provides information about the function call that requires approval.
    /// </summary>
    public sealed class ApprovalContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ApprovalContext"/> class.
        /// </summary>
        /// <param name="functionCall">The <see cref="FunctionCallContent"/> containing the details of the invocation.</param>
        /// <exception cref="ArgumentNullException"><paramref name="functionCall"/> is null.</exception>
        public ApprovalContext(FunctionCallContent functionCall)
        {
            FunctionCall = Throw.IfNull(functionCall);
        }

        /// <summary>
        /// Gets the <see cref="FunctionCallContent"/> containing the details of the invocation that will be made if approval is granted.
        /// </summary>
        public FunctionCallContent FunctionCall { get; }
    }
}
