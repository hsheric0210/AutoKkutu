using Dapper;

namespace AutoKkutuLib.Database.Sql.Query;
public class MySqlIsColumnExistsQuery : IsColumnExistQueryBase
{
	private readonly string dbName;

	internal MySqlIsColumnExistsQuery(DbConnectionBase connection, string dbName, string tableName, string columnName) : base(connection, tableName, columnName) => this.dbName = dbName;

	public override bool Execute()
	{
		try
		{
			return Connection.ExecuteScalar<int>("SELECT COUNT(*) FROM Information_schema.columns WHERE table_schema=@DbName AND table_name=@TableName AND column_name=@ColumnName;", new
			{
				DbName = dbName,
				TableName,
				ColumnName
			}) > 0;
		}
		catch (Exception ex)
		{
			LibLogger.Error<MySqlIsColumnExistsQuery>(ex, DatabaseConstants.ErrorIsColumnExists, ColumnName, TableName);
			return false;
		}
	}
}
