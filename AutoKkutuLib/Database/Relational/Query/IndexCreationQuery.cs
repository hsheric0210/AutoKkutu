using Dapper;

namespace AutoKkutuLib.Database.Relational.Query;
public class IndexCreationQuery : SqlQuery
{
	private readonly string tableName;
	private readonly string columnName;

	public IndexCreationQuery(AbstractDatabaseConnection connection, string tableName, string columnName) : base(connection)
	{
		this.tableName = tableName;
		this.columnName = columnName;
	}

	public override void Execute() => Connection.Execute($"CREATE INDEX IF NOT EXISTS {columnName} ON {tableName} ({columnName})");
}
