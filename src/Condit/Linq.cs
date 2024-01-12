namespace Condit;

internal static partial class Functions
{
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
