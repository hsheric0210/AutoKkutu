using AutoKkutu.Databases.Extension;
using MySqlConnector;
using Serilog;
using System;
using System.Globalization;

namespace AutoKkutu.Databases.MySQL
{
	public partial class MySqlDatabase : DatabaseWithDefaultConnection
	{
		private readonly string ConnectionString;

		private readonly string DatabaseName = "";

		public MySqlDatabase(string connectionString)
		{
			ConnectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));

			try
			{
				int databaseNameIndex = connectionString.IndexOf("database", StringComparison.InvariantCultureIgnoreCase) + 9;
				int databaseNameIndexEnd = connectionString.IndexOf(';', databaseNameIndex) - databaseNameIndex;
				DatabaseName = connectionString.Substring(databaseNameIndex, databaseNameIndexEnd);
				Log.Information("MySQL database name is {databaseName}.", DatabaseName);

				// Open the connection
				Log.Information("Opening database connection...");
				var connection = new MySqlConnection(connectionString);
				connection.Open();
				RegisterDefaultConnection(new MySqlDatabaseConnection(connection, DatabaseName));

				DefaultConnection.TryExecuteNonQuery("drop existing RearrangeFunc", $"DROP FUNCTION IF EXISTS {DefaultConnection.GetRearrangeFuncName()};");
				DefaultConnection.TryExecuteNonQuery("register RearrangeFunc", $@"CREATE FUNCTION {DefaultConnection.GetRearrangeFuncName()}(flags INT, endWordFlag INT, attackWordFlag INT, endWordOrdinal INT, attackWordOrdinal INT, normalWordOrdinal INT) RETURNS INT
DETERMINISTIC
NO SQL
BEGIN
	IF (flags & endWordFlag) != 0 THEN
		RETURN endWordOrdinal * {DatabaseConstants.MaxWordLength};
	END IF;
	IF (flags & attackWordFlag) != 0 THEN
		RETURN attackWordOrdinal * {DatabaseConstants.MaxWordLength};
	END IF;
	RETURN normalWordOrdinal * {DatabaseConstants.MaxWordLength};
END;
");

				DefaultConnection.TryExecuteNonQuery("drop existing RearrangeMissionFunc", $"DROP FUNCTION IF EXISTS {DefaultConnection.GetRearrangeMissionFuncName()};");
				DefaultConnection.TryExecuteNonQuery("register RearrangeMissionFunc", $@"CREATE FUNCTION {DefaultConnection.GetRearrangeMissionFuncName()}(word VARCHAR(256), flags INT, missionword VARCHAR(2), endWordFlag INT, attackWordFlag INT, endMissionWordOrdinal INT, endWordOrdinal INT, attackMissionWordOrdinal INT, attackWordOrdinal INT, missionWordOrdinal INT, normalWordOrdinal INT) RETURNS INT
DETERMINISTIC
NO SQL
BEGIN
	DECLARE occurrence INT;

	SET occurrence = ROUND((LENGTH(word) - LENGTH(REPLACE(LOWER(word), LOWER(missionWord), ''))) / LENGTH(missionWord));
	IF (flags & endWordFlag) != 0 THEN
		IF occurrence > 0 THEN
			RETURN endMissionWordOrdinal * {DatabaseConstants.MaxWordPriorityLength} + occurrence * 256;
		ELSE
			RETURN endWordOrdinal * {DatabaseConstants.MaxWordPriorityLength};
		END IF;
	END IF;
	IF (flags & attackWordFlag) != 0 THEN
		IF occurrence > 0 THEN
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
");

				// Check the database tables
				DefaultConnection.CheckTable();

				Log.Information("Successfully established database connection.");
			}
			catch (Exception ex)
			{
				Log.Error(ex, DatabaseConstants.ErrorConnect);
				DatabaseEvents.TriggerDatabaseError();
			}
		}

		public override void CheckConnectionType(AbstractDatabaseConnection connection)
		{
			if (connection is not null and not MySqlDatabaseConnection)
				throw new NotSupportedException($"Connection is not {nameof(MySqlConnection)}");
		}

		public override string GetDBType() => "MySQL";

		public override AbstractDatabaseConnection OpenSecondaryConnection()
		{
			var connection = new MySqlConnection(ConnectionString);
			connection.Open();
			return new MySqlDatabaseConnection(connection, DatabaseName);
		}
	}
}
