using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using log4net;
using Microsoft.Data.Sqlite;
using static AutoKkutu.Constants;

namespace AutoKkutu
{
	public class DatabaseManager
	{
		public static class DatabaseConstants
		{
			public const string DatabaseFileName = "path.sqlite";

			public const string WordListName = "word_list";
			public const string EndWordListName = "endword_list";
			public const string ReverseEndWordListName = "reverse_endword_list";
			public const string KkutuEndWordListName = "kkutu_endword_list";
			public const string AttackWordListName = "attackword_list";
			public const string ReverseAttackWordListName = "reverse_attackword_list";
			public const string KkutuAttackWordListName = "kkutu_attackword_list";
			public const int QueryResultLimit = 128;
		}

		public static string GetDBInfo() => "Offline Database (Sqlite)";

		private static readonly ILog Logger = LogManager.GetLogger("DatabaseManager");

		private static readonly bool _isInited = false;

		private static SqliteConnection DatabaseConnection;

		private static readonly string _sqliteDatabaseFilePath = $"{Environment.CurrentDirectory}\\{DatabaseConstants.DatabaseFileName}";

		public static EventHandler DBError;
		public static EventHandler DBJobStart;
		public static EventHandler DBJobDone;
		public static void Init()
		{
			if (!_isInited)
			{
				try
				{
					Logger.Info("_currentOperationMode = DBMethod.Local.");
					if (!new FileInfo(_sqliteDatabaseFilePath).Exists)
					{
						Logger.Info(_sqliteDatabaseFilePath + " does not exist. Create new file.");
						File.Create(_sqliteDatabaseFilePath).Close();
					}
					Logger.Info("Opening database connection.");
					DatabaseConnection = new SqliteConnection("Data Source=" + _sqliteDatabaseFilePath);
					DatabaseConnection.Open();
					DatabaseConnection.CreateFunction("checkMissionChar", (string str, string ch) =>
					{
						int occurrence = 0;
						char target = ch.First();
						foreach (char c in str.ToCharArray())
							if (c == target)
								occurrence++;
						// 데이터베이스 구조 상으로 256단어보다 긴 단어는 들어갈 수 없기에, 인덱스에 256 이상을 더해주면 항상 맨 위로 올라오게 된다
						return occurrence > 0 ? 256 + occurrence : 0;
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

					if (!CheckTable_Check(DatabaseConstants.WordListName))
					{
						Logger.InfoFormat("Database doesn't contain table '{0}'", DatabaseConstants.WordListName);
						return;
					}

					int WordCount = 0;
					int EndWordCount = 0;
					int ReverseEndWordCount = 0;
					int KkutuEndWordCount = 0;

					bool hasIsEndwordColumn = CheckColumnExistence("is_endword", dbConnection: externalDBConnection);

					using (SqliteDataReader reader = new SqliteCommand($"SELECT * FROM {DatabaseConstants.WordListName}", externalDBConnection).ExecuteReader())
						while (reader.Read())
						{
							string word = reader["word"].ToString().Trim();
							if (hasIsEndwordColumn)
							{
								// Legacy support
								bool isEndWord = Convert.ToBoolean(Convert.ToInt32(reader["is_endword"]));
								if (AddWord(word, isEndWord ? WordFlags.EndWord : WordFlags.None))
									Logger.InfoFormat("Imported word '{0}' {1}", word, (isEndWord ? "(EndWord)" : ""));
								else
									Logger.WarnFormat("Word '{0}' is already existing in database.", word);
							}
							else
							{
								int flags = Convert.ToInt32(reader["flags"]);
								if (AddWord(word, (WordFlags)flags))
									Logger.InfoFormat("Imported word '{0}' flags: {1}", word, flags);
								else
									Logger.WarnFormat("Word '{0}' is already existing in database.", word);
							}

							WordCount++;
						}

					if (CheckTable_Check(DatabaseConstants.EndWordListName))
						using (SqliteDataReader reader = new SqliteCommand($"SELECT * FROM {DatabaseConstants.EndWordListName}", externalDBConnection).ExecuteReader())
							while (reader.Read())
							{
								string endword = reader["word_index"].ToString();
								if (AddNode(endword))
									Logger.InfoFormat("Added end-word '{0}'", endword);
								else
									Logger.WarnFormat("End-word '{0}' is already existing in database.", endword);
								EndWordCount++;
							}

					if (CheckTable_Check(DatabaseConstants.ReverseEndWordListName))
						using (SqliteDataReader reader = new SqliteCommand($"SELECT * FROM {DatabaseConstants.ReverseEndWordListName}", externalDBConnection).ExecuteReader())
							while (reader.Read())
							{
								string endword = reader["word_index"].ToString();
								if (AddNode(endword, DatabaseConstants.ReverseEndWordListName))
									Logger.InfoFormat("Added reverse end-word '{0}'", endword);
								else
									Logger.WarnFormat("Reverse End-word '{0}' is already existing in database.", endword);
								ReverseEndWordCount++;
							}

					if (CheckTable_Check(DatabaseConstants.KkutuEndWordListName))
						using (SqliteDataReader reader = new SqliteCommand($"SELECT * FROM {DatabaseConstants.KkutuEndWordListName}", externalDBConnection).ExecuteReader())
							while (reader.Read())
							{
								string endword = reader["word_index"].ToString();
								if (AddNode(endword, DatabaseConstants.KkutuEndWordListName))
									Logger.InfoFormat("Added reverse end-word '{0}'", endword);
								else
									Logger.WarnFormat("Reverse End-word '{0}' is already existing in database.", endword);
								KkutuEndWordCount++;
							}

					Logger.InfoFormat("DB Import Complete. ({0} Words / {1} EndWord Nodes / {2} Reverse EndWord Nodes / {3} Kkutu EndWord Nodes)", WordCount, EndWordCount, ReverseEndWordCount, KkutuEndWordCount);
					if (DBJobDone != null)
						DBJobDone(null, new DBJobArgs("데이터베이스 불러오기", $"{WordCount} 개의 단어 / {EndWordCount} 개의 한방 노드 / {ReverseEndWordCount} 개의 앞말잇기 한방 노드 / {KkutuEndWordCount} 개의 끄투 한방 노드"));
				}
				catch (Exception ex)
				{
					Logger.Error("Failed to connect external DB", ex);
				}
			});
		}

		public static List<string> GetNodeList(string tableName)
		{
			var result = new List<string>();

			using (SqliteDataReader reader = new SqliteCommand($"SELECT * FROM {tableName}", DatabaseConnection).ExecuteReader())
				while (reader.Read())
					result.Add(reader["word_index"].ToString());
			Logger.InfoFormat("Found Total {0} nodes in {1}.", result.Count, tableName);
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
				Logger.Error($"Failed to execute SQLite query '{command}'", ex);
			}
			return -1;
		}

