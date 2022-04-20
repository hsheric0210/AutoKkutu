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

					if (!target.IsTableExists(DatabaseConstants.WordListName))
					{
						Logger.InfoFormat("Database doesn't contain table '{0}'", DatabaseConstants.WordListName);
						return;
					}

					int WordCount = 0;
					int EndWordCount = 0;
					int ReverseEndWordCount = 0;
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

					if (target.IsTableExists(DatabaseConstants.KkutuEndWordListName))
						using (SqliteDataReader reader = new SqliteCommand($"SELECT * FROM {DatabaseConstants.KkutuEndWordListName}", externalDBConnection).ExecuteReader())
							while (reader.Read())
							{
								string endword = reader["word_index"].ToString();
								if (target.AddNode(endword, DatabaseConstants.KkutuEndWordListName))
									Logger.InfoFormat("Added reverse end-word '{0}'", endword);
								else
									Logger.WarnFormat("Reverse End-word '{0}' is already existing in database.", endword);
								KkutuEndWordCount++;
							}

					Logger.InfoFormat("DB Import Complete. ({0} Words / {1} EndWord Nodes / {2} Reverse EndWord Nodes / {3} Kkutu EndWord Nodes)", WordCount, EndWordCount, ReverseEndWordCount, KkutuEndWordCount);
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
	}
}
