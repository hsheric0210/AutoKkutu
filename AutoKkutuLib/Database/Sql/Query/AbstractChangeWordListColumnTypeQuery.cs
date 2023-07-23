namespace AutoKkutuLib.Database.Sql.Query;
public abstract class AbstractChangeWordListColumnTypeQuery : SqlQuery<bool>
{
	protected string TableName { get; }
	protected string ColumnName { get; }
	protected string NewType { get; }

	protected AbstractChangeWordListColumnTypeQuery(DbConnectionBase connection, string tableName, string columnName, string newType) : base(connection)
	{
		TableName = tableName;
		ColumnName = columnName;
		NewType = newType;
	}
}
