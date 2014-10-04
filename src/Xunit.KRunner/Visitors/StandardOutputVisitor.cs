// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using Xunit.Abstractions;

namespace Xunit.ConsoleClient
{
    public class StandardOutputVisitor : XmlTestExecutionVisitor
    {
        string assemblyName;
        readonly object consoleLock;
        readonly ConcurrentDictionary<string, ExecutionSummary> completionMessages;

        public StandardOutputVisitor(object consoleLock,
                                     Func<bool> cancelThunk,
                                     ConcurrentDictionary<string, ExecutionSummary> completionMessages = null)
            : base(cancelThunk)
        {
            this.consoleLock = consoleLock;
            this.completionMessages = completionMessages;
        }

        protected override bool Visit(ITestAssemblyStarting assemblyStarting)
        {
            assemblyName = assemblyStarting.AssemblyFileName;

            lock (consoleLock)
                Console.WriteLine("Starting: {0}", assemblyName);

            return base.Visit(assemblyStarting);
        }

        protected override bool Visit(ITestAssemblyFinished assemblyFinished)
        {
            // Base class does computation of results, so call it first.
            var result = base.Visit(assemblyFinished);

            lock (consoleLock)
                Console.WriteLine("Finished: {0}", assemblyName);

            if (completionMessages != null)
                completionMessages.TryAdd(assemblyName, new ExecutionSummary
                {
                    Total = assemblyFinished.TestsRun,
                    Failed = assemblyFinished.TestsFailed,
                    Skipped = assemblyFinished.TestsSkipped,
                    Time = assemblyFinished.ExecutionTime
                });

            return result;
        }

        protected override bool Visit(IErrorMessage error)
        {
            lock (consoleLock)
            {
#if ASPNET50
                Console.ForegroundColor = ConsoleColor.Red;
#endif
                Console.Error.WriteLine("   {0} [FATAL]", Escape(error.ExceptionTypes[0]));
#if ASPNET50
                Console.ForegroundColor = ConsoleColor.Gray;
#endif
                Console.Error.WriteLine("      {0}", Escape(ExceptionUtility.CombineMessages(error)));

                WriteStackTrace(ExceptionUtility.CombineStackTraces(error));
            }

            return base.Visit(error);
        }

        protected override bool Visit(ITestFailed testFailed)
        {
            lock (consoleLock)
            {
                // TODO: Thread-safe way to figure out the default foreground color
#if ASPNET50
                Console.ForegroundColor = ConsoleColor.Red;
#endif
                Console.Error.WriteLine("   {0} [FAIL]", Escape(testFailed.TestDisplayName));
#if ASPNET50
                Console.ForegroundColor = ConsoleColor.Gray;
#endif
                Console.Error.WriteLine("      {0}", Escape(ExceptionUtility.CombineMessages(testFailed)));

                WriteStackTrace(ExceptionUtility.CombineStackTraces(testFailed));
            }

            return base.Visit(testFailed);
        }

        protected override bool Visit(ITestPassed testPassed)
        {
            return base.Visit(testPassed);
        }

        protected override bool Visit(ITestSkipped testSkipped)
        {
            lock (consoleLock)
            {
                // TODO: Thread-safe way to figure out the default foreground color
#if ASPNET50
                Console.ForegroundColor = ConsoleColor.Yellow;
#endif
                Console.Error.WriteLine("   {0} [SKIP]", Escape(testSkipped.TestDisplayName));
#if ASPNET50
                Console.ForegroundColor = ConsoleColor.Gray;
#endif
                Console.Error.WriteLine("      {0}", Escape(testSkipped.Reason));
            }

            return base.Visit(testSkipped);
        }

        protected override bool Visit(ITestStarting testStarting)
        {
            return base.Visit(testStarting);
        }

        void WriteStackTrace(string stackTrace)
        {
            if (String.IsNullOrWhiteSpace(stackTrace))
                return;

#if ASPNET50
            Console.ForegroundColor = ConsoleColor.DarkGray;
#endif
            Console.Error.WriteLine("      Stack Trace:");

#if ASPNET50
            Console.ForegroundColor = ConsoleColor.Gray;
#endif
            foreach (var stackFrame in stackTrace.Split(new[] { Environment.NewLine }, StringSplitOptions.None))
                Console.Error.WriteLine("         {0}", stackFrame);
        }
    }
}
