namespace AutoKkutuLib.Database.Sql.Query;
public abstract class IsColumnExistQueryBase : SqlQuery<bool>
{
	protected string TableName { get; }
	protected string ColumnName { get; }

	protected IsColumnExistQueryBase(DbConnectionBase connection, string tableName, string columnName) : base(connection)
	{
		TableName = tableName;
		ColumnName = columnName;
	}
}
