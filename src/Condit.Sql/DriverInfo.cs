namespace Condit.Sql;

public interface IDriverInfo
{
	/// <summary>
	/// Creates a collection of parameters specific to the driver.
	/// </summary>
	IParameters CreateParameters(IAsyncCommand asyncCommand);
}

public interface IParameters
{
	/// <summary>
	/// Define a parameter for the given value and return its name as
	/// it should appear in the SQL statement.  Implementations that
	/// support named paramters might return the same parameter name
	/// for the same value.
	/// </summary>
	string DefineParameter(object? value);
}