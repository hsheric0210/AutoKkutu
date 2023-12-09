using AutoKkutuLib.Database.Sql;
using AutoKkutuLib.Database.Sql.Query;
using AutoKkutuLib.Postgres.Database.PostgreSql.Query;
using AutoKkutuLib.Sqlite.Properties;
using Dapper;
using Microsoft.Data.Sqlite;
using System.Text;

namespace AutoKkutuLib.Database.Sqlite;

public sealed class SqliteDbConnection : DbConnectionBase
{
	private QueryFactory query = null!;
	public override QueryFactory Query => query;
	public override string DbType => "SQLite";
	private const string regexpFileName = "regexp.dll";

	private SqliteDbConnection(SqliteConnection connection) : base(connection) { }

	public override string GetWordPriorityFuncName() => "WordPriority";

	public override string GetMissionWordPriorityFuncName() => "MissionWordPriority";

	public override string GetWordListColumnOptions()
	{
		var builder = new StringBuilder();
		builder.Append(DatabaseConstants.SequenceColumnName).Append(" INTEGER PRIMARY KEY AUTOINCREMENT, ");
		builder.Append(DatabaseConstants.WordColumnName).Append(" VARCHAR(256) UNIQUE NOT NULL, ");
		builder.Append(DatabaseConstants.WordIndexColumnName).Append(" CHAR(1) NOT NULL, ");
		builder.Append(DatabaseConstants.ReverseWordIndexColumnName).Append(" CHAR(1) NOT NULL, ");
		builder.Append(DatabaseConstants.KkutuWordIndexColumnName).Append(" VARCHAR(2) NOT NULL, ");
		builder.Append(DatabaseConstants.TypeColumnName).Append(" INT NOT NULL, ");
		builder.Append(DatabaseConstants.ThemeColumn1Name).Append(" BIGINT NOT NULL, ");
		builder.Append(DatabaseConstants.ThemeColumn2Name).Append(" BIGINT NOT NULL, ");
		builder.Append(DatabaseConstants.ThemeColumn3Name).Append(" BIGINT NOT NULL, ");
		builder.Append(DatabaseConstants.ThemeColumn4Name).Append(" BIGINT NOT NULL, ");
		builder.Append(DatabaseConstants.ChoseongColumnName).Append(" VARCHAR(256) NOT NULL, ");
		builder.Append(DatabaseConstants.MeaningColumnName).Append(" TEXT NOT NULL, ");
		builder.Append(DatabaseConstants.FlagsColumnName).Append(" INT NOT NULL");
		return builder.ToString();
	}

	public static SqliteDbConnection? Create(string connectionString)
	{
		try
		{
			if (!File.Exists(regexpFileName))
				File.WriteAllBytes(regexpFileName, Resources.sqlean_regexp_library);

			// Open the connection
			var nativeConnection = SqliteDatabaseHelper.OpenConnection(connectionString);
			var connection = new SqliteDbConnection(nativeConnection);
			connection.query = new SqliteQueryFactory(connection);

			nativeConnection.LoadExtension(regexpFileName); // 'regexp_like' 명령을 사용하기 위해서 필수적인 라이브러리
			nativeConnection.CreateFunction<int, int, int, int, int, int, int>(connection.GetWordPriorityFuncName(), WordPriorityFunc, true);
			nativeConnection.CreateFunction<string, int, string, int, int, int, int, int, int, int, int, int>(connection.GetMissionWordPriorityFuncName(), MissionWordPriorityFunc, true);

			// Speed optimization
			nativeConnection.Execute("PRAGMA synchronous = OFF;");
			nativeConnection.Execute("PRAGMA journal_mode = MEMORY;");

			// Check the database tables
			connection.CheckTable();

			LibLogger.Info<SqliteDbConnection>("Established SQLite connection.");

			return connection;
		}
		catch (Exception ex)
		{
			LibLogger.Error<SqliteDbConnection>(ex, DatabaseConstants.ErrorConnect);
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
