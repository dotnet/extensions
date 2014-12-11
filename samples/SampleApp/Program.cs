using System;
using System.Collections.Generic;
using Microsoft.Framework.Logging;
using Microsoft.Framework.Logging.Console;
using ILogger = Microsoft.Framework.Logging.ILogger;
#if !ASPNETCORE50
using Serilog;
using Serilog.Sinks.IOFile;
using Serilog.Formatting.Raw;
using Serilog.Sinks.RollingFile;
using Serilog.Formatting.Json;
#endif

namespace SampleApp
{
    public class Program
    {
        private readonly ILogger _logger;

        public Program()
        {
            // a DI based application would get ILoggerFactory injected instead
            var factory = new LoggerFactory();

            // getting the logger immediately using the class's name is conventional
            _logger = factory.Create(typeof(Program).FullName);

            // providers may be added to an ILoggerFactory at any time, existing ILoggers are updated
#if !ASPNETCORE50
            factory.AddNLog(new global::NLog.LogFactory());

            factory.AddSerilog(new Serilog.LoggerConfiguration()
                .Enrich.WithMachineName()
                .Enrich.WithProcessId()
                .Enrich.WithThreadId()
                .MinimumLevel.Debug()
                .WriteTo.RollingFile("file-{Date}.txt", outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level}:{EventId} [{SourceContext}] {Message}{NewLine}{Exception}")
                .WriteTo.Sink(new RollingFileSink("file-{Date}.json", new JsonFormatter(), null, null))
                .WriteTo.Sink(new FileSink("dump.txt", new RawFormatter(), null)));
#endif
            //factory.AddConsole();
            //factory.AddConsole((category, logLevel) => logLevel >= LogLevel.Critical && category.Equals(typeof(Program).FullName));
        }
        
        public void Main(string[] args)
        {
            _logger.WriteInformation("Starting");

            _logger.WriteInformation(1, "Started at '{StartTime}' and 0x{Hello:X} is hex of 42", DateTimeOffset.UtcNow, 42);

            try
            {
                throw new Exception("Boom");
            }
            catch (Exception ex)
            {
                _logger.WriteCritical("Unexpected critical error starting application", ex);
                _logger.Write(LogLevel.Critical, 0, "Unexpected critical error", ex, null);
                // This write should not log anything
                _logger.Write(LogLevel.Critical, 0, null, null, null);
                _logger.WriteError("Unexpected error", ex);
                _logger.WriteWarning("Unexpected warning", ex);
            }

            using (_logger.BeginScope("Main"))
            {
                Console.WriteLine("Hello World");

                _logger.WriteInformation("Waiting for user input");
                Console.ReadLine();
            }

            _logger.WriteInformation(2, "Stopping at '{StopTime}'", DateTimeOffset.UtcNow);

            _logger.WriteInformation("Stopping");

        }
    }
}
