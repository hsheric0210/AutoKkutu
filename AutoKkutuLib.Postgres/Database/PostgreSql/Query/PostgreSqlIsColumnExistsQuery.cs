using Dapper;

namespace AutoKkutuLib.Database.Sql.Query;
public class PostgreSqlIsColumnExistsQuery : IsColumnExistQueryBase
{
	internal PostgreSqlIsColumnExistsQuery(DbConnectionBase connection, string tableName, string columnName) : base(connection, tableName, columnName) { }

	public override bool Execute()
	{
		try
		{
			return Connection.ExecuteScalar<int>("SELECT COUNT(*) FROM information_schema.columns WHERE table_name=@TableName AND column_name=@ColumnName;", new
			{
				TableName,
				ColumnName
			}) > 0;
		}
		catch (Exception ex)
		{
			LibLogger.Error<PostgreSqlIsColumnExistsQuery>(ex, DatabaseConstants.ErrorIsColumnExists, ColumnName, TableName);
			return false;
		}
	}
}
