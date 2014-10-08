using System;
using System.Linq.Expressions;

namespace Microsoft.Framework.DependencyInjection.ServiceLookup
{
    /// <summary>
    /// Summary description for IServiceCallSite
    /// </summary>
    internal interface IServiceCallSite
    {
        object Invoke(ServiceProvider provider);

        Expression Build(Expression provider);
    }
}