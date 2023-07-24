using Dapper;

namespace AutoKkutuLib.Database.Sql.Query;
public class SqliteIsTableExistsQuery : IsTableExistQueryBase
{
	internal SqliteIsTableExistsQuery(DbConnectionBase connection, string tableName) : base(connection, tableName) { }

	public override bool Execute()
	{
		try
		{
			return Connection.ExecuteScalar<int>("SELECT COUNT(*) FROM sqlite_master WHERE name=@TableName;", new
			{
				TableName
			}) > 0;
		}
		catch (Exception ex)
		{
			LibLogger.Error<SqliteIsTableExistsQuery>(ex, DatabaseConstants.ErrorIsTableExists, TableName);
			return false;
		}
	}
}
