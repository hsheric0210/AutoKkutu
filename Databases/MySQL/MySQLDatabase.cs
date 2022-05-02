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
			if (connectionString == null)
				throw new ArgumentNullException(nameof(connectionString));

			ConnectionString = connectionString;

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

				DefaultConnection.TryExecuteNonQuery("drop existing checkMissionCharFunc", $"DROP FUNCTION IF EXISTS {DefaultConnection.GetCheckMissionCharFuncName()};");
				DefaultConnection.TryExecuteNonQuery("register checkMissionCharFunc", $@"CREATE FUNCTION {DefaultConnection.GetCheckMissionCharFuncName()}(word VARCHAR(256), missionWord VARCHAR(2)) RETURNS INT
DETERMINISTIC
NO SQL
BEGIN
	DECLARE occurrence INT;

	SET occurrence = ROUND((LENGTH(word) - LENGTH(REPLACE(LOWER(word), LOWER(missionWord), ''))) / LENGTH(missionWord));
	IF occurrence > 0 THEN
		RETURN occurrence + {DatabaseConstants.MissionCharIndexPriority};
	ELSE
		RETURN 0;
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
			if (connection != null && connection.GetType() != typeof(MySqlConnection))
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
