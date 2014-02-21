using System;
using System.Threading.Tasks;

namespace Microsoft.AspNet.DependencyInjection
{
    public interface INestedProviderAsync<T> : INestedProvider<T>
    {
        Task InvokeAsync(NestedProviderContext<T> context, Func<Task> callNext);
    }
}
