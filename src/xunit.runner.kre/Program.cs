using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Framework.Runtime;
using Microsoft.Framework.TestAdapter;
using Xunit.Abstractions;
using VsTestCase = Microsoft.Framework.TestAdapter.Test;

namespace Xunit.ConsoleClient
{
    public class Program
    {
        volatile bool cancel;
        bool failed;
        readonly ConcurrentDictionary<string, ExecutionSummary> completionMessages = new ConcurrentDictionary<string, ExecutionSummary>();

        private readonly IApplicationEnvironment _appEnv;
        private readonly IServiceProvider _services;

        public Program(IApplicationEnvironment appEnv, IServiceProvider services)
        {
            _appEnv = appEnv;
            _services = services;
        }

        [STAThread]
        public int Main(string[] args)
        {
            args = Enumerable.Repeat(_appEnv.ApplicationName + ".dll", 1).Concat(args).ToArray();

            var originalForegroundColor = Console.ForegroundColor;

            try
            {
                var framework = _appEnv.RuntimeFramework;
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("xUnit.net K Runtime Environment test runner ({0}-bit {1} {2})", IntPtr.Size * 8, framework.Identifier, framework.Version);
                Console.WriteLine("Copyright (C) 2014 Outercurve Foundation.");
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Gray;

                if (args.Length == 0 || args[0] == "-?")
                {
                    PrintUsage();
                    return 1;
                }

#if !ASPNETCORE50
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
#endif

                var defaultDirectory = Directory.GetCurrentDirectory();
                if (!defaultDirectory.EndsWith(new String(new[] { Path.DirectorySeparatorChar })))
                    defaultDirectory += Path.DirectorySeparatorChar;

                var commandLine = CommandLine.Parse(args);

                var failCount = RunProject(defaultDirectory, commandLine.Project, commandLine.TeamCity,
                                           commandLine.ParallelizeTestCollections,
                                           commandLine.MaxParallelThreads,
                                           commandLine.DesignTime,
                                           commandLine.List,
                                           commandLine.DesignTimeTestUniqueNames);

                if (commandLine.Wait)
                {
                    Console.WriteLine();
#if ASPNETCORE50
                    Console.Write("Press ENTER to continue...");
                    Console.ReadLine();
#else
                    Console.Write("Press any key to continue...");
                    Console.ReadKey();
#endif
                    Console.WriteLine();
                }

                return failCount;
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine("error: {0}", ex.Message);
                return 1;
            }
            catch (BadImageFormatException ex)
            {
                Console.WriteLine("{0}", ex.Message);
                return 1;
            }
            finally
            {
                Console.ForegroundColor = originalForegroundColor;
            }
        }

#if !ASPNETCORE50
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
            Console.WriteLine("usage: xunit.runner.kre [assemblyFile ...] [options]");
            Console.WriteLine();
            Console.WriteLine("Valid options:");
            Console.WriteLine("  -parallel option       : set parallelization based on option");
            Console.WriteLine("                         :   none - turn off all parallelization");
            Console.WriteLine("                         :   collections - only parallelize collections");
            Console.WriteLine("                         :   all - parallelize collections");
            Console.WriteLine("  -maxthreads count      : maximum thread count for collection parallelization");
            Console.WriteLine("                         :   0 - run with unbounded thread count");
            Console.WriteLine("                         :   >0 - limit task thread pool size to 'count'");
            Console.WriteLine("  -noshadow              : do not shadow copy assemblies");
            Console.WriteLine("  -teamcity              : forces TeamCity mode (normally auto-detected)");
            Console.WriteLine("  -wait                  : wait for input after completion");
            Console.WriteLine("  -trait \"name=value\"    : only run tests with matching name/value traits");
            Console.WriteLine("                         : if specified more than once, acts as an OR operation");
            Console.WriteLine("  -notrait \"name=value\"  : do not run tests with matching name/value traits");
            Console.WriteLine("                         : if specified more than once, acts as an AND operation");
            Console.WriteLine("  -method \"name\"         : run a given test method (should be fully specified;");
            Console.WriteLine("                         : i.e., 'MyNamespace.MyClass.MyTestMethod')");
            Console.WriteLine("                         : if specified more than once, acts as an OR operation");
            Console.WriteLine("  -class \"name\"          : run all methods in a given test class (should be fully");
            Console.WriteLine("                         : specified; i.e., 'MyNamespace.MyClass')");
            Console.WriteLine("                         : if specified more than once, acts as an OR operation");

