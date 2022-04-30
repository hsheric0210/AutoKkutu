using Microsoft.Data.Sqlite;
using System;
using System.IO;
using System.Threading.Tasks;
using static AutoKkutu.CommonDatabase;
using static AutoKkutu.Constants;

namespace AutoKkutu.Databases
{
	public static class SQLiteDatabaseHelper
	{
		public static bool IsColumnExists(SqliteConnection databaseConnection, string tableName, string columnName)
		{
			try
			{
				using (SqliteDataReader reader = new SqliteCommand($"PRAGMA table_info({tableName})", databaseConnection).ExecuteReader())
				{
					int nameIndex = reader.GetOrdinal("Name");
					while (reader.Read())
						if (reader.GetString(nameIndex).Equals(columnName))
							return true;
				}
			}
			catch (Exception)
			{
			}

			return false;
		}

		private struct SQLiteImportArgs
		{
			public CommonDatabase targetDatabase;
			public SqliteConnection externalSQLiteConnection;
		}

		private static void ImportWords(SQLiteImportArgs args, ref int WordCount)
		{
			if (!SQLiteDatabaseHelper.IsTableExists(args.externalSQLiteConnection, DatabaseConstants.WordListTableName))
			{
				Logger.InfoFormat("External SQLite Database doesn't contain word list table '{0}'", DatabaseConstants.WordListTableName);
				return;
			}

			bool hasIsEndwordColumn = IsColumnExists(args.externalSQLiteConnection, DatabaseConstants.WordListTableName, DatabaseConstants.IsEndwordColumnName);
			using (SqliteDataReader reader = ExecuteReader(args.externalSQLiteConnection, $"SELECT * FROM {DatabaseConstants.WordListTableName}"))
				while (reader.Read())
				{
					string word = reader[DatabaseConstants.WordColumnName].ToString().Trim();
					if (hasIsEndwordColumn)
					{
						// Legacy support
						bool isEndWord = Convert.ToBoolean(Convert.ToInt32(reader[DatabaseConstants.IsEndwordColumnName]));
						if (args.targetDatabase.AddWord(word, isEndWord ? WordFlags.EndWord : WordFlags.None))
							Logger.InfoFormat("Imported word '{0}' {1}", word, (isEndWord ? "(EndWord)" : ""));
						else
							Logger.WarnFormat("Word '{0}' is already existing in database.", word);
					}
					else
					{
						int flags = Convert.ToInt32(reader[DatabaseConstants.FlagsColumnName]);
						if (args.targetDatabase.AddWord(word, (WordFlags)flags))
							Logger.InfoFormat("Imported word '{0}' flags: {1}", word, flags);
						else
							Logger.WarnFormat("Word '{0}' is already existing in database.", word);
					}

					WordCount++;
				}
		}

		private static void ImportNode(SQLiteImportArgs args, string tableName, ref int Count)
		{
			if (!args.targetDatabase.IsTableExists(tableName))
			{
				Logger.InfoFormat("External SQLite Database doesn't contain node list table '{0}'", tableName);
				return;
			}

			using (SqliteDataReader reader = new SqliteCommand($"SELECT * FROM {tableName}", args.externalSQLiteConnection).ExecuteReader())
				while (reader.Read())
				{
					string endword = reader[DatabaseConstants.WordIndexColumnName].ToString();
					if (args.targetDatabase.AddNode(endword))
						Logger.InfoFormat("Added {0} '{1}'", tableName, endword);
					else
						Logger.WarnFormat("{0} '{1}' is already existing in database.", tableName, endword);
					Count++;
				}
		}

