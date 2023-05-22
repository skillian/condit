namespace Condit;

abstract class LocalEnumerable<TSource, TResult>
	: IAsyncEnumerable<TResult>
{
	protected readonly IAsyncEnumerable<TSource> source;

	public LocalEnumerable(IAsyncEnumerable<TSource> source)
	{
		this.source = source ?? throw new ArgumentNullException(nameof(source));
	}

	public abstract IAsyncEnumerator<TResult> GetAsyncEnumerator(CancellationToken cancellationToken = default);

	protected abstract class Enumerator<TLocalEnumerable>
		: IAsyncEnumerator<TResult>
			where TLocalEnumerable : LocalEnumerable<TSource, TResult>
	{
		protected readonly TLocalEnumerable enumerable;
		protected readonly IAsyncEnumerator<TSource> source;
		protected readonly CancellationToken cancellationToken;

		public Enumerator(
			TLocalEnumerable enumerable,
			IAsyncEnumerator<TSource> source,
			CancellationToken cancellationToken
		)
		{
			this.enumerable = enumerable ?? throw new ArgumentNullException(nameof(enumerable));
			this.source = source ?? throw new ArgumentNullException(nameof(source));
			this.cancellationToken = cancellationToken;
		}

		public abstract TResult Current { get; }

		public virtual ValueTask DisposeAsync()
			=> source.DisposeAsync();

		public virtual ValueTask<bool> MoveNextAsync()
			=> source.MoveNextAsync();
	}
}

class LocalFilter<T>
	: LocalEnumerable<T, T>
{
	readonly System.Linq.Expressions.Expression<Func<T, bool>> filterExpression;
	readonly Lazy<Func<T, bool>> filter;
	public LocalFilter(IAsyncEnumerable<T> source, System.Linq.Expressions.Expression<Func<T, bool>> filterExpression)
		: base(source)
	{
		this.filterExpression = filterExpression ?? throw new ArgumentNullException(nameof(filterExpression));
		filter = new Lazy<Func<T, bool>>(filterExpression.Compile, LazyThreadSafetyMode.ExecutionAndPublication);
	}

	public override IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
		=> new FilterEnumerator(this, source.GetAsyncEnumerator(cancellationToken), cancellationToken);

	class FilterEnumerator : LocalEnumerable<T, T>.Enumerator<LocalFilter<T>>
	{
		Func<T, bool> filter;
		public FilterEnumerator(
			LocalFilter<T> enumerable,
			IAsyncEnumerator<T> source,
			CancellationToken cancellationToken
		)
			: base(enumerable, source, cancellationToken)
		{
			filter = enumerable.filter.Value;
		}

		public override T Current => source.Current;

		public override async ValueTask<bool> MoveNextAsync()
		{
			while (await base.MoveNextAsync())
			{
				if (filter(source.Current))
					return true;

				if (cancellationToken.IsCancellationRequested)
					return false;	// TODO: Is this right?
			}

			return false;
		}
	}
}

class LocalProjector<TSource, TResult>
	: LocalEnumerable<TSource, TResult>
{
	readonly System.Linq.Expressions.Expression<Func<TSource, TResult>> projectorExpression;
	readonly Lazy<Func<TSource, TResult>> projector;
	public LocalProjector(IAsyncEnumerable<TSource> source, System.Linq.Expressions.Expression<Func<TSource, TResult>> projectorExpression) : base(source)
	{
		this.projectorExpression = projectorExpression ?? throw new ArgumentNullException(nameof(projectorExpression));
		projector = new Lazy<Func<TSource, TResult>>(projectorExpression.Compile, LazyThreadSafetyMode.ExecutionAndPublication);
	}

	public override IAsyncEnumerator<TResult> GetAsyncEnumerator(CancellationToken cancellationToken = default)
		=> new ProjectorEnumerator(this, source.GetAsyncEnumerator(cancellationToken), cancellationToken);

	class ProjectorEnumerator
		: Enumerator<LocalProjector<TSource, TResult>>
	{
		readonly Func<TSource, TResult> projector;
		public ProjectorEnumerator(
			LocalProjector<TSource, TResult> enumerable,
			IAsyncEnumerator<TSource> source,
			CancellationToken cancellationToken
		)
			: base(enumerable, source, cancellationToken)
		{
			projector = enumerable.projector.Value;
		}

		TResult current = default!;
		public override TResult Current => current;

		public override async ValueTask<bool> MoveNextAsync()
		{
			if (!await base.MoveNextAsync())
				return false;

			current = projector(source.Current);
			return true;
		}
	}
}