            foreach (var transform in TransformFactory.AvailableTransforms)
                Console.WriteLine("  {0} : {1}",
                                  String.Format("-{0} <filename>", transform.CommandLine).PadRight(22).Substring(0, 22),
                                  transform.Description);
        }

        int RunProject(string defaultDirectory, XunitProject project, bool teamcity, bool parallelizeTestCollections, int maxThreadCount, bool designTime, bool list, IReadOnlyList<string> designTimeFullyQualifiedNames)
        {
            XElement assembliesElement = null;
            var xmlTransformers = TransformFactory.GetXmlTransformers(project);
            var needsXml = xmlTransformers.Count > 0;
            var consoleLock = new object();

            if (needsXml)
                assembliesElement = new XElement("assemblies");

            var originalWorkingFolder = Directory.GetCurrentDirectory();

            using (AssemblyHelper.SubscribeResolve())
            {
                var clockTime = Stopwatch.StartNew();

                foreach (var assembly in project.Assemblies)
                {
                    var assemblyElement = ExecuteAssembly(consoleLock, defaultDirectory, assembly, needsXml, teamcity, parallelizeTestCollections, maxThreadCount, project.Filters, designTime, list, designTimeFullyQualifiedNames);
                    if (assemblyElement != null)
                        assembliesElement.Add(assemblyElement);
                }

                clockTime.Stop();

                if (completionMessages.Count > 0)
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine();
                    Console.WriteLine("=== TEST EXECUTION SUMMARY ===");
                    Console.ForegroundColor = ConsoleColor.Gray;

                    var totalTestsRun = completionMessages.Values.Sum(summary => summary.Total);
                    var totalTestsFailed = completionMessages.Values.Sum(summary => summary.Failed);
                    var totalTestsSkipped = completionMessages.Values.Sum(summary => summary.Skipped);
                    var totalTime = completionMessages.Values.Sum(summary => summary.Time).ToString("0.000s");
                    var totalErrors = completionMessages.Values.Sum(summary => summary.Errors);
                    var longestAssemblyName = completionMessages.Keys.Max(key => key.Length);
                    var longestTotal = totalTestsRun.ToString().Length;
                    var longestFailed = totalTestsFailed.ToString().Length;
                    var longestSkipped = totalTestsSkipped.ToString().Length;
                    var longestTime = totalTime.Length;
                    var longestErrors = totalErrors.ToString().Length;

                    foreach (var message in completionMessages.OrderBy(m => m.Key))
                        Console.WriteLine("   {0}  Total: {1}, Errors: {2}, Failed: {3}, Skipped: {4}, Time: {5}",
                                          message.Key.PadRight(longestAssemblyName),
                                          message.Value.Total.ToString().PadLeft(longestTotal),
                                          message.Value.Errors.ToString().PadLeft(longestErrors),
                                          message.Value.Failed.ToString().PadLeft(longestFailed),
                                          message.Value.Skipped.ToString().PadLeft(longestSkipped),
                                          message.Value.Time.ToString("0.000s").PadLeft(longestTime));

                    if (completionMessages.Count > 1)
                        Console.WriteLine("   {0}         {1}          {2}          {3}           {4}        {5}" + Environment.NewLine +
                                          "           {6} {7}          {8}          {9}           {10}        {11} ({12})",
                                          " ".PadRight(longestAssemblyName),
                                          "-".PadRight(longestTotal, '-'),
                                          "-".PadRight(longestErrors, '-'),
                                          "-".PadRight(longestFailed, '-'),
                                          "-".PadRight(longestSkipped, '-'),
                                          "-".PadRight(longestTime, '-'),
                                          "GRAND TOTAL:".PadLeft(longestAssemblyName),
                                          totalTestsRun,
                                          totalErrors,
                                          totalTestsFailed,
                                          totalTestsSkipped,
                                          totalTime,
                                          clockTime.Elapsed.TotalSeconds.ToString("0.000s"));

                }
            }

            Directory.SetCurrentDirectory(originalWorkingFolder);

