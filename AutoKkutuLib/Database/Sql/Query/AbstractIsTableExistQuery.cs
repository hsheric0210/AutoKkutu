namespace AutoKkutuLib.Database.Sql.Query;
public abstract class AbstractIsTableExistsQuery : SqlQuery<bool>
{
	protected string TableName { get; }

	protected AbstractIsTableExistsQuery(DbConnectionBase connection, string tableName) : base(connection) => TableName = tableName;
}
