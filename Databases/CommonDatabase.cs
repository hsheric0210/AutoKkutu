using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using AutoKkutu.Databases;
using log4net;
using Microsoft.Data.Sqlite;
using static AutoKkutu.Constants;

namespace AutoKkutu
{
	public abstract class CommonDatabase
	{
		public const string LoadFromLocalSQLite = "SQLite 데이터베이스 불러오기";
		public static readonly ILog Logger = LogManager.GetLogger("DatabaseManager");

		public static EventHandler DBError;
		public static EventHandler DBJobStart;
		public static EventHandler DBJobDone;

		public CommonDatabase()
		{
		}

		public List<string> GetNodeList(string tableName)
		{
			var result = new List<string>();

			using (CommonDatabaseReader reader = ExecuteReader($"SELECT * FROM {tableName}"))
				while (reader.Read())
					result.Add(reader.GetObject("word_index").ToString());
			Logger.InfoFormat("Found Total {0} nodes in {1}.", result.Count, tableName);
			return result;
		}

		public int DeleteWord(string word)
		{
			int count = ExecuteNonQuery($"DELETE FROM {DatabaseConstants.WordListName} WHERE word = '{word}'");
			if (count > 0)
				Logger.Info($"Deleted '{word}' from database");
			return count;
		}

		public bool AddNode(string node, string tableName = null)
		{
			if (string.IsNullOrWhiteSpace(tableName))
				tableName = DatabaseConstants.EndWordListName;

			if (string.IsNullOrWhiteSpace(node))
				throw new ArgumentNullException("node");

			if (int.TryParse(ExecuteScalar($"SELECT COUNT(*) FROM {tableName} WHERE word_index = '{node[0]}';").ToString(), out int i) && i > 0)
				return false;

			ExecuteNonQuery($"INSERT INTO {tableName}(word_index) VALUES('{node[0]}')");
			return true;
		}

		public bool AddNode(string node, NodeFlags types)
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

		public int DeleteNode(string node, string tableName = null)
		{
			if (string.IsNullOrWhiteSpace(tableName))
				tableName = DatabaseConstants.EndWordListName;

			int count = ExecuteNonQuery($"DELETE FROM {tableName} WHERE word_index = '{node}'");
			if (count > 0)
				Logger.Info($"Deleted '{node}' from {tableName}");
			return count;
		}

		public int DeleteNode(string node, NodeFlags types)
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

