using AutoKkutu.Databases.Extension;
using MySqlConnector;
using System;

namespace AutoKkutu.Databases.MySQL
{
	public partial class MySQLDatabase : DatabaseWithDefaultConnection
	{
		private readonly string ConnectionString;

		private readonly string DatabaseName = "";

		public MySQLDatabase(string connectionString)
		{
			ConnectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));

			try
			{
				int databaseNameIndex = connectionString.IndexOf("database", StringComparison.InvariantCultureIgnoreCase) + 9;
				int databaseNameIndexEnd = connectionString.IndexOf(';', databaseNameIndex) - databaseNameIndex;
				DatabaseName = connectionString.Substring(databaseNameIndex, databaseNameIndexEnd);
				Logger.InfoFormat("MySQL database name is '{0}'", DatabaseName);

				// Open the connection
				Logger.Info("Opening database connection...");
				var connection = new MySqlConnection(connectionString);
				connection.Open();
				RegisterDefaultConnection(new MySQLDatabaseConnection(connection, DatabaseName));

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
			RETURN endMissionWordOrdinal * {DatabaseConstants.MaxWordPlusMissionLength} + occurrence * 256;
		ELSE
			RETURN endWordOrdinal * {DatabaseConstants.MaxWordPlusMissionLength};
		END IF;
	END IF;
	IF (flags & attackWordFlag) != 0 THEN
		IF occurrence > 0 THEN
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

		public override void CheckConnectionType(CommonDatabaseConnection connection)
		{
			if (connection is not null and not MySQLDatabaseConnection)
				throw new NotSupportedException($"Connection is not {nameof(MySqlConnection)}");
		}

		public override string GetDBType() => "MySQL";

		public override CommonDatabaseConnection OpenSecondaryConnection()
		{
			var connection = new MySqlConnection(ConnectionString);
			connection.Open();
			return new MySQLDatabaseConnection(connection, DatabaseName);
		}
	}
}
