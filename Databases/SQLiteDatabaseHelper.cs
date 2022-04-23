using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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

		private static void ImportNode(CommonDatabase database, SqliteConnection connection, string type, string tableName, ref int Count)
		{
			if (database.IsTableExists(tableName))
				using (SqliteDataReader reader = new SqliteCommand($"SELECT * FROM {tableName}", connection).ExecuteReader())
					while (reader.Read())
					{
						string endword = reader["word_index"].ToString();
						if (database.AddNode(endword))
							Logger.InfoFormat("Added {0} '{1}'", type, endword);
						else
							Logger.WarnFormat("{0} '{1}' is already existing in database.", type, endword);
						Count++;
					}
		}

		public static void LoadFromExternalSQLite(CommonDatabase target, string externalSQLiteFilePath)
		{
			if (!new FileInfo(externalSQLiteFilePath).Exists)
				return;

			if (CommonDatabase.DBJobStart != null)
				CommonDatabase.DBJobStart(null, new DBJobArgs(DatabaseConstants.LoadFromLocalSQLite));

			Task.Run(() =>
			{
				try
				{
					CommonDatabase.Logger.InfoFormat("Loading external SQLite database: {0}", externalSQLiteFilePath);
					var externalDBConnection = new SqliteConnection("Data Source=" + externalSQLiteFilePath);
					externalDBConnection.Open();

					if (!SQLiteDatabaseHelper.IsTableExists(externalDBConnection, DatabaseConstants.WordListTableName))
					{
						Logger.InfoFormat("Database doesn't contain table '{0}'", DatabaseConstants.WordListTableName);
						return;
					}

					int WordCount = 0;
					int AttackWordCount = 0;
					int EndWordCount = 0;
					int ReverseAttackWordCount = 0;
					int ReverseEndWordCount = 0;
					int KkutuAttackWordCount = 0;
					int KkutuEndWordCount = 0;

					bool hasIsEndwordColumn = IsColumnExists(externalDBConnection, DatabaseConstants.WordListTableName, "is_endword");

					using (SqliteDataReader reader = ExecuteReader(externalDBConnection, $"SELECT * FROM {DatabaseConstants.WordListTableName}"))
						while (reader.Read())
						{
							string word = reader["word"].ToString().Trim();
							if (hasIsEndwordColumn)
							{
								// Legacy support
								bool isEndWord = Convert.ToBoolean(Convert.ToInt32(reader["is_endword"]));
								if (target.AddWord(word, isEndWord ? WordFlags.EndWord : WordFlags.None))
									Logger.InfoFormat("Imported word '{0}' {1}", word, (isEndWord ? "(EndWord)" : ""));
								else
									Logger.WarnFormat("Word '{0}' is already existing in database.", word);
							}
							else
							{
								int flags = Convert.ToInt32(reader["flags"]);
								if (target.AddWord(word, (WordFlags)flags))
									Logger.InfoFormat("Imported word '{0}' flags: {1}", word, flags);
								else
									Logger.WarnFormat("Word '{0}' is already existing in database.", word);
							}

							WordCount++;
						}

					ImportNode(target, externalDBConnection, "Attack word", DatabaseConstants.AttackWordListTableName, ref AttackWordCount);
					ImportNode(target, externalDBConnection, "End-word", DatabaseConstants.EndWordListTableName, ref EndWordCount);
					ImportNode(target, externalDBConnection, "Reverse attack word", DatabaseConstants.ReverseAttackWordListTableName, ref ReverseAttackWordCount);
					ImportNode(target, externalDBConnection, "Reverse end-word", DatabaseConstants.ReverseEndWordListTableName, ref ReverseEndWordCount);
					ImportNode(target, externalDBConnection, "Kkutu attack word", DatabaseConstants.KkutuAttackWordListTableName, ref KkutuAttackWordCount);
					ImportNode(target, externalDBConnection, "Kkutu end-word", DatabaseConstants.KkutuEndWordListTableName, ref KkutuEndWordCount);

					Logger.InfoFormat("DB Import Complete. ({0} Words / {1} Attack word nodes / {2} End-word nodes / {3} Reverse attack word nodes / {4} Reverse end-word nodes / {5} Kkutu attack word nodes / {6} Kkutu end-word nodes)", WordCount, AttackWordCount, EndWordCount, ReverseAttackWordCount, ReverseEndWordCount, KkutuAttackWordCount, KkutuEndWordCount);
					if (CommonDatabase.DBJobDone != null)
						CommonDatabase.DBJobDone(null, new DBJobArgs(DatabaseConstants.LoadFromLocalSQLite, $"{WordCount} 개의 단어 / {AttackWordCount} 개의 공격 노드 / {EndWordCount} 개의 한방 노드 / {ReverseAttackWordCount} 개의 앞말잇기 공격 노드 / {ReverseEndWordCount} 개의 앞말잇기 한방 노드 / {KkutuAttackWordCount} 개의 끄투 공격 노드 / {KkutuEndWordCount} 개의 끄투 한방 노드"));
				}
				catch (Exception ex)
				{
					Logger.Error("Failed to connect external DB", ex);
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