		public void CheckDB(bool UseOnlineDB)
		{
			if (UseOnlineDB && string.IsNullOrWhiteSpace(DatabaseManagement.EvaluateJS("document.getElementById('dict-output').style")))
			{
				MessageBox.Show("사전 창을 감지하지 못했습니다.\n끄투 사전 창을 여십시오.", "데이터베이스 관리자", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			if (DBJobStart != null)
				DBJobStart(null, new DBJobArgs("데이터베이스 무결성 검증"));


			Task.Run(() =>
			{
				try
				{
					Logger.Info("Database Intergrity Check....\nIt will be very long task.");
					int dbTotalCount;

					int.TryParse(ExecuteScalar($"SELECT COUNT(*) FROM {DatabaseConstants.WordListName}").ToString(), out dbTotalCount);

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

					Logger.Info("Opening auxiliary SQLite connection...");
					using (var auxiliaryConnection = OpenSecondaryConnection())
					{
						try
						{
							DeduplicatedCount = DeduplicateDatabase(auxiliaryConnection);
							Logger.InfoFormat("Removed {0} duplicate entries.", DeduplicatedCount);
						}
						catch (Exception ex)
						{
							Logger.Error("Deduplication failed", ex);
						}

						// Check for errors
						using (CommonDatabaseReader reader = ExecuteReader($"SELECT * FROM {DatabaseConstants.WordListName} ORDER BY(word) DESC", auxiliaryConnection))
						{
							while (reader.Read())
							{
								elementCount++;
								string content = reader.GetObject("word").ToString();
								Logger.InfoFormat("Total {0} of {1} ('{2}')", dbTotalCount, elementCount, content);

								// Check word validity
								if (content.Length == 1 || int.TryParse(content[0].ToString(), out int _) || content[0] == '[' || content[0] == ')' || content[0] == '-' || content[0] == '.' || content.Contains(" ") || content.Contains(":"))
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
								if (correctWordIndex != reader.GetObject("word_index").ToString())
								{
									Logger.InfoFormat("Invaild Word Index; Will be fixed to '{0}'.", correctWordIndex);
									WordIndexCorrection.Add(content);
								}

								// Check ReverseWordIndex tag
								string correctReverseWordIndex = content.Last().ToString();
								if (correctReverseWordIndex != reader.GetObject("reverse_word_index").ToString())
								{
									Logger.InfoFormat("Invaild Reverse Word Index; Will be fixed to '{0}'.", correctReverseWordIndex);
									ReverseWordIndexCorrection.Add(content);
								}

								// Check KkutuIndex tag
								string correctKkutuIndex = content.Length > 2 ? content.Substring(0, 2) : content.First().ToString();
								if (correctKkutuIndex != reader.GetObject("kkutu_index").ToString())
								{
									Logger.InfoFormat("Invaild Kkutu Index; Will be fixed to '{0}'.", correctKkutuIndex);
									KkutuIndexCorrection.Add(content);
								}

								int correctFlags = (int)Utils.GetFlags(content);
								if (correctFlags != Convert.ToInt32(reader.GetObject("flags")))
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
							ExecuteNonQuery($"DELETE FROM {DatabaseConstants.WordListName} WHERE word = '" + content + "'");
						}

						foreach (var pair in WordFixList)
						{
							FixedCount++;

							Logger.InfoFormat("Fixed word from '{0}' to '{1}'.", pair.Key, pair.Value);
							ExecuteNonQuery($"UPDATE {DatabaseConstants.WordListName} SET word = '{pair.Value}' WHERE word = '{pair.Key}';");
						}

						foreach (string content in WordIndexCorrection)
						{
							FixedCount++;

							string correctWordIndex = content.First().ToString();
							Logger.InfoFormat("Fixed word_index of '{0}' to '{1}'.", content, correctWordIndex);
							ExecuteNonQuery($"UPDATE {DatabaseConstants.WordListName} SET word_index = '{correctWordIndex}' WHERE word = '{content}';");
						}

						foreach (string content in ReverseWordIndexCorrection)
						{
							FixedCount++;

							string correctReverseWordIndex = content.Last().ToString();
							Logger.InfoFormat("Fixed reverse_word_index of '{0}' to '{1}'.", content, correctReverseWordIndex);
							ExecuteNonQuery($"UPDATE {DatabaseConstants.WordListName} SET reverse_word_index = '{correctReverseWordIndex}' WHERE word = '{content}';");
						}

						foreach (string content in KkutuIndexCorrection)
						{
							FixedCount++;

							string correctKkutuIndex = content.Length > 2 ? content.Substring(0, 2) : content.First().ToString();
							Logger.InfoFormat("Fixed kkutu_index of '{0}' to '{1}'.", content, correctKkutuIndex);
							ExecuteNonQuery($"UPDATE {DatabaseConstants.WordListName} SET kkutu_index = '{correctKkutuIndex}' WHERE word = '{content}';");
						}

						foreach (var pair in FlagCorrection)
						{
							FixedCount++;

							Logger.InfoFormat("Fixed flags of '{0}' to '{1}'.", pair.Key, pair.Value);
							ExecuteNonQuery($"UPDATE {DatabaseConstants.WordListName} SET flags = {pair.Value} WHERE word = '{pair.Key}';");
						}

						Logger.InfoFormat("Execute vacuum", ExecuteNonQuery("VACUUM")); // Vacuum
					}

					Logger.InfoFormat("Total {0} / Removed {1} / Fixed {2}.", dbTotalCount, RemovedCount, FixedCount);
					Logger.Info("Database Check Completed.");

					if (DBJobDone != null)
						DBJobDone(null, new DBJobArgs("데이터베이스 무결성 검증", $"{RemovedCount} 개 항목 제거됨 / {FixedCount} 개 항목 수정됨"));
				}
				catch (Exception ex)
				{
					Logger.Error($"Exception while checking database", ex);
				}
			});
		}

		public void LoadFromExternalSQLite(string fileName) => SQLiteDatabaseHelper.LoadFromExternalSQLite(this, fileName);

		private bool CheckElementOnline(string i)
		{
			bool result = DatabaseManagement.KkutuOnlineDictCheck(i.Trim());
			if (!result)
				ExecuteNonQuery($"DELETE FROM {DatabaseConstants.WordListName} WHERE word = '{i}'");
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

		public List<PathFinder.PathObject> FindWord(CommonHandler.ResponsePresentedWord data, string missionChar, PathFinderFlags flags, WordPreference wordPreference, GameMode mode)
		{
			var result = new List<PathFinder.PathObject>();
			string query = CreateQuery(data, missionChar, flags, wordPreference, mode);
			//Logger.InfoFormat("Query: {0}", query);
			using (CommonDatabaseReader reader = ExecuteReader(query))
				while (reader.Read())
				{
					string word = reader.GetObject("word").ToString().Trim();
					result.Add(new PathFinder.PathObject(word, (WordFlags)Convert.ToInt32(reader.GetObject("flags")), !string.IsNullOrWhiteSpace(missionChar) && word.Any(c => c == missionChar.First()))); 
				}
			return result;
		}

		private string CreateQuery(CommonHandler.ResponsePresentedWord data, string missionChar, PathFinderFlags flags, WordPreference wordPreference, GameMode mode)
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
				auxiliaryCondition += $"AND (flags & {endWordFlag} = 0)";
			else if (wordPreference == WordPreference.ATTACK_DAMAGE)
				auxiliaryOrderCondition += $"(CASE WHEN (flags & {endWordFlag} != 0) THEN {DatabaseConstants.EndWordIndexPriority} ELSE 0 END) +";

			// 공격 단어
			if (!flags.HasFlag(PathFinderFlags.USING_ATTACK_WORD))
				auxiliaryCondition += $"AND (flags & {attackWordFlag} = 0)";
			else if (wordPreference == WordPreference.ATTACK_DAMAGE)
				auxiliaryOrderCondition += $"(CASE WHEN (flags & {attackWordFlag} != 0) THEN {DatabaseConstants.AttackWordIndexPriority} ELSE 0 END) +";

			// 미션 단어
			string orderCondition;
			if (string.IsNullOrWhiteSpace(missionChar))
				orderCondition = $"({auxiliaryOrderCondition} LENGTH(word))";
			else
				orderCondition = $"({GetCheckMissionCharFuncName()}(word, '{missionChar}') + {auxiliaryOrderCondition} LENGTH(word))";

			if (mode == GameMode.All)
				condition = auxiliaryCondition = "";

			return $"SELECT * FROM {DatabaseConstants.WordListName} {condition} {auxiliaryCondition} ORDER BY {orderCondition} DESC LIMIT {DatabaseConstants.QueryResultLimit}";
		}

		public bool AddWord(string word, WordFlags flags)
		{
			if (string.IsNullOrWhiteSpace(word))
				throw new ArgumentNullException("word");

			if (int.TryParse(ExecuteScalar($"SELECT COUNT(*) FROM {DatabaseConstants.WordListName} WHERE word = '{word}';").ToString(), out int i) && i > 0)
				return false;

			ExecuteNonQuery($"INSERT INTO {DatabaseConstants.WordListName}(word_index, reverse_word_index, kkutu_index, word, flags) VALUES('{word.First()}', '{word.Last()}', '{(word.Length >= 2 ? word.Substring(0, 2) : word.First().ToString())}', '{word}', {((int)flags)})");
			return true;
		}

		protected void CheckTable()
		{
			if (!IsTableExists(DatabaseConstants.WordListName))
				MakeTable(DatabaseConstants.WordListName);
			else
			{
				// For backward compatibility
				if (!IsColumnExists("reverse_word_index"))
				{
					try
					{
						ExecuteNonQuery($"ALTER TABLE {DatabaseConstants.WordListName} ADD COLUMN reverse_word_index CHAR(1) NOT NULL DEFAULT ' '");
						Logger.Warn("Added reverse_word_index column");
					}
					catch (Exception ex)
					{
						Logger.Error($"Failed to add reverse_word_index", ex);
					}
				}

				if (!IsColumnExists("kkutu_index"))
				{
					try
					{
						ExecuteNonQuery($"ALTER TABLE {DatabaseConstants.WordListName} ADD COLUMN kkutu_index CHAR(2) NOT NULL DEFAULT ' '");
						Logger.Warn("Added kkutu_index column");
					}
					catch (Exception ex)
					{
						Logger.Error($"Failed to add kkutu_index", ex);
					}
				}

				if (IsColumnExists("is_endword"))
				{
					try
					{
						if (!IsColumnExists("flags"))
						{
							ExecuteNonQuery($"ALTER TABLE {DatabaseConstants.WordListName} ADD COLUMN flags SMALLINT NOT NULL DEFAULT 0");
							Logger.Warn("Added flags column.");
							ExecuteNonQuery($"UPDATE {DatabaseConstants.WordListName} SET flags = CAST(is_endword AS SMALLINT)");
							Logger.Warn("Converted is_endword column into flags column.");
						}

						// We can't drop a column from table with single query, as yet.
						ExecuteNonQuery($"ALTER TABLE {DatabaseConstants.WordListName} RENAME TO _{DatabaseConstants.WordListName};");
						MakeTable(DatabaseConstants.WordListName);
						ExecuteNonQuery($"INSERT INTO {DatabaseConstants.WordListName} (word, word_index, reverse_word_index, kkutu_index, flags) SELECT word, word_index, reverse_word_index, kkutu_index, flags FROM _{DatabaseConstants.WordListName};");
						ExecuteNonQuery($"DROP TABLE _{DatabaseConstants.WordListName};");
						ExecuteNonQuery("VACUUM"); // Clean-up

						Logger.Warn("Dropped is_endword column as it is no longer used.");
					}
					catch (Exception ex)
					{
						Logger.Error($"Failed to add flags", ex);
					}
				}
			}

			if (!IsTableExists(DatabaseConstants.EndWordListName))
				MakeTable(DatabaseConstants.EndWordListName);
			if (!IsTableExists(DatabaseConstants.ReverseEndWordListName))
				MakeTable(DatabaseConstants.ReverseEndWordListName);
			if (!IsTableExists(DatabaseConstants.KkutuEndWordListName))
				MakeTable(DatabaseConstants.KkutuEndWordListName);
			if (!IsTableExists(DatabaseConstants.AttackWordListName))
				MakeTable(DatabaseConstants.AttackWordListName);
			if (!IsTableExists(DatabaseConstants.ReverseAttackWordListName))
				MakeTable(DatabaseConstants.ReverseAttackWordListName);
			if (!IsTableExists(DatabaseConstants.KkutuAttackWordListName))
				MakeTable(DatabaseConstants.KkutuAttackWordListName);
		}

		private void MakeTable(string tablename)
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
			ExecuteNonQuery($"CREATE TABLE {tablename} {columnOptions};");
		}

		public bool IsTableExists(string tablename)
		{
			Logger.InfoFormat("Check Table : {0}", tablename);
			try
			{
				return int.TryParse(ExecuteScalar($"SELECT COUNT(*) FROM sqlite_master WHERE name='{tablename}';").ToString(), out int i) && i > 0;
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

		public abstract bool IsColumnExists(string tableName, string columnName, IDisposable connection = null);

		public abstract string GetDBInfo();

		protected abstract int ExecuteNonQuery(string query, IDisposable connection = null);

		protected abstract object ExecuteScalar(string query, IDisposable connection = null);

		protected abstract CommonDatabaseReader ExecuteReader(string query, IDisposable connection = null);

		public abstract string GetCheckMissionCharFuncName();

		protected abstract int DeduplicateDatabase(IDisposable connection);

		protected abstract IDisposable OpenSecondaryConnection();

		protected abstract bool IsColumnExists(string columnName, string tableName = null);

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
