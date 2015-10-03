// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Internal;

namespace Microsoft.Extensions.Logging
{
    /// <summary>
    /// Formatters for common logging scenarios.
    /// </summary>
    public static class LogFormatter
    {
        private const string space = "  ";

        /// <summary>
        /// Formats a message from the given state and exception, in the form 
        /// "state
        /// exception".
        /// If state is an <see cref="ILogValues"/>, <see cref="LogFormatter.FormatLogValues(ILogValues)"/> 
        /// is used to format the message, otherwise the state's ToString() is used.
        /// </summary>
        public static string Formatter(object state, Exception e)
        {
            var result = string.Empty;
            if (state != null)
            {
                var values = state as ILogValues;
                if (values != null)
                {
                    result += FormatLogValues(values);
                }
                else
                {
                    result += state;
                }
            }
            if (e != null)
            {
                result += Environment.NewLine + e;
            }

            return result;
        }

        /// <summary>
        /// Formats an <see cref="ILogValues"/>.
        /// </summary>
        /// <param name="logValues">The <see cref="ILogValues"/> to format.</param>
        /// <returns>A string representation of the given <see cref="ILogValues"/>.</returns>
        public static string FormatLogValues([NotNull] ILogValues logValues)
        {
            var builder = new StringBuilder();
            FormatLogValues(logValues, builder);
            return builder.ToString();
        }

        /// <summary>
        /// Formats an <see cref="ILogValues"/>.
        /// </summary>
        /// <param name="logValues">The <see cref="ILogValues"/> to format.</param>
        /// <param name="builder">The <see cref="StringBuilder"/> to append to.</param>
        private static void FormatLogValues([NotNull] ILogValues logValues, [NotNull] StringBuilder builder)
        {
            var values = logValues.GetValues();
            if (values == null)
            {
                return;
            }
            
            foreach (var kvp in values)
            {
                IEnumerable<ILogValues> structureEnumerable;
                ILogValues logs;
                builder.Append(kvp.Key);
                builder.Append(": ");
                if ((structureEnumerable = kvp.Value as IEnumerable<ILogValues>) != null)
                {
                    var valArray = structureEnumerable.ToArray();
                    for (int j = 0; j < valArray.Length - 1; j++)
                    {
                        FormatLogValues(valArray[j], builder);
                        builder.Append(", ");
                    }
                    if (valArray.Length > 0)
                    {
                        FormatLogValues(valArray[valArray.Length - 1], builder);
                    }
                }
                else if ((logs = kvp.Value as ILogValues) != null)
                {
                    FormatLogValues(logs, builder);
                }
                else
                {
                    builder.Append(kvp.Value);
                }
                builder.Append(space);
            }
            // get rid of the extra whitespace
            if (builder.Length > 0)
            {
                builder.Length -= space.Length;
            }
        }
    }
}