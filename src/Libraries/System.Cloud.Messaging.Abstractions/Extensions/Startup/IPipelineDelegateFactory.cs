// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace System.Cloud.Messaging;

/// <summary>
/// Factory interface for obtaining a composable <see cref="IMessageDelegate"/> from the registered pipeline of <see cref="IMessageMiddleware"/>
/// and a terminal <see cref="IMessageDelegate"/> types which can act on the messages from <see cref="IMessageConsumer"/>.
/// </summary>
public interface IPipelineDelegateFactory
{
    /// <summary>
    /// Creates the <see cref="IMessageDelegate"/> given <see cref="IServiceProvider"/>.
    /// </summary>
    /// <param name="pipelineName">Name of the pipeline.</param>
    /// <returns>Function of <see cref="IServiceProvider"/> and <see cref="IMessageDelegate"/>.</returns>
    public IMessageDelegate Create(string pipelineName);
}
