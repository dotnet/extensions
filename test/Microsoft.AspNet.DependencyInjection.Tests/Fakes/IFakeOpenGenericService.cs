namespace Microsoft.AspNet.DependencyInjection.Tests.Fakes
{
    public interface IFakeOpenGenericService<T>
    {
        T SimpleMethod();
    }
}
