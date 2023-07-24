using Dapper;

namespace AutoKkutuLib.Database.Sql.Query;
public class SqliteIsColumnExistsQuery : IsColumnExistQueryBase
{
	internal SqliteIsColumnExistsQuery(DbConnectionBase connection, string tableName, string columnName) : base(connection, tableName, columnName) { }

	public override bool Execute()
	{
		try
		{
			return Connection.ExecuteScalar<int>("SELECT COUNT(name) FROM pragma_table_info(@TableName) WHERE name = @ColumnName;", new
			{
				TableName,
				ColumnName
			}) > 0;
		}
		catch (Exception ex)
		{
			LibLogger.Error<SqliteIsColumnExistsQuery>(ex, DatabaseConstants.ErrorIsColumnExists, ColumnName, TableName);
			return false;
		}
	}
}
