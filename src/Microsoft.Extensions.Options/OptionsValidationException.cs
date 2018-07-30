// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.Options
{
    /// <summary>
    /// Thrown when options validation fails.
    /// </summary>
    public class OptionsValidationException : Exception
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="failureMessages">The validation failure messages.</param>
        public OptionsValidationException(IEnumerable<string> failureMessages)
            => Failures = failureMessages ?? new List<string>();

        /// <summary>
        /// The validation failures.
        /// </summary>
        public IEnumerable<string> Failures { get; }
    }
}