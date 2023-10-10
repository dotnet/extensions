// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Options.Contextual;

/// <summary>
/// Implementation of <see cref="IContextualOptionsFactory{TOptions}"/>.
/// </summary>
/// <typeparam name="TOptions">The type of options being requested.</typeparam>
internal sealed class ContextualOptionsFactory<TOptions> : IContextualOptionsFactory<TOptions>
    where TOptions : class
{
    private readonly IOptionsFactory<TOptions> _baseFactory;
    private readonly ILoadContextualOptions<TOptions>[] _loaders;
    private readonly IPostConfigureContextualOptions<TOptions>[] _postConfigures;
    private readonly IValidateContextualOptions<TOptions>[] _validations;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContextualOptionsFactory{TOptions}"/> class.
    /// </summary>
    /// <param name="baseFactory">The factory to create instances of <typeparamref name="TOptions"/> with.</param>
    /// <param name="loaders">The configuration loaders to run.</param>
    /// <param name="postConfigures">The initialization actions to run.</param>
    /// <param name="validations">The validations to run.</param>
    public ContextualOptionsFactory(
        IOptionsFactory<TOptions> baseFactory,
        IEnumerable<ILoadContextualOptions<TOptions>> loaders,
        IEnumerable<IPostConfigureContextualOptions<TOptions>> postConfigures,
        IEnumerable<IValidateContextualOptions<TOptions>> validations)
    {
        _baseFactory = baseFactory;
        _loaders = loaders.ToArray();
        _postConfigures = postConfigures.ToArray();
        _validations = validations.ToArray();
    }

    /// <inheritdoc/>
    [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "The ValueTasks are awaited only once.")]
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "We need to catch it all so we can rethrow it all.")]
    public ValueTask<TOptions> CreateAsync<TContext>(string name, in TContext context, CancellationToken cancellationToken)
        where TContext : notnull, IOptionsContext
    {
        _ = Throw.IfNull(name);
        _ = Throw.IfNull(context);

        cancellationToken.ThrowIfCancellationRequested();
        var options = _baseFactory.Create(name);
        return ConfigureOptions(context);

        async ValueTask<TOptions> ConfigureOptions(TContext context)
        {
            var loadTasks = ArrayPool<ValueTask<IConfigureContextualOptions<TOptions>>>.Shared.Rent(_loaders.Length);
            var tasksCreated = 0;
            List<Exception>? loadExceptions = null;

            foreach (var loader in _loaders)
            {
                try
                {
                    loadTasks[tasksCreated] = loader.LoadAsync(name, context, cancellationToken);
                    tasksCreated++;
                }
                catch (Exception e)
                {
                    loadExceptions ??= [];
                    loadExceptions.Add(e);
                    break;
                }
            }

            for (var i = 0; i < tasksCreated; i++)
            {
                try
                {
                    using var configurer = await loadTasks[i].ConfigureAwait(false); // ValueTasks are awaited only here and only once.
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        configurer.Configure(options);
                    }
                }
                catch (Exception e)
                {
                    loadExceptions ??= [];
                    loadExceptions.Add(e);
                }
                finally
                {
                    loadTasks[i] = default;
                }
            }

            ArrayPool<ValueTask<IConfigureContextualOptions<TOptions>>>.Shared.Return(loadTasks);

            if (loadExceptions is not null)
            {
                throw new AggregateException(loadExceptions);
            }

            cancellationToken.ThrowIfCancellationRequested();

            foreach (var post in _postConfigures)
            {
                post.PostConfigure(name, context, options);
            }

            List<string>? failures = default;
            foreach (var validate in _validations)
            {
                var result = validate.Validate(name, options);
                if (result.Failed)
                {
                    failures ??= [];
                    failures.AddRange(result.Failures);
                }
            }

            if (failures is not null)
            {
                throw new OptionsValidationException(name, typeof(TOptions), failures);
            }

            return options;
        }
    }
}
