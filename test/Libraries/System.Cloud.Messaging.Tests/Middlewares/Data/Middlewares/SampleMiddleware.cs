// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;

namespace System.Cloud.Messaging.Middlewares.Tests.Data.Middlewares;

internal class SampleMiddleware : IMessageMiddleware
{
    /// <inheritdoc/>
    public ValueTask InvokeAsync(MessageContext context, MessageDelegate nextHandler) => nextHandler.Invoke(context);
}
