using System.Linq.Expressions;

namespace Condit.Sql;

public interface IQuery
{
	Expression? Filter { get; }
}

static class QueryExtension
{
	public static IQuery AsIQuery(this IQuery query) => query;
}

interface IFilter
{
	Expression Expression { get; }
}



abstract class Query<T>
	: Condit.IFilter
	, IQuery
{
	Expression? IQuery.Filter => GetFilter();
	public IAsyncEnumerable<T> Filter(Expression<Func<T, bool>> filterExpression)
		=> new FilterQuery<T>(this, filterExpression);

	object Condit.IFilter.Filter(Expression filterExpression)
		=> Filter((Expression<Func<T, bool>>)filterExpression);

	protected abstract Expression? GetFilter();
}

class DatabaseQuery<T>
	: Query<T>
	, IQuery
{
	readonly DatabaseContext databaseContext;
	public DatabaseQuery(DatabaseContext databaseContext)
	{
		this.databaseContext = databaseContext;
	}
	protected override Expression? GetFilter() => default;

	public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
	{
		var queryBuilder = new QueryBuilder<T>()
	}
}

abstract class QueryQuery<T>
	: Query<T>
	, IQuery
{
	protected readonly Query<T> source;
	protected QueryQuery(Query<T> source)
	{
		this.source = source;
	}

	protected override Expression? GetFilter()
		=> Functions.Walk(this, x => x.source as QueryQuery<T>)
			.OfType<IFilter>()
			.FirstOrDefault()?.Expression;

	public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
	{
		
	}
}

sealed class FilterQuery<T>
	: QueryQuery<T>
	, IFilter
{
	readonly Expression<Func<T, bool>> filterExpression;
	Expression IFilter.Expression => filterExpression;

	public FilterQuery(Query<T> query, Expression<Func<T, bool>> filterExpression)
		: base(query)
	{
		this.filterExpression = filterExpression ?? throw new ArgumentNullException(nameof(filterExpression));
	}
}

sealed class QueryResult<T>
	: IAsyncEnumerator<T>
{
	readonly Query<T> query;
	readonly CancellationToken cancellationToken;

	public QueryResult(Query<T> query, CancellationToken cancellationToken)
	{
		this.query = query ?? throw new ArgumentNullException(nameof(query));
		this.cancellationToken = cancellationToken;
	}

	public T Current { get; private set; } = default!;

	public ValueTask DisposeAsync()
	{
		throw new NotImplementedException();
	}

	public ValueTask<bool> MoveNextAsync()
	{
		throw new NotImplementedException();
	}
}