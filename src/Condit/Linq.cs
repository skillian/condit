namespace Condit;

internal static partial class Functions
{
	public static IAsyncEnumerable<object?> AsObjectAsyncEnumerable<T>(this IAsyncEnumerable<T> source)
		=> (source as IAsyncEnumerable<object?>)
			?? new ObjectAsyncEnumerable<T>(source);

	[return: System.Diagnostics.CodeAnalysis.NotNull]
	public static IEnumerable<T> EmptyIfNull<T>(this IEnumerable<T>? source)
		=> source ?? Enumerable.Empty<T>();

	public static IEnumerable<IndexValuePair<TValue>> Enumerate<TValue>(this IEnumerable<TValue> source)
		=> source.EmptyIfNull().Select((x, i) => IndexValuePair.Create(i, x));

	public static IEnumerable<T> Walk<T>(T? value, Func<T, T?> nextSelector, Func<T?, bool>? continuePredicate = default)
	{
		continuePredicate ??= Reflect<T>.IsNotNullFunc;

		if (!continuePredicate(value))
			yield break;

		yield return value!;

		while (continuePredicate(value = nextSelector(value!)))
			yield return value!;
	}
}

struct IndexValuePair
{
	public static IndexValuePair<T> Create<T>(int index, T value)
		=> new IndexValuePair<T>(index, value);
}

struct IndexValuePair<TValue>
{
	public readonly int Index;
	public readonly TValue Value;

	public IndexValuePair(int index, TValue value)
	{
		Index = index;
		Value = value;
	}

	public void Deconstruct(out int index, out TValue value)
	{
		index = Index;
		value = Value;
	}
}

public struct ObjectAsyncEnumerable<T> : IAsyncEnumerable<object?>
{
	readonly IAsyncEnumerable<T> source;
	public ObjectAsyncEnumerable(IAsyncEnumerable<T> source)
	{
		this.source = source;
	}

	public IAsyncEnumerator<object?> GetAsyncEnumerator(CancellationToken cancellationToken = default)
		=> new Enumerator(source.GetAsyncEnumerator(cancellationToken));

	public struct Enumerator : IAsyncEnumerator<object?>
	{
		public IAsyncEnumerator<T> source;
		public Enumerator(IAsyncEnumerator<T> source)
		{
			this.source = source;
		}

		public object? Current => source.Current;

		public ValueTask DisposeAsync()
			=> source.DisposeAsync();

		public ValueTask<bool> MoveNextAsync()
			=> source.MoveNextAsync();
	}
}