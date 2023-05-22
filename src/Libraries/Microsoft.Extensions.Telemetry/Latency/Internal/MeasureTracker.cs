// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Telemetry.Latency;

namespace Microsoft.Extensions.Telemetry.Latency.Internal;

internal sealed class MeasureTracker : IResettable
{
    private readonly Registry _registeredMeasureNames;

    private readonly string[] _measureNames;

    private readonly long[] _measureValues;

    private readonly Measure[] _measures;

    private readonly int[] _measurePosition;

    private readonly object _measurePositionLock = new();

    private int _numMeasures;

    /// <summary>
    /// Initializes a new instance of the <see cref="MeasureTracker"/> class.
    /// </summary>
    /// <param name="registry">Registry of measure names.</param>
    public MeasureTracker(Registry registry)
    {
        _registeredMeasureNames = registry;
        int arraySize = _registeredMeasureNames.KeyCount + 1;
        _measurePosition = new int[arraySize];
        _measures = new Measure[arraySize];
        _measureNames = new string[arraySize];
        _measureValues = new long[arraySize];
    }

    /// <summary>
    /// Resets the MeasureTracker.
    /// </summary>
    public bool TryReset()
    {
#if NET6_0_OR_GREATER
        Array.Clear(_measurePosition);
#else
        Array.Clear(_measurePosition, 0, _measurePosition.Length);
#endif
        _numMeasures = 0;
        return true;
    }

    public MeasureToken GetToken(string name)
    {
        int pos = _registeredMeasureNames.GetRegisteredKeyIndex(name);
        return new MeasureToken(name, pos);
    }

    /// <summary>
    /// Add value to measure.
    /// </summary>
    /// <param name="token">Token for measure.</param>
    /// <param name="value">Value of measure.</param>
    public void AddLong(MeasureToken token, long value)
    {
        if (token.Position > -1)
        {
            int pos = GetPositionOfMeasure(token);
            _ = Interlocked.Add(ref _measureValues[pos], value);
        }
    }

    /// <summary>
    /// Set value of measure.
    /// </summary>
    /// <param name="token">Token for measure.</param>
    /// <param name="value">Value of measure.</param>
    public void SetLong(MeasureToken token, long value)
    {
        if (token.Position > -1)
        {
            int pos = GetPositionOfMeasure(token);
            _measureValues[pos] = value;
        }
    }

    /// <summary>
    /// Gets the position at which a measure has been added.
    /// </summary>
    /// <param name="measureToken">Token for the measure.</param>
    /// <returns>Position of the measure.</returns>
    /// <remarks> This function uses _measurePosition as a dictionary. The key is
    /// the order of the name in the registry. The value is the position in the tracking arrays,
    /// _measureNames and _measureValues. </remarks>
    private int GetPositionOfMeasure(MeasureToken measureToken)
    {
        int pos = measureToken.Position;

        // If measure with the name has already been added, return position.
        // If being used for the first time, assign a position to it and initialize the tracking
        // arrays.
        if (_measurePosition[pos] == 0)
        {
            lock (_measurePositionLock)
            {
                if (_measurePosition[pos] == 0)
                {
                    _numMeasures++;
                    _measureNames[_numMeasures] = measureToken.Name;
                    _measureValues[_numMeasures] = 0;
                    _measurePosition[pos] = _numMeasures;
                }
            }
        }

        return _measurePosition[pos];
    }

    /// <summary>
    /// Gets the list of measures added.
    /// </summary>
    public ArraySegment<Measure> Measures
    {
        get
        {
            for (int i = 1; i <= _numMeasures; i++)
            {
                _measures[i] = new Measure(_measureNames[i], _measureValues[i]);
            }

            return new(_measures, 1, _numMeasures);
        }
    }
}
