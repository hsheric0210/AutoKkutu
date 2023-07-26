using Dapper;

namespace AutoKkutuLib.Database.Sql.Query;
public class AddColumnQuery : SqlQuery<int>
{
	private readonly string tableName;
	private readonly string columnName;
	private readonly string columnType;

	public AddColumnQuery(DbConnectionBase connection, string tableName, string columnName, string columnType) : base(connection)
	{
		this.tableName = tableName;
		this.columnName = columnName;
		this.columnType = columnType;
	}

	public override int Execute()
	{
		LibLogger.Verbose<AddColumnQuery>("Creating {type} column {name} on table {table}.", columnType, columnName, tableName);
		return Connection.Execute($"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnType}");
	}
}
