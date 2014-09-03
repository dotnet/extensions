// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Framework.Runtime;
using Microsoft.Framework.TestAdapter;
using Xunit.Abstractions;
using Xunit.ConsoleClient;
using Xunit.Sdk;
#if !NET45
using System.Diagnostics;
#endif

namespace Xunit.KRunner
{
    public class Program
    {
        volatile bool cancel;
        bool failed;
        readonly ConcurrentDictionary<string, ExecutionSummary> completionMessages = new ConcurrentDictionary<string, ExecutionSummary>();
        private readonly IApplicationEnvironment _environment;
        private readonly IFileMonitor _fileMonitor;
        private readonly IServiceProvider _services;

        public Program(IServiceProvider services, IApplicationEnvironment environment, IFileMonitor fileMonitor)
        {
            _services = services;
            _environment = environment;
            _fileMonitor = fileMonitor;
        }

        public int Main(string[] args)
        {
            Console.WriteLine("xUnit.net Project K test runner ({0}-bit {1})", IntPtr.Size * 8, _environment.TargetFramework);
            Console.WriteLine("Copyright (C) 2014 Outercurve Foundation, Microsoft Open Technologies, Inc.");
            Console.WriteLine();

            if (args.Length > 0 && args[0] == "-?")
            {
                PrintUsage();
                return 1;
            }

#if NET45
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

            Console.CancelKeyPress += (sender, e) =>
            {
                if (!cancel)
                {
                    Console.WriteLine("Canceling... (Press Ctrl+C again to terminate)");
                    cancel = true;
                    e.Cancel = true;
                }
            };
            _fileMonitor.OnChanged += _ => Environment.Exit(-409);
#else
            _fileMonitor.OnChanged += _ => Process.GetCurrentProcess().Kill();
#endif

            try
            {
                var commandLine = CommandLine.Parse(args);

                int failCount = RunProject(commandLine);

                return failCount;
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine("error: {0}", ex.Message);
                Console.WriteLine(ex);
                return 1;
            }
            catch (BadImageFormatException ex)
            {
                Console.WriteLine("{0}", ex.Message);
                Console.WriteLine(ex);
                return 1;
            }
        }

#if NET45
        static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;

            if (ex != null)
                Console.WriteLine(ex.ToString());
            else
                Console.WriteLine("Error of unknown type thrown in application domain");


            Environment.Exit(1);
        }
#endif

        static void PrintUsage()
        {
            Console.WriteLine("usage: Xunit.KRunner [options]");
            Console.WriteLine();
            Console.WriteLine("Valid options:");
            Console.WriteLine("  -parallel option       : set parallelization based on option");
            Console.WriteLine("                         :   none - turn off all parallelization");
            Console.WriteLine("                         :   collections - only parallelize collections");
            Console.WriteLine("                         :   all - parallelize collections");
            Console.WriteLine("  -maxthreads count      : maximum thread count for collection parallelization");
            Console.WriteLine("                         :   0 - run with unbounded thread count");
            Console.WriteLine("                         :   >0 - limit task thread pool size to 'count'");
            Console.WriteLine("  -teamcity              : forces TeamCity mode (normally auto-detected)");
        }

        int RunProject(CommandLine options)
        {
            var consoleLock = new object();

            ExecuteAssembly(consoleLock, _environment.ApplicationName, options);

            if (completionMessages.Count > 0)
            {
                Console.WriteLine();
                Console.WriteLine("=== TEST EXECUTION SUMMARY ===");
                int longestAssemblyName = completionMessages.Keys.Max(key => key.Length);
                int longestTotal = completionMessages.Values.Max(summary => summary.Total.ToString().Length);
                int longestFailed = completionMessages.Values.Max(summary => summary.Failed.ToString().Length);
                int longestSkipped = completionMessages.Values.Max(summary => summary.Skipped.ToString().Length);
                int longestTime = completionMessages.Values.Max(summary => summary.Time.ToString("0.000s").Length);

                foreach (var message in completionMessages.OrderBy(m => m.Key))
                    Console.WriteLine("   {0}  Total: {1}, Failed: {2}, Skipped: {3}, Time: {4}",
                                      message.Key.PadRight(longestAssemblyName),
                                      message.Value.Total.ToString().PadLeft(longestTotal),
                                      message.Value.Failed.ToString().PadLeft(longestFailed),
                                      message.Value.Skipped.ToString().PadLeft(longestSkipped),
                                      message.Value.Time.ToString("0.000s").PadLeft(longestTime));
            }

            return failed ? 1 : completionMessages.Values.Sum(summary => summary.Failed);
        }

