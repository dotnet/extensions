// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

namespace System.Cloud.Messaging.DependencyInjection.Tests.Data.Delegates;

internal class SampleWriterDelegate
{
    public IMessageDestination MessageDestination { get; }

    public SampleWriterDelegate(IMessageDestination messageWriter)
    {
        MessageDestination = Throw.IfNull(messageWriter);
    }

    /// <summary>
    /// The <see cref="MessageDelegate"/> implementation.
    /// </summary>
    public ValueTask InvokeAsync(MessageContext context) => MessageDestination.WriteAsync(context);
}
