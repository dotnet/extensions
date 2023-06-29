// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if !NET6_0_OR_GREATER

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Shared.Diagnostics;
using Microsoft.Shared.Pools;
using Microsoft.Shared.Text;

namespace Microsoft.Extensions.Options.Validation;

internal sealed class ValidationHostedService : IHostedService
{
    internal const string FriendlyMessageTemplate = "Option instance of type '{0}' with name '{1}' is invalid because: '{2}'. Failed members: {3}";
    internal const string MemberSeparator = "; ";
    internal const string Unknown = "Unknown";

    private static readonly CompositeFormat _friendlyMessage = CompositeFormat.Parse(FriendlyMessageTemplate);
    private readonly FrozenDictionary<(Type, string), Action> _validators;

    public ValidationHostedService(IOptions<ValidatorOptions> options)
    {
        _ = Throw.IfMemberNull(options, options.Value);

        if (options.Value.Validators.Count == 0)
        {
            Throw.ArgumentException(nameof(options), "No validators specified");
        }

#if FIXME
// FIXME: this should be set to true, but this currently bafs as of 04/03/2023
#endif
        _validators = options.Value.Validators.ToFrozenDictionary();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        List<Exception>? exceptions = null;

        foreach (var pair in _validators)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var (optionsType, optionsName) = pair.Key;
            var validator = pair.Value;

            try
            {
                validator();
            }
            catch (ValidationException exception)
            {
                exceptions ??= new();
                var friendlyMessage = GetFriendlyMessage(exception, optionsType, optionsName);

                exceptions.Add(new OptionsValidationException(optionsName, optionsType, new[] { friendlyMessage }));
            }
            catch (OptionsValidationException exception)
            {
                exceptions ??= new();

                exceptions.Add(exception);
            }
        }

        if (exceptions != null)
        {
            if (exceptions.Count == 1)
            {
                // Rethrow if it's a single error
                ExceptionDispatchInfo.Capture(exceptions[0]).Throw();
            }
            else
            {
                // Aggregate if we have many errors
                throw new AggregateException(exceptions);
            }
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static string GetFriendlyMessage(ValidationException exception, Type optionsType, string optionsName)
    {
        var invalidMembers = PoolFactory.SharedStringBuilderPool.Get();

        try
        {
            foreach (var invalidMember in exception.ValidationResult.MemberNames)
            {
                _ = invalidMembers
                      .Append(invalidMember)
                      .Append(MemberSeparator);
            }

            _ = invalidMembers.Length != 0 ? invalidMembers.Remove(invalidMembers.Length - MemberSeparator.Length, MemberSeparator.Length) : invalidMembers.Append(Unknown);

            return _friendlyMessage.Format(CultureInfo.InvariantCulture, optionsType.FullName, optionsName, exception.ValidationResult.ErrorMessage, invalidMembers);
        }
        finally
        {
            PoolFactory.SharedStringBuilderPool.Return(invalidMembers);
        }
    }
}

#endif
