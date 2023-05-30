// Assembly 'System.Cloud.DocumentDb.Abstractions'

namespace System.Cloud.DocumentDb;

/// <summary>
/// An interface for injecting <see cref="T:System.Cloud.DocumentDb.IDocumentDatabase" /> to a specific context.
/// </summary>
/// <typeparam name="TContext">The context type, indicating injection preferences.</typeparam>
public interface IDocumentDatabase<TContext> : IDocumentDatabase where TContext : class
{
}
