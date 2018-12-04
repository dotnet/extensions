// all existing listeners that enabled that EventSource will receive the event.
// This makes running EventSourceLogger tests in parallel difficult. We mark this assembly
// with CollectionBehavior.CollectionPerAssembly to ensure that all tests in this assembly are executed serially.

[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly)]
