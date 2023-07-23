namespace AutoKkutuLib.Database.Sql;
public abstract class SqlQuery<T> : QueryBase<T>
{
	protected SqlQuery(DbConnectionBase connection) : base(connection) { }
}
