namespace Microsoft.AspNet.DependencyInjection
{
    public interface INestedProviderManager<T>
    {
        void Invoke(NestedProviderContext<T> context);
    }
}
