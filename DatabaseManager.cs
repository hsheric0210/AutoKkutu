﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.Data.Sqlite;

namespace AutoKkutu
{
	internal class DatabaseManager
	{
		public static string GetDBInfo() => "Offline Database (Sqlite)";

		private const string LOG_MODULE_NAME = "DatabaseManager";

		public static void Init()
		{
			bool isInited = _isInited;
			if (!isInited)
			{
				try
				{
					ConsoleManager.Log(ConsoleManager.LogType.Info, "_currentOperationMode = DBMethod.Local.", LOG_MODULE_NAME);
					if (!new FileInfo(_sqlLiteDBLocation).Exists)
					{
						ConsoleManager.Log(ConsoleManager.LogType.Info, _sqlLiteDBLocation + " does not exist. Create new file.", LOG_MODULE_NAME);
						File.Create(_sqlLiteDBLocation).Close();
					}
					ConsoleManager.Log(ConsoleManager.LogType.Info, "Opening _sqlLiteConnection.", LOG_MODULE_NAME);
					_sqlLiteConnection = new SqliteConnection("Data Source=" + _sqlLiteDBLocation);
					_sqlLiteConnection.Open();

					CheckTable();
					ConsoleManager.Log(ConsoleManager.LogType.Info, "DB Open Complete.", LOG_MODULE_NAME);
				}
				catch (Exception e)
				{
					if (DBError != null)
						DBError(null, EventArgs.Empty);
					ConsoleManager.Log(ConsoleManager.LogType.Error, "Failed to connect DB : " + e.ToString(), LOG_MODULE_NAME);
				}
			}
		}

		public static List<string> GetEndWordList()
		{
			var result = new List<string>();

			using (SqliteDataReader reader2 = new SqliteCommand("SELECT * FROM endword_list", _sqlLiteConnection).ExecuteReader())
				while (reader2.Read())
					result.Add(reader2["word_index"].ToString());
			ConsoleManager.Log(ConsoleManager.LogType.Info, string.Format("Found Total {0} end word.", result.Count), LOG_MODULE_NAME);
			return result;
		}

		private static void ExecuteCommand(string command)
		{
			try
			{
				new SqliteCommand(command, _sqlLiteConnection).ExecuteNonQuery();
			}
			catch (Exception e)
			{
				ConsoleManager.Log(ConsoleManager.LogType.Error, "Failed to Execute DB Command ' " + command + " ' : " + e.ToString(), LOG_MODULE_NAME);
			}
		}

		public static void DeleteWord(string word)
		{
			ConsoleManager.Log(ConsoleManager.LogType.Info, "Delete '" + word + "' from db...", LOG_MODULE_NAME);
			ExecuteCommand("DELETE FROM word_list WHERE word = '" + word + "'");
		}

		public static bool AddEndWord(string node)
		{
			if (string.IsNullOrWhiteSpace(node))
				throw new ArgumentNullException("node");

			if (int.TryParse(new SqliteCommand(string.Format("SELECT COUNT(*) FROM endword_list WHERE word_index = '{0}';", node[0]), _sqlLiteConnection).ExecuteScalar().ToString(), out int i) && i > 0)
				return false;

			ExecuteCommand(string.Format("INSERT INTO endword_list(word_index) VALUES('{0}')", node[0]));
			return true;
		}

