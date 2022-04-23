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

					if (!SQLiteDatabaseHelper.IsTableExists(externalDBConnection, DatabaseConstants.WordListName))
					{
						Logger.InfoFormat("Database doesn't contain table '{0}'", DatabaseConstants.WordListName);
						return;
					}

					int WordCount = 0;
					int AttackWordCount = 0;
					int EndWordCount = 0;
					int ReverseAttackWordCount = 0;
					int ReverseEndWordCount = 0;
					int KkutuAttackWordCount = 0;
					int KkutuEndWordCount = 0;

					bool hasIsEndwordColumn = IsColumnExists(externalDBConnection, DatabaseConstants.WordListName, "is_endword");

					using (SqliteDataReader reader = ExecuteReader(externalDBConnection, $"SELECT * FROM {DatabaseConstants.WordListName}"))
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

					if (target.IsTableExists(DatabaseConstants.AttackWordListName))
						using (SqliteDataReader reader = new SqliteCommand($"SELECT * FROM {DatabaseConstants.AttackWordListName}", externalDBConnection).ExecuteReader())
							while (reader.Read())
							{
								string endword = reader["word_index"].ToString();
								if (target.AddNode(endword, DatabaseConstants.AttackWordListName))
									Logger.InfoFormat("Added attack word '{0}'", endword);
								else
									Logger.WarnFormat("Attack word '{0}' is already existing in database.", endword);
								AttackWordCount++;
							}

					if (target.IsTableExists(DatabaseConstants.EndWordListName))
						using (SqliteDataReader reader = new SqliteCommand($"SELECT * FROM {DatabaseConstants.EndWordListName}", externalDBConnection).ExecuteReader())
							while (reader.Read())
							{
								string endword = reader["word_index"].ToString();
								if (target.AddNode(endword))
									Logger.InfoFormat("Added end-word '{0}'", endword);
								else
									Logger.WarnFormat("End-word '{0}' is already existing in database.", endword);
								EndWordCount++;
							}

					if (target.IsTableExists(DatabaseConstants.ReverseAttackWordListName))
						using (SqliteDataReader reader = new SqliteCommand($"SELECT * FROM {DatabaseConstants.ReverseAttackWordListName}", externalDBConnection).ExecuteReader())
							while (reader.Read())
							{
								string endword = reader["word_index"].ToString();
								if (target.AddNode(endword, DatabaseConstants.ReverseAttackWordListName))
									Logger.InfoFormat("Added reverse attack word '{0}'", endword);
								else
									Logger.WarnFormat("Reverse Attack word '{0}' is already existing in database.", endword);
								ReverseAttackWordCount++;
							}

					if (target.IsTableExists(DatabaseConstants.ReverseEndWordListName))
						using (SqliteDataReader reader = new SqliteCommand($"SELECT * FROM {DatabaseConstants.ReverseEndWordListName}", externalDBConnection).ExecuteReader())
							while (reader.Read())
							{
								string endword = reader["word_index"].ToString();
								if (target.AddNode(endword, DatabaseConstants.ReverseEndWordListName))
									Logger.InfoFormat("Added reverse end-word '{0}'", endword);
								else
									Logger.WarnFormat("Reverse End-word '{0}' is already existing in database.", endword);
								ReverseEndWordCount++;
							}

					if (target.IsTableExists(DatabaseConstants.KkutuAttackWordListName))
						using (SqliteDataReader reader = new SqliteCommand($"SELECT * FROM {DatabaseConstants.KkutuAttackWordListName}", externalDBConnection).ExecuteReader())
							while (reader.Read())
							{
								string endword = reader["word_index"].ToString();
								if (target.AddNode(endword, DatabaseConstants.KkutuAttackWordListName))
									Logger.InfoFormat("Added kkutu attack word '{0}'", endword);
								else
									Logger.WarnFormat("Kkutu attack word '{0}' is already existing in database.", endword);
								KkutuAttackWordCount++;
							}

					if (target.IsTableExists(DatabaseConstants.KkutuEndWordListName))
						using (SqliteDataReader reader = new SqliteCommand($"SELECT * FROM {DatabaseConstants.KkutuEndWordListName}", externalDBConnection).ExecuteReader())
							while (reader.Read())
							{
								string endword = reader["word_index"].ToString();
								if (target.AddNode(endword, DatabaseConstants.KkutuEndWordListName))
									Logger.InfoFormat("Added kkutu end-word '{0}'", endword);
								else
									Logger.WarnFormat("Kkutu End-word '{0}' is already existing in database.", endword);
								KkutuEndWordCount++;
							}

					Logger.InfoFormat("DB Import Complete. ({0} Words / {1} Attack word nodes / {2} End-word nodes / {3} Reverse attack word nodes / {4} Reverse end-word nodes / {5} Kkutu attack word nodes / {6} Kkutu end-word nodes)", WordCount, AttackWordCount, EndWordCount, ReverseAttackWordCount, ReverseEndWordCount, KkutuAttackWordCount, KkutuEndWordCount);
					if (CommonDatabase.DBJobDone != null)
						CommonDatabase.DBJobDone(null, new DBJobArgs(DatabaseConstants.LoadFromLocalSQLite, $"{WordCount} 개의 단어 / {EndWordCount} 개의 한방 노드 / {ReverseEndWordCount} 개의 앞말잇기 한방 노드 / {KkutuEndWordCount} 개의 끄투 한방 노드"));
				}
				catch (Exception ex)
				{
					Logger.Error("Failed to connect external DB", ex);
				}
			});
		}

		public static int ExecuteNonQuery(SqliteConnection connection, string query)
		{
			try
			{
				using (var command = new SqliteCommand(query, connection))
					return command.ExecuteNonQuery();
			}
			catch (Exception ex)
			{
				Logger.Error($"Failed to execute SQLite query '{query}'", ex);
			}
			return -1;
		}

		public static object ExecuteScalar(SqliteConnection connection, string query)
		{
			try
			{
				using (var command = new SqliteCommand(query, connection))
					return command.ExecuteScalar();
			}
			catch (Exception ex)
			{
				Logger.Error($"Failed to execute SQLite query '{query}'", ex);
			}
			return null;
		}

		public static SqliteDataReader ExecuteReader(SqliteConnection connection, string query)
		{
			try
			{
				return new SqliteCommand(query, connection).ExecuteReader();
			}
			catch (Exception ex)
			{
				Logger.Error($"Failed to execute SQLite query '{query}'", ex);
			}
			return null;
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
				Logger.Info($"Failed to check the existence of table '{tableName}' : {ex.ToString()}");
				return false;
			}
		}

		public static string GetColumnType(SqliteConnection databaseConnection, string tableName, string columnName)
		{
			try
			{
				using (SqliteDataReader reader = new SqliteCommand($"PRAGMA table_info({tableName})", databaseConnection).ExecuteReader())
				{
					int nameIndex = reader.GetOrdinal("Name");
					int typeIndex = reader.GetOrdinal("Type");
					while (reader.Read())
						if (reader.GetString(nameIndex).Equals(columnName))
							return reader.GetString(typeIndex);
				}
			}
			catch (Exception)
			{
			}

			return null;
		}
	}
}
