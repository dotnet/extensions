// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading;

namespace Microsoft.Extensions.Primitives
{
    public static class CorrelationGenerator
    {
        private static long _lastId = GenerateIdSeed();

        public static CorrelationId GetNextId()
        {
            return new CorrelationId(Interlocked.Increment(ref _lastId));
        }

        private static long GenerateIdSeed()
        {
            // Seed the _lastId for this application instance with a roughly increasing CorrelationId over restarts

            // The number of 100-nanosecond intervals that have elapsed since the release of ASP.NET Core 1.0
            var dateTimeSeed = (DateTime.UtcNow.Ticks - new DateTime(2016, 6, 27).Ticks);
            // Mask off max imprecision 
            var imprecision = TimeSpan.TicksPerMillisecond * 30;
            dateTimeSeed -= dateTimeSeed % imprecision;

            unchecked
            {
                // Increase events per second x 100. 
                // Decrease from 10000 years without seed wrap to 100 years without seed wrap (e.g. 2116)
                // Means 1 billion events until next second is encountered rather than 10 million events
                dateTimeSeed *= 100;
            }
            imprecision *= 100;
            // Fill masked off imprecision with Stopwatch.GetTimestamp in case restarts happen in same 30ms period
            dateTimeSeed += Stopwatch.GetTimestamp() % imprecision;

            return dateTimeSeed;
        }
    }
}
