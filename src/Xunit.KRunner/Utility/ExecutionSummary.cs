// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Xunit
{
    /// <summary>
    /// Collects execution totals for a group of test cases.
    /// </summary>
    public class ExecutionSummary
    {
        /// <summary>
        /// Gets or set the total number of tests run.
        /// </summary>
        public int Total { get; set; }

        /// <summary>
        /// Gets or sets the number of failed tests.
        /// </summary>
        public int Failed { get; set; }

        /// <summary>
        /// Gets or sets the number of skipped tests.
        /// </summary>
        public int Skipped { get; set; }

        /// <summary>
        /// Gets or sets the total execution time for the tests.
        /// </summary>
        public decimal Time { get; set; }
    }
}