using AutoKkutu.Databases.Extension;
using Npgsql;
using System;

namespace AutoKkutu.Databases.PostgreSQL
{
	public partial class PostgreSQLDatabase : DatabaseWithDefaultConnection
	{
		private readonly string ConnectionString;

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

				// Rearrange(int endWordFlag, int attackWordFlag, int endWordOrdinal, int attackWordOrdinal, int normalWordOrdinal)
				DefaultConnection.TryExecuteNonQuery("register RearrangeFunc", $@"CREATE OR REPLACE FUNCTION {DefaultConnection.GetRearrangeFuncName()}(flags INT, endWordFlag INT, attackWordFlag INT, endWordOrdinal INT, attackWordOrdinal INT, normalWordOrdinal INT)
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
				DefaultConnection.TryExecuteNonQuery("register RearrangeMissionFunc", $@"CREATE OR REPLACE FUNCTION {DefaultConnection.GetRearrangeMissionFuncName()}(word VARCHAR, flags INT, missionword VARCHAR, endWordFlag INT, attackWordFlag INT, endMissionWordOrdinal INT, endWordOrdinal INT, attackMissionWordOrdinal INT, attackWordOrdinal INT, missionWordOrdinal INT, normalWordOrdinal INT)
RETURNS INTEGER AS $$
DECLARE
	occurrence INTEGER;
BEGIN
	occurrence := ROUND((LENGTH(word) - LENGTH(REPLACE(LOWER(word), LOWER(missionWord), ''))) / LENGTH(missionWord));

	IF ((flags & endWordFlag) != 0) THEN
		IF (occurrence > 0) THEN
			RETURN endMissionWordOrdinal * {DatabaseConstants.MaxWordPlusMissionLength} + occurrence * 256;
		ELSE
			RETURN endWordOrdinal * {DatabaseConstants.MaxWordPlusMissionLength};
		END IF;
	END IF;
	IF ((flags & attackWordFlag) != 0) THEN
		IF (occurrence > 0) THEN
			RETURN attackMissionWordOrdinal * {DatabaseConstants.MaxWordPlusMissionLength} + occurrence * 256;
		ELSE
			RETURN attackWordOrdinal * {DatabaseConstants.MaxWordPlusMissionLength};
		END IF;
	END IF;

	IF occurrence > 0 THEN
		RETURN missionWordOrdinal * {DatabaseConstants.MaxWordPlusMissionLength} + occurrence * 256;
	ELSE
		RETURN normalWordOrdinal * {DatabaseConstants.MaxWordPlusMissionLength};
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
			if (connection is not null and not PostgreSQLDatabaseConnection)
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
