namespace Microsoft.AspNet.DependencyInjection
{
    public interface INestedProviderManager<T>
    {
        void Invoke(T context);
    }
}
