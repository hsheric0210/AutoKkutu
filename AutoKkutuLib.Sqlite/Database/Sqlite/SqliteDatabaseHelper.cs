using Serilog;
using Microsoft.Data.Sqlite;
using Dapper;
using System.ComponentModel.DataAnnotations.Schema;
using AutoKkutuLib.Database.Sql;
using System.Diagnostics;

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
				var connection = SqliteDatabaseConnection.Create(externalSQLiteFilePath);
				if (connection == null)
					Log.Error("Failed to open SQLite connection");

				var args = new SQLiteImportArgs { destination = targetDatabase, source = connection };
				var WordCount = LogImportProcess("Import words", () => ImportWordsFromExternalSQLite(args));
				var AttackWordCount = LogImportProcess("Import attack-nodes", () => ImportNode(args, DatabaseConstants.AttackNodeIndexTableName));
				var EndWordCount = LogImportProcess("Import end-nodes", () => ImportNode(args, DatabaseConstants.EndNodeIndexTableName));
				var ReverseAttackWordCount = LogImportProcess("Import reverse attack-nodes", () => ImportNode(args, DatabaseConstants.ReverseAttackNodeIndexTableName));
				var ReverseEndWordCount = LogImportProcess("Import reverse end-nodes", () => ImportNode(args, DatabaseConstants.ReverseEndNodeIndexTableName));
				var KkutuAttackWordCount = LogImportProcess("Import Kkutu attack-nodes", () => ImportNode(args, DatabaseConstants.KkutuAttackNodeIndexTableName));
				var KkutuEndWordCount = LogImportProcess("Import Kkutu end-nodes", () => ImportNode(args, DatabaseConstants.KkutuEndNodeIndexTableName));
				var KKTAttackWordCount = LogImportProcess("Import KungKungTta attack-nodes", () => ImportNode(args, DatabaseConstants.KKTAttackNodeIndexTableName));
				var KKTEndWordCount = LogImportProcess("Import KungKungTta end-nodes", () => ImportNode(args, DatabaseConstants.KKTEndNodeIndexTableName));

				new DatabaseImportEventArgs(DatabaseConstants.LoadFromLocalSQLite, $"{WordCount} 개의 단어 / {AttackWordCount} 개의 공격 노드 / {EndWordCount} 개의 한방 노드 / {ReverseAttackWordCount} 개의 앞말잇기 공격 노드 / {ReverseEndWordCount} 개의 앞말잇기 한방 노드 / {KkutuAttackWordCount} 개의 끄투 공격 노드 / {KkutuEndWordCount} 개의 끄투 한방 노드 / {KKTAttackWordCount} 개의 쿵쿵따 공격 노드 / {KKTEndWordCount} 개의 쿵쿵따 한방 노드").TriggerDatabaseImportDone();
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Failed to import external database.");
			}
		});
	}

	private static int LogImportProcess(string taskName, Func<int> task)
	{
		var sw = new Stopwatch();
		Log.Information("Beginning {task}", taskName);
		sw.Start();
		var affected = task();
		sw.Stop();
		Log.Information("Task {task} took {time}ms and {count} elements affected.", taskName, sw.ElapsedMilliseconds, affected);
		return affected;
	}

	public static SqliteConnection OpenConnection(string connectionString)
	{
		var connection = new SqliteConnection(connectionString);
		connection.Open();
		return connection;
	}

	private static int ImportNode(SQLiteImportArgs args, string tableName)
	{
		if (!args.destination.Query.IsTableExists(tableName).Execute())
		{
			Log.Warning("External SQLite Database doesn't contain node list table {tableName}.", tableName);
			return 0;
		}

		var counter = 0;

		// Inevitable dynamically-formatted SQL: The table name could't be parameterized
		foreach (var wordIndex in args.source.Query<string>($"SELECT {DatabaseConstants.WordIndexColumnName} FROM {tableName}"))
		{
			if (!args.destination.Query.AddNode(tableName).Execute(wordIndex))
				Log.Warning("{node} in {tableName} already exists in database.", wordIndex, tableName);
			counter++;
		}
		return counter;
	}

	private static void ImportSingleWord(this AbstractDatabaseConnection destination, string word, int flags)
	{
		if (!destination.Query.AddWord().Execute(word, (WordFlags)flags))
			Log.Warning("Word {word} already exists in database.", word);
	}

	private static void ImportSingleWordLegacy(this AbstractDatabaseConnection destination, string word, int isEndWordInt)
	{
		// Legacy support
		var isEndWord = Convert.ToBoolean(isEndWordInt);
		if (!destination.Query.AddWord().Execute(word, isEndWord ? WordFlags.EndWord : WordFlags.None))
			Log.Warning("(Legacy) Word {word} already exists in database.", word);
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
