using MySqlConnector;
using AutoKkutuLib.MySql.Database.MySql.Query;

namespace AutoKkutuLib.Database.MySql;

public class MySqlDatabaseConnection : AbstractDatabaseConnection
{
	public MySqlDatabaseConnection(MySqlConnection connection, string dbName) => Initialize(connection, new MySqlQueryFactory(this, dbName));

	public override string GetWordPriorityFuncName() => "__AutoKkutu_Rearrange";

	public override string GetMissionWordPriorityFuncName() => "__AutoKkutu_RearrangeMission";

	public override string GetWordListColumnOptions() => "seq INT NOT NULL AUTO_INCREMENT PRIMARY KEY, word VARCHAR(256) UNIQUE NOT NULL, word_index CHAR(1) NOT NULL, reverse_word_index CHAR(1) NOT NULL, kkutu_index VARCHAR(2) NOT NULL, flags SMALLINT NOT NULL";
}