		public static int DeleteWord(string word)
		{
			int count = ExecuteCommand($"DELETE FROM {DatabaseConstants.WordListName} WHERE word = '{word}'");
			if (count > 0)
				Logger.Info($"Deleted '{word}' from database");
			return count;
		}

		public static bool AddNode(string node, string tableName = null)
		{
			if (string.IsNullOrWhiteSpace(tableName))
				tableName = DatabaseConstants.EndWordListName;

			if (string.IsNullOrWhiteSpace(node))
				throw new ArgumentNullException("node");

			if (int.TryParse(new SqliteCommand($"SELECT COUNT(*) FROM {tableName} WHERE word_index = '{node[0]}';", DatabaseConnection).ExecuteScalar().ToString(), out int i) && i > 0)
				return false;

			ExecuteCommand($"INSERT INTO {tableName}(word_index) VALUES('{node[0]}')");
			return true;
		}

		public static bool AddNode(string node, NodeFlags types)
		{
			bool result = false;
			if (types.HasFlag(NodeFlags.EndWord))
				result = AddNode(node, DatabaseConstants.EndWordListName) || result;
			if (types.HasFlag(NodeFlags.AttackWord))
				result = AddNode(node, DatabaseConstants.AttackWordListName) || result;
			if (types.HasFlag(NodeFlags.ReverseEndWord))
				result = AddNode(node, DatabaseConstants.ReverseEndWordListName) || result;
			if (types.HasFlag(NodeFlags.ReverseAttackWord))
				result = AddNode(node, DatabaseConstants.ReverseAttackWordListName) || result;
			if (types.HasFlag(NodeFlags.KkutuEndWord))
				result = AddNode(node, DatabaseConstants.KkutuEndWordListName) || result;
			if (types.HasFlag(NodeFlags.KkutuAttackWord))
				result = AddNode(node, DatabaseConstants.KkutuAttackWordListName) || result;
			return result;
		}

