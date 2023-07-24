using AutoKkutuLib.Database;
using AutoKkutuLib.Database.Sql;
using Dapper;

namespace AutoKkutuLib.Sqlite.Database.Sqlite;
public static class SqliteExtension
{
	public static void RebuildWordList(this DbConnectionBase connection)
	{
		var transaction = connection.BeginTransaction();
		var columns = $"{DatabaseConstants.WordColumnName}, {DatabaseConstants.WordIndexColumnName}, {DatabaseConstants.ReverseWordIndexColumnName}, {DatabaseConstants.KkutuWordIndexColumnName}, {DatabaseConstants.TypeColumnName}, {DatabaseConstants.ThemeColumn1Name}, {DatabaseConstants.ThemeColumn2Name}, {DatabaseConstants.ThemeColumn3Name}, {DatabaseConstants.ThemeColumn4Name}, {DatabaseConstants.ChoseongColumnName}, {DatabaseConstants.MeaningColumnName}, {DatabaseConstants.FlagsColumnName}";
		connection.Execute($"ALTER TABLE {DatabaseConstants.WordTableName} RENAME TO _{DatabaseConstants.WordTableName};");
		connection.MakeTable(DatabaseConstants.WordTableName);
		connection.Execute($"INSERT INTO {DatabaseConstants.WordTableName} ({columns}) SELECT {columns} FROM _{DatabaseConstants.WordTableName};");
		connection.Execute($"DROP TABLE _{DatabaseConstants.WordTableName};");
		transaction.Commit();
	}
}
