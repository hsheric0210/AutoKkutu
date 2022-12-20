using Dapper;
using Serilog;

namespace AutoKkutuLib.Database.Relational.Query;
public abstract class SqlQuery<T> : AbstractQuery<T>
{
	protected SqlQuery(AbstractDatabaseConnection connection) : base(connection)
	{
	}

	protected int TryExecute(string query, object? parameters = null)
	{
		try
		{
			return Connection.Execute(query, parameters);
		}
		catch (Exception ex)
		{
			Log.Error(ex, "SQL execution error of query {query}.", query);
		}
		return 0;
	}

	protected V? TryExecuteScalar<V>(string query, object? parameters = null)
	{
		try
		{
			return Connection.ExecuteScalar<V>(query, parameters);
		}
		catch (Exception ex)
		{
			Log.Error(ex, "SQL scalar-execution error of query {query}.", query);
		}
		return default;
	}
}
