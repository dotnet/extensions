// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.Logging.Azure.AppServicesDiagnostics.Test
{
    internal static class RetryHelper
    {
        public const int DefaultRetriesCount = 3;

        public static void Retry(Func<bool> action, int retries = DefaultRetriesCount)
        {
            while (retries > 0)
            {
                if (action())
                {
                    return;
                }

                retries--;
            }

            throw new TimeoutException("Maximum number of retries reached.");
        }
    }
}
