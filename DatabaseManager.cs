using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Data.Sqlite;

namespace AutoKkutu
{
	internal class DatabaseManager
	{
		public static string GetDBInfo() => "Offline Database (Sqlite)";

		private const string LOG_MODULE_NAME = "DatabaseManager";

		private static readonly bool _isInited = false;

		private static SqliteConnection DatabaseConnection;

		private static readonly string _sqlLiteDBLocation = Environment.CurrentDirectory + "\\path.sqlite";

		public static EventHandler DBError;
		public static EventHandler DBJobStart;
		public static EventHandler DBJobDone;
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
					ConsoleManager.Log(ConsoleManager.LogType.Info, "Opening database connection.", LOG_MODULE_NAME);
					DatabaseConnection = new SqliteConnection("Data Source=" + _sqlLiteDBLocation);
					DatabaseConnection.Open();
					DatabaseConnection.CreateFunction("checkMissionChar", (string str, string ch) =>
					{
						int occurrence = 0;
						char target = ch.First();
						foreach (char c in str.ToCharArray())
							if (c == target)
								occurrence++;
						return occurrence > 0 ? 256 + occurrence : 0; // 데이터베이스 상, 256단어보다 긴 글자는 없다
					});

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

		public static void LoadFromDB(string dbFileName)
		{
			if (!new FileInfo(dbFileName).Exists)
				return;

			if (DBJobStart != null)
				DBJobStart(null, new DBJobArgs("데이터베이스 불러오기"));

			Task.Run(() =>
			{
				try
				{

					ConsoleManager.Log(ConsoleManager.LogType.Info, $"Loading external database: {dbFileName}", LOG_MODULE_NAME);
					var externalDBConnection = new SqliteConnection("Data Source=" + dbFileName);
					externalDBConnection.Open();

					if (!CheckTable_Check("word_list"))
					{
						ConsoleManager.Log(ConsoleManager.LogType.Info, $"Database doesn't contain table 'word_list'", LOG_MODULE_NAME);
						return;
					}
					if (!CheckTable_Check("endword_list"))
					{
						ConsoleManager.Log(ConsoleManager.LogType.Info, $"Database doesn't contain table 'endword_list'", LOG_MODULE_NAME);
						return;
					}

					int WordCount = 0;
					int EndWordCount = 0;
					using (SqliteDataReader reader2 = new SqliteCommand($"SELECT * FROM word_list", externalDBConnection).ExecuteReader())
						while (reader2.Read())
						{
							string word = reader2["word"].ToString().Trim();
							bool isEndWord = Convert.ToBoolean(Convert.ToInt32(reader2["is_endword"]));
							if (AddWord(word, isEndWord))
								ConsoleManager.Log(ConsoleManager.LogType.Info, $"Imported word '{word}' {(isEndWord ? "(EndWord)" : "")}", LOG_MODULE_NAME);
							else
								ConsoleManager.Log(ConsoleManager.LogType.Warning, $"Word '{word}' is already existing in database.", LOG_MODULE_NAME);
							WordCount++;
						}

					using (SqliteDataReader reader2 = new SqliteCommand("SELECT * FROM endword_list", externalDBConnection).ExecuteReader())
						while (reader2.Read())
						{
							string endword = reader2["word_index"].ToString();
							if (AddEndWord(endword))
								ConsoleManager.Log(ConsoleManager.LogType.Info, $"Added end-word '{endword}'", LOG_MODULE_NAME);
							else
								ConsoleManager.Log(ConsoleManager.LogType.Warning, $"End-word '{endword}' is already existing in database.", LOG_MODULE_NAME);
							EndWordCount++;
						}

					ConsoleManager.Log(ConsoleManager.LogType.Info, $"DB Import Complete. ({WordCount} Words / {EndWordCount} EndWordNodes)", LOG_MODULE_NAME);
					if (DBJobDone != null)
						DBJobDone(null, new DBJobArgs("데이터베이스 불러오기", $"{WordCount} 개의 단어 / {EndWordCount} 개의 한방 노드"));
				}
				catch (Exception e)
				{
					ConsoleManager.Log(ConsoleManager.LogType.Error, "Failed to connect external DB : " + e.ToString(), LOG_MODULE_NAME);
				}
			});
		}

		public static List<string> GetEndWordList()
		{
			var result = new List<string>();

			using (SqliteDataReader reader2 = new SqliteCommand("SELECT * FROM endword_list", DatabaseConnection).ExecuteReader())
				while (reader2.Read())
					result.Add(reader2["word_index"].ToString());
			ConsoleManager.Log(ConsoleManager.LogType.Info, string.Format("Found Total {0} end word.", result.Count), LOG_MODULE_NAME);
			return result;
		}

