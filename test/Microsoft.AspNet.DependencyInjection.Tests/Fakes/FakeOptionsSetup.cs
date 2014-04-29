namespace Microsoft.AspNet.DependencyInjection.Tests.Fakes
{
    public class FakeOptionsSetupA : IOptionsSetup<FakeOptions>
    {
        public int Order {
            get { return -1; }
        }

        public void Setup(FakeOptions options)
        {
            options.Message += "A";
        }
    }

    public class FakeOptionsSetupB : IOptionsSetup<FakeOptions>
    {
        public int Order
        {
            get { return 10; }
        }

        public void Setup(FakeOptions options)
        {
            options.Message += "B";
        }
    }

    public class FakeOptionsSetupC : IOptionsSetup<FakeOptions>
    {
        public int Order
        {
            get { return 1000; }
        }

        public void Setup(FakeOptions options)
        {
            options.Message += "C";
        }
    }
}
