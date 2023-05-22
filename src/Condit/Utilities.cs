namespace Condit;

internal static partial class Functions
{
	[return: System.Diagnostics.CodeAnalysis.NotNull]
	public static T MustNotBeNull<T>(this T value, [System.Runtime.CompilerServices.CallerArgumentExpression(nameof(value))] string name = "value")
		=> value ?? throw new NullReferenceException(name);

	public static TResult Then<TValue, TResult>(this TValue value, Func<TValue, TResult> projection)
		=> projection(value);
		
	public static T Then<T>(this T value, Action<T> action)
	{
		action(value);
		return value;
	}
}