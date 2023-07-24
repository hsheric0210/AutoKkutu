namespace AutoKkutuLib.Database.Sql.Query;
public abstract class ChangeWordListColumnTypeQueryBase : SqlQuery<bool>
{
	protected string TableName { get; }
	protected string ColumnName { get; }
	protected string NewType { get; }

	protected ChangeWordListColumnTypeQueryBase(DbConnectionBase connection, string tableName, string columnName, string newType) : base(connection)
	{
		TableName = tableName;
		ColumnName = columnName;
		NewType = newType;
	}
}
