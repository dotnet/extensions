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
    /// Represents execution options for xUnit.net v2 tests.
    /// </summary>
    public class XunitExecutionOptions : TestFrameworkOptions
    {
        /// <summary>
        /// Gets or sets a flag to disable parallelization.
        /// </summary>
        public bool DisableParallelization
        {
            get { return GetValue<bool>(TestOptionsNames.Execution.DisableParallelization, false); }
            set { SetValue(TestOptionsNames.Execution.DisableParallelization, value); }
        }

        /// <summary>
        /// Gets or sets the maximum number of threads to use when running tests in parallel.
        /// If set to 0 (the default value), does not limit the number of threads.
        /// </summary>
        public int MaxParallelThreads
        {
            get { return GetValue<int>(TestOptionsNames.Execution.MaxParallelThreads, 0); }
            set { SetValue(TestOptionsNames.Execution.MaxParallelThreads, value); }
        }
    }
}
