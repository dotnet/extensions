using System;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Http;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.Logging;
using Microsoft.Framework.Logging.Serilog;
using Serilog;

namespace SerilogSampleApp
{
    public class Startup
    {
        private readonly Microsoft.Framework.Logging.ILogger _logger;

        public Startup(
            IHostingEnvironment hostingEnvironment,
            ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.Create<Startup>();

            var configuration = new Configuration();
            configuration.AddJsonFile("config.json");
            configuration.AddEnvironmentVariables();
            var loggingConfiguration = configuration.GetSubKey("Logging");

            var serilog = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .Enrich.WithMachineName()
                .Enrich.WithProcessId()
                .Enrich.WithThreadId();

            if (string.Equals(hostingEnvironment.EnvironmentName, "Development", StringComparison.OrdinalIgnoreCase))
            {
                serilog.WriteTo.ColoredConsole();
            }

            string elasticSearchConnectionString;
            if (loggingConfiguration.TryGet("ElasticSearch:Server", out elasticSearchConnectionString))
            {
                serilog.WriteTo.ElasticSearch(node: new Uri(elasticSearchConnectionString));
            }

            loggerFactory.AddSerilog(serilog);
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment hostingEnvironment)
        {
            app.Use(next => async ctx =>
            {
                using (_logger.BeginScope(new RequestScope(ctx)))
                {
                    _logger.WriteInformation(new BeginRequest(ctx));
                    try
                    {
                        await next(ctx);
                    }
                    finally
                    {
                        _logger.WriteInformation(new EndRequest(ctx));
                    }
                }
            });

            _logger.WriteWarning("Boo!");
        }

        private class RequestScope : LoggerStructureBase
        {
            private readonly HttpContext _context;
            private readonly string _activityId = Guid.NewGuid().ToString("n");

            public RequestScope(HttpContext context)
            {
                _context = context;
            }

            public string ActivityId { get { return _activityId; } }
            public string ActivityPath { get { return _context.Request.Path.Value; } }

            public override string Format()
            {
                return string.Format("Scope {0} {1}", ActivityId, ActivityPath);
            }
        }

        private class BeginRequest : LoggerStructureBase
        {
            private readonly HttpContext _context;

            public BeginRequest(HttpContext context)
            {
                _context = context;
            }

            public string Protocol { get { return _context.Request.Protocol; } }
            public string Scheme { get { return _context.Request.Scheme; } }
            public string Host { get { return _context.Request.Host.Value; } }
            public string PathBase { get { return _context.Request.PathBase.Value; } }
            public string Path { get { return _context.Request.Path.Value; } }
            public string Query { get { return _context.Request.QueryString.Value; } }
            public string Method { get { return _context.Request.Method; } }

            public override string Format()
            {
                return string.Format(
                    "Request starting {0} {1}://{2}{3}{4}{5} {6}",
                    Method, Scheme, Host, PathBase, Path, Query, Protocol);
            }
        }

        private class EndRequest : LoggerStructureBase
        {
            private readonly HttpContext _context;

            public EndRequest(HttpContext context)
            {
                _context = context;
            }

            public int StatusCode { get { return _context.Response.StatusCode; } }
            public string ContentType { get { return _context.Response.ContentType; } }

            public override string Format()
            {
                return string.Format(
                    "Request ending {0} {1}",
                    StatusCode, ContentType);
            }
        }
    }
}
