// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

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