		public static async void CheckDB(bool UseOnlineDB)
		{
			if (UseOnlineDB && string.IsNullOrWhiteSpace(DatabaseManagement.EvaluateJS("document.getElementById('dict-output').style")))
			{
				MessageBox.Show("사전 창을 감지하지 못했습니다.\n끄투 사전 창을 키십시오.", "데이터베이스 관리자", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			try
			{
				ConsoleManager.Log(ConsoleManager.LogType.Info, "Database Intergrity Check....\nIt will be very long task.", LOG_MODULE_NAME);
				int dbTotalCount;

				int.TryParse(new SqliteCommand("SELECT COUNT(*) FROM word_list", _sqlLiteConnection).ExecuteScalar().ToString(), out dbTotalCount);

				ConsoleManager.Log(ConsoleManager.LogType.Info, $"Database has Total {dbTotalCount} elements.", LOG_MODULE_NAME);
				ConsoleManager.Log(ConsoleManager.LogType.Info, "Getting all elements from database..", LOG_MODULE_NAME);
				int elementCount = 0;
				int DeduplicatedCount = 0;
				int RemovedCount = 0;
				int FixedCount = 0;
				var DeletionList = new List<string>();
				var WordIndexCorrection = new List<string>();
				var IsEndWordCorrection = new Dictionary<string, int>();
				ConsoleManager.Log(ConsoleManager.LogType.Info, "Opening _ChecksqlLiteConnection.", LOG_MODULE_NAME);
				
				var _ChecksqlLiteConnection = new SqliteConnection("Data Source=" + _sqlLiteDBLocation);
				_ChecksqlLiteConnection.Open();

				// Deduplicate db
				// https://wiki.postgresql.org/wiki/Deleting_duplicates
				try
				{
					DeduplicatedCount = new SqliteCommand("DELETE FROM word_list WHERE _rowid_ IN (SELECT _rowid_ FROM (SELECT _rowid_, ROW_NUMBER() OVER w as rnum FROM word_list WINDOW w AS (PARTITION BY word ORDER BY _rowid_)) t WHERE t.rnum > 1);", _ChecksqlLiteConnection).ExecuteNonQuery();
					ConsoleManager.Log(ConsoleManager.LogType.Info, $"Deduplicated {DeduplicatedCount} entries.", LOG_MODULE_NAME);
				}
				catch (Exception ex)
				{
					ConsoleManager.Log(ConsoleManager.LogType.Error, $"Word deduplication failed: {ex}", LOG_MODULE_NAME);
				}

				// Check for errors
				using (SqliteDataReader reader = new SqliteCommand("SELECT * FROM word_list ORDER BY(word) DESC", _ChecksqlLiteConnection).ExecuteReader())
				{
					while (reader.Read())
					{
						elementCount++;
						string content = reader["word"].ToString();
						ConsoleManager.Log(ConsoleManager.LogType.Info, $"Total {dbTotalCount} of {elementCount} ({content})", LOG_MODULE_NAME);
						
						// Check word validity
						if (content.Length == 1 || int.TryParse(content[0].ToString(), out int _) || content[0] == '[' || content[0] == ' ' || content[0] == '-' || content[0] == '.')
						{
							ConsoleManager.Log(ConsoleManager.LogType.Info, $"Not a valid word; Will be removed.", LOG_MODULE_NAME);
							DeletionList.Add(content);
							continue;
						}
						
						// Online verify
						if (UseOnlineDB && !CheckElementOnline(content))
						{
							DeletionList.Add(content);
							continue;
						}

						// Check WordIndex tag
						string correctWordIndex = content[0].ToString();
						if (correctWordIndex != reader["word_index"].ToString())
						{
							ConsoleManager.Log(ConsoleManager.LogType.Info, $"Invaild Word Index; Will be fixed to '{correctWordIndex}'.", LOG_MODULE_NAME);
							WordIndexCorrection.Add(content);
						}
						
						// Check IsEndWord tag
						int CorrectIsEndWord = Convert.ToInt32(PathFinder.EndWordList.Contains(content.Last().ToString()));
						if (CorrectIsEndWord != Convert.ToInt32(reader["is_endword"].ToString()))
						{
							ConsoleManager.Log(ConsoleManager.LogType.Info, $"Invaild Is_EndWord Tag; Will be fixed to '{CorrectIsEndWord}'.", LOG_MODULE_NAME);
							IsEndWordCorrection.Add(content, CorrectIsEndWord);
						}
					}
				}

				// Start fixing
				foreach (string content in DeletionList)
				{
					RemovedCount++;
					ConsoleManager.Log(ConsoleManager.LogType.Info, $"Removed '{content}' from database.", LOG_MODULE_NAME);
					ExecuteCommand("DELETE FROM word_list WHERE word = '" + content + "'");
				}

				foreach (string content in WordIndexCorrection)
				{
					FixedCount++;

					string correctWordIndex = content.First().ToString();
					ConsoleManager.Log(ConsoleManager.LogType.Info, $"Fixed word_index of '{content}' to '{correctWordIndex}'.", LOG_MODULE_NAME);
					ExecuteCommand($"UPDATE word_list SET word_index = '{correctWordIndex}' WHERE word = '{content}';");
				}

				foreach (var pair in IsEndWordCorrection)
				{
					FixedCount++;

					ConsoleManager.Log(ConsoleManager.LogType.Info, $"Fixed is_endword of '{pair.Key}' to '{pair.Value}'.", LOG_MODULE_NAME);
					ExecuteCommand($"UPDATE word_list SET is_endword = '{pair.Value}' WHERE word = '{pair.Key}';");
				}

				_ChecksqlLiteConnection.Close();

				ConsoleManager.Log(ConsoleManager.LogType.Info, $"Total {dbTotalCount} / Removed {RemovedCount} / Fixed {FixedCount}.", LOG_MODULE_NAME);
				ConsoleManager.Log(ConsoleManager.LogType.Info, "Database Check Completed.", LOG_MODULE_NAME);
			}
			catch (Exception ex)
			{
				ConsoleManager.Log(ConsoleManager.LogType.Error, $"Exception while checking database: {ex}", LOG_MODULE_NAME);
			}
		}

		private static bool CheckElementOnline(string i)
		{
			bool result = DatabaseManagement.KkutuOnlineDictCheck(i.Trim());
			if (!result)
				ExecuteCommand("DELETE FROM word_list WHERE word = '" + i + "'");
			return result;
		}

		public static List<PathFinder.PathObject> FindWord(CommonHandler.ResponsePresentedWord data, bool preferEndWord)
		{
			int UseEndWord;
			if (preferEndWord)
				UseEndWord = 1;
			else
				UseEndWord = 0;
			var result = new List<PathFinder.PathObject>();
			string command;
			if (data.CanSubstitution)
				command = $"SELECT * FROM word_list WHERE (word_index = '{data.Content}' OR word_index = '{data.Substitution}') AND is_endword = {UseEndWord} ORDER BY LENGTH(word) DESC LIMIT {128}";
			else
				command = $"SELECT * FROM word_list WHERE word_index = '{data.Content}' AND is_endword = {UseEndWord} ORDER BY LENGTH(word) DESC LIMIT {128}";
			using (SqliteDataReader reader2 = new SqliteCommand(command, _sqlLiteConnection).ExecuteReader())
				while (reader2.Read())
					result.Add(new PathFinder.PathObject(reader2["word"].ToString().Trim(), Convert.ToBoolean(Convert.ToInt32(reader2["is_endword"]))));

			return result;
		}

		public static bool AddWord(string word, bool IsEndword)
		{
			if (string.IsNullOrWhiteSpace(word))
				throw new ArgumentNullException("word");

			int _isEndWord = Convert.ToInt32(IsEndword);

			if (int.TryParse(new SqliteCommand($"SELECT COUNT(*) FROM word_list WHERE word = '{word}';", _sqlLiteConnection).ExecuteScalar().ToString(), out int i) && i > 0)
				return false;

			ExecuteCommand(string.Format("INSERT INTO word_list(word_index, word, is_endword) VALUES('{0}', '{1}', {2})", word[0], word, _isEndWord));
			return true;
		}

		private static void CheckTable()
		{
			if (!CheckTable_Check("word_list"))
				MakeTable("word_list");
			if (!CheckTable_Check("endword_list"))
				MakeTable("endword_list");
		}

		private static void MakeTable(string tablename)
		{
			ConsoleManager.Log(ConsoleManager.LogType.Info, "Create Table : " + tablename, LOG_MODULE_NAME);
			if (tablename == "word_list")
				ExecuteCommand("CREATE TABLE word_list (word VARCHAR(60) NOT NULL, word_index CHAR(1) NOT NULL, is_endword TINYINT(1) NOT NULL);");
			else if (tablename == "endword_list")
				ExecuteCommand("CREATE TABLE endword_list (word_index CHAR(1) NOT NULL);");
		}

		private static bool CheckTable_Check(string tablename)
		{
			ConsoleManager.Log(ConsoleManager.LogType.Info, "Check Table : " + tablename, LOG_MODULE_NAME);
			try
			{
				return int.TryParse(new SqliteCommand("SELECT COUNT(*) FROM sqlite_master WHERE name='" + tablename + "';", _sqlLiteConnection).ExecuteScalar().ToString(), out int i) && i > 0;
			}
			catch (Exception e2)
			{
				ConsoleManager.Log(ConsoleManager.LogType.Info, "Failed to Execute Check DB Table ' " + tablename + " ' : " + e2.ToString(), LOG_MODULE_NAME);
				return false;
			}
		}

		private static readonly bool _isInited = false;

		private static SqliteConnection _sqlLiteConnection;

		private static readonly string _sqlLiteDBLocation = Environment.CurrentDirectory + "\\path.sqlite";

		public static EventHandler DBError;
	}
}