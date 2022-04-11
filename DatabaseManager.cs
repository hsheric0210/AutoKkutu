using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using log4net;
using Microsoft.Data.Sqlite;

namespace AutoKkutu
{
	internal class DatabaseManager
	{
		public static string GetDBInfo() => "Offline Database (Sqlite)";

		private static readonly ILog Logger = LogManager.GetLogger("DatabaseManager");

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
					Logger.Info("_currentOperationMode = DBMethod.Local.");
					if (!new FileInfo(_sqlLiteDBLocation).Exists)
					{
						Logger.Info(_sqlLiteDBLocation + " does not exist. Create new file.");
						File.Create(_sqlLiteDBLocation).Close();
					}
					Logger.Info("Opening database connection.");
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
					Logger.Info("DB Open Complete.");
				}
				catch (Exception ex)
				{
					if (DBError != null)
						DBError(null, EventArgs.Empty);
					Logger.Error("Failed to connect DB", ex);
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

				Logger.InfoFormat("Loading external database: {0}", dbFileName);
				var externalDBConnection = new SqliteConnection("Data Source=" + dbFileName);
				externalDBConnection.Open();

				if (!CheckTable_Check("word_list"))
				{
					Logger.Info("Database doesn't contain table 'word_list'");
					return;
				}
				if (!CheckTable_Check("endword_list"))
				{
					Logger.Info("Database doesn't contain table 'endword_list'");
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
							Logger.InfoFormat("Imported word '{0}' {1}", word, (isEndWord ? "(EndWord)" : ""));
							else
								Logger.WarnFormat("Word '{0}' is already existing in database.", word);
							WordCount++;
						}

					using (SqliteDataReader reader2 = new SqliteCommand("SELECT * FROM endword_list", externalDBConnection).ExecuteReader())
						while (reader2.Read())
						{
							string endword = reader2["word_index"].ToString();
							if (AddEndWord(endword))
								Logger.InfoFormat("Added end-word '{0}'", endword);
							else
								Logger.WarnFormat("End-word '{0}' is already existing in database.", endword);
							EndWordCount++;
						}

					Logger.InfoFormat("DB Import Complete. ({0} Words / {1} EndWordNodes)", WordCount, EndWordCount);
					if (DBJobDone != null)
						DBJobDone(null, new DBJobArgs("데이터베이스 불러오기", $"{WordCount} 개의 단어 / {EndWordCount} 개의 한방 노드"));
				}
				catch (Exception ex)
				{
					Logger.Error("Failed to connect external DB", ex);
				}
			});
		}

		public static List<string> GetEndWordList()
		{
			var result = new List<string>();

			using (SqliteDataReader reader2 = new SqliteCommand("SELECT * FROM endword_list", DatabaseConnection).ExecuteReader())
				while (reader2.Read())
					result.Add(reader2["word_index"].ToString());
			Logger.Info(string.Format("Found Total {0} end word.", result.Count));
			return result;
		}

		private static int ExecuteCommand(string command)
		{
			try
			{
				return new SqliteCommand(command, DatabaseConnection).ExecuteNonQuery();
			}
			catch (Exception ex)
			{
				Logger.Error($"Failed to execute Sqlite query '{command}'", ex);
			}
			return -1;
		}

		public static int DeleteWord(string word)
		{
			Logger.Info("Delete '" + word + "' from db...");
			return ExecuteCommand("DELETE FROM word_list WHERE word = '" + word + "'");
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
					Logger.Info("Database Intergrity Check....\nIt will be very long task.");
					int dbTotalCount;

					int.TryParse(new SqliteCommand("SELECT COUNT(*) FROM word_list", DatabaseConnection).ExecuteScalar().ToString(), out dbTotalCount);

					Logger.InfoFormat("Database has Total {0} elements.", dbTotalCount);
					Logger.Info("Getting all elements from database..");
					int elementCount = 0;
					int DeduplicatedCount = 0;
					int RemovedCount = 0;
					int FixedCount = 0;
					var DeletionList = new List<string>();
					var WordIndexCorrection = new List<string>();
					var ReverseWordIndexCorrection = new List<string>();
					var KkutuIndexCorrection = new List<string>();
					var IsEndWordCorrection = new Dictionary<string, int>();
					Logger.Info("Opening _ChecksqlLiteConnection.");

					var _ChecksqlLiteConnection = new SqliteConnection("Data Source=" + _sqlLiteDBLocation);
					_ChecksqlLiteConnection.Open();

					// Deduplicate db
					// https://wiki.postgresql.org/wiki/Deleting_duplicates
					try
					{
						DeduplicatedCount = new SqliteCommand("DELETE FROM word_list WHERE _rowid_ IN (SELECT _rowid_ FROM (SELECT _rowid_, ROW_NUMBER() OVER w as rnum FROM word_list WINDOW w AS (PARTITION BY word ORDER BY _rowid_)) t WHERE t.rnum > 1);", _ChecksqlLiteConnection).ExecuteNonQuery();
						Logger.InfoFormat("Deduplicated {0} entries.", DeduplicatedCount);
					}
					catch (Exception ex)
					{
						Logger.Error("Word deduplication failed", ex);
					}

					// Check for errors
					using (SqliteDataReader reader = new SqliteCommand("SELECT * FROM word_list ORDER BY(word) DESC", _ChecksqlLiteConnection).ExecuteReader())
					{
						while (reader.Read())
						{
							elementCount++;
							string content = reader["word"].ToString();
							Logger.InfoFormat("Total {0} of {1} ({2})", dbTotalCount, elementCount, content);

							// Check word validity
							if (content.Length == 1 || int.TryParse(content[0].ToString(), out int _) || content[0] == '[' || content[0] == '-' || content[0] == '.' || content.Contains(" "))
							{
								Logger.Info("Not a valid word; Will be removed.");
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
								Logger.InfoFormat("Invaild Word Index; Will be fixed to '{0}'.", correctWordIndex);
								WordIndexCorrection.Add(content);
							}

							// Check ReverseWordIndex tag
							string correctReverseWordIndex = content.Last().ToString();
							if (correctReverseWordIndex != reader["reverse_word_index"].ToString())
							{
								Logger.InfoFormat("Invaild Reverse Word Index; Will be fixed to '{0}'.", correctReverseWordIndex);
								ReverseWordIndexCorrection.Add(content);
							}

							// Check KkutuIndex tag
							string correctKkutuIndex = content.Last().ToString();
							if (correctKkutuIndex != reader["kkutu_index"].ToString())
							{
								Logger.InfoFormat("Invaild Kkutu Index; Will be fixed to '{0}'.", correctKkutuIndex);
								KkutuIndexCorrection.Add(content);
							}

							// Check IsEndWord tag
							int CorrectIsEndWord = Convert.ToInt32(PathFinder.EndWordList.Contains(content.Last().ToString()));
							if (CorrectIsEndWord != Convert.ToInt32(reader["is_endword"].ToString()))
							{
								Logger.InfoFormat("Invaild Is_EndWord Tag; Will be fixed to '{0}'.", CorrectIsEndWord);
								IsEndWordCorrection.Add(content, CorrectIsEndWord);
							}
						}
					}

					// Start fixing
					foreach (string content in DeletionList)
					{
						RemovedCount++;
						Logger.InfoFormat("Removed '{0}' from database.", content);
						ExecuteCommand("DELETE FROM word_list WHERE word = '" + content + "'");
					}

					foreach (string content in WordIndexCorrection)
					{
						FixedCount++;

						string correctWordIndex = content.First().ToString();
						Logger.InfoFormat("Fixed word_index of '{0}' to '{1}'.", content, correctWordIndex);
						ExecuteCommand($"UPDATE word_list SET word_index = '{correctWordIndex}' WHERE word = '{content}';");
					}

					foreach (string content in ReverseWordIndexCorrection)
					{
						FixedCount++;

						string correctReverseWordIndex = content.Last().ToString();
						Logger.InfoFormat("Fixed reverse_word_index of '{0}' to '{1}'.", content, correctReverseWordIndex);
						ExecuteCommand($"UPDATE word_list SET reverse_word_index = '{correctReverseWordIndex}' WHERE word = '{content}';");
					}

					foreach (string content in KkutuIndexCorrection)
					{
						FixedCount++;

						string correctKkutuIndex = content.Length >= 2 ? content.Substring(0, 2) : content.First().ToString();
						Logger.InfoFormat("Fixed kkutu_index of '{0}' to '{1}'.", content, correctKkutuIndex);
						ExecuteCommand($"UPDATE word_list SET kkutu_index = '{correctKkutuIndex}' WHERE word = '{content}';");
					}

					foreach (var pair in IsEndWordCorrection)
					{
						FixedCount++;

						Logger.InfoFormat("Fixed is_endword of '{0}' to '{1}'.", pair.Key, pair.Value);
						ExecuteCommand($"UPDATE word_list SET is_endword = '{pair.Value}' WHERE word = '{pair.Key}';");
					}

					_ChecksqlLiteConnection.Close();

					Logger.InfoFormat("Total {0} / Removed {1} / Fixed {2}.", dbTotalCount, RemovedCount, FixedCount);
					Logger.Info("Database Check Completed.");

					if (DBJobDone != null)
						DBJobDone(null, new DBJobArgs("데이터베이스 검증", $"{RemovedCount} 개 항목 제거됨 / {FixedCount} 개 항목 수정됨"));
				}
				catch (Exception ex)
				{
					Logger.Error($"Exception while checking database", ex);
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

		public static List<PathFinder.PathObject> FindWord(CommonHandler.ResponsePresentedWord data, string missionChar, int endWord, GameMode mode)
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

			string index;
				//case GameMode.Typing_Battle:
				//	break;
				//case GameMode.All:
				//	break;
				//case GameMode.Free:
				//	break;
			switch (mode)
			{
				case GameMode.First_and_Last:
					index = "reverse_word_index";
					break;
				case GameMode.Kkutu:
					index = "kkutu_index";
					break;
				default:
					index = "word_index";
					break;
			}

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
			//Logger.InfoFormat("Query: {0}", query);
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

			ExecuteCommand($"INSERT INTO word_list(word_index, reverse_word_index, kkutu_index, word, is_endword) VALUES('{word.First()}', '{word.Last()}', '{(word.Length >= 2 ? word.Substring(0, 2) : word.First().ToString())}', '{word}', {_isEndWord})");
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
						Logger.Warn("Added reverse_word_index column");
					}
					catch (Exception ex)
					{
						Logger.Error($"Failed to add reverse_word_index", ex);
					}
				}

				if (!CheckColumnExistence("kkutu_index"))
				{
					try
					{
						new SqliteCommand("ALTER TABLE word_list ADD COLUMN kkutu_index CHAR(2) NOT NULL DEFAULT ' '", DatabaseConnection).ExecuteNonQuery();
						Logger.Warn("Added kkutu_index column");
					}
					catch (Exception ex)
					{
						Logger.Error($"Failed to add kkutu_index", ex);
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
				Logger.Error($"Failed to check {columnName} existence", ex);
			}

			return false;
		}


		private static void MakeTable(string tablename)
		{
			Logger.Info("Create Table : " + tablename);
			if (tablename == "word_list")
				ExecuteCommand("CREATE TABLE word_list (word VARCHAR(60) NOT NULL, word_index CHAR(1) NOT NULL, reverse_word_index CHAR(1) NOT NULL, kkutu_index CHAR(2) NOT NULL, is_endword TINYINT(1) NOT NULL);");
			else if (tablename == "endword_list")
				ExecuteCommand("CREATE TABLE endword_list (word_index CHAR(1) NOT NULL);");
		}

		private static bool CheckTable_Check(string tablename)
		{
			Logger.Info("Check Table : " + tablename);
			try
			{
				return int.TryParse(new SqliteCommand("SELECT COUNT(*) FROM sqlite_master WHERE name='" + tablename + "';", DatabaseConnection).ExecuteScalar().ToString(), out int i) && i > 0;
			}
			catch (Exception e2)
			{
				Logger.Info("Failed to Execute Check DB Table ' " + tablename + " ' : " + e2.ToString());
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
