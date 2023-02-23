﻿using Dapper;
using Serilog;

namespace AutoKkutuLib.Database.Sql.Query;
public class MySqlIsColumnExistsQuery : AbstractIsColumnExistsQuery
{
	private readonly string dbName;

	internal MySqlIsColumnExistsQuery(AbstractDatabaseConnection connection, string dbName, string tableName, string columnName) : base(connection, tableName, columnName) => this.dbName = dbName;

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
			Log.Error(ex, DatabaseConstants.ErrorIsColumnExists, ColumnName, TableName);
			return false;
		}
	}
}