// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI.Functions;

/// <summary>
/// Defines an exception raised in an <see cref="AIFunction"/> invocation containing a message intended for the model detailing the reason for the failure.
/// </summary>
#pragma warning disable CA1032 // Implement standard exception constructors
public sealed class AIFunctionException : Exception
#pragma warning restore CA1032 // Implement standard exception constructors
{
    /// <summary>
    /// Gets the message intended for the model.
    /// </summary>
    public string MessageForModel { get; private init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AIFunctionException"/> class.
    /// </summary>
    /// <param name="messageForModel">The message intended for the model.</param>
    /// <param name="message">The message that can contain information not intended to share with the model.</param>
    public AIFunctionException(string messageForModel, string? message = null)
        : base(GetMessageForBase(messageForModel, message))
    {
        if (messageForModel is null)
        {
            Throw.ArgumentNullException(nameof(messageForModel));
        }

        MessageForModel = messageForModel;
    }

    private static string GetMessageForBase(string messageForModel, string? message)
    {
        if (message is null)
        {
            return messageForModel;
        }

        return message + " Message for model: " + messageForModel;
    }
}
