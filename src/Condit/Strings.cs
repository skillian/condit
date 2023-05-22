namespace Condit;

internal static partial class Functions
{
	public static string ToParameterCase(string typeCaseName)
		=> String.Concat(typeCaseName.Select((c, i) => i == 0 ? Char.ToLowerInvariant(c) : c));
}