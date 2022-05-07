using log4net;
using System;
using System.Data.Common;

namespace AutoKkutu.Databases.Extension
{
	public static class DatabaseExtension
	{
		public static CommonDatabaseCommand CreateCommand(this CommonDatabaseConnection connection, string command, params CommonDatabaseParameter[] parameters)
		{
			if (connection == null)
				throw new ArgumentNullException(nameof(connection));

			CommonDatabaseCommand _command = connection.CreateCommand(command);
			_command.AddParameters(parameters);
			return _command;
		}

		public static int ExecuteNonQuery(this CommonDatabaseConnection connection, string command, params CommonDatabaseParameter[] parameters)
		{
			using CommonDatabaseCommand _command = connection.CreateCommand(command, parameters);
			return _command.ExecuteNonQuery();
		}

		public static object? ExecuteScalar(this CommonDatabaseConnection connection, string command, params CommonDatabaseParameter[] parameters)
		{
			using CommonDatabaseCommand _command = connection.CreateCommand(command, parameters);
			return _command.ExecuteScalar();
		}

		public static DbDataReader ExecuteReader(this CommonDatabaseConnection connection, string command, params CommonDatabaseParameter[] parameters)
		{
			CommonDatabaseCommand _command = connection.CreateCommand(command, parameters);
			DbDataReader reader = _command.ExecuteReader();
			return new WrappedDbDataReader(_command, reader);
		}

		public static int TryExecuteNonQuery(this CommonDatabaseConnection connection, string action, string command, params CommonDatabaseParameter[] parameters)
		{
			using CommonDatabaseCommand _command = connection.CreateCommand(command, parameters);
			return _command.TryExecuteNonQuery(action);
		}

		public static object? TryExecuteScalar(this CommonDatabaseConnection connection, string action, string command, params CommonDatabaseParameter[] parameters)
		{
			using CommonDatabaseCommand _command = connection.CreateCommand(command, parameters);
			return _command.TryExecuteScalar(action);
		}

		public static DbDataReader? TryExecuteReader(this CommonDatabaseConnection connection, string action, string command, params CommonDatabaseParameter[] parameters)
		{
			CommonDatabaseCommand _command = connection.CreateCommand(command, parameters);
			DbDataReader? reader = _command.TryExecuteReader(action);
			return reader != null ? new WrappedDbDataReader(_command, reader) : null;
		}

		public static int DeduplicateDatabase(this CommonDatabaseConnection connection)
		{
			if (connection == null)
				throw new ArgumentNullException(nameof(connection));

			return connection.ExecuteNonQuery(DatabaseConstants.DeduplicationQuery);
		}

		public static void CreateIndex(this CommonDatabaseConnection connection, string tableName, string columnName) => connection.ExecuteNonQuery($"CREATE INDEX IF NOT EXISTS {columnName} ON {tableName} ({columnName})");
	}
}
