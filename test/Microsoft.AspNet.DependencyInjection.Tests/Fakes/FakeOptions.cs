namespace Microsoft.AspNet.DependencyInjection.Tests.Fakes
{
    public class FakeOptions
    {
        public FakeOptions()
        {
            Message = "";
        }

        public string Message { get; set; }
    }
}