		public static int DeleteNode(string node, string tableName = null)
		{
			if (string.IsNullOrWhiteSpace(tableName))
				tableName = DatabaseConstants.EndWordListName;

			int count = ExecuteCommand($"DELETE FROM {tableName} WHERE word_index = '{node}'");
			if (count > 0)
				Logger.Info($"Deleted '{node}' from {tableName}");
			return count;
		}

		public static int DeleteNode(string node, NodeFlags types)
		{
			int count = 0;
			if (types.HasFlag(NodeFlags.EndWord))
				count += DeleteNode(node, DatabaseConstants.EndWordListName);
			if (types.HasFlag(NodeFlags.AttackWord))
				count += DeleteNode(node, DatabaseConstants.AttackWordListName);
			if (types.HasFlag(NodeFlags.ReverseEndWord))
				count += DeleteNode(node, DatabaseConstants.ReverseEndWordListName);
			if (types.HasFlag(NodeFlags.ReverseAttackWord))
				count += DeleteNode(node, DatabaseConstants.ReverseAttackWordListName);
			if (types.HasFlag(NodeFlags.KkutuEndWord))
				count += DeleteNode(node, DatabaseConstants.KkutuEndWordListName);
			if (types.HasFlag(NodeFlags.KkutuAttackWord))
				count += DeleteNode(node, DatabaseConstants.KkutuAttackWordListName);
			return count;
		}

