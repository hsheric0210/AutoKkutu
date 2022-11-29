using AutoKkutu.Databases.Extension;
using Npgsql;
using System;
using Serilog;

namespace AutoKkutu.Databases.PostgreSQL
{
	public partial class PostgreSqlDatabase : AbstractDatabase
	{
		private readonly string ConnectionString;

		public PostgreSqlDatabase(string connectionString) : base()
		{
			ConnectionString = connectionString;

			try
			{
				// Open the connection
				Log.Information("Opening database connection...");
				var connection = new NpgsqlConnection(connectionString);
				connection.Open();
				Initialize(new PostgreSqlDatabaseConnection(connection));
				Connection.TryExecuteNonQuery("set application name", $"SET Application_Name TO 'AutoKkutu v{MainWindow.VERSION}';");

				// Rearrange(int endWordFlag, int attackWordFlag, int endWordOrdinal, int attackWordOrdinal, int normalWordOrdinal)
				Connection.TryExecuteNonQuery("register RearrangeFunc", $@"CREATE OR REPLACE FUNCTION {Connection.GetRearrangeFuncName()}(flags INT, endWordFlag INT, attackWordFlag INT, endWordOrdinal INT, attackWordOrdinal INT, normalWordOrdinal INT)
RETURNS INTEGER AS $$
BEGIN
	IF ((flags & endWordFlag) != 0) THEN
		RETURN endWordOrdinal * {DatabaseConstants.MaxWordLength};
	END IF;
	IF ((flags & attackWordFlag) != 0) THEN
		RETURN attackWordOrdinal * {DatabaseConstants.MaxWordLength};
	END IF;
	RETURN normalWordOrdinal * {DatabaseConstants.MaxWordLength};
END;
$$ LANGUAGE plpgsql
");

				// Rearrange_Mission(string word, int flags, string missionword, int endWordFlag, int attackWordFlag, int endMissionWordOrdinal, int endWordOrdinal, int attackMissionWordOrdinal, int attackWordOrdinal, int missionWordOrdinal, int normalWordOrdinal)
				Connection.TryExecuteNonQuery("register RearrangeMissionFunc", $@"CREATE OR REPLACE FUNCTION {Connection.GetRearrangeMissionFuncName()}(word VARCHAR, flags INT, missionword VARCHAR, endWordFlag INT, attackWordFlag INT, endMissionWordOrdinal INT, endWordOrdinal INT, attackMissionWordOrdinal INT, attackWordOrdinal INT, missionWordOrdinal INT, normalWordOrdinal INT)
RETURNS INTEGER AS $$
DECLARE
	occurrence INTEGER;
BEGIN
	occurrence := ROUND((LENGTH(word) - LENGTH(REPLACE(LOWER(word), LOWER(missionWord), ''))) / LENGTH(missionWord));

	IF ((flags & endWordFlag) != 0) THEN
		IF (occurrence > 0) THEN
			RETURN endMissionWordOrdinal * {DatabaseConstants.MaxWordPriorityLength} + occurrence * 256;
		ELSE
			RETURN endWordOrdinal * {DatabaseConstants.MaxWordPriorityLength};
		END IF;
	END IF;
	IF ((flags & attackWordFlag) != 0) THEN
		IF (occurrence > 0) THEN
			RETURN attackMissionWordOrdinal * {DatabaseConstants.MaxWordPriorityLength} + occurrence * 256;
		ELSE
			RETURN attackWordOrdinal * {DatabaseConstants.MaxWordPriorityLength};
		END IF;
	END IF;

	IF occurrence > 0 THEN
		RETURN missionWordOrdinal * {DatabaseConstants.MaxWordPriorityLength} + occurrence * 256;
	ELSE
		RETURN normalWordOrdinal * {DatabaseConstants.MaxWordPriorityLength};
	END IF;
END;
$$ LANGUAGE plpgsql
");

				// Check the database tables
				Connection.CheckTable();

				Log.Information("Successfully established database connection.");
			}
			catch (Exception ex)
			{
				Log.Error(ex, DatabaseConstants.ErrorConnect);
				DatabaseEvents.TriggerDatabaseError();
			}
		}

		public override string GetDBType() => "PostgreSQL";

		public override void CheckConnectionType(AbstractDatabaseConnection connection)
		{
			if (connection is not null and not PostgreSqlDatabaseConnection)
				throw new NotSupportedException($"Connection is not {nameof(NpgsqlConnection)}");
		}

		public override AbstractDatabaseConnection OpenSecondaryConnection()
		{
			var connection = new NpgsqlConnection(ConnectionString);
			connection.Open();
			var wrappedConnection = new PostgreSqlDatabaseConnection(connection);
			wrappedConnection.TryExecuteNonQuery("set application name", $"SET Application_Name TO 'AutoKkutu v{MainWindow.VERSION}';");
			return wrappedConnection;
		}
	}
}
