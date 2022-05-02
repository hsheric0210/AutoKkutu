using Npgsql;
using System;

namespace AutoKkutu.Databases.PostgreSQL
{
	public partial class PostgreSQLDatabase : DatabaseWithDefaultConnection
	{
		private readonly string ConnectionString;

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000", Justification = "Implemented Dispose()")]
		public PostgreSQLDatabase(string connectionString) : base()
		{
			ConnectionString = connectionString;

			try
			{
				// Open the connection
				Logger.Info("Opening database connection...");
				var connection = new NpgsqlConnection(connectionString);
				connection.Open();
				RegisterDefaultConnection(new PostgreSQLDatabaseConnection(connection));
				DefaultConnection.TryExecuteNonQuery("set application name", $"SET Application_Name TO 'AutoKkutu v{MainWindow.VERSION}';");

				DefaultConnection.TryExecuteNonQuery("register checkMissionCharFunc", $@"CREATE OR REPLACE FUNCTION {DefaultConnection.GetCheckMissionCharFuncName()}(word VARCHAR, missionWord VARCHAR)
RETURNS INTEGER AS $$
DECLARE
	occurrence INTEGER;
BEGIN
	occurrence := ROUND((LENGTH(word) - LENGTH(REPLACE(LOWER(word), LOWER(missionWord), ''))) / LENGTH(missionWord));
	IF occurrence > 0 THEN
		RETURN occurrence + {DatabaseConstants.MissionCharIndexPriority};
	ELSE
		RETURN 0;
	END IF;
END;
$$ LANGUAGE plpgsql
");

				// Check the database tables
				DefaultConnection.CheckTable();

				Logger.Info("Successfully established database connection.");
			}
			catch (Exception ex)
			{
				Logger.Error(DatabaseConstants.ErrorConnect, ex);
				DatabaseEvents.TriggerDatabaseError();
			}
		}

		public override string GetDBType() => "PostgreSQL";

		public override void CheckConnectionType(CommonDatabaseConnection connection)
		{
			if (connection != null && connection.GetType() != typeof(NpgsqlConnection))
				throw new NotSupportedException($"Connection is not {nameof(NpgsqlConnection)}");
		}

		public override CommonDatabaseConnection OpenSecondaryConnection()
		{
			var connection = new NpgsqlConnection(ConnectionString);
			connection.Open();
			var wrappedConnection = new PostgreSQLDatabaseConnection(connection);
			wrappedConnection.TryExecuteNonQuery("set application name", $"SET Application_Name TO 'AutoKkutu v{MainWindow.VERSION}';");
			return wrappedConnection;
		}
	}
}
