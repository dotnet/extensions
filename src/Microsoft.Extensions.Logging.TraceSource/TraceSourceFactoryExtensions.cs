// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging.TraceSource;

namespace Microsoft.Extensions.Logging
{
    public static class TraceSourceFactoryExtensions
    {
        /// <summary>
        /// Adds a TraceSource logger named 'TraceSource' to the factory.
        /// </summary>
        /// <param name="factory">The <see cref="LoggerFactory"/> to use.</param>
        /// <param name="switchName">The name of the <see cref="SourceSwitch"/> to use.</param>
        public static LoggerFactory AddTraceSource(
            this LoggerFactory factory,
            string switchName)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            if (switchName == null)
            {
                throw new ArgumentNullException(nameof(switchName));
            }

            return factory.AddTraceSource(new SourceSwitch(switchName));
        }

        /// <summary>
        /// Adds a TraceSource logger named 'TraceSource' to the factory.
        /// </summary>
        /// <param name="factory">The <see cref="LoggerFactory"/> to use.</param>
        /// <param name="switchName">The name of the <see cref="SourceSwitch"/> to use.</param>
        /// <param name="listener">The <see cref="TraceListener"/> to use.</param>
        public static LoggerFactory AddTraceSource(
            this LoggerFactory factory,
            string switchName,
            TraceListener listener)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            if (switchName == null)
            {
                throw new ArgumentNullException(nameof(switchName));
            }

            if (listener == null)
            {
                throw new ArgumentNullException(nameof(listener));
            }

            return factory.AddTraceSource(new SourceSwitch(switchName), listener);
        }

        /// <summary>
        /// Adds a TraceSource logger named 'TraceSource' to the factory.
        /// </summary>
        /// <param name="factory">The <see cref="LoggerFactory"/> to use.</param>
        /// <param name="sourceSwitch">The <see cref="SourceSwitch"/> to use.</param>
        public static LoggerFactory AddTraceSource(
            this LoggerFactory factory,
            SourceSwitch sourceSwitch)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            if (sourceSwitch == null)
            {
                throw new ArgumentNullException(nameof(sourceSwitch));
            }

            factory.AddProvider("TraceSource", new TraceSourceLoggerProvider(sourceSwitch));

            return factory;
        }

        /// <summary>
        /// Adds a TraceSource logger named 'TraceSource' to the factory.
        /// </summary>
        /// <param name="factory">The <see cref="LoggerFactory"/> to use.</param>
        /// <param name="sourceSwitch">The <see cref="SourceSwitch"/> to use.</param>
        /// <param name="listener">The <see cref="TraceListener"/> to use.</param>
        public static LoggerFactory AddTraceSource(
            this LoggerFactory factory,
            SourceSwitch sourceSwitch,
            TraceListener listener)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            if (sourceSwitch == null)
            {
                throw new ArgumentNullException(nameof(sourceSwitch));
            }

            if (listener == null)
            {
                throw new ArgumentNullException(nameof(listener));
            }

            factory.AddProvider("TraceSource", new TraceSourceLoggerProvider(sourceSwitch, listener));

            return factory;
        }

        /// <summary>
        /// <para>
        /// This method is obsolete and will be removed in a future version. The recommended alternative is to call the Microsoft.Extensions.Logging.AddTraceSource() extension method on the Microsoft.Extensions.Logging.LoggerFactory instance.
        /// </para>
        /// </summary>
        /// <param name="factory">The <see cref="ILoggerFactory"/> to use.</param>
        /// <param name="switchName">The name of the <see cref="SourceSwitch"/> to use.</param>
        [Obsolete("This method is obsolete and will be removed in a future version. The recommended alternative is to call the Microsoft.Extensions.Logging.AddTraceSource() extension method on the Microsoft.Extensions.Logging.LoggerFactory instance.")]
        public static ILoggerFactory AddTraceSource(
            this ILoggerFactory factory,
            string switchName)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            if (switchName == null)
            {
                throw new ArgumentNullException(nameof(switchName));
            }

            return factory.AddTraceSource(new SourceSwitch(switchName));
        }

        /// <summary>
        /// <para>
        /// This method is obsolete and will be removed in a future version. The recommended alternative is to call the Microsoft.Extensions.Logging.AddTraceSource() extension method on the Microsoft.Extensions.Logging.LoggerFactory instance.
        /// </para>
        /// </summary>
        /// <param name="factory">The <see cref="ILoggerFactory"/> to use.</param>
        /// <param name="switchName">The name of the <see cref="SourceSwitch"/> to use.</param>
        /// <param name="listener">The <see cref="TraceListener"/> to use.</param>
        [Obsolete("This method is obsolete and will be removed in a future version. The recommended alternative is to call the Microsoft.Extensions.Logging.AddTraceSource() extension method on the Microsoft.Extensions.Logging.LoggerFactory instance.")]
        public static ILoggerFactory AddTraceSource(
            this ILoggerFactory factory,
            string switchName,
            TraceListener listener)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            if (switchName == null)
            {
                throw new ArgumentNullException(nameof(switchName));
            }

            if (listener == null)
            {
                throw new ArgumentNullException(nameof(listener));
            }

            return factory.AddTraceSource(new SourceSwitch(switchName), listener);
        }

        /// <summary>
        /// <para>
        /// This method is obsolete and will be removed in a future version. The recommended alternative is to call the Microsoft.Extensions.Logging.AddTraceSource() extension method on the Microsoft.Extensions.Logging.LoggerFactory instance.
        /// </para>
        /// </summary>
        /// <param name="factory">The <see cref="ILoggerFactory"/> to use.</param>
        /// <param name="sourceSwitch">The <see cref="SourceSwitch"/> to use.</param>
        [Obsolete("This method is obsolete and will be removed in a future version. The recommended alternative is to call the Microsoft.Extensions.Logging.AddTraceSource() extension method on the Microsoft.Extensions.Logging.LoggerFactory instance.")]
        public static ILoggerFactory AddTraceSource(
            this ILoggerFactory factory,
            SourceSwitch sourceSwitch)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            if (sourceSwitch == null)
            {
                throw new ArgumentNullException(nameof(sourceSwitch));
            }

            factory.AddProvider(new TraceSourceLoggerProvider(sourceSwitch));

            return factory;
        }

        /// <summary>
        /// <para>
        /// This method is obsolete and will be removed in a future version. The recommended alternative is to call the Microsoft.Extensions.Logging.AddTraceSource() extension method on the Microsoft.Extensions.Logging.LoggerFactory instance.
        /// </para>
        /// </summary>
        /// <param name="factory">The <see cref="ILoggerFactory"/> to use.</param>
        /// <param name="sourceSwitch">The <see cref="SourceSwitch"/> to use.</param>
        /// <param name="listener">The <see cref="TraceListener"/> to use.</param>
        [Obsolete("This method is obsolete and will be removed in a future version. The recommended alternative is to call the Microsoft.Extensions.Logging.AddTraceSource() extension method on the Microsoft.Extensions.Logging.LoggerFactory instance.")]
        public static ILoggerFactory AddTraceSource(
            this ILoggerFactory factory,
            SourceSwitch sourceSwitch,
            TraceListener listener)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            if (sourceSwitch == null)
            {
                throw new ArgumentNullException(nameof(sourceSwitch));
            }

            if (listener == null)
            {
                throw new ArgumentNullException(nameof(listener));
            }

            factory.AddProvider(new TraceSourceLoggerProvider(sourceSwitch, listener));

            return factory;
        }
    }
}