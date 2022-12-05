using AutoKkutuLib.Database;
using AutoKkutuLib.Database.Extension;
using Dapper;
using Microsoft.Data.Sqlite;
using Serilog;
using System;

namespace AutoKkutuLib.Database.Sqlite;

public class SqliteDatabaseConnection : AbstractDatabaseConnection
{
	public SqliteDatabaseConnection(SqliteConnection connection) => Initialize(connection);

	public override void AddSequenceColumnToWordList() => RebuildWordList();

	public override void ChangeWordListColumnType(string tableName, string columnName, string newType) => RebuildWordList();

	public override void DropWordListColumn(string columnName) => RebuildWordList();

	public override string? GetColumnType(string tableName, string columnName)
	{
		try
		{
			return this.ExecuteScalar<string>("SELECT type FROM pragma_table_info(@TableName) WHERE name = @ColumnName;", new
			{
				TableName = tableName,
				ColumnName = columnName
			});
		}
		catch (Exception ex)
		{
			Log.Error(ex, DatabaseConstants.ErrorGetColumnType, columnName, tableName);
		}

		return null;
	}

	public override string GetWordPriorityFuncName() => "WordPriority";

	public override string GetMissionWordPriorityFuncName() => "MissionWordPriority";

	public override string GetWordListColumnOptions() => "seq INTEGER PRIMARY KEY AUTOINCREMENT, word VARCHAR(256) UNIQUE NOT NULL, word_index CHAR(1) NOT NULL, reverse_word_index CHAR(1) NOT NULL, kkutu_index VARCHAR(2) NOT NULL, flags SMALLINT NOT NULL";

	public override bool IsColumnExists(string tableName, string columnName)
	{
		try
		{
			return Connection.ExecuteScalar<int>("SELECT COUNT(name) FROM pragma_table_info(@TableName) WHERE name = @ColumnName;", new
			{
				TableName = tableName,
				ColumnName = columnName
			}) > 0;
		}
		catch (Exception ex)
		{
			Log.Warning(ex, DatabaseConstants.ErrorIsColumnExists, columnName, tableName);
		}

		return false;
	}

	public override bool IsTableExists(string tableName)
	{
		try
		{
			return Connection.ExecuteScalar<int>("SELECT COUNT(*) FROM sqlite_master WHERE name=@TableName;", new
			{
				TableName = tableName
			}) > 0;
		}
		catch (Exception ex)
		{
			Log.Error(ex, DatabaseConstants.ErrorIsTableExists, tableName);
		}
		return false;
	}

	public override void ExecuteVacuum() => Connection.Execute("VACUUM;");

	private void RebuildWordList()
	{
		Connection.Execute($"ALTER TABLE {DatabaseConstants.WordTableName} RENAME TO _{DatabaseConstants.WordTableName};");
		this.MakeTable(DatabaseConstants.WordTableName);
		Connection.Execute($"INSERT INTO {DatabaseConstants.WordTableName} (word, word_index, reverse_word_index, kkutu_index, flags) SELECT word, word_index, reverse_word_index, kkutu_index, flags FROM _{DatabaseConstants.WordTableName};");
		Connection.Execute($"DROP TABLE _{DatabaseConstants.WordTableName};");
	}
}
