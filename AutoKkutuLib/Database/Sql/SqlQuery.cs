namespace AutoKkutuLib.Database.Sql;
public abstract class SqlQuery<T> : AbstractQuery<T>
{
	protected SqlQuery(AbstractDatabaseConnection connection) : base(connection) { }
}
