// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Extensions.Http.Diagnostics;
using Microsoft.Shared.Pools;

namespace Microsoft.Extensions.Telemetry.Internal;

internal sealed class HttpHeadersRedactor : IHttpHeadersRedactor
{
    private const char SeparatorChar = ',';

    private readonly IRedactorProvider _redactorProvider;

    public HttpHeadersRedactor(IRedactorProvider redactorProvider)
    {
        _redactorProvider = redactorProvider;
    }

    public string Redact(IEnumerable<string> headerValues, DataClassification classification) =>
        headerValues switch
        {
            IReadOnlyList<string> headerValueList => RedactList(headerValueList, classification),
            { } => RedactIEnumerable(headerValues, classification),
            _ => TelemetryConstants.Unknown
        };

    private string RedactIEnumerable(IEnumerable<string> input, DataClassification classification)
    {
        var redactor = _redactorProvider.GetRedactor(classification);

        using var enumerator = input.GetEnumerator();
        if (!enumerator.MoveNext())
        {
            return string.Empty;
        }

        var firstItem = enumerator.Current.AsSpan();

        ReadOnlySpan<char> currentItem;
        var redactedSize = 0;
        var counter = 1;
        while (enumerator.MoveNext())
        {
            counter++;
            redactedSize++; // for a separator char

            currentItem = enumerator.Current.AsSpan();
            if (!currentItem.IsEmpty)
            {
                redactedSize += redactor.GetRedactedLength(currentItem);
            }
        }

        if (counter == 1)
        {
            return redactor.Redact(firstItem);
        }

        if (!firstItem.IsEmpty)
        {
            redactedSize += redactor.GetRedactedLength(firstItem);
        }

        using var rental = new RentedSpan<char>(redactedSize);
        var destinationMany = rental.Rented ? rental.Span : stackalloc char[redactedSize];

        enumerator.Reset();

        // don't insert SeparatorChar before the first item.
        _ = enumerator.MoveNext();
        currentItem = enumerator.Current.AsSpan();
        var index = 0;
        if (!currentItem.IsEmpty)
        {
            index += redactor.Redact(currentItem, destinationMany.Slice(index));
        }

        while (enumerator.MoveNext())
        {
            // insert SeparatorChar before every item, starting from the second.
            destinationMany[index++] = SeparatorChar;
            currentItem = enumerator.Current.AsSpan();
            if (!currentItem.IsEmpty)
            {
                index += redactor.Redact(currentItem, destinationMany.Slice(index));
            }
        }

        return destinationMany.ToString();
    }

    private string RedactList(IReadOnlyList<string> input, DataClassification classification)
    {
        if (input.Count == 0)
        {
            return string.Empty;
        }

        var redactor = _redactorProvider.GetRedactor(classification);
        var firstItem = input[0].AsSpan();
        if (input.Count == 1)
        {
            return redactor.Redact(firstItem);
        }

        var redactedSize = 0;
        if (!firstItem.IsEmpty)
        {
            redactedSize += redactor.GetRedactedLength(firstItem);
        }

        ReadOnlySpan<char> currentItem;
        for (int i = 1; i < input.Count; i++)
        {
            redactedSize++; // for a separator char
            currentItem = input[i].AsSpan();
            if (!currentItem.IsEmpty)
            {
                redactedSize += redactor.GetRedactedLength(currentItem);
            }
        }

        using var rental = new RentedSpan<char>(redactedSize);

        // Stryker disable once all
        Span<char> destinationMany = rental.Rented ? rental.Span : stackalloc char[redactedSize];

        var index = 0;
        for (int i = 0; i < input.Count; i++)
        {
            currentItem = input[i].AsSpan();
            if (!currentItem.IsEmpty)
            {
                index += redactor.Redact(currentItem, destinationMany.Slice(index));
            }

            if (i < input.Count - 1)
            {
                destinationMany[index++] = SeparatorChar;
            }
        }

        return destinationMany.ToString();
    }
}
