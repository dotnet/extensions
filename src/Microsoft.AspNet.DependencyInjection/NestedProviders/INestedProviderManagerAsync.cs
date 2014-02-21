using System.Threading.Tasks;

namespace Microsoft.AspNet.DependencyInjection
{
    public interface INestedProviderManagerAsync<T>
    {
        Task InvokeAsync(NestedProviderContext<T> context);
    }
}
