using Dapper;

namespace AutoKkutuLib.Database.Sql.Query;
public class SqliteGetColumnTypeQuery : GetColumnTypeQueryBase
{
	internal SqliteGetColumnTypeQuery(DbConnectionBase connection, string tableName, string columnName) : base(connection, tableName, columnName) { }

	public override string Execute()
	{
		try
		{
			return Connection.ExecuteScalar<string>("SELECT type FROM pragma_table_info(@TableName) WHERE name = @ColumnName;", new
			{
				TableName,
				ColumnName
			});
		}
		catch (Exception ex)
		{
			LibLogger.Error<SqliteGetColumnTypeQuery>(ex, DatabaseConstants.ErrorGetColumnType, ColumnName, TableName);
		}
		return "";
	}
}