		public static void LoadFromExternalSQLite(CommonDatabase targetDatabase, string externalSQLiteFilePath)
		{
			if (!new FileInfo(externalSQLiteFilePath).Exists)
				return;

			if (CommonDatabase.DBJobStart != null)
				CommonDatabase.DBJobStart(null, new DBJobArgs(DatabaseConstants.LoadFromLocalSQLite));

			Task.Run(() =>
			{
				try
				{
					Logger.InfoFormat("Loading external SQLite database: {0}", externalSQLiteFilePath);
					var externalSQLiteConnection = new SqliteConnection("Data Source=" + externalSQLiteFilePath);
					externalSQLiteConnection.Open();

					var args = new SQLiteImportArgs { targetDatabase = targetDatabase, externalSQLiteConnection = externalSQLiteConnection };
					int WordCount = 0, AttackWordCount = 0, EndWordCount = 0, ReverseAttackWordCount = 0, ReverseEndWordCount = 0, KkutuAttackWordCount = 0, KkutuEndWordCount = 0;
					ImportWords(args, ref WordCount);
					ImportNode(args, DatabaseConstants.AttackWordListTableName, ref AttackWordCount);
					ImportNode(args, DatabaseConstants.EndWordListTableName, ref EndWordCount);
					ImportNode(args, DatabaseConstants.ReverseAttackWordListTableName, ref ReverseAttackWordCount);
					ImportNode(args, DatabaseConstants.ReverseEndWordListTableName, ref ReverseEndWordCount);
					ImportNode(args, DatabaseConstants.KkutuAttackWordListTableName, ref KkutuAttackWordCount);
					ImportNode(args, DatabaseConstants.KkutuEndWordListTableName, ref KkutuEndWordCount);

					Logger.InfoFormat("DB Import Complete. ({0} Words / {1} Attack word nodes / {2} End-word nodes / {3} Reverse attack word nodes / {4} Reverse end-word nodes / {5} Kkutu attack word nodes / {6} Kkutu end-word nodes)", WordCount, AttackWordCount, EndWordCount, ReverseAttackWordCount, ReverseEndWordCount, KkutuAttackWordCount, KkutuEndWordCount);
					if (CommonDatabase.DBJobDone != null)
						CommonDatabase.DBJobDone(null, new DBJobArgs(DatabaseConstants.LoadFromLocalSQLite, $"{WordCount} 개의 단어 / {AttackWordCount} 개의 공격 노드 / {EndWordCount} 개의 한방 노드 / {ReverseAttackWordCount} 개의 앞말잇기 공격 노드 / {ReverseEndWordCount} 개의 앞말잇기 한방 노드 / {KkutuAttackWordCount} 개의 끄투 공격 노드 / {KkutuEndWordCount} 개의 끄투 한방 노드"));
				}
				catch (Exception ex)
				{
					Logger.Error("Failed to import external DB", ex);
				}
			});
		}

		public static int ExecuteNonQuery(SqliteConnection connection, string query)
		{
			using (var command = new SqliteCommand(query, connection))
				return command.ExecuteNonQuery();
		}

		public static object ExecuteScalar(SqliteConnection connection, string query)
		{
			using (var command = new SqliteCommand(query, connection))
				return command.ExecuteScalar();
		}

		public static SqliteDataReader ExecuteReader(SqliteConnection connection, string query)
		{
			return new SqliteCommand(query, connection).ExecuteReader();
		}

		public static SqliteConnection OpenConnection(string databaseFile)
		{
			var connection = new SqliteConnection($"Data Source={databaseFile}");
			connection.Open();
			return connection;
		}

		public static bool IsTableExists(SqliteConnection connection, string tableName)
		{
			try
			{
				return Convert.ToInt32(ExecuteScalar(connection, $"SELECT COUNT(*) FROM sqlite_master WHERE name='{tableName}';")) > 0;
			}
			catch (Exception ex)
			{
				Logger.Error(string.Format(DatabaseConstants.Error_IsTableExists, tableName), ex);
				return false;
			}
		}

		public static string GetColumnType(SqliteConnection databaseConnection, string tableName, string columnName)
		{
			try
			{
				using (SqliteDataReader reader = new SqliteCommand($"PRAGMA table_info({tableName})", databaseConnection).ExecuteReader())
				{
					while (reader.Read())
						if (reader["Name"].Equals(columnName))
							return reader.GetString(reader.GetOrdinal("Type"));
				}
			}
			catch (Exception ex)
			{
				Logger.Error(string.Format(DatabaseConstants.Error_GetColumnType, columnName, tableName), ex);
			}

			return null;
		}
	}
}
