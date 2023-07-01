namespace Condit.Sql;

public interface IAsyncConnection
{
	IDatabase Database { get; }
	IDriverInfo DriverInfo { get; }
	IAsyncCommand CreateAsyncCommand();
}

public interface IAsyncCommand
{
	string CommandText { get;set; }
	TimeSpan CommandTimeout { get; set;}
	IAsyncConnection AsyncConnection { get; }
	System.Data.IDataParameterCollection Parameters { get; }
	ValueTask<System.Data.IDataReader> ExecuteReaderAsync(CancellationToken cancellationToken = default);
	ValueTask<System.Numerics.BigInteger> ExecuteNonQueryAsync(CancellationToken cancellationToken = default);
	ValueTask CancelAsync();
	System.Data.IDbDataParameter CreateParameter();
	ValueTask PrepareAsync(CancellationToken cancellationToken = default);

}

