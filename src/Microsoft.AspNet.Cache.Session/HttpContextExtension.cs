using System;
using Microsoft.AspNet.Cache.Session;

namespace Microsoft.AspNet.Http
{
    public static class HttpContextExtension
    {
        public static ISessionCollection GetSession(this HttpContext context)
        {
            var feature = context.GetFeature<ISessionFeature>();
            if (feature == null)
            {
                throw new InvalidOperationException("Session has not been configured for this application or request.");
            }
            if (feature.Session == null)
            {
                feature.Session = feature.Factory.Create();
            }
            return feature.Session.Collection;
        }
    }
}