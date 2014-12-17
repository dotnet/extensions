using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit.Sdk;

internal class SkipReasonTestCase : IXunitTestCase
{
    private readonly string _skipReason;
    private readonly IXunitTestCase _wrappedTestCase;

    public SkipReasonTestCase(string skipReason, IXunitTestCase wrappedBase)
    {
        _skipReason = wrappedBase.SkipReason ?? skipReason;
        _wrappedTestCase = wrappedBase;
    }

    public string DisplayName
    {
        get
        {
            return _wrappedTestCase.DisplayName;
        }
    }

    public IMethodInfo Method
    {
        get
        {
            return _wrappedTestCase.Method;
        }
    }

    public string SkipReason
    {
        get
        {
            return _skipReason;
        }
    }

    public ISourceInformation SourceInformation
    {
        get
        {
            return _wrappedTestCase.SourceInformation;
        }

        set
        {
            _wrappedTestCase.SourceInformation = value;
        }
    }

    public ITestMethod TestMethod
    {
        get
        {
            return _wrappedTestCase.TestMethod;
        }
    }

    public object[] TestMethodArguments
    {
        get
        {
            return _wrappedTestCase.TestMethodArguments;
        }
    }

    public Dictionary<string, List<string>> Traits
    {
        get
        {
            return _wrappedTestCase.Traits;
        }
    }

    public string UniqueID
    {
        get
        {
            return _wrappedTestCase.UniqueID;
        }
    }

    public Task<RunSummary> RunAsync(IMessageBus messageBus, object[] constructorArguments, ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource)
    {
        return new XunitTestCaseRunner(this, DisplayName, _skipReason, constructorArguments, TestMethodArguments, messageBus, aggregator, cancellationTokenSource).RunAsync();
    }
}