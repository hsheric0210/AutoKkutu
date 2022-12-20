using Dapper;
using Serilog;

namespace AutoKkutuLib.Database.Sql.Query;
public class SqliteIsTableExistsQuery : AbstractIsTableExistsQuery
{
	internal SqliteIsTableExistsQuery(AbstractDatabaseConnection connection, string tableName) : base(connection, tableName) { }

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
			Log.Error(ex, DatabaseConstants.ErrorIsTableExists, TableName);
			return false;
		}
	}
}
