namespace AutoKkutuLib.Database.Sql.Query;
public abstract class IsTableExistQueryBase : SqlQuery<bool>
{
	protected string TableName { get; }

	protected IsTableExistQueryBase(DbConnectionBase connection, string tableName) : base(connection) => TableName = tableName;
}
