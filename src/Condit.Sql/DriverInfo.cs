namespace Condit.Sql;

public interface IDriverInfo
{
	IParameters CreateParameters(IAsyncCommand asyncCommand);
}

public interface IParameters
{
	string DefineParameter(object value);
}