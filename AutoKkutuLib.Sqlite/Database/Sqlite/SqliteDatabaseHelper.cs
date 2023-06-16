using Serilog;
using Microsoft.Data.Sqlite;
using Dapper;
using System.ComponentModel.DataAnnotations.Schema;
using AutoKkutuLib.Database.Sql;

namespace AutoKkutuLib.Database.Sqlite;

public static class SqliteDatabaseHelper
{
	static SqliteDatabaseHelper() => typeof(CompatibleWordModel).RegisterMapping();

	public static void LoadFromExternalSQLite(AbstractDatabaseConnection targetDatabase, string externalSQLiteFilePath)
	{
		if (!new FileInfo(externalSQLiteFilePath).Exists)
			return;

		new DatabaseImportEventArgs(DatabaseConstants.LoadFromLocalSQLite).TriggerDatabaseImportStart();

		Task.Run(() =>
		{
			try
			{
				var connection = SqliteDatabaseConnection.Create("Data Source=" + externalSQLiteFilePath);
				if (connection == null)
					Log.Error("Failed to open SQLite connection");

				var args = new SQLiteImportArgs { destination = targetDatabase, source = connection };
				var WordCount = ImportWordsFromExternalSQLite(args);
				var AttackWordCount = ImportNode(args, DatabaseConstants.AttackNodeIndexTableName);
				var EndWordCount = ImportNode(args, DatabaseConstants.EndNodeIndexTableName);
				var ReverseAttackWordCount = ImportNode(args, DatabaseConstants.ReverseAttackNodeIndexTableName);
				var ReverseEndWordCount = ImportNode(args, DatabaseConstants.ReverseEndNodeIndexTableName);
				var KkutuAttackWordCount = ImportNode(args, DatabaseConstants.KkutuAttackNodeIndexTableName);
				var KkutuEndWordCount = ImportNode(args, DatabaseConstants.KkutuEndNodeIndexTableName);
				var KKTAttackWordCount = ImportNode(args, DatabaseConstants.KKTAttackNodeIndexTableName);
				var KKTEndWordCount = ImportNode(args, DatabaseConstants.KKTEndNodeIndexTableName);

				Log.Information("DB Import Complete. ({0} Words / {1} Attack word nodes / {2} End-word nodes / {3} Reverse attack word nodes / {4} Reverse end-word nodes / {5} Kkutu attack word nodes / {6} Kkutu end-word nodes / {7} KungKuntTta attack word nodes / {8} KungKungTta end-word nodes)", WordCount, AttackWordCount, EndWordCount, ReverseAttackWordCount, ReverseEndWordCount, KkutuAttackWordCount, KkutuEndWordCount, KKTAttackWordCount, KKTEndWordCount);

				new DatabaseImportEventArgs(DatabaseConstants.LoadFromLocalSQLite, $"{WordCount} 개의 단어 / {AttackWordCount} 개의 공격 노드 / {EndWordCount} 개의 한방 노드 / {ReverseAttackWordCount} 개의 앞말잇기 공격 노드 / {ReverseEndWordCount} 개의 앞말잇기 한방 노드 / {KkutuAttackWordCount} 개의 끄투 공격 노드 / {KkutuEndWordCount} 개의 끄투 한방 노드 / {KKTAttackWordCount} 개의 쿵쿵따 공격 노드 / {KKTEndWordCount} 개의 쿵쿵따 한방 노드").TriggerDatabaseImportDone();
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Failed to import external database.");
			}
		});
	}

	public static SqliteConnection OpenConnection(string databaseFile)
	{
		var connection = new SqliteConnection($"Data Source={databaseFile}");
		connection.Open();
		return connection;
	}

	private static int ImportNode(SQLiteImportArgs args, string tableName)
	{
		if (!args.destination.Query.IsTableExists(tableName).Execute())
		{
			Log.Information("External SQLite Database doesn't contain node list table {tableName}.", tableName);
			return 0;
		}

		var counter = 0;

		// Inevitable dynamically-formatted SQL: The table name could't be parameterized
		foreach (var wordIndex in args.source.Query<string>($"SELECT {DatabaseConstants.WordIndexColumnName} FROM {tableName}"))
		{
			if (args.destination.Query.AddNode(tableName).Execute(wordIndex))
				Log.Information("Added {node} to {tableName}.", wordIndex, tableName);
			else
				Log.Warning("{node} in {tableName} already exists in database.", wordIndex, tableName);
			counter++;
		}
		return counter;
	}

	private static void ImportSingleWord(this AbstractDatabaseConnection destination, string word, int flags)
	{
		if (destination.Query.AddWord().Execute(word, (WordFlags)flags))
			Log.Information("Imported word {word} with flags: {flags}", word, flags);
		else
			Log.Warning("Word {word} already exists in database.", word);
	}

	private static void ImportSingleWordLegacy(this AbstractDatabaseConnection destination, string word, int isEndWordInt)
	{
		// Legacy support
		var isEndWord = Convert.ToBoolean(isEndWordInt);
		if (destination.Query.AddWord().Execute(word, isEndWord ? WordFlags.EndWord : WordFlags.None))
			Log.Information("Imported word {word} {flags:l}", word, isEndWord ? "(EndWord)" : "");
		else
			Log.Warning("Word {word} already exists in database.", word);
	}

	private static int ImportWordsFromExternalSQLite(SQLiteImportArgs args)
	{
		if (!args.source.Query.IsTableExists(DatabaseConstants.WordTableName).Execute())
		{
			Log.Information($"External SQLite Database doesn't contain word list table {DatabaseConstants.WordTableName}");
			return 0;
		}

		var counter = 0;
		var hasIsEndwordColumn = args.source.Query.IsColumnExists(DatabaseConstants.WordTableName, DatabaseConstants.IsEndwordColumnName).Execute();
		var columns = hasIsEndwordColumn
			? DatabaseConstants.WordColumnName + ", " + DatabaseConstants.IsEndwordColumnName
			: DatabaseConstants.WordColumnName + ", " + DatabaseConstants.FlagsColumnName;

		// Inevitable dynamically-formatted SQL: The column name could't be parameterized
		foreach (CompatibleWordModel word in args.source.Query<CompatibleWordModel>($"SELECT {columns} FROM {DatabaseConstants.WordTableName}"))
		{
			if (hasIsEndwordColumn)
				args.destination.ImportSingleWordLegacy(word.Word, word.Flags);
			else
				args.destination.ImportSingleWord(word.Word, word.Flags);

			counter++;
		}

		return counter;
	}

	private sealed class CompatibleWordModel
	{
		[Column(DatabaseConstants.WordColumnName)]
		public string Word
		{
			get; set;
		} = "";

		[Column(DatabaseConstants.FlagsColumnName)]
		public int Flags
		{
			get; set;
		}

		[Column(DatabaseConstants.IsEndwordColumnName)]
		public int IsEndWord
		{
			get; set;
		}
	}

	private struct SQLiteImportArgs
	{
		public AbstractDatabaseConnection source;
		public AbstractDatabaseConnection destination;
	}
}
