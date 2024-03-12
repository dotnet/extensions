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
using Microsoft.Extensions.Options.Contextual.Provider;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Options.Contextual.Internal;

/// <summary>
/// Implementation of <see cref="IContextualOptionsFactory{TOptions}"/>.
/// </summary>
/// <typeparam name="TOptions">The type of options being requested.</typeparam>
internal sealed class ContextualOptionsFactory<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TOptions> : IContextualOptionsFactory<TOptions>
    where TOptions : class
{
    private readonly ILoadContextualOptions<TOptions>[] _loaders;

    private readonly IConfigureOptions<TOptions>[] _setups;
    private readonly IPostConfigureOptions<TOptions>[] _postConfigures;
    private readonly IValidateOptions<TOptions>[] _validations;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContextualOptionsFactory{TOptions}"/> class.
    /// </summary>
    /// <param name="loaders">The configuration loaders to run.</param>
    /// <param name="setups">The configuration actions to run.</param>
    /// <param name="postConfigures">The initialization actions to run.</param>
    /// <param name="validations">The validations to run.</param>
    public ContextualOptionsFactory(
        IEnumerable<ILoadContextualOptions<TOptions>> loaders,
        IEnumerable<IConfigureOptions<TOptions>> setups,
        IEnumerable<IPostConfigureOptions<TOptions>> postConfigures,
        IEnumerable<IValidateOptions<TOptions>> validations)
    {
        _loaders = loaders.ToArray();
        _setups = setups as IConfigureOptions<TOptions>[] ?? setups.ToArray();
        _postConfigures = postConfigures as IPostConfigureOptions<TOptions>[] ?? postConfigures.ToArray();
        _validations = validations as IValidateOptions<TOptions>[] ?? validations.ToArray();
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
        TOptions options = Activator.CreateInstance<TOptions>();

        // Set the default setup.
        foreach (IConfigureOptions<TOptions> setup in _setups)
        {
            if (setup is IConfigureNamedOptions<TOptions> namedSetup)
            {
                namedSetup.Configure(name, options);
            }
            else if (name == Options.DefaultName)
            {
                setup.Configure(options);
            }
        }

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

            foreach (IPostConfigureOptions<TOptions> post in _postConfigures)
            {
                post.PostConfigure(name, options);
            }

            if (_validations.Length > 0)
            {
                var failures = new List<string>();
                foreach (IValidateOptions<TOptions> validate in _validations)
                {
                    ValidateOptionsResult result = validate.Validate(name, options);
                    if (result is not null && result.Failed)
                    {
                        failures.AddRange(result.Failures);
                    }
                }

                if (failures.Count > 0)
                {
                    throw new OptionsValidationException(name, typeof(TOptions), failures);
                }
            }

            return options;
        }
    }
}
