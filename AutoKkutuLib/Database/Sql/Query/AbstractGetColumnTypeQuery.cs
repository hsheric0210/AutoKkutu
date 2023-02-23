namespace AutoKkutuLib.Database.Sql.Query;
public abstract class AbstractGetColumnTypeQuery : SqlQuery<string>
{
	protected string TableName { get; }
	protected string ColumnName { get; }

	protected AbstractGetColumnTypeQuery(AbstractDatabaseConnection connection, string tableName, string columnName) : base(connection)
	{
		TableName = tableName;
		ColumnName = columnName;
	}
}
