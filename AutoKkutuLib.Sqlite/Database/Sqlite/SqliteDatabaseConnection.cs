using AutoKkutuLib.Database.Sql;
using AutoKkutuLib.Database.Sql.Query;
using AutoKkutuLib.Postgres.Database.PostgreSql.Query;
using Dapper;
using Microsoft.Data.Sqlite;

namespace AutoKkutuLib.Database.Sqlite;

public sealed class SqliteDatabaseConnection : DbConnectionBase
{
	private QueryFactory query = null!;
	public override QueryFactory Query => query;
	public override string DbType => "SQLite";

	private SqliteDatabaseConnection(SqliteConnection connection) : base(connection) { }

	public override string GetWordPriorityFuncName() => "WordPriority";

	public override string GetMissionWordPriorityFuncName() => "MissionWordPriority";

	public override string GetWordListColumnOptions() => $"{DatabaseConstants.SequenceColumnName} INTEGER PRIMARY KEY AUTOINCREMENT, {DatabaseConstants.WordColumnName} VARCHAR(256) UNIQUE NOT NULL, {DatabaseConstants.WordIndexColumnName} CHAR(1) NOT NULL, {DatabaseConstants.ReverseWordIndexColumnName} CHAR(1) NOT NULL, {DatabaseConstants.KkutuWordIndexColumnName} VARCHAR(2) NOT NULL, {DatabaseConstants.FlagsColumnName} SMALLINT NOT NULL";

	public static SqliteDatabaseConnection? Create(string connectionString)
	{
		try
		{
			// Open the connection
			var nativeConnection = SqliteDatabaseHelper.OpenConnection(connectionString);
			var connection = new SqliteDatabaseConnection(nativeConnection);
			connection.query = new SqliteQueryFactory(connection);

			nativeConnection.CreateFunction<int, int, int, int, int, int, int>(connection.GetWordPriorityFuncName(), WordPriorityFunc, true);
			nativeConnection.CreateFunction<string, int, string, int, int, int, int, int, int, int, int, int>(connection.GetMissionWordPriorityFuncName(), MissionWordPriorityFunc, true);

			// Speed optimization
			nativeConnection.Execute("PRAGMA synchronous = OFF;");
			nativeConnection.Execute("PRAGMA journal_mode = MEMORY;");

			// Check the database tables
			connection.CheckTable();

			LibLogger.Info<SqliteDatabaseConnection>("Established SQLite connection.");

			return connection;
		}
		catch (Exception ex)
		{
			LibLogger.Error<SqliteDatabaseConnection>(ex, DatabaseConstants.ErrorConnect);
			DatabaseEvents.TriggerDatabaseError();
		}

		return null;

		static int MissionWordPriorityFunc(
			string word,
			int flags,
			string missionWord,
			int endWordFlag,
			int attackWordFlag,
			int endMissionWordOrdinal,
			int endWordOrdinal,
			int attackMissionWordOrdinal,
			int attackWordOrdinal,
			int missionWordOrdinal,
			int normalWordOrdinal)
		{
			var missionChar = char.ToUpperInvariant(missionWord[0]);
			var missionOccurrence = (from char c in word.ToUpperInvariant() where c == missionChar select c).Count();
			var hasMission = missionOccurrence > 0;

			// End-word
			if ((flags & endWordFlag) != 0)
				return (hasMission ? endMissionWordOrdinal : endWordOrdinal) * DatabaseConstants.MaxWordPriorityLength + missionOccurrence * 256;

			// Attack-word
			if ((flags & attackWordFlag) != 0)
				return (hasMission ? attackMissionWordOrdinal : attackWordOrdinal) * DatabaseConstants.MaxWordPriorityLength + missionOccurrence * 256;

			// Normal word
			return (hasMission ? missionWordOrdinal : normalWordOrdinal) * DatabaseConstants.MaxWordPriorityLength + missionOccurrence * 256;
		}

		static int WordPriorityFunc(
			int flags,
			int endWordFlag,
			int attackWordFlag,
			int endWordOrdinal,
			int attackWordOrdinal,
			int normalWordOrdinal)
		{
			// End-word
			if ((flags & endWordFlag) != 0)
				return endWordOrdinal * DatabaseConstants.MaxWordLength;

			// Attack-word
			if ((flags & attackWordFlag) != 0)
				return attackWordOrdinal * DatabaseConstants.MaxWordLength;

			// Normal word
			return normalWordOrdinal * DatabaseConstants.MaxWordLength;
		}
	}
}
