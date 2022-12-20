using Dapper;

namespace AutoKkutuLib.Database.Sql.Query;
public class IndexCreationQuery : SqlQuery<int>
{
	private readonly string tableName;
	private readonly string columnName;

	internal IndexCreationQuery(AbstractDatabaseConnection connection, string tableName, string columnName) : base(connection)
	{
		this.tableName = tableName;
		this.columnName = columnName;
	}

	public override int Execute() => Connection.Execute($"CREATE INDEX IF NOT EXISTS {columnName} ON {tableName} ({columnName})");
}
