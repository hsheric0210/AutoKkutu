using AutoKkutuLib.Database;
using AutoKkutuLib.Database.Sql;
using Dapper;

namespace AutoKkutuLib.Sqlite.Database.Sqlite;
public static class SqliteExtension
{
	public static void RebuildWordList(this AbstractDatabaseConnection connection)
	{
		connection.Execute($"ALTER TABLE {DatabaseConstants.WordTableName} RENAME TO _{DatabaseConstants.WordTableName};");
		connection.MakeTable(DatabaseConstants.WordTableName);
		connection.Execute($"INSERT INTO {DatabaseConstants.WordTableName} (word, word_index, reverse_word_index, kkutu_index, flags) SELECT word, word_index, reverse_word_index, kkutu_index, flags FROM _{DatabaseConstants.WordTableName};");
		connection.Execute($"DROP TABLE _{DatabaseConstants.WordTableName};");
	}
}
