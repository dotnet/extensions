using System.Threading.Tasks;

namespace Microsoft.AspNet.DependencyInjection
{
    public interface INestedProviderManagerAsync<T>
    {
        void Invoke(NestedProviderContext<T> context);

        Task InvokeAsync(NestedProviderContext<T> context);
    }
}
