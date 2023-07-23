namespace AutoKkutuLib.Database.Sql.Query;
public abstract class AbstractIsColumnExistsQuery : SqlQuery<bool>
{
	protected string TableName { get; }
	protected string ColumnName { get; }

	protected AbstractIsColumnExistsQuery(DbConnectionBase connection, string tableName, string columnName) : base(connection)
	{
		TableName = tableName;
		ColumnName = columnName;
	}
}
