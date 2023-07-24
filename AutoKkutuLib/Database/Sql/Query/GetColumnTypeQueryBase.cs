namespace AutoKkutuLib.Database.Sql.Query;
public abstract class GetColumnTypeQueryBase : SqlQuery<string>
{
	protected string TableName { get; }
	protected string ColumnName { get; }

	protected GetColumnTypeQueryBase(DbConnectionBase connection, string tableName, string columnName) : base(connection)
	{
		TableName = tableName;
		ColumnName = columnName;
	}
}
