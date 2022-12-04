using AutoKkutu.Constants;
using AutoKkutu.Database.Extension;
using Serilog;
using Microsoft.Data.Sqlite;
using System;
using System.IO;
using System.Threading.Tasks;
using Dapper;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoKkutu.Database.SQLite
{
	public static class SqliteDatabaseHelper
	{
		static SqliteDatabaseHelper()
		{
			typeof(CompatibleWordModel).RegisterMapping();
		}

		public static void LoadFromExternalSQLite(AbstractDatabaseConnection targetDatabase, string externalSQLiteFilePath)
		{
			if (!new FileInfo(externalSQLiteFilePath).Exists)
				return;

			DatabaseEvents.TriggerDatabaseImportStart(new DatabaseImportEventArgs(DatabaseConstants.LoadFromLocalSQLite));

			Task.Run(() =>
			{
				try
				{
					Log.Information("Loading external SQLite database: {path}", externalSQLiteFilePath);
					var sourceConnection = new SqliteConnection("Data Source=" + externalSQLiteFilePath);
					sourceConnection.Open();
					var sourceConnectionAbstr = new SqliteDatabaseConnection(sourceConnection);

					var args = new SQLiteImportArgs { destination = targetDatabase, source = sourceConnectionAbstr };
					int WordCount = ImportWordsFromExternalSQLite(args);
					int AttackWordCount = ImportNode(args, DatabaseConstants.AttackNodeIndexTableName);
					int EndWordCount = ImportNode(args, DatabaseConstants.EndNodeIndexTableName);
					int ReverseAttackWordCount = ImportNode(args, DatabaseConstants.ReverseAttackNodeIndexTableName);
					int ReverseEndWordCount = ImportNode(args, DatabaseConstants.ReverseEndNodeIndexTableName);
					int KkutuAttackWordCount = ImportNode(args, DatabaseConstants.KkutuAttackNodeIndexTableName);
					int KkutuEndWordCount = ImportNode(args, DatabaseConstants.KkutuEndNodeIndexTableName);

					Log.Information("DB Import Complete. ({0} Words / {1} Attack word nodes / {2} End-word nodes / {3} Reverse attack word nodes / {4} Reverse end-word nodes / {5} Kkutu attack word nodes / {6} Kkutu end-word nodes)", WordCount, AttackWordCount, EndWordCount, ReverseAttackWordCount, ReverseEndWordCount, KkutuAttackWordCount, KkutuEndWordCount);

					DatabaseEvents.TriggerDatabaseImportDone(new DatabaseImportEventArgs(DatabaseConstants.LoadFromLocalSQLite, $"{WordCount} 개의 단어 / {AttackWordCount} 개의 공격 노드 / {EndWordCount} 개의 한방 노드 / {ReverseAttackWordCount} 개의 앞말잇기 공격 노드 / {ReverseEndWordCount} 개의 앞말잇기 한방 노드 / {KkutuAttackWordCount} 개의 끄투 공격 노드 / {KkutuEndWordCount} 개의 끄투 한방 노드"));
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
			if (!args.destination.IsTableExists(tableName))
			{
				Log.Information("External SQLite Database doesn't contain node list table {tableName}.", tableName);
				return 0;
			}

			int counter = 0;
			foreach (string wordIndex in args.source.Query<string>($"SELECT {DatabaseConstants.WordIndexColumnName} FROM @TableName", new
			{
				TableName = tableName
			}))
			{
				if (args.destination.AddNode(wordIndex))
					Log.Information("Added {node} to {tableName}.", wordIndex, tableName);
				else
					Log.Warning("{node} in {tableName} already exists in database.", wordIndex, tableName);
				counter++;
			}
			return counter;
		}

		private static void ImportSingleWord(this AbstractDatabaseConnection destination, string word, int flags)
		{
			if (destination.AddWord(word, (WordFlags)flags))
				Log.Information("Imported word {word} with flags: {flags}", word, flags);
			else
				Log.Warning("Word {word} already exists in database.", word);
		}

		private static void ImportSingleWordLegacy(this AbstractDatabaseConnection destination, string word, int isEndWordInt)
		{
			// Legacy support
			bool isEndWord = Convert.ToBoolean(isEndWordInt);
			if (destination.AddWord(word, isEndWord ? WordFlags.EndWord : WordFlags.None))
				Log.Information("Imported word {word} {flags:l}", word, isEndWord ? "(EndWord)" : "");
			else
				Log.Warning("Word {word} already exists in database.", word);
		}

		private static int ImportWordsFromExternalSQLite(SQLiteImportArgs args)
		{
			if (!args.source.IsTableExists(DatabaseConstants.WordTableName))
			{
				Log.Information($"External SQLite Database doesn't contain word list table {DatabaseConstants.WordTableName}");
				return 0;
			}

			int counter = 0;
			bool hasIsEndwordColumn = args.source.IsColumnExists(DatabaseConstants.WordTableName, DatabaseConstants.IsEndwordColumnName);
			string columns;
			if (hasIsEndwordColumn)
				columns = DatabaseConstants.WordColumnName + ", " + DatabaseConstants.IsEndwordColumnName;
			else
				columns = DatabaseConstants.WordColumnName + ", " + DatabaseConstants.FlagsColumnName;

			foreach (CompatibleWordModel word in args.source.Query<CompatibleWordModel>($"SELECT {columns} FROM {DatabaseConstants.WordTableName}"))
			{
				if (hasIsEndwordColumn)
					ImportSingleWordLegacy(args.destination, word.Word, word.Flags);
				else
					ImportSingleWord(args.destination, word.Word, word.Flags);

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
}
