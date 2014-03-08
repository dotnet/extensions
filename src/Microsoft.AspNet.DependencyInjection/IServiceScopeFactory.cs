using System;
using Microsoft.Net.Runtime;

namespace Microsoft.AspNet.DependencyInjection
{
    [NotAssemblyNeutral]
    public interface IServiceScopeFactory
    {
        /// <summary>
        /// Create an <see cref="System.IServiceProvider"/> which can be used to resolve
        /// dependencies from a newly created scope.
        /// </summary>
        /// <param name="scope">
        /// The <see cref="System.IServiceProvider"/> used to resolve dependencies from the scope.
        /// </param>
        /// <returns>
        /// An <see cref="System.IDisposable"/> controlling the lifetime of the scope. Once this is
        /// disposed, any scoped services that have been resolved from the scope will be disposed.
        /// </returns>
        IDisposable CreateScope(out IServiceProvider scope);
    }
}
