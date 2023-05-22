// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Http.Resilience.Internal.Validators;

[Experimental("Required for Experimental public API since 1.21.0. Internal use only.")]
internal sealed class FallbackClientHandlerOptionsValidator : IValidateOptions<FallbackClientHandlerOptions>
{
    private const string NoPathAndQuery = "/";

    public ValidateOptionsResult Validate(string? name, FallbackClientHandlerOptions options)
    {
        var builder = new ValidateOptionsResultBuilder();

        if (options.FallbackPolicyOptions is null)
        {
            builder.AddError("must be configured to define a fallback policy.", nameof(options.FallbackPolicyOptions));
        }

        var fallbackUri = options.BaseFallbackUri;

        if (fallbackUri is null)
        {
            builder.AddError("must be configured.", nameof(options.BaseFallbackUri));
        }
        else
        {
            if (!fallbackUri.IsAbsoluteUri)
            {
                builder.AddError("must be an absolute uri.", nameof(options.BaseFallbackUri));
            }
            else if (fallbackUri.PathAndQuery != NoPathAndQuery)
            {
                builder.AddError("must be a base uri, hence it may contain only the schema, host and port.", nameof(options.BaseFallbackUri));
            }
        }

        return builder.Build();
    }
}
