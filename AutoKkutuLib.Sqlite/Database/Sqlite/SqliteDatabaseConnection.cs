using AutoKkutuLib.Postgres.Database.PostgreSql.Query;
using Microsoft.Data.Sqlite;

namespace AutoKkutuLib.Database.Sqlite;

public class SqliteDatabaseConnection : AbstractDatabaseConnection
{
	public SqliteDatabaseConnection(SqliteConnection connection) => Initialize(connection, new SqliteQueryFactory(this));

	public override string GetWordPriorityFuncName() => "WordPriority";

	public override string GetMissionWordPriorityFuncName() => "MissionWordPriority";

	public override string GetWordListColumnOptions() => "seq INTEGER PRIMARY KEY AUTOINCREMENT, word VARCHAR(256) UNIQUE NOT NULL, word_index CHAR(1) NOT NULL, reverse_word_index CHAR(1) NOT NULL, kkutu_index VARCHAR(2) NOT NULL, flags SMALLINT NOT NULL";
}
