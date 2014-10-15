using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.Framework.Logging;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.AspNet.Cache.Session
{
    public class SessionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly SessionOptions _options;
        private readonly ILogger _logger;

        public SessionMiddleware([NotNull] RequestDelegate next, [NotNull] ILoggerFactory loggerFactory, [NotNull] IOptions<SessionOptions> options, [NotNull] ConfigureOptions<SessionOptions> configureOptions)
        {
            _next = next;
            _logger = loggerFactory.Create<SessionMiddleware>();
            if (configureOptions != null)
            {
                _options = options.GetNamedOptions(configureOptions.Name);
                configureOptions.Configure(_options);
            }
            else
            {
                _options = options.Options;
            }

            if (_options.Store == null)
            {
                throw new ArgumentException("ISessionStore must be specified");
            }

            _options.Store.Connect();
        }

        public async Task Invoke(HttpContext context)
        {
            // TODO: Create a new cookie only in OnSendingHeaders if session has been modified?
            var sessionKey = GetOrSetCookie(context);

            var feature = new SessionFeature();
            feature.Factory = new SessionFactory(sessionKey, _options.Store, _options.IdleTimeout);
            feature.Session = feature.Factory.Create();
            context.SetFeature<ISessionFeature>(feature);

            try
            {
                await _next(context);
            }
            finally
            {
                context.SetFeature<ISessionFeature>(null);

                if (feature.Session != null)
                {
                    try
                    {
                        // TODO: try/catch log?
                        feature.Session.Commit();
                    }
                    catch (Exception ex)
                    {
                        _logger.WriteError("Error closing the session.", ex);
                    }
                }
            }
        }

        private string GetOrSetCookie(HttpContext context)
        {
            var sessionKey = context.Request.Cookies.Get(_options.CookieName);

            if (string.IsNullOrEmpty(sessionKey))
            {
                sessionKey = Guid.NewGuid().ToString(); // TODO: Crypto-random GUID

                var cookieOptions = new CookieOptions
                {
                    Domain = _options.CookieDomain,
                    HttpOnly = _options.CookieHttpOnly,
                    Path = _options.CookiePath ?? "/",
                };

                context.Response.Cookies.Append(_options.CookieName, sessionKey, cookieOptions);

                context.Response.Headers.Set(
                    "Cache-Control",
                    "no-cache");

                context.Response.Headers.Set(
                    "Pragma",
                    "no-cache");

                context.Response.Headers.Set(
                    "Expires",
                    "-1");
            }
            return sessionKey;
        }
    }
}