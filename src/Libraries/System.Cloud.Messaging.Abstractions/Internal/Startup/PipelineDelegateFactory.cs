// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Shared.Diagnostics;

namespace System.Cloud.Messaging.Internal;

/// <summary>
/// Implementation for <see cref="IPipelineDelegateFactory"/>.
/// </summary>
internal sealed class PipelineDelegateFactory : IPipelineDelegateFactory
{
    public const string IncorrectTerminalDelegateConfigurationError = $"Please check the configuration for the injected terminal {nameof(IMessageDelegate)}.";
    public const string IncorrectMiddlewaresConfigurationError = $"Please check the configuration for the injected pipeline of {nameof(IMessageMiddleware)}.";

    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="PipelineDelegateFactory"/> class.
    /// </summary>
    /// <param name="serviceProvider"><see cref="IServiceProvider"/>.</param>
    public PipelineDelegateFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = Throw.IfNull(serviceProvider);
    }

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException">If the any of the parameters is null/empty.</exception>
    public IMessageDelegate Create(string pipelineName)
    {
        _ = Throw.IfNullOrEmpty(pipelineName);

        var namedMessageDelegateFactory = _serviceProvider.GetRequiredService<INamedServiceProvider<Func<IServiceProvider, IMessageDelegate>>>();
        Func<IServiceProvider, IMessageDelegate> messageDelegateFactory = namedMessageDelegateFactory.GetRequiredService(pipelineName);
        _ = Throw.IfNull(messageDelegateFactory);

        IMessageDelegate currentMessageDelegate;
        try
        {
            currentMessageDelegate = messageDelegateFactory(_serviceProvider);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(IncorrectTerminalDelegateConfigurationError, ex);
        }

        _ = Throw.IfNull(currentMessageDelegate);

        IEnumerable<IMessageMiddleware> middlewares;
        try
        {
            var middlewaresProvider = _serviceProvider.GetRequiredService<INamedServiceProvider<IMessageMiddleware>>();
            middlewares = middlewaresProvider.GetServices(pipelineName);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(IncorrectMiddlewaresConfigurationError, ex);
        }

        Stack<IMessageMiddleware> middlewaresStack = new(middlewares);
        while (middlewaresStack.Count > 0)
        {
            IMessageMiddleware messageMiddleware = middlewaresStack.Pop();
            _ = Throw.IfNull(messageMiddleware);

            currentMessageDelegate = messageMiddleware.Stitch(currentMessageDelegate);
            _ = Throw.IfNull(currentMessageDelegate);
        }

        return currentMessageDelegate;
    }
}