class LocalInnerJoiner<TOuter, TInner, TResult>
	: LocalEnumerable<TOuter, TResult>
{
	readonly IAsyncEnumerable<TInner> inner;
	readonly System.Linq.Expressions.Expression<Func<TOuter, TInner, bool>> whenExpression;
	readonly Lazy<Func<TOuter, TInner, bool>> when;
	readonly System.Linq.Expressions.Expression<Func<TOuter, TInner, TResult>> thenExpression;
	readonly Lazy<Func<TOuter, TInner, TResult>> then;
	public LocalInnerJoiner(
		IAsyncEnumerable<TOuter> source,
		IAsyncEnumerable<TInner> inner,
		System.Linq.Expressions.Expression<Func<TOuter, TInner, bool>> whenExpression,
		System.Linq.Expressions.Expression<Func<TOuter, TInner, TResult>> thenExpression
	)
		: base(source)
	{
		this.inner = inner ?? throw new ArgumentNullException(nameof(inner));

		this.whenExpression = whenExpression ?? throw new ArgumentNullException(nameof(whenExpression));
		when = new Lazy<Func<TOuter, TInner, bool>>(whenExpression.Compile, LazyThreadSafetyMode.ExecutionAndPublication);

		this.thenExpression = thenExpression ?? throw new ArgumentNullException(nameof(thenExpression));
		then = new Lazy<Func<TOuter, TInner, TResult>>(thenExpression.Compile, LazyThreadSafetyMode.ExecutionAndPublication);
	}

	public override IAsyncEnumerator<TResult> GetAsyncEnumerator(CancellationToken cancellationToken = default)
		=> new InnerJoinerEnumerator(this, source.GetAsyncEnumerator(cancellationToken), inner.GetAsyncEnumerator(cancellationToken), cancellationToken);

	class InnerJoinerEnumerator : Enumerator<LocalInnerJoiner<TOuter, TInner, TResult>>
	{
		static readonly NLog.ILogger logger = new NLog.Config.LoggingConfiguration()
			.Then(config => {
				NLog.Targets.Target target = new NLog.Targets.ConsoleTarget();
				config.AddRule(NLog.LogLevel.Debug, NLog.LogLevel.Fatal, target);
				
				target = new NLog.Targets.FileTarget() { FileName = "log.txt" };
				config.AddRule(NLog.LogLevel.Debug, NLog.LogLevel.Fatal, target);

				NLog.LogManager.Configuration = config;
				
				return NLog.LogManager.GetCurrentClassLogger(typeof(InnerJoinerEnumerator));
			});

		readonly Func<TOuter, TInner, bool> when;
		readonly Func<TOuter, TInner, TResult> then;
		IAsyncEnumerator<TInner> inner;
		public InnerJoinerEnumerator(
			LocalInnerJoiner<TOuter, TInner, TResult> enumerable,
			IAsyncEnumerator<TOuter> source,
			IAsyncEnumerator<TInner> inner,
			CancellationToken cancellationToken
		)
			: base(enumerable, source, cancellationToken)
		{
			when = enumerable.when.Value;
			then = enumerable.then.Value;
			this.inner = inner ?? throw new ArgumentNullException(nameof(inner));
		}

		TResult current = default!;
		public override TResult Current => current;

		delegate ValueTask<(MoveNextAsyncFunc? next, bool callNext)> MoveNextAsyncFunc(InnerJoinerEnumerator self);

		static readonly MoveNextAsyncFunc initialMoveNextAsync
			= async @this => {
				if (!await @this.source.MoveNextAsync())
				{
					return (null, false);
				}

				@this.inner = @this.enumerable.inner.GetAsyncEnumerator(
					@this.cancellationToken
				);

				return (subsequentMoveNextAsync, true);
			};

		static readonly MoveNextAsyncFunc subsequentMoveNextAsync
			= async @this => {
				while (await @this.inner.MoveNextAsync())
				{
					if (@this.when(@this.source.Current, @this.inner.Current))
					{
						@this.current = @this.then(@this.source.Current, @this.inner.Current);
						return (subsequentMoveNextAsync, false);
					}
				}

				return (initialMoveNextAsync, true);
			};

		(MoveNextAsyncFunc?, bool) moveNextAsyncState = (initialMoveNextAsync, true);

		public override async ValueTask<bool> MoveNextAsync()
		{
			if (moveNextAsyncState.Item1 is null)
				return false;

			while (moveNextAsyncState.Item2)
			{
				moveNextAsyncState = await moveNextAsyncState.Item1.MustNotBeNull()(this);
			}

			moveNextAsyncState = (moveNextAsyncState.Item1, true);

			return moveNextAsyncState.Item1 is not null;
		}
	}
}
