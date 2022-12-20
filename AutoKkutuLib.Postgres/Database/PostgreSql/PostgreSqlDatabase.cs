using Npgsql;
using Serilog;
using AutoKkutuLib.Database.Sql;

namespace AutoKkutuLib.Database.PostgreSql;

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
			Connection.TryExecute($"SET Application_Name TO 'AutoKkutu';");

			// Rearrange(int endWordFlag, int attackWordFlag, int endWordOrdinal, int attackWordOrdinal, int normalWordOrdinal)
			Connection.TryExecute($@"CREATE OR REPLACE FUNCTION {Connection.GetWordPriorityFuncName()}(flags INT, endWordFlag INT, attackWordFlag INT, endWordOrdinal INT, attackWordOrdinal INT, normalWordOrdinal INT)
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
			Connection.TryExecute($@"CREATE OR REPLACE FUNCTION {Connection.GetMissionWordPriorityFuncName()}(word VARCHAR, flags INT, missionword VARCHAR, endWordFlag INT, attackWordFlag INT, endMissionWordOrdinal INT, endWordOrdinal INT, attackMissionWordOrdinal INT, attackWordOrdinal INT, missionWordOrdinal INT, normalWordOrdinal INT)
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

	public override AbstractDatabaseConnection OpenSecondaryConnection()
	{
		var connection = new NpgsqlConnection(ConnectionString);
		connection.Open();
		var wrappedConnection = new PostgreSqlDatabaseConnection(connection);
		wrappedConnection.TryExecute($"SET Application_Name TO 'AutoKkutu';");
		return wrappedConnection;
	}
}
