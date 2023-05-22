namespace Condit.Sql;

public interface IDialect
{
	DialectFlags DialectFlags { get; }
	(char Start, char End) QuoteChars { get;}
}

public struct DialectFlags
{
	const string TopDescription = "Uses \"TOP (n)\" to constrain the number of results in SELECT statements, or affected rows in DELETE and UPDATE statements.";
	[System.ComponentModel.DataAnnotations.Display(
		Name = nameof(Top), Description = TopDescription
	)]
	public static readonly DialectFlag Top = new DialectFlag(1, nameof(Top), TopDescription);

	readonly System.Numerics.BigInteger value;
	public DialectFlags(IEnumerable<DialectFlag> dialectFlags)
	{
		foreach (var dialectFlag in dialectFlags)
			value |= System.Numerics.BigInteger.Pow(
				System.Numerics.BigInteger.One,
				checked((int)dialectFlag.BitPosition)
			);
	}
	private DialectFlags(System.Numerics.BigInteger value)
	{
		this.value = value;
	}

	public static implicit operator DialectFlags(DialectFlag dialectFlag)
		=> new DialectFlags(new [] { dialectFlag });

	public static DialectFlags operator|(DialectFlags dialectFlags, DialectFlag dialectFlag)
		=> new DialectFlags(dialectFlags.value | System.Numerics.BigInteger.Pow(
			System.Numerics.BigInteger.One,
			checked((int)dialectFlag.BitPosition)
		));
}

public struct DialectFlag
{
	public readonly ulong BitPosition;
	public readonly string Name;
	public readonly string Description;

	internal DialectFlag(ulong bitPosition, string name, string description)
	{
		BitPosition = bitPosition;
		Name = name;
		Description = description;
	}
}