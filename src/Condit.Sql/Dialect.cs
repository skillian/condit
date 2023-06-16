namespace Condit.Sql;

public interface IDialect
{
	(char Start, char End) QuoteChars { get;}
}

public abstract class QueryBuilder
{
	public virtual ValueTask AppendSelectAsync(System.Text.StringBuilder stringBuilder, IQuery query, CancellationToken cancellationToken = default)
		=> Append(stringBuilder, "SELECT");
	public virtual ValueTask AppendFromAsync(System.Text.StringBuilder stringBuilder, IQuery query, CancellationToken cancellationToken = default)
		=> Append(stringBuilder, "FROM");
	public virtual ValueTask AppendWhereAsync(System.Text.StringBuilder stringBuilder, IQuery query, CancellationToken cancellationToken = default)
		=> Append(stringBuilder, "WHERE");
	public virtual ValueTask AppendOrderByAsync(System.Text.StringBuilder stringBuilder, IQuery query, CancellationToken cancellationToken = default)
		=> Append(stringBuilder, "ORDER BY");
	ValueTask Append(System.Text.StringBuilder stringBuilder, string what)
	{
		stringBuilder.Append(what);
		return ValueTask.CompletedTask;
	}
}

public abstract class QueryBuilder<TDialect>
	: QueryBuilder
		where TDialect : IDialect
{
	protected TDialect Dialect { get; }

	protected QueryBuilder(TDialect dialect)
	{
		Dialect = dialect;
	}
}
