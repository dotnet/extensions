// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.AspNetCore.HeaderParsing;

/// <summary>
/// Keeps header parsing state and provides parsing features.
/// </summary>
public sealed partial class HeaderParsingFeature
{
    private readonly ILogger _logger;
    private readonly HeaderParsingMetrics _metrics;

    // This is a heterogeneous array of Box<T> instances. These boxes let us keep different Ts
    // in a single array, while preventing boxing of the T. The boxes are allocated up front
    // and are reused over time, avoid further allocations. Clever, eh?
    private Box?[] _boxes = Array.Empty<Box?>();

    internal HttpContext? Context { get; set; }

    internal HeaderParsingFeature(ILogger<HeaderParsingFeature> logger, HeaderParsingMetrics metrics)
    {
        _logger = logger;
        _metrics = metrics;
    }

    /// <summary>
    /// Tries to get a header value if it exists and can be parsed.
    /// </summary>
    /// <typeparam name="T">The type of the header value.</typeparam>
    /// <param name="header">The header to parse.</param>
    /// <param name="value">A resulting value.</param>
    /// <returns><see langword="true"/> if the header value was successfully fetched parsed.</returns>
    public bool TryGetHeaderValue<T>(HeaderKey<T> header, [NotNullWhen(true)] out T? value)
        where T : notnull
    {
        return TryGetHeaderValue(header, out value, out _);
    }

    /// <summary>
    /// Tries to get a header value if it exists and can be parsed.
    /// </summary>
    /// <typeparam name="T">The type of the header value.</typeparam>
    /// <param name="header">The header to parse.</param>
    /// <param name="value">A resulting value.</param>
    /// <param name="result">Details on the parsing operation.</param>
    /// <returns><see langword="true"/> if the header value was successfully fetched parsed.</returns>
    public bool TryGetHeaderValue<T>(HeaderKey<T> header, [NotNullWhen(true)] out T? value, out ParsingResult result)
        where T : notnull
    {
        _ = Throw.IfNull(header);

        if (header.Position >= _boxes.Length)
        {
            Array.Resize(ref _boxes, header.Position + 1);
        }

        var box = (Box<T>?)_boxes[header.Position];
        if (box is null)
        {
            box = new Box<T>();
            _boxes[header.Position] = box;
        }

        return box.Process(this, header, out value, out result);
    }

    private void Reset()
    {
        Context = null;
        foreach (var box in _boxes)
        {
            box?.Reset();
        }
    }

    internal sealed class PoolHelper : IDisposable
    {
        public HeaderParsingFeature Feature { get; }

        private readonly ObjectPool<PoolHelper> _pool;

        public PoolHelper(
            ObjectPool<PoolHelper> pool,
            ILogger<HeaderParsingFeature> logger,
            HeaderParsingMetrics metrics)
        {
            _pool = pool;
            Feature = new HeaderParsingFeature(logger, metrics);
        }

        public void Dispose()
        {
            Feature.Reset();
            _pool.Return(this);
        }
    }

    private enum BoxState
    {
        Uninitialized = -1,
        Success = ParsingResult.Success,
        Error = ParsingResult.Error,
        NotFound = ParsingResult.NotFound,
    }

    [SuppressMessage("Minor Code Smell", "S1694:An abstract class should have both abstract and concrete methods", Justification = "Analyzer issue")]
    private abstract class Box
    {
        public abstract void Reset();
    }

    private sealed class Box<T> : Box
        where T : notnull
    {
        private BoxState _state = BoxState.Uninitialized;
        private T? _value;

        public override void Reset()
        {
            _state = BoxState.Uninitialized;
            _value = default;
        }

        public bool Process(HeaderParsingFeature feature, HeaderKey<T> header, out T? value, out ParsingResult result)
        {
            if (_state == BoxState.Uninitialized)
            {
                if (feature.Context!.Request.Headers.TryGetValue(header.Name, out var values))
                {
                    if (header.Cacheable)
                    {
                        var o = header.GetCachedValue(values);
                        if (o != null)
                        {
                            feature._metrics.CacheAccessCounter.Add(1, header.Name, "Hit");
                            var b = (Box<T>)o;
                            b.CopyTo(this);
                            value = _value;
                            result = (ParsingResult)_state;
                            return result == ParsingResult.Success;
                        }

                        feature._metrics.CacheAccessCounter.Add(1, header.Name, "Miss");
                    }

                    if (header.TryParse(values, out _value, out var error))
                    {
                        _state = BoxState.Success;

                        if (header.Cacheable)
                        {
                            var b = new Box<T>();
                            CopyTo(b);
                            header.AddCachedValue(values, b);
                        }
                    }
                    else
                    {
                        _state = BoxState.Error;
                        feature.LogParsingError(header.Name, error!);
                        feature._metrics.ParsingErrorCounter.Add(1, header.Name, error);
                    }
                }
                else if (header.HasDefaultValue)
                {
                    _state = BoxState.Success;
                    _value = header.DefaultValue;
                    feature.LogDefaultUsage(header.Name);
                }
                else
                {
                    _state = BoxState.NotFound;
                    feature.LogNotFound(header.Name);
                }
            }

            value = _value;
            result = (ParsingResult)_state;

            return result == ParsingResult.Success;
        }

        private void CopyTo(Box<T> to)
        {
            to._state = _state;
            to._value = _value;
        }
    }

    [LoggerMessage(LogLevel.Debug, "Can't parse header '{HeaderName}' due to '{Error}'.")]
    private partial void LogParsingError(string headerName, string error);

    [LoggerMessage(LogLevel.Debug, "Using a default value for header '{HeaderName}'.")]
    private partial void LogDefaultUsage(string headerName);

    [LoggerMessage(LogLevel.Debug, "Header '{HeaderName}' not found.")]
    private partial void LogNotFound(string headerName);
}
