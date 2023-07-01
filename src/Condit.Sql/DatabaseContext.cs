namespace Condit.Sql;

public interface IQueryable
{
	IAsyncEnumerable<T> Query<T>();
}

public interface IDatabase
	: IQueryable
{
	IDialect Dialect { get; }
	ValueTask SaveAsync(IAsyncEnumerable<object> records, CancellationToken cancellationToken = default);
}

public sealed class DatabaseContext
	: IQueryable
{
	readonly Func<IAsyncConnection> connectionFactory;
	readonly System.Collections.Concurrent.ConcurrentBag<IAsyncConnection> connections = new();
	public DatabaseContext(Func<IAsyncConnection> connectionFactory)
	{
		this.connectionFactory = connectionFactory;
	}

	public IAsyncEnumerable<T> Query<T>()
		=> new DatabaseQuery<T>(this);
}