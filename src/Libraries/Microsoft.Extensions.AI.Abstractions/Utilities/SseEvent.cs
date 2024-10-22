// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable SA1114 // Parameter list should follow declaration
#pragma warning disable CA1815 // Override equals and operator equals on value types

namespace Microsoft.Extensions.AI;

/// <summary>Represents a server-sent event.</summary>
/// <typeparam name="T">Specifies the type of data payload in the event.</typeparam>
public readonly struct SseEvent<T>
{
    /// <summary>Initializes a new instance of the <see cref="SseEvent{T}"/> struct.</summary>
    /// <param name="data">The event's payload.</param>
    public SseEvent(T data)
    {
        Data = data;
    }

    /// <summary>Gets the event's payload.</summary>
    public T Data { get; }

    /// <summary>Gets the event's type.</summary>
    public string? EventType { get; init; }

    /// <summary>Gets the event's identifier.</summary>
    public string? Id { get; init; }
}
