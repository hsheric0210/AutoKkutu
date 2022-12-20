namespace AutoKkutuLib.Database;
public abstract class AbstractQuery<T>
{
	protected AbstractDatabaseConnection Connection { get; }

	protected AbstractQuery(AbstractDatabaseConnection connection) => Connection = connection;

	/// <summary>
	/// Execute this query
	/// </summary>
	public abstract T Execute();
}
