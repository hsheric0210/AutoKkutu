using AutoKkutu.Constants;
using AutoKkutu.Databases.Extension;
using NLog;
using Microsoft.Data.Sqlite;
using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

namespace AutoKkutu.Databases.SQLite
{
	public static class SQLiteDatabaseHelper
	{
		private static readonly Logger Logger = LogManager.GetLogger(nameof(SQLiteDatabaseHelper));

		public static int ExecuteNonQuery(this SqliteConnection connection, string query, params SqliteParameter[] parameters)
		{
			if (string.IsNullOrWhiteSpace(query))
				throw new ArgumentException(query, nameof(query));

			using var command = new SqliteCommand(query, connection);
			if (parameters?.Length > 0)
				command.Parameters.AddRange(parameters);
			command.Prepare();
			return command.ExecuteNonQuery();
		}

		public static SqliteDataReader ExecuteReader(this SqliteConnection connection, string query, params SqliteParameter[] parameters)
		{
			if (string.IsNullOrWhiteSpace(query))
				throw new ArgumentException(query, nameof(query));

			var command = new SqliteCommand(query, connection);
			if (parameters?.Length > 0)
				command.Parameters.AddRange(parameters);
			command.Prepare();
			return command.ExecuteReader();
		}

		public static object? ExecuteScalar(this SqliteConnection connection, string query, params SqliteParameter[] parameters)
		{
			if (string.IsNullOrWhiteSpace(query))
				throw new ArgumentException(query, nameof(query));

			using var command = new SqliteCommand(query, connection);
			if (parameters?.Length > 0)
				command.Parameters.AddRange(parameters);
			command.Prepare();
			return command.ExecuteScalar();
		}

		public static string? GetColumnType(SqliteConnection databaseConnection, string tableName, string columnName)
		{
			if (tableName == null)
				return DatabaseConstants.WordListTableName;

			try
			{
				using var command = new SqliteCommand("SELECT * FROM pragma_table_info(@tableName);", databaseConnection);
				command.Parameters.AddWithValue("@tableName", tableName);
				using SqliteDataReader reader = command.ExecuteReader();
				int nameOrdinal = reader.GetOrdinal("Name");
				int typeOrdinal = reader.GetOrdinal("Type");
				while (reader.Read())
				{
					if (reader.GetString(nameOrdinal).Equals(columnName, StringComparison.OrdinalIgnoreCase))
						return reader.GetString(typeOrdinal);
				}
			}
			catch (Exception ex)
			{
				Logger.Error(ex, CultureInfo.CurrentCulture, DatabaseConstants.ErrorGetColumnType, columnName, tableName);
			}

			return null;
		}

		public static bool IsColumnExists(SqliteConnection databaseConnection, string tableName, string columnName)
		{
			if (string.IsNullOrWhiteSpace(tableName))
				tableName = DatabaseConstants.WordListTableName;

			try
			{
				using var command = new SqliteCommand("SELECT * FROM pragma_table_info(@tableName);", databaseConnection);
				command.Parameters.AddWithValue("@tableName", tableName);
				using SqliteDataReader reader = command.ExecuteReader();
				int nameOrdinal = reader.GetOrdinal("Name");
				while (reader.Read())
				{
					if (reader.GetString(nameOrdinal).Equals(columnName, StringComparison.OrdinalIgnoreCase))
						return true;
				}
			}
			catch (Exception ex)
			{
				Logger.Warn(ex, CultureInfo.CurrentCulture, DatabaseConstants.ErrorIsColumnExists, columnName, tableName);
			}

			return false;
		}

		public static bool IsTableExists(SqliteConnection connection, string tableName)
		{
			try
			{
				return Convert.ToInt32(ExecuteScalar(connection, "SELECT COUNT(*) FROM sqlite_master WHERE name=@tableName;", new SqliteParameter("@tableName", tableName)), CultureInfo.InvariantCulture) > 0;
			}
			catch (Exception ex)
			{
				Logger.Error(ex, CultureInfo.CurrentCulture, DatabaseConstants.ErrorIsTableExists, tableName);
				return false;
			}
		}

