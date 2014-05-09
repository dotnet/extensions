// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

internal static class TestOptionsNames
{
    internal static class Discovery
    {
    }

    internal static class Execution
    {
        public static readonly string DisableParallelization = "xunit.DisableParallelization";
        public static readonly string MaxParallelThreads = "xunit.MaxParallelThreads";
    }
}