		public static void CheckDB(bool UseOnlineDB)
		{
			if (UseOnlineDB && string.IsNullOrWhiteSpace(DatabaseManagement.EvaluateJS("document.getElementById('dict-output').style")))
			{
				MessageBox.Show("사전 창을 감지하지 못했습니다.\n끄투 사전 창을 여십시오.", "데이터베이스 관리자", MessageBoxButton.OK, MessageBoxImage.Warning);
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

					int.TryParse(new SqliteCommand($"SELECT COUNT(*) FROM {DatabaseConstants.WordListName}", DatabaseConnection).ExecuteScalar().ToString(), out dbTotalCount);

					Logger.InfoFormat("Database has Total {0} elements.", dbTotalCount);
					Logger.Info("Getting all elements from database..");
					int elementCount = 0;
					int DeduplicatedCount = 0;
					int RemovedCount = 0;
					int FixedCount = 0;
					var DeletionList = new List<string>();
					var WordFixList = new Dictionary<string, string>();
					var WordIndexCorrection = new List<string>();
					var ReverseWordIndexCorrection = new List<string>();
					var KkutuIndexCorrection = new List<string>();
					var FlagCorrection = new Dictionary<string, int>();
					Logger.Info("Opening _ChecksqlLiteConnection.");

					var _ChecksqlLiteConnection = new SqliteConnection("Data Source=" + _sqliteDatabaseFilePath);
					_ChecksqlLiteConnection.Open();

					// Deduplicate db
					// https://wiki.postgresql.org/wiki/Deleting_duplicates
					try
					{
						DeduplicatedCount = new SqliteCommand($"DELETE FROM {DatabaseConstants.WordListName} WHERE _rowid_ IN (SELECT _rowid_ FROM (SELECT _rowid_, ROW_NUMBER() OVER w as rnum FROM {DatabaseConstants.WordListName} WINDOW w AS (PARTITION BY word ORDER BY _rowid_)) t WHERE t.rnum > 1);", _ChecksqlLiteConnection).ExecuteNonQuery();
						Logger.InfoFormat("Deduplicated {0} entries.", DeduplicatedCount);
					}
					catch (Exception ex)
					{
						Logger.Error("Word deduplication failed", ex);
					}

					// Check for errors
					using (SqliteDataReader reader = new SqliteCommand($"SELECT * FROM {DatabaseConstants.WordListName} ORDER BY(word) DESC", _ChecksqlLiteConnection).ExecuteReader())
					{
						while (reader.Read())
						{
							elementCount++;
							string content = reader["word"].ToString();
							Logger.InfoFormat("Total {0} of {1} ('{2}')", dbTotalCount, elementCount, content);

							// Check word validity
							if (content.Length == 1 || int.TryParse(content[0].ToString(), out int _) || content[0] == '[' || content[0] == '-' || content[0] == '.' || content.Contains(" "))
							{
								Logger.Info("Not a valid word; Will be removed.");
								DeletionList.Add(content);
								continue;
							}

							if (content.StartsWith("(") && content.EndsWith(")"))
							{
								string fixedto = content.Substring(1, content.Length - 2);
								Logger.Info($"Word with parenthese; Will be fixed to {fixedto}");
								WordFixList.Add(content, fixedto);
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
							string correctKkutuIndex = content.Length > 2 ? content.Substring(0, 2) : content.First().ToString();
							if (correctKkutuIndex != reader["kkutu_index"].ToString())
							{
								Logger.InfoFormat("Invaild Kkutu Index; Will be fixed to '{0}'.", correctKkutuIndex);
								KkutuIndexCorrection.Add(content);
							}

							int correctFlags = (int)Utils.GetFlags(content);
							if (correctFlags != Convert.ToInt32(reader["flags"]))
							{
								Logger.InfoFormat("Invaild flags; Will be fixed to '{0}'.", correctFlags);
								FlagCorrection.Add(content, correctFlags);
							}
						}
					}

					// Start fixing
					foreach (string content in DeletionList)
					{
						RemovedCount++;
						Logger.InfoFormat("Removed '{0}' from database.", content);
						ExecuteCommand($"DELETE FROM {DatabaseConstants.WordListName} WHERE word = '" + content + "'");
					}

					foreach (var pair in WordFixList)
					{
						FixedCount++;

						Logger.InfoFormat("Fixed word from '{0}' to '{1}'.", pair.Key, pair.Value);
						ExecuteCommand($"UPDATE {DatabaseConstants.WordListName} SET word = '{pair.Value}' WHERE word = '{pair.Key}';");
					}

					foreach (string content in WordIndexCorrection)
					{
						FixedCount++;

						string correctWordIndex = content.First().ToString();
						Logger.InfoFormat("Fixed word_index of '{0}' to '{1}'.", content, correctWordIndex);
						ExecuteCommand($"UPDATE {DatabaseConstants.WordListName} SET word_index = '{correctWordIndex}' WHERE word = '{content}';");
					}

					foreach (string content in ReverseWordIndexCorrection)
					{
						FixedCount++;

						string correctReverseWordIndex = content.Last().ToString();
						Logger.InfoFormat("Fixed reverse_word_index of '{0}' to '{1}'.", content, correctReverseWordIndex);
						ExecuteCommand($"UPDATE {DatabaseConstants.WordListName} SET reverse_word_index = '{correctReverseWordIndex}' WHERE word = '{content}';");
					}

					foreach (string content in KkutuIndexCorrection)
					{
						FixedCount++;

						string correctKkutuIndex = content.Length > 2 ? content.Substring(0, 2) : content.First().ToString();
						Logger.InfoFormat("Fixed kkutu_index of '{0}' to '{1}'.", content, correctKkutuIndex);
						ExecuteCommand($"UPDATE {DatabaseConstants.WordListName} SET kkutu_index = '{correctKkutuIndex}' WHERE word = '{content}';");
					}

					foreach (var pair in FlagCorrection)
					{
						FixedCount++;

						Logger.InfoFormat("Fixed flags of '{0}' to '{1}'.", pair.Key, pair.Value);
						ExecuteCommand($"UPDATE {DatabaseConstants.WordListName} SET flags = {pair.Value} WHERE word = '{pair.Key}';");
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
				ExecuteCommand($"DELETE FROM {DatabaseConstants.WordListName} WHERE word = '" + i + "'");
			return result;
		}

		private static string GetIndexColumnName(CommonHandler.ResponsePresentedWord presentedWord, GameMode mode)
		{
			switch (mode)
			{
				case GameMode.First_and_Last:
					return "reverse_word_index";
				case GameMode.Kkutu:
					if (presentedWord.Content.Length > 1) // TODO: 세 글자용 인덱스도 만들기
						return "kkutu_index";
					break;
			}
			return "word_index";
		}

		public static List<PathFinder.PathObject> FindWord(CommonHandler.ResponsePresentedWord data, string missionChar, PathFinderFlags flags, WordPreference wordPreference, GameMode mode)
		{
			var result = new List<PathFinder.PathObject>();
			string query = CreateQuery(data, missionChar, flags, wordPreference, mode);
			//Logger.InfoFormat("Query: {0}", query);
			using (SqliteDataReader reader2 = new SqliteCommand(query, DatabaseConnection).ExecuteReader())
				while (reader2.Read())
					result.Add(new PathFinder.PathObject(reader2["word"].ToString().Trim(), (WordFlags)Convert.ToInt32(reader2["flags"])));
			return result;
		}

		private static string CreateQuery(CommonHandler.ResponsePresentedWord data, string missionChar, PathFinderFlags flags, WordPreference wordPreference, GameMode mode)
		{
			string indexColumnName = GetIndexColumnName(data, mode);
			string condition;
			if (data.CanSubstitution)
				condition = $"WHERE ({indexColumnName} = '{data.Content}' OR {indexColumnName} = '{data.Substitution}')";
			else
				condition = $"WHERE {indexColumnName} = '{data.Content}'";

			string auxiliaryCondition = "";
			string auxiliaryOrderCondition = "";

			int endWordFlag;
			int attackWordFlag;
			switch (mode)
			{
				case GameMode.First_and_Last:
					endWordFlag = (int)WordFlags.ReverseEndWord;
					attackWordFlag = (int)WordFlags.ReverseAttackWord;
					break;
				case GameMode.Middle_and_First:
					endWordFlag = (int)WordFlags.MiddleEndWord;
					attackWordFlag = (int)WordFlags.MiddleAttackWord;
					break;
				case GameMode.Kkutu:
					endWordFlag = (int)WordFlags.KkutuEndWord;
					attackWordFlag = (int)WordFlags.KkutuAttackWord;
					break;
				default:
					endWordFlag = (int)WordFlags.EndWord;
					attackWordFlag = (int)WordFlags.AttackWord;
					break;
			}

			// 한방 단어
			if (!flags.HasFlag(PathFinderFlags.USING_END_WORD))
				auxiliaryCondition = $"AND (flags & {endWordFlag} = 0)";
			else if (wordPreference == WordPreference.ATTACK_DAMAGE)
				auxiliaryOrderCondition = $"(CASE WHEN (flags & {endWordFlag} != 0) THEN 768 ELSE 0 END) +";

			// 공격 단어
			if (!flags.HasFlag(PathFinderFlags.USING_ATTACK_WORD))
				auxiliaryCondition = $"AND (flags & {attackWordFlag} = 0)";
			else if (wordPreference == WordPreference.ATTACK_DAMAGE)
				auxiliaryOrderCondition = $"(CASE WHEN (flags & {attackWordFlag} != 0) THEN 512 ELSE 0 END) +";

			// 미션 단어
			string orderCondition;
			if (string.IsNullOrWhiteSpace(missionChar))
				orderCondition = $"({auxiliaryOrderCondition} LENGTH(word))";
			else
				orderCondition = $"(checkMissionChar(word, '{missionChar}') + {auxiliaryOrderCondition} LENGTH(word))";

			if (mode == GameMode.All)
				condition = auxiliaryCondition = "";

			return $"SELECT * FROM {DatabaseConstants.WordListName} {condition} {auxiliaryCondition} ORDER BY {orderCondition} DESC LIMIT {DatabaseConstants.QueryResultLimit}";
		}

		public static bool AddWord(string word, WordFlags flags)
		{
			if (string.IsNullOrWhiteSpace(word))
				throw new ArgumentNullException("word");

			if (int.TryParse(new SqliteCommand($"SELECT COUNT(*) FROM {DatabaseConstants.WordListName} WHERE word = '{word}';", DatabaseConnection).ExecuteScalar().ToString(), out int i) && i > 0)
				return false;

			ExecuteCommand($"INSERT INTO {DatabaseConstants.WordListName}(word_index, reverse_word_index, kkutu_index, word, flags) VALUES('{word.First()}', '{word.Last()}', '{(word.Length >= 2 ? word.Substring(0, 2) : word.First().ToString())}', '{word}', {((int)flags)})");
			return true;
		}

		private static void CheckTable()
		{
			if (!CheckTable_Check(DatabaseConstants.WordListName))
				MakeTable(DatabaseConstants.WordListName);
			else
			{
				// For backward compatibility
				if (!CheckColumnExistence("reverse_word_index"))
				{
					try
					{
						new SqliteCommand($"ALTER TABLE {DatabaseConstants.WordListName} ADD COLUMN reverse_word_index CHAR(1) NOT NULL DEFAULT ' '", DatabaseConnection).ExecuteNonQuery();
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
						new SqliteCommand($"ALTER TABLE {DatabaseConstants.WordListName} ADD COLUMN kkutu_index CHAR(2) NOT NULL DEFAULT ' '", DatabaseConnection).ExecuteNonQuery();
						Logger.Warn("Added kkutu_index column");
					}
					catch (Exception ex)
					{
						Logger.Error($"Failed to add kkutu_index", ex);
					}
				}

				if (CheckColumnExistence("is_endword"))
				{
					try
					{
						if (!CheckColumnExistence("flags"))
						{
							new SqliteCommand($"ALTER TABLE {DatabaseConstants.WordListName} ADD COLUMN flags SMALLINT NOT NULL DEFAULT 0", DatabaseConnection).ExecuteNonQuery();
							Logger.Warn("Added flags column.");
							new SqliteCommand($"UPDATE {DatabaseConstants.WordListName} SET flags = CAST(is_endword AS SMALLINT)", DatabaseConnection).ExecuteNonQuery();
							Logger.Warn("Converted is_endword column into flags column.");
						}

						// We can't drop a column from table with single query, as yet.
						new SqliteCommand($"ALTER TABLE {DatabaseConstants.WordListName} RENAME TO _{DatabaseConstants.WordListName};", DatabaseConnection).ExecuteNonQuery();
						MakeTable(DatabaseConstants.WordListName);
						new SqliteCommand($"INSERT INTO {DatabaseConstants.WordListName} (word, word_index, reverse_word_index, kkutu_index, flags) SELECT word, word_index, reverse_word_index, kkutu_index, flags FROM _{DatabaseConstants.WordListName};", DatabaseConnection).ExecuteNonQuery();
						new SqliteCommand($"DROP TABLE _{DatabaseConstants.WordListName};", DatabaseConnection).ExecuteNonQuery();

						Logger.Warn("Dropped is_endword column as it is no longer used.");
					}
					catch (Exception ex)
					{
						Logger.Error($"Failed to add flags", ex);
					}
				}
			}

			if (!CheckTable_Check(DatabaseConstants.EndWordListName))
				MakeTable(DatabaseConstants.EndWordListName);
			if (!CheckTable_Check(DatabaseConstants.ReverseEndWordListName))
				MakeTable(DatabaseConstants.ReverseEndWordListName);
			if (!CheckTable_Check(DatabaseConstants.KkutuEndWordListName))
				MakeTable(DatabaseConstants.KkutuEndWordListName);
			if (!CheckTable_Check(DatabaseConstants.AttackWordListName))
				MakeTable(DatabaseConstants.AttackWordListName);
			if (!CheckTable_Check(DatabaseConstants.ReverseAttackWordListName))
				MakeTable(DatabaseConstants.ReverseAttackWordListName);
			if (!CheckTable_Check(DatabaseConstants.KkutuAttackWordListName))
				MakeTable(DatabaseConstants.KkutuAttackWordListName);
		}

		private static bool CheckColumnExistence(string columnName, string tableName = null, SqliteConnection dbConnection = null)
		{
			try
			{
				using (SqliteDataReader reader = new SqliteCommand($"PRAGMA table_info({tableName ?? DatabaseConstants.WordListName})", dbConnection ?? DatabaseConnection).ExecuteReader())
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
			string columnOptions;
			switch (tablename)
			{
				case DatabaseConstants.WordListName:
					columnOptions = "(word VARCHAR(256) NOT NULL, word_index CHAR(1) NOT NULL, reverse_word_index CHAR(1) NOT NULL, kkutu_index CHAR(2) NOT NULL, flags SMALLINT NOT NULL)";
					break;
				case DatabaseConstants.KkutuEndWordListName:
					columnOptions = "(word_index CHAR(2) NOT NULL)";
					break;
				default:
					columnOptions = "(word_index CHAR(1) NOT NULL)";
					break;
			}
			ExecuteCommand($"CREATE TABLE {tablename} {columnOptions};");
		}

		private static bool CheckTable_Check(string tablename)
		{
			Logger.InfoFormat("Check Table : {0}", tablename);
			try
			{
				return int.TryParse(new SqliteCommand($"SELECT COUNT(*) FROM sqlite_master WHERE name='{tablename}';", DatabaseConnection).ExecuteScalar().ToString(), out int i) && i > 0;
			}
			catch (Exception ex)
			{
				Logger.Info($"Failed to Execute Check DB Table '{tablename}' : {ex.ToString()}");
				return false;
			}
		}

		private bool GetWordIndexColumnName(GameMode gameMode, out string str)
		{
			switch (gameMode)
			{
				case GameMode.Last_and_First:
				case GameMode.Middle_and_First:
					str = "word_index";
					break;
				case GameMode.First_and_Last:
					str = "reverse_word_index";
					break;
				case GameMode.Kkutu:
					str = "kkutu_index";
					break;
				default:
					str = null;
					return false;
			}

			return true;
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
