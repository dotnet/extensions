using Microsoft.Framework.Logging;
using System;

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
#endif
        }

        public void Main(string[] args)
        {
            _logger.WriteInformation("Starting");

            try
            {
                throw new Exception("Boom");
            }
            catch (Exception ex)
            {
                _logger.WriteError("Unexpected error starting application", ex);
                _logger.Write(TraceType.Critical, 0, "unexpected error", ex, null);
                _logger.Write(TraceType.Critical, 0, null, null, null);
            }

            using (_logger.BeginScope("Main"))
            {
                Console.WriteLine("Hello World");

                _logger.WriteInformation("Waiting for user input");
                Console.ReadLine();
            }

            _logger.WriteInformation("Stopping");

        }
    }
}
