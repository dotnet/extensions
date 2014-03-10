using System;
using Microsoft.Net.Runtime;

namespace Microsoft.AspNet.DependencyInjection
{
    [NotAssemblyNeutral]
    public interface IServiceScopeFactory
    {
        /// <summary>
        /// Create an <see cref="Microsoft.AspNet.DependencyInjection.IServiceScope"/> which
        /// contains an <see cref="System.IServiceProvider"/> used to resolve dependencies from a
        /// newly created scope.
        /// </summary>
        /// <returns>
        /// An <see cref="Microsoft.AspNet.DependencyInjection.IServiceScope"/> controlling the
        /// lifetime of the scope. Once this is disposed, any scoped services that have been resolved
        /// from the <see cref="Microsoft.AspNet.DependencyInjection.IServiceScope.ServiceProvider"/>
        /// will also be disposed.
        /// </returns>
        IServiceScope CreateScope();
    }
}
