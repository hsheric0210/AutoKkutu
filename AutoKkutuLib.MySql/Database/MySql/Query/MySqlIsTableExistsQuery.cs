﻿using Dapper;
using Serilog;

namespace AutoKkutuLib.Database.Sql.Query;
public class MySqlIsTableExistsQuery : AbstractIsTableExistsQuery
{
	private readonly string dbName;
	internal MySqlIsTableExistsQuery(AbstractDatabaseConnection connection, string dbName, string tableName) : base(connection, tableName) => this.dbName = dbName;

	public override bool Execute()
	{
		try
		{
			return Connection.ExecuteScalar<int>("SELECT COUNT(*) FROM information_schema.tables WHERE table_schema=@DbName AND table_name=@TableName;", new
			{
				DbName = dbName,
				TableName
			}) > 0;
		}
		catch (Exception ex)
		{
			Log.Information(ex, DatabaseConstants.ErrorIsTableExists, TableName);
			return false;
		}
	}
}
