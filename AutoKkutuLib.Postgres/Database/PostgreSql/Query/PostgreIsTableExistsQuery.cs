using Dapper;
using Serilog;

namespace AutoKkutuLib.Database.Sql.Query;
public class PostgreIsTableExistsQuery : AbstractIsTableExistsQuery
{
	internal PostgreIsTableExistsQuery(AbstractDatabaseConnection connection, string tableName) : base(connection, tableName) { }

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
			Log.Error(ex, DatabaseConstants.ErrorIsTableExists, TableName);
			return false;
		}
	}
}
