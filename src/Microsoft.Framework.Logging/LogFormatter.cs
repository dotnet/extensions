using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;

namespace Microsoft.Framework.Logging
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
        /// If state is an <see cref="ILoggerStructure"/>, <see cref="LogFormatter.FormatStructure(ILoggerStructure)"/> 
        /// is used to format the message, otherwise the state's ToString() is used.
        /// </summary>
        public static string Formatter(object state, Exception e)
        {
            var result = string.Empty;
            if (state != null)
            {
                var structure = state as ILoggerStructure;
                if (structure != null)
                {
                    result += FormatStructure(structure);
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
        /// Formats an <see cref="ILoggerStructure"/>.
        /// </summary>
        /// <param name="structure">The <see cref="ILoggerStructure"/> to format.</param>
        /// <returns>A string representation of the given <see cref="ILoggerStructure"/>.</returns>
        public static string FormatStructure([NotNull] ILoggerStructure structure)
        {
            var builder = new StringBuilder();
            FormatStructure(structure, builder);
            return builder.ToString();
        }

        /// <summary>
        /// Formats an <see cref="ILoggerStructure"/>.
        /// </summary>
        /// <param name="structure">The <see cref="ILoggerStructure"/> to format.</param>
        /// <param name="builder">The <see cref="StringBuilder"/> to append to.</param>
        private static void FormatStructure([NotNull] ILoggerStructure structure, [NotNull] StringBuilder builder)
        {
            var values = structure.GetValues();
            if (values == null)
            {
                return;
            }
            
            foreach (var kvp in values)
            {
                IEnumerable<ILoggerStructure> structureEnumerable;
                ILoggerStructure loggerStructure;
                builder.Append(kvp.Key);
                builder.Append(": ");
                if ((structureEnumerable = kvp.Value as IEnumerable<ILoggerStructure>) != null)
                {
                    var valArray = structureEnumerable.ToArray();
                    for (int j = 0; j < valArray.Length - 1; j++)
                    {
                        FormatStructure(valArray[j], builder);
                        builder.Append(", ");
                    }
                    if (valArray.Length > 0)
                    {
                        FormatStructure(valArray[valArray.Length - 1], builder);
                    }
                }
                else if ((loggerStructure = kvp.Value as ILoggerStructure) != null)
                {
                    FormatStructure(loggerStructure, builder);
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