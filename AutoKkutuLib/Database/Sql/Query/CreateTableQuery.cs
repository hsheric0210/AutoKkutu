using Dapper;

namespace AutoKkutuLib.Database.Sql.Query;
public class CreateTableQuery : SqlQuery<int>
{
	private readonly string tableName;
	private readonly string columns;

	public CreateTableQuery(DbConnectionBase connection, string tableName, string columns) : base(connection)
	{
		this.tableName = tableName;
		this.columns = columns;
	}

	public override int Execute()
	{
		LibLogger.Verbose<CreateTableQuery>("Creating the table {table} with columns '{columns}'.", tableName, columns);
		return Connection.Execute($"CREATE TABLE {tableName} ({columns})");
	}
}
