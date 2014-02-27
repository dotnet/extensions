using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using Microsoft.Net.Runtime;
using Xunit.ConsoleClient;

namespace Xunit.KRunner
{
    internal class Program
    {
        private volatile bool cancel;
        private readonly ConcurrentDictionary<string, ExecutionSummary> completionMessages =
            new ConcurrentDictionary<string, ExecutionSummary>();
        private readonly IApplicationEnvironment _environment;
        private readonly IFileMonitor _fileMonitor;

        public Program(IApplicationEnvironment environment, IFileMonitor fileMonitor)
        {
            _environment = environment;
            _fileMonitor = fileMonitor;
        }

        private int Main(string[] args)
        {
            Console.WriteLine(
                "xUnit.net Project K test runner ({0}-bit {1} {2})",
                IntPtr.Size * 8,
                _environment.TargetFramework.Identifier,
                _environment.TargetFramework.Version);
            Console.WriteLine("Copyright (C) 2014 Outercurve Foundation, Microsoft Open Technologies, Inc.");
            Console.WriteLine();

            if (args.Length > 0 && args[0] == "-?")
            {
                PrintUsage();
                return 1;
            }

            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
#if NET45
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

                int failCount = RunProject(commandLine.TeamCity);

                return failCount;
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine();
                Console.WriteLine("error: {0}", ex.Message);
                return 1;
            }
            catch (BadImageFormatException ex)
            {
                Console.WriteLine();
                Console.WriteLine("{0}", ex.Message);
                return 1;
            }
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;

            if (ex != null)
                Console.WriteLine(ex.ToString());
            else
                Console.WriteLine("Error of unknown type thrown in application domain");

#if NET45            
            Environment.Exit(1);
#else
            Process.GetCurrentProcess().Kill();
#endif
        }

        private static void PrintUsage()
        {
            Console.WriteLine("usage: Xunit.KRunner [options]");
            Console.WriteLine();
            Console.WriteLine("Valid options:");
            Console.WriteLine("  -teamcity              : forces TeamCity mode (normally auto-detected)");
        }

        private int RunProject(bool teamcity)
        {
            var consoleLock = new object();

            ExecuteAssembly(consoleLock, _environment.ApplicationName, teamcity);

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

            return completionMessages.Values.Sum(summary => summary.Failed);
        }

        private XmlTestExecutionVisitor CreateVisitor(object consoleLock, bool teamCity)
        {
            if (teamCity)
                return new TeamCityVisitor(() => cancel);

            return new StandardOutputVisitor(consoleLock, () => cancel, completionMessages);
        }

        private void ExecuteAssembly(object consoleLock, string assemblyName, bool teamCity)
        {
            if (cancel)
                return;

            try
            {
                var framework = new Xunit.Sdk.XunitTestFramework();
                var executor = framework.GetExecutor(assemblyName);

                var resultsVisitor = CreateVisitor(consoleLock, teamCity);
                executor.Run(resultsVisitor, new XunitDiscoveryOptions(), new XunitExecutionOptions());
                resultsVisitor.Finished.WaitOne();
            }
            catch (Exception ex)
            {
                var e = ex;

                while (e != null)
                {
                    Console.WriteLine("{0}: {1}", e.GetType().FullName, e.Message);

                    foreach (string stackLine in e.StackTrace.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
                        Console.WriteLine(stackLine);

                    e = e.InnerException;
                }
            }
        }
    }
}
