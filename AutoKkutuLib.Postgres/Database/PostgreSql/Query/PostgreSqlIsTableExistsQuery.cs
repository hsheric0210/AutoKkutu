using Dapper;

namespace AutoKkutuLib.Database.Sql.Query;
public class PostgreSqlIsTableExistsQuery : IsTableExistQueryBase
{
	internal PostgreSqlIsTableExistsQuery(DbConnectionBase connection, string tableName) : base(connection, tableName) { }

	public override bool Execute()
	{
		try
		{
			return Connection.ExecuteScalar<int>("SELECT COUNT(*) FROM information_schema.tables WHERE table_name=@TableName;", new
			{
				TableName
			}) > 0;
		}
		catch (Exception ex)
		{
			LibLogger.Error<PostgreSqlIsTableExistsQuery>(ex, DatabaseConstants.ErrorIsTableExists, TableName);
			return false;
		}
	}
}
