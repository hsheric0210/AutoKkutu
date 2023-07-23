using Dapper;

namespace AutoKkutuLib.Database.Sql.Query;
public class IndexCreationQuery : SqlQuery<int>
{
	private readonly string tableName;
	private readonly string columnName;

	public IndexCreationQuery(DbConnectionBase connection, string tableName, string columnName) : base(connection)
	{
		this.tableName = tableName;
		this.columnName = columnName;
	}

	public override int Execute()
	{
		LibLogger.Verbose<IndexCreationQuery>("Creating the index of table {0} column {1}.", tableName, columnName);
		return Connection.Execute($"CREATE INDEX IF NOT EXISTS {columnName} ON {tableName} ({columnName});");
	}
}
