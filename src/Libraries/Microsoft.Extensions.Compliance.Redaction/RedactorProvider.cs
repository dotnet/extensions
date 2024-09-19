// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Options;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Compliance.Redaction;

[SuppressMessage("Performance", "CA1812: Avoid uninstantiated internal classes", Justification = "Instantiated via reflection.")]
internal sealed class RedactorProvider : IRedactorProvider
{
    private readonly FrozenDictionary<DataClassificationSet, Redactor> _classRedactors;
    private readonly Redactor _fallbackRedactor;

    public RedactorProvider(IEnumerable<Redactor> redactors, IOptions<RedactorProviderOptions> options)
    {
        var value = Throw.IfMemberNull(options, options.Value);

        _classRedactors = GetClassRedactorMap(redactors, value.Redactors);
        _fallbackRedactor = GetFallbackRedactor(redactors, options.Value.FallbackRedactor);
    }

    public Redactor GetRedactor(DataClassificationSet classifications)
    {
        if (_classRedactors.TryGetValue(classifications, out var result))
        {
            return result;
        }

        return _fallbackRedactor;
    }

    private static FrozenDictionary<DataClassificationSet, Redactor> GetClassRedactorMap(IEnumerable<Redactor> redactors, Dictionary<DataClassificationSet, Type> map)
    {
        if (!map.ContainsKey(DataClassification.None))
        {
            map.Add(DataClassification.None, typeof(NullRedactor));
            redactors = [.. redactors, NullRedactor.Instance];
        }

        var dict = new Dictionary<DataClassificationSet, Redactor>(map.Count);
        foreach (var m in map)
        {
            foreach (var r in redactors)
            {
                if (r.GetType() == m.Value)
                {
                    dict[m.Key] = r;
                    break;
                }
            }
        }

        return dict.ToFrozenDictionary();
    }

    private static Redactor GetFallbackRedactor(IEnumerable<Redactor> redactors, Type defaultRedactorType)
    {
        foreach (var r in redactors)
        {
            if (r.GetType() == defaultRedactorType)
            {
                return r;
            }
        }

        // can't use exception helper here since it confuses the compiler's control flow analysis
        throw new InvalidOperationException($"Couldn't find redactor of type {defaultRedactorType} in the dependency injection container.");
    }
}
