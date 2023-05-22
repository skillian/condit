using System.Data;

namespace Condit.Sql;

struct MssqlAsyncCommand : IAsyncCommand
{
	readonly Microsoft.Data.SqlClient.SqlCommand command;

	public string CommandText
	{
		get => command.CommandText;
		set => command.CommandText = value;
	}

	public TimeSpan CommandTimeout
	{
		get => TimeSpan.FromSeconds(command.CommandTimeout);
		set => command.CommandTimeout = Convert.ToInt32(value.TotalSeconds);
	}

	IDataParameterCollection IAsyncCommand.Parameters => ((IDbCommand)command).Parameters;
	Microsoft.Data.SqlClient.SqlParameterCollection Parameters => command.Parameters;

	public MssqlAsyncCommand(Microsoft.Data.SqlClient.SqlCommand command)
	{
		this.command = command ?? throw new ArgumentNullException(nameof(command));
	}

	public async ValueTask<System.Data.IDataReader> ExecuteReaderAsync(CancellationToken cancellationToken)
		=> await command.ExecuteReaderAsync(cancellationToken);

	public async ValueTask<System.Numerics.BigInteger> ExecuteNonQueryAsync(CancellationToken cancellationToken)
		=> await command.ExecuteNonQueryAsync(cancellationToken);

	public ValueTask CancelAsync() => Task.Run(command.Cancel).ToValueTask();
	IDbDataParameter IAsyncCommand.CreateParameter() => ((IDbCommand)command).CreateParameter();
	public Microsoft.Data.SqlClient.SqlParameter CreateParameter() => command.CreateParameter();
	public ValueTask PrepareAsync(CancellationToken cancellationToken) => command.PrepareAsync(cancellationToken).ToValueTask();
	public ValueTask DisposeAsync() => command.DisposeAsync();
}