            foreach (var transformer in xmlTransformers) transformer(assembliesElement);

            return failed ? 1 : completionMessages.Values.Sum(summary => summary.Failed);
        }

        TestMessageVisitor<ITestAssemblyFinished> CreateVisitor(object consoleLock, string defaultDirectory, XElement assemblyElement, bool teamCity)
        {
            if (teamCity)
                return new TeamCityVisitor(assemblyElement, () => cancel);

            return new StandardOutputVisitor(consoleLock, defaultDirectory, assemblyElement, () => cancel, completionMessages);
        }

        XElement ExecuteAssembly(object consoleLock, string defaultDirectory, XunitProjectAssembly assembly, bool needsXml, bool teamCity, bool parallelizeTestCollections, int maxThreadCount, XunitFilters filters, bool designTime, bool list, IReadOnlyList<string> designTimeFullyQualifiedNames)
        {
            if (cancel)
                return null;

            var assemblyElement = needsXml ? new XElement("assembly") : null;

            try
            {
                lock (consoleLock)
                    Console.WriteLine("Discovering: {0}", Path.GetFileNameWithoutExtension(assembly.AssemblyFilename));

                using (var controller = new XunitFrontController(assembly.AssemblyFilename, assembly.ConfigFilename, assembly.ShadowCopy))
                using (var discoveryVisitor = new TestDiscoveryVisitor())
                {
                    controller.Find(includeSourceInformation: false, messageSink: discoveryVisitor, options: new TestFrameworkOptions());
                    discoveryVisitor.Finished.WaitOne();

                    IDictionary<ITestCase, VsTestCase> vsTestcases = null;
                    if (designTime)
                    {
                        vsTestcases = DesignTimeTestConverter.Convert(discoveryVisitor.TestCases);
                    }

                    lock (consoleLock)
                        Console.WriteLine("Discovered:  {0}", Path.GetFileNameWithoutExtension(assembly.AssemblyFilename));

                    if (list)
                    {
                        lock (consoleLock)
                        {
                            if (designTime)
                            {
                                var sink = (ITestDiscoverySink)_services.GetService(typeof(ITestDiscoverySink));

                                foreach (var testcase in vsTestcases.Values)
                                {
                                    if (sink != null)
                                    {
                                        sink.SendTest(testcase);
                                    }

                                    Console.WriteLine(testcase.FullyQualifiedName);
                                }
                            }
                            else
                            {
                                foreach (var testcase in discoveryVisitor.TestCases)
                                {
                                    Console.WriteLine(testcase.DisplayName);
                                }
                            }
                        }

                        return assemblyElement;
                    }

                    var executionOptions = new XunitExecutionOptions { DisableParallelization = !parallelizeTestCollections, MaxParallelThreads = maxThreadCount };
                    var resultsVisitor = CreateVisitor(consoleLock, defaultDirectory, assemblyElement, teamCity);

                    if (designTime)
                    {
                        var sink = (ITestExecutionSink)_services.GetService(typeof(ITestExecutionSink));
                        resultsVisitor = new DesignTimeExecutionVisitor(
                            sink,
                            vsTestcases,
                            resultsVisitor);
                    }

                    IList<ITestCase> filteredTestCases;
                    if (!designTime || designTimeFullyQualifiedNames.Count == 0)
                    {
                        filteredTestCases = discoveryVisitor.TestCases.Where(filters.Filter).ToList();
                    }
                    else
                    {
                        filteredTestCases = (from t in vsTestcases
                                             where designTimeFullyQualifiedNames.Contains(t.Value.FullyQualifiedName)
                                             select t.Key)
                                            .ToList();
                    }

                    if (filteredTestCases.Count == 0)
                    {
                        lock (consoleLock)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("ERROR:       {0} has no tests to run", Path.GetFileNameWithoutExtension(assembly.AssemblyFilename));
                            Console.ForegroundColor = ConsoleColor.Gray;
                        }
                    }
                    else
                    {
                        controller.RunTests(filteredTestCases, resultsVisitor, executionOptions);
                        resultsVisitor.Finished.WaitOne();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0}: {1}", ex.GetType().FullName, ex.Message);
                failed = true;
            }

            return assemblyElement;
        }
    }
}