		public static void LoadFromExternalSQLite(CommonDatabaseConnection targetDatabase, string externalSQLiteFilePath)
		{
			if (!new FileInfo(externalSQLiteFilePath).Exists)
				return;

			DatabaseEvents.TriggerDatabaseImportStart(new DatabaseImportEventArgs(DatabaseConstants.LoadFromLocalSQLite));

			Task.Run(() =>
			{
				try
				{
					Logger.Info(CultureInfo.CurrentCulture, "Loading external SQLite database: {path}", externalSQLiteFilePath);
					var externalSQLiteConnection = new SqliteConnection("Data Source=" + externalSQLiteFilePath);
					externalSQLiteConnection.Open();

					var args = new SQLiteImportArgs { targetDatabaseConnection = targetDatabase, externalSQLiteConnection = externalSQLiteConnection };
					int WordCount = ImportWordsFromExternalSQLite(args);
					int AttackWordCount = ImportNode(args, DatabaseConstants.AttackWordListTableName);
					int EndWordCount = ImportNode(args, DatabaseConstants.EndWordListTableName);
					int ReverseAttackWordCount = ImportNode(args, DatabaseConstants.ReverseAttackWordListTableName);
					int ReverseEndWordCount = ImportNode(args, DatabaseConstants.ReverseEndWordListTableName);
					int KkutuAttackWordCount = ImportNode(args, DatabaseConstants.KkutuAttackWordListTableName);
					int KkutuEndWordCount = ImportNode(args, DatabaseConstants.KkutuEndWordListTableName);

					Logger.Info(CultureInfo.CurrentCulture, "DB Import Complete. ({0} Words / {1} Attack word nodes / {2} End-word nodes / {3} Reverse attack word nodes / {4} Reverse end-word nodes / {5} Kkutu attack word nodes / {6} Kkutu end-word nodes)", WordCount, AttackWordCount, EndWordCount, ReverseAttackWordCount, ReverseEndWordCount, KkutuAttackWordCount, KkutuEndWordCount);

					DatabaseEvents.TriggerDatabaseImportDone(new DatabaseImportEventArgs(DatabaseConstants.LoadFromLocalSQLite, $"{WordCount} 개의 단어 / {AttackWordCount} 개의 공격 노드 / {EndWordCount} 개의 한방 노드 / {ReverseAttackWordCount} 개의 앞말잇기 공격 노드 / {ReverseEndWordCount} 개의 앞말잇기 한방 노드 / {KkutuAttackWordCount} 개의 끄투 공격 노드 / {KkutuEndWordCount} 개의 끄투 한방 노드"));
				}
				catch (Exception ex)
				{
					Logger.Error(ex, "Failed to import external database.");
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
			if (!args.targetDatabaseConnection.IsTableExists(tableName))
			{
				Logger.Info(CultureInfo.CurrentCulture, "External SQLite Database doesn't contain node list table {tableName}.", tableName);
				return 0;
			}

			int count = 0;
			using (var command = new SqliteCommand("SELECT * FROM @tableName", args.externalSQLiteConnection))
			{
				command.Parameters.AddWithValue("@tableName", tableName);
				using SqliteDataReader reader = command.ExecuteReader();
				int wordIndexOrdinal = reader.GetOrdinal(DatabaseConstants.WordIndexColumnName);
				while (reader.Read())
				{
					string wordIndex = reader.GetString(wordIndexOrdinal);
					if (args.targetDatabaseConnection.AddNode(wordIndex))
						Logger.Info(CultureInfo.CurrentCulture, "Added {node} to {tableName}.", wordIndex, tableName);
					else
						Logger.Warn(CultureInfo.CurrentCulture, "{node} in {tableName} already exists in database.", wordIndex, tableName);
					count++;
				}
			}

			return count;
		}

		private static void ImportSingleWord(SQLiteImportArgs args, SqliteDataReader reader, string word)
		{
			int flags = Convert.ToInt32(reader[DatabaseConstants.FlagsColumnName], CultureInfo.InvariantCulture);
			if (args.targetDatabaseConnection.AddWord(word, (WordDatabaseAttributes)flags))
				Logger.Info(CultureInfo.CurrentCulture, "Imported word {word} with flags: {flags}", word, flags);
			else
				Logger.Warn(CultureInfo.CurrentCulture, "Word {word} already exists in database.", word);
		}

		private static void ImportSingleWordLegacy(SQLiteImportArgs args, SqliteDataReader reader, string word)
		{
			// Legacy support
			bool isEndWord = Convert.ToBoolean(Convert.ToInt32(reader[DatabaseConstants.IsEndwordColumnName], CultureInfo.InvariantCulture));
			if (args.targetDatabaseConnection.AddWord(word, isEndWord ? WordDatabaseAttributes.EndWord : WordDatabaseAttributes.None))
				Logger.Info(CultureInfo.CurrentCulture, "Imported word {word} {flags:l}", word, isEndWord ? "(EndWord)" : "");
			else
				Logger.Warn(CultureInfo.CurrentCulture, "Word {word} already exists in database.", word);
		}

		private static int ImportWordsFromExternalSQLite(SQLiteImportArgs args)
		{
			if (!SQLiteDatabaseHelper.IsTableExists(args.externalSQLiteConnection, DatabaseConstants.WordListTableName))
			{
				Logger.Info($"External SQLite Database doesn't contain word list table {DatabaseConstants.WordListTableName}");
				return 0;
			}

			int count = 0;
			bool hasIsEndwordColumn = IsColumnExists(args.externalSQLiteConnection, DatabaseConstants.WordListTableName, DatabaseConstants.IsEndwordColumnName);
			using (SqliteDataReader reader = ExecuteReader(args.externalSQLiteConnection, $"SELECT * FROM {DatabaseConstants.WordListTableName}"))
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
			public SqliteConnection externalSQLiteConnection;
			public CommonDatabaseConnection targetDatabaseConnection;
		}
	}
}
