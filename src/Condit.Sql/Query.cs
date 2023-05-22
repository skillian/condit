using System.Linq.Expressions;

namespace Condit.Sql;

interface IQuery
{
	IAsyncConnection AsyncConnection { get; }
	Expression? GetFilter();
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
	: IAsyncEnumerable<T>
	, IFilter<T>
{
	public IAsyncEnumerable<T> Filter(Expression<Func<T, bool>> filterExpression)
		=> new FilterQuery<T>(this, filterExpression);

	public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
	{
		var queryBuilder = new Builder<T>(this);

		return queryBuilder.GetAsyncEnumerator(cancellationToken);
	}

	object Condit.IFilter.Filter(Expression filterExpression)
		=> Filter((Expression<Func<T, bool>>)filterExpression);

	class Builder : IAsyncEnumerable<T>
	{
		readonly Query<T> query;
		readonly ExpressionBuilder expressionBuilder;
		public Builder(Query<T> query)
		{
			this.query = query;
			expressionBuilder = new ExpressionBuilder(
				query.AsyncConnection.CreateAsyncCommand()
			);
		}
	}
}

class ConnectionQuery<T>
	: Query<T>
	, IQuery
{
	readonly IAsyncConnection asyncConnection;
	IAsyncConnection IQuery.AsyncConnection => asyncConnection;
	Expression? IQuery.GetFilter() => default;

	public ConnectionQuery(IAsyncConnection asyncConnection)
	{
		this.asyncConnection = asyncConnection ?? throw new ArgumentNullException(nameof(asyncConnection));
	}
}

abstract class QueryQuery<T>
	: Query<T>
	, IQuery
{
	protected readonly Query<T> source;

	IAsyncConnection IQuery.AsyncConnection
		=> (
			Functions.Walk(this, x => x.source as QueryQuery<T>)
				.OfType<ConnectionQuery<T>>()
				.FirstOrDefault()
					?? throw new Exception($"query has no {nameof(ConnectionQuery<T>)}")
		).AsIQuery().AsyncConnection;

	Expression? IQuery.GetFilter()
		=> Functions.Walk(this, x => x.source as QueryQuery<T>)
			.OfType<IFilter>()
			.Select(x => x.Expression)
			.Aggregate(Expression.And);

	protected QueryQuery(Query<T> source)
	{
		this.source = source;
	}
}

class FilterQuery<T>
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