        TestMessageVisitor<ITestAssemblyFinished> CreateVisitor(object consoleLock, CommandLine options)
        {
            if (options.TeamCity)
            {
                return new TeamCityVisitor(() => cancel);
            }

            if (options.DesignTime)
            {
                var executionSink = (ITestExecutionSink)_services.GetService(typeof(ITestExecutionSink));
                if (executionSink != null)
                {
                    return new DesignTimeExecutionVisitor(executionSink);
                }
            }

            return new StandardOutputVisitor(consoleLock, () => cancel, completionMessages);
        }

        void ExecuteAssembly(object consoleLock, string assemblyName, CommandLine options)
        {
            if (cancel)
                return;

            try
            {
                var name = new AssemblyName(assemblyName);
                var assembly = Reflector.Wrap(Assembly.Load(name));
                var framework = GetFramework(assembly);
                var discoverer = framework.GetDiscoverer(assembly);
                var executor = framework.GetExecutor(name);
                var discoveryVisitor = new TestDiscoveryVisitor();

                discoverer.Find(includeSourceInformation: true, messageSink: discoveryVisitor, options: new TestFrameworkOptions());
                discoveryVisitor.Finished.WaitOne();

                if (options.List)
                {
                    ITestDiscoverySink discoverySink = null;
                    if (options.DesignTime)
                    {
                        discoverySink = (ITestDiscoverySink)_services.GetService(typeof(ITestDiscoverySink));
                    }

                    lock (consoleLock)
                    {
                        foreach (var test in discoveryVisitor.TestCases)
                        {
                            if (discoverySink != null)
                            {
                                discoverySink.SendTest(test.ToDesignTimeTest());
                            }

                            Console.WriteLine(test.DisplayName);
                        }
                    }

                    return;
                }

                var executionOptions = new XunitExecutionOptions { DisableParallelization = !options.ParallelizeTestCollections, MaxParallelThreads = options.MaxParallelThreads };
                var resultsVisitor = CreateVisitor(consoleLock, options);

                var tests = discoveryVisitor.TestCases;
                if (options.Tests != null && options.Tests.Count > 0)
                {
                    tests = tests.Where(t => IsTestNameMatch(t, options.Tests)).ToList();
                }

                executor.RunTests(tests, resultsVisitor, executionOptions);
                resultsVisitor.Finished.WaitOne();

                // When executing under TeamCity, we record the results in a format TeamCity understands, but do not return an error code.
                // This causes TeamCity to treat the step as completed, but the build as failed. We'll work around this by special casing the TeamCityVisitor
                var teamCityVisitor = resultsVisitor as TeamCityVisitor;
                if (teamCityVisitor != null)
                {
                    failed = teamCityVisitor.Failed > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0}: {1}", ex.GetType().FullName, ex.Message);
                Console.WriteLine(ex);

                failed = true;
            }
        }

        ITestFramework GetFramework(IAssemblyInfo assemblyInfo)
        {
            var frameworkAttribute = assemblyInfo.GetCustomAttributes(typeof(TestFrameworkAttribute)).FirstOrDefault();
            if (frameworkAttribute == null)
            {
                return new XunitTestFramework();
            }
            var ctorArgs = frameworkAttribute.GetConstructorArguments().Cast<string>().ToArray();
            var testFrameworkType = Reflector.GetType(ctorArgs[1], ctorArgs[0]);
            var framework = Activator.CreateInstance(testFrameworkType) as ITestFramework;
            return framework ?? new XunitTestFramework();
        }

        // Performs fuzzy matching for test names specified at the commandline.
        // - test name specified at the command line might be a test uniqueId (guid) OR
        // - test name might be a full test display name (including parameters)
        // - test name might be the test class + method name
        private bool IsTestNameMatch(ITestCase test, IList<string> testNames)
        {
            foreach (var testName in testNames)
            {
                if (string.Equals(testName, test.UniqueID, StringComparison.Ordinal))
                {
                    return true;
                }
                else if (string.Equals(testName, test.DisplayName, StringComparison.Ordinal))
                {
                    return true;
                }
                else if (!testName.Contains('(') && test.DisplayName.Contains('('))
                {
                    // No parameters in testName, and parameters in the displayname, it might be
                    // the 'short' display name (without parameters).
                    var shortName = test.DisplayName.Substring(0, test.DisplayName.IndexOf('('));
                    if (string.Equals(testName, shortName, StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
