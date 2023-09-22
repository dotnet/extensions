// Assembly 'Microsoft.AspNetCore.HeaderParsing'

namespace Microsoft.AspNetCore.HeaderParsing;

public interface IHeaderRegistry
{
    HeaderKey<T> Register<T>(HeaderSetup<T> setup) where T : notnull;
}
