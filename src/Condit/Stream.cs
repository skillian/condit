namespace Condit;

using System.Linq.Expressions;

public interface IFilter
{
	object Filter(Expression filterExpression);
}

public interface IFilter<T>
	: IFilter
	, IAsyncEnumerable<T>
{
	IAsyncEnumerable<T> Filter(Expression<Func<T, bool>> filterExpression);
}

public interface IProjector
{
	object Project(Expression projectionExpression);
}

public interface IProjector<TSource>
	: IProjector
	, IAsyncEnumerable<TSource>
{
	IAsyncEnumerable<TResult> Project<TResult>(Expression<Func<TSource, TResult>> projectionExpression);
}

public interface IInnerJoiner
{
	object InnerJoin(IAsyncEnumerable<object?> inner, Expression whenExpression, Expression thenExpression);
}

public interface IInnerJoiner<TOuter>
	: IInnerJoiner
	, IAsyncEnumerable<TOuter>
{
	IAsyncEnumerable<TResult> InnerJoin<TInner, TResult>(IAsyncEnumerable<TInner> inner, Expression<Func<TOuter, TInner, bool>> whenExpression, Expression<Func<TOuter, TInner, TResult>> thenExpression);
}

public static class AsyncEnumerableExtensions
{
	public static IAsyncEnumerable<T> Filter<T>(this IAsyncEnumerable<T> source, Expression<Func<T, bool>> filterExpression)
	{
		if (source is IFilter<T> filter)
			return filter.Filter(filterExpression);

		return new LocalFilter<T>(source, filterExpression);
	}

	public static IAsyncEnumerable<TResult> Project<TSource, TResult>(this IAsyncEnumerable<TSource> source, Expression<Func<TSource, TResult>> projectorExpression)
	{
		if (source is IProjector<TSource> projector)
			return projector.Project<TResult>(projectorExpression);

		return new LocalProjector<TSource, TResult>(source, projectorExpression);
	}

	public static IAsyncEnumerable<TResult> InnerJoin<TOuter, TInner, TResult>(
		this IAsyncEnumerable<TOuter> source,
		IAsyncEnumerable<TInner> inner,
		Expression<Func<TOuter, TInner, bool>> whenExpression,
		Expression<Func<TOuter, TInner, TResult>> thenExpression
	)
	{
		if (source is IInnerJoiner<TOuter> joiner)
			return joiner.InnerJoin(inner, whenExpression, thenExpression);

		return new LocalInnerJoiner<TOuter, TInner, TResult>(
			source, inner, whenExpression, thenExpression
		);
	}
}
