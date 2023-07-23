namespace AutoKkutuLib.Database;
public abstract class QueryBase<T>
{
	protected DbConnectionBase Connection { get; }

	protected QueryBase(DbConnectionBase connection) => Connection = connection;

	/// <summary>
	/// Execute this query
	/// </summary>
	public abstract T Execute();
}
