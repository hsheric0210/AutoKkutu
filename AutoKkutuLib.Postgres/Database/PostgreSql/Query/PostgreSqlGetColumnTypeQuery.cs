using Dapper;

namespace AutoKkutuLib.Database.Sql.Query;
public class PostgreSqlGetColumnTypeQuery : GetColumnTypeQueryBase
{
	internal PostgreSqlGetColumnTypeQuery(DbConnectionBase connection, string tableName, string columnName) : base(connection, tableName, columnName) { }

	public override string Execute()
	{
		try
		{
			return Connection.ExecuteScalar<string>("SELECT data_type FROM information_schema.columns WHERE table_name=@TableName AND column_name=@ColumnName;", new
			{
				TableName,
				ColumnName
			});
		}
		catch (Exception ex)
		{
			LibLogger.Error<PostgreSqlGetColumnTypeQuery>(ex, DatabaseConstants.ErrorGetColumnType, ColumnName, TableName);
		}
		return "";
	}
}
