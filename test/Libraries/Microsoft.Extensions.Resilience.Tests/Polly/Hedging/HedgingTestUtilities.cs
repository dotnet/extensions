// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Resilience.Options;
using Polly;
using Polly.Utilities;

namespace Microsoft.Extensions.Resilience.Polly.Test.Hedging;

#pragma warning disable CS0618 // access obsoleted members

public static class HedgingTestUtilities<T>
{
    public static TimeSpan DefaultHedgingDelay { get; } = TimeSpan.FromSeconds(1);

    public static Func<HedgingDelayArguments, TimeSpan> DefaultHedgingDelayGenerator { get; } = (_) => DefaultHedgingDelay;

    public static Func<DelegateResult<T>, Context, int, CancellationToken, Task> EmptyOnHedgingTask { get; } =
        (_, _, _, _) => Task.CompletedTask;

    public static class HedgedTasksHandler
    {
        public static HedgedTaskProvider<T> FunctionsProvider { get; } =
           new((HedgingTaskProviderArguments hedgingTaskProviderArguments, out Task<T>? result) =>
           {
               if (hedgingTaskProviderArguments.AttemptNumber <= Functions!.Count)
               {
                   var function = Functions![hedgingTaskProviderArguments.AttemptNumber - 1];
                   result = function(hedgingTaskProviderArguments.Context, hedgingTaskProviderArguments.CancellationToken)!;
                   return true;
               }

               result = null;
               return false;
           });

        public static HedgedTaskProvider FunctionsProviderNonGeneric { get; } =
           new((HedgingTaskProviderArguments hedgingTaskProviderArguments, out Task? result) =>
           {
               if (hedgingTaskProviderArguments.AttemptNumber <= Functions!.Count)
               {
                   var function = Functions![hedgingTaskProviderArguments.AttemptNumber - 1];
                   result = function(hedgingTaskProviderArguments.Context, hedgingTaskProviderArguments.CancellationToken)!;
                   return true;
               }

               result = null;
               return false;
           });

        public static HedgedTaskProvider FunctionsProviderNonGenericReturnsFalse { get; } =
#pragma warning disable S3257 // Declarations and initializations should be as concise as possible
           new((HedgingTaskProviderArguments hedgingTaskProviderArguments, out Task? result) =>
#pragma warning restore S3257 // Declarations and initializations should be as concise as possible
           {
               result = null;
               return false;
           });

        public static List<Func<Context, CancellationToken, Task<T>?>> Functions { get; } =
            new()
            {
                GetApples,
                GetOranges,
                GetPears
            };

        private static async Task<T> GetApples(Context context, CancellationToken token)
        {
            await SystemClock.SleepAsync(TimeSpan.FromMilliseconds(10 * 1000), token);
#pragma warning disable S4056 // Overloads with a "CultureInfo" or an "IFormatProvider" parameter should be used
            return (T)Convert.ChangeType("Apples", typeof(T));
#pragma warning restore S4056 // Overloads with a "CultureInfo" or an "IFormatProvider" parameter should be used
        }

        private static async Task<T> GetPears(Context context, CancellationToken token)
        {
            await SystemClock.SleepAsync(TimeSpan.FromMilliseconds(3 * 1000), token);
#pragma warning disable S4056 // Overloads with a "CultureInfo" or an "IFormatProvider" parameter should be used
            return (T)Convert.ChangeType("Pears", typeof(T));
#pragma warning restore S4056 // Overloads with a "CultureInfo" or an "IFormatProvider" parameter should be used
        }

        private static async Task<T> GetOranges(Context context, CancellationToken token)
        {
            await SystemClock.SleepAsync(TimeSpan.FromMilliseconds(2 * 1000), token);
#pragma warning disable S4056 // Overloads with a "CultureInfo" or an "IFormatProvider" parameter should be used
            return (T)Convert.ChangeType("Oranges", typeof(T));
#pragma warning restore S4056 // Overloads with a "CultureInfo" or an "IFormatProvider" parameter should be used
        }

        public static HedgedTaskProvider<T> GetCustomTaskProvider(Func<CancellationToken, Task<T>> task)
            => new((HedgingTaskProviderArguments args, out Task<T>? result) =>
            {
                result = task(args.CancellationToken);
                return true;
            });

        public static int MaxHedgedTasks { get; } = Functions.Count + 1;
    }

    public static class PrimaryStringTasks
    {
        public const string InstantTaskResult = "Instant";

        public const string FastTaskResult = "I am fast!";

        public const string SlowTaskResult = "I am so slow!";

        public static Task<string> InstantTask()
        {
            return Task.FromResult(InstantTaskResult);
        }

        public static async Task<string> FastTask(CancellationToken token)
        {
            await SystemClock.SleepAsync(TimeSpan.FromMilliseconds(10), token);
            return FastTaskResult;
        }

        public static async Task<string> SlowTask(Context _, CancellationToken token)
        {
            await SystemClock.SleepAsync(TimeSpan.FromDays(1), token);
            return SlowTaskResult;
        }

        public static async Task<T> GenericFastTask(T result, CancellationToken token)
        {
            await SystemClock.SleepAsync(TimeSpan.FromMilliseconds(10), token);
            return result;
        }
    }
}
