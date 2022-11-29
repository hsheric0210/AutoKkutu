using AutoKkutu.Constants;
using AutoKkutu.Databases.Extension;
using Serilog;
using Microsoft.Data.Sqlite;
using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

namespace AutoKkutu.Databases.SQLite
{
	public static class SqliteDatabaseHelper
	{
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
					var externalSQLiteConnection = new SqliteConnection("Data Source=" + externalSQLiteFilePath);
					externalSQLiteConnection.Open();

					var args = new SQLiteImportArgs { destination = targetDatabase, source = externalSQLiteConnection };
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

			int count = 0;
			using (var command = new SqliteCommand("SELECT * FROM @tableName", args.source))
			{
				command.Parameters.AddWithValue("@tableName", tableName);
				using SqliteDataReader reader = command.ExecuteReader();
				int wordIndexOrdinal = reader.GetOrdinal(DatabaseConstants.WordIndexColumnName);
				while (reader.Read())
				{
					string wordIndex = reader.GetString(wordIndexOrdinal);
					if (args.destination.AddNode(wordIndex))
						Log.Information("Added {node} to {tableName}.", wordIndex, tableName);
					else
						Log.Warning("{node} in {tableName} already exists in database.", wordIndex, tableName);
					count++;
				}
			}

			return count;
		}

		private static void ImportSingleWord(SQLiteImportArgs args, SqliteDataReader reader, string word)
		{
			int flags = Convert.ToInt32(reader[DatabaseConstants.FlagsColumnName], CultureInfo.InvariantCulture);
			if (args.destination.AddWord(word, (WordDbTypes)flags))
				Log.Information("Imported word {word} with flags: {flags}", word, flags);
			else
				Log.Warning("Word {word} already exists in database.", word);
		}

		private static void ImportSingleWordLegacy(SQLiteImportArgs args, SqliteDataReader reader, string word)
		{
			// Legacy support
			bool isEndWord = Convert.ToBoolean(Convert.ToInt32(reader[DatabaseConstants.IsEndwordColumnName], CultureInfo.InvariantCulture));
			if (args.destination.AddWord(word, isEndWord ? WordDbTypes.EndWord : WordDbTypes.None))
				Log.Information("Imported word {word} {flags:l}", word, isEndWord ? "(EndWord)" : "");
			else
				Log.Warning("Word {word} already exists in database.", word);
		}

		private static int ImportWordsFromExternalSQLite(SQLiteImportArgs args)
		{
			if (!SqliteDatabaseHelper.IsTableExists(args.source, DatabaseConstants.WordTableName))
			{
				Log.Information($"External SQLite Database doesn't contain word list table {DatabaseConstants.WordTableName}");
				return 0;
			}

			int count = 0;
			bool hasIsEndwordColumn = IsColumnExists(args.source, DatabaseConstants.WordTableName, DatabaseConstants.IsEndwordColumnName);
			using (SqliteDataReader reader = ExecuteReader(args.source, $"SELECT * FROM {DatabaseConstants.WordTableName}"))
			{
				int wordOrdinal = reader.GetOrdinal(DatabaseConstants.WordColumnName);
				while (reader.Read())
				{
					string word = reader.GetString(wordOrdinal).Trim();
					if (hasIsEndwordColumn)
						ImportSingleWordLegacy(args, reader, word);
					else
						ImportSingleWord(args, reader, word);

					count++;
				}
			}

			return count;
		}

		private struct SQLiteImportArgs
		{
			public SqliteConnection source;
			public AbstractDatabaseConnection destination;
		}
	}
}
