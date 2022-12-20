using Npgsql;

namespace AutoKkutuLib.Database.PostgreSql;

public partial class PostgreSqlDatabaseConnection : AbstractDatabaseConnection
{
	public PostgreSqlDatabaseConnection(NpgsqlConnection connection) => Initialize(connection, new PostgreQueryFactory());

	public override string GetWordPriorityFuncName() => "__AutoKkutu_Rearrange";

	public override string GetMissionWordPriorityFuncName() => "__AutoKkutu_RearrangeMission";

	public override string GetWordListColumnOptions() => "seq SERIAL PRIMARY KEY, word CHAR VARYING(256) UNIQUE NOT NULL, word_index CHAR(1) NOT NULL, reverse_word_index CHAR(1) NOT NULL, kkutu_index VARCHAR(2) NOT NULL, flags SMALLINT NOT NULL";
}