		private static void ExecuteCommand(string command)
		{
			try
			{
				new SqliteCommand(command, DatabaseConnection).ExecuteNonQuery();
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

			if (int.TryParse(new SqliteCommand(string.Format("SELECT COUNT(*) FROM endword_list WHERE word_index = '{0}';", node[0]), DatabaseConnection).ExecuteScalar().ToString(), out int i) && i > 0)
				return false;

			ExecuteCommand(string.Format("INSERT INTO endword_list(word_index) VALUES('{0}')", node[0]));
			return true;
		}

		public static void CheckDB(bool UseOnlineDB)
		{
			if (UseOnlineDB && string.IsNullOrWhiteSpace(DatabaseManagement.EvaluateJS("document.getElementById('dict-output').style")))
			{
				MessageBox.Show("사전 창을 감지하지 못했습니다.\n끄투 사전 창을 키십시오.", "데이터베이스 관리자", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			if (DBJobStart != null)
				DBJobStart(null, new DBJobArgs("데이터베이스 검증"));


			Task.Run(() =>
			{
				try
				{
					ConsoleManager.Log(ConsoleManager.LogType.Info, "Database Intergrity Check....\nIt will be very long task.", LOG_MODULE_NAME);
					int dbTotalCount;

					int.TryParse(new SqliteCommand("SELECT COUNT(*) FROM word_list", DatabaseConnection).ExecuteScalar().ToString(), out dbTotalCount);

					ConsoleManager.Log(ConsoleManager.LogType.Info, $"Database has Total {dbTotalCount} elements.", LOG_MODULE_NAME);
					ConsoleManager.Log(ConsoleManager.LogType.Info, "Getting all elements from database..", LOG_MODULE_NAME);
					int elementCount = 0;
					int DeduplicatedCount = 0;
					int RemovedCount = 0;
					int FixedCount = 0;
					var DeletionList = new List<string>();
					var WordIndexCorrection = new List<string>();
					var ReverseWordIndexCorrection = new List<string>();
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
							if (content.Length == 1 || int.TryParse(content[0].ToString(), out int _) || content[0] == '[' || content[0] == '-' || content[0] == '.' || content.Contains(" "))
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
							string correctWordIndex = content.First().ToString();
							if (correctWordIndex != reader["word_index"].ToString())
							{
								ConsoleManager.Log(ConsoleManager.LogType.Info, $"Invaild Word Index; Will be fixed to '{correctWordIndex}'.", LOG_MODULE_NAME);
								WordIndexCorrection.Add(content);
							}

							// Check WordIndex tag
							string correctReverseWordIndex = content.Last().ToString();
							if (correctReverseWordIndex != reader["reverse_word_index"].ToString())
							{
								ConsoleManager.Log(ConsoleManager.LogType.Info, $"Invaild Reverse Word Index; Will be fixed to '{correctReverseWordIndex}'.", LOG_MODULE_NAME);
								ReverseWordIndexCorrection.Add(content);
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

					foreach (string content in ReverseWordIndexCorrection)
					{
						FixedCount++;

						string correctReverseWordIndex = content.Last().ToString();
						ConsoleManager.Log(ConsoleManager.LogType.Info, $"Fixed reverse_word_index of '{content}' to '{correctReverseWordIndex}'.", LOG_MODULE_NAME);
						ExecuteCommand($"UPDATE word_list SET reverse_word_index = '{correctReverseWordIndex}' WHERE word = '{content}';");
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

					if (DBJobDone != null)
						DBJobDone(null, new DBJobArgs("데이터베이스 검증", $"{RemovedCount} 개 항목 제거됨 / {FixedCount} 개 항목 수정됨"));
				}
				catch (Exception ex)
				{
					ConsoleManager.Log(ConsoleManager.LogType.Error, $"Exception while checking database: {ex}", LOG_MODULE_NAME);
				}
			});
		}

		private static bool CheckElementOnline(string i)
		{
			bool result = DatabaseManagement.KkutuOnlineDictCheck(i.Trim());
			if (!result)
				ExecuteCommand("DELETE FROM word_list WHERE word = '" + i + "'");
			return result;
		}

		public static List<PathFinder.PathObject> FindWord(CommonHandler.ResponsePresentedWord data, string missionChar, int endWord, bool reverse)
		{
			// endWord
			// 0 - Except end-words
			// 1 - Include end-words
			// 2 - Don't care
			int UseEndWord = endWord <= 1 ? endWord : 0;
			var result = new List<PathFinder.PathObject>();
			string condition;
			string endWordCondition = "";
			string orderCondition2 = "";
			string orderCondition;

			string index = reverse ? "reverse_word_index" : "word_index";

			if (data.CanSubstitution)
				condition = $"({index} = '{data.Content}' OR {index} = '{data.Substitution}')";
			else
				condition = $"{index} = '{data.Content}'";

			switch (endWord)
			{
				case 0:
					endWordCondition = "AND is_endword = '0'";
					break;
				case 1:
					orderCondition2 = $"(CASE WHEN is_endword = '1' THEN 512 ELSE 0 END) +";
					break;
				default:
					endWordCondition = "";
					break;
			}

			if (string.IsNullOrWhiteSpace(missionChar))
				orderCondition = $"({orderCondition2} LENGTH(word))";
			else
				orderCondition = $"(checkMissionChar(word, '{missionChar}') + {orderCondition2} LENGTH(word))";

			string query = $"SELECT * FROM word_list WHERE {condition} {endWordCondition} ORDER BY {orderCondition} DESC LIMIT {50}";
			ConsoleManager.Log(ConsoleManager.LogType.Info, $"Query: {query}", LOG_MODULE_NAME);
			using (SqliteDataReader reader2 = new SqliteCommand(query, DatabaseConnection).ExecuteReader())
				while (reader2.Read())
					result.Add(new PathFinder.PathObject(reader2["word"].ToString().Trim(), Convert.ToBoolean(Convert.ToInt32(reader2["is_endword"]))));

			return result;
		}

		public static bool AddWord(string word, bool IsEndword)
		{
			if (string.IsNullOrWhiteSpace(word))
				throw new ArgumentNullException("word");

			int _isEndWord = Convert.ToInt32(IsEndword);

			if (int.TryParse(new SqliteCommand($"SELECT COUNT(*) FROM word_list WHERE word = '{word}';", DatabaseConnection).ExecuteScalar().ToString(), out int i) && i > 0)
				return false;

			ExecuteCommand($"INSERT INTO word_list(word_index, reverse_word_index, word, is_endword) VALUES('{word.First()}', '{word.Last()}', '{word}', {_isEndWord})");
			return true;
		}

		private static void CheckTable()
		{
			if (!CheckTable_Check("word_list"))
				MakeTable("word_list");
			else
			{
				// For backward compatibility
				if (!CheckColumnExistence("reverse_word_index"))
				{
					try
					{
						new SqliteCommand("ALTER TABLE word_list ADD COLUMN reverse_word_index CHAR(1) NOT NULL DEFAULT ' '", DatabaseConnection).ExecuteNonQuery();
						ConsoleManager.Log(ConsoleManager.LogType.Warning, "Added reverse_word_index column", LOG_MODULE_NAME);
					}
					catch (Exception ex)
					{
						ConsoleManager.Log(ConsoleManager.LogType.Error, $"Failed to add reverse_word_index: {ex}", LOG_MODULE_NAME);
					}
				}
			}

			if (!CheckTable_Check("endword_list"))
				MakeTable("endword_list");
		}

		private static bool CheckColumnExistence(string columnName)
		{
			try
			{
				using (SqliteDataReader reader = new SqliteCommand("PRAGMA table_info(word_list)", DatabaseConnection).ExecuteReader())
				{
					int nameIndex = reader.GetOrdinal("Name");
					while (reader.Read())
						if (reader.GetString(nameIndex).Equals(columnName))
							return true;
				}
			}
			catch (Exception ex)
			{
				ConsoleManager.Log(ConsoleManager.LogType.Error, $"Failed to check {columnName} existence: {ex}", LOG_MODULE_NAME);
			}

			return false;
		}


		private static void MakeTable(string tablename)
		{
			ConsoleManager.Log(ConsoleManager.LogType.Info, "Create Table : " + tablename, LOG_MODULE_NAME);
			if (tablename == "word_list")
				ExecuteCommand("CREATE TABLE word_list (word VARCHAR(60) NOT NULL, word_index CHAR(1) NOT NULL, reverse_word_index CHAR(1) NOT NULL, is_endword TINYINT(1) NOT NULL);");
			else if (tablename == "endword_list")
				ExecuteCommand("CREATE TABLE endword_list (word_index CHAR(1) NOT NULL);");
		}

		private static bool CheckTable_Check(string tablename)
		{
			ConsoleManager.Log(ConsoleManager.LogType.Info, "Check Table : " + tablename, LOG_MODULE_NAME);
			try
			{
				return int.TryParse(new SqliteCommand("SELECT COUNT(*) FROM sqlite_master WHERE name='" + tablename + "';", DatabaseConnection).ExecuteScalar().ToString(), out int i) && i > 0;
			}
			catch (Exception e2)
			{
				ConsoleManager.Log(ConsoleManager.LogType.Info, "Failed to Execute Check DB Table ' " + tablename + " ' : " + e2.ToString(), LOG_MODULE_NAME);
				return false;
			}
		}

		public class DBJobArgs : EventArgs
		{
			public string JobName;
			public string Result;

			public DBJobArgs(string jobName) => JobName = jobName;

			public DBJobArgs(string jobName, string result)
			{
				JobName = jobName;
				Result = result;
			}
		}
	}
}
