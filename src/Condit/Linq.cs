namespace Condit;

internal static partial class Functions
{
	[return: System.Diagnostics.CodeAnalysis.NotNull]
	public static IEnumerable<T> EmptyIfNull<T>(this IEnumerable<T>? source)
		=> source ?? Enumerable.Empty<T>();

	public static IEnumerable<IndexValuePair<TValue>> Enumerate<TValue>(this IEnumerable<TValue> source)
		=> source.EmptyIfNull().Select(IndexValuePair.Create);

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
	public static IndexValuePair<T> Create<T>(T value, int index)
		=> new IndexValuePair<T>(value, index);
}

struct IndexValuePair<TValue>
{
	public readonly TValue Value;
	public readonly int Index;
	public IndexValuePair(TValue value, int index)
	{
		Value = value;
		Index = index;
	}

	public void Deconstruct(out TValue value, out int index)
	{
		value = Value;
		index = Index;
	}
}
