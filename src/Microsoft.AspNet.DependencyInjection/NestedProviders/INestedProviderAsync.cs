using System;
using System.Threading.Tasks;

namespace Microsoft.AspNet.DependencyInjection
{
    public interface INestedProviderAsync<T> : INestedProvider<T>
    {
        Task InvokeAsync(T context, Func<Task> callNext);
    }
}
