namespace Condit;

internal static partial class Functions
{
	public static ValueTask<T> ToValueTask<T>(this Task<T> task)
		=> new ValueTask<T>(task);

	public static ValueTask ToValueTask(this Task task)
		=> new ValueTask(task);

	public static ValueTask<T> ToValueTask<T>(this T value)
		=> new ValueTask<T>(value);
}