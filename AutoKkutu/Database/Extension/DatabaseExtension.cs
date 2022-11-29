using Dapper;
using Serilog;
using System;
using System.Data;

namespace AutoKkutu.Database.Extension
{
	public static class DatabaseExtension
	{
		public static int TryExecute(this IDbConnection connection, string sql, object? param = null)
		{
			if (connection is null)
				throw new ArgumentNullException(nameof(connection));

			try
			{
				return connection.Execute(sql, param);
			}
			catch (Exception ex)
			{
				Log.Error(ex, "SQL execute error.");
			}
			return 0;
		}

		public static T? TryExecuteScalar<T>(this IDbConnection connection, string sql, object? param = null)
		{
			if (connection is null)
				throw new ArgumentNullException(nameof(connection));

			try
			{
				return connection.ExecuteScalar<T>(sql, param);
			}
			catch (Exception ex)
			{
				Log.Error(ex, "SQL execute-scalar error.");
			}
			return default;
		}

		public static int DeduplicateDatabase(this IDbConnection connection)
		{
			if (connection is null)
				throw new ArgumentNullException(nameof(connection));
			return connection.Execute(DatabaseConstants.DeduplicationQuery);
		}

		public static void CreateIndex(this IDbConnection connection, string tableName, string columnName)
		{
			if (connection is null)
				throw new ArgumentNullException(nameof(connection));

			connection.Execute($"CREATE INDEX IF NOT EXISTS {columnName} ON {tableName} ({columnName})");
		}
	}
}
