using System;
using System.Collections.Generic;
using System.Configuration;
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
	public abstract class CommonDatabase : IDisposable
	{
		public static readonly ILog Logger = LogManager.GetLogger("DatabaseManager");

		public static EventHandler DBError;
		public static EventHandler DBJobStart;
		public static EventHandler DBJobDone;

		public CommonDatabase()
		{
		}

		public static CommonDatabase GetInstance(Configuration config)
		{
			switch (((DatabaseTypeSection)config.GetSection("dbtype")).Type.ToLowerInvariant())
			{
				case "mysql":
				case "mariadb":
					string mysqlConnectionString = ((MySQLSection)config.GetSection("mysql")).ConnectionString;
					Logger.InfoFormat("MySQL selected: {0}", mysqlConnectionString);
					return new MySQLDatabase(mysqlConnectionString);
				case "postgresql":
				case "pgsql":
					string pgsqlConnectionString = ((PostgreSQLSection)config.GetSection("postgresql")).ConnectionString;
					Logger.InfoFormat("PostgreSQL selected: {0}", pgsqlConnectionString);
					return new PostgreSQLDatabase(pgsqlConnectionString);
			}

			string file = ((SQLiteSection)config.GetSection("sqlite")).File;
			Logger.InfoFormat("SQLite selected: File={0}", file);
			return new SQLiteDatabase(file);
		}

		public List<string> GetNodeList(string tableName)
		{
			var result = new List<string>();

			using (CommonDatabaseReader reader = ExecuteReader($"SELECT * FROM {tableName}"))
				while (reader.Read())
					result.Add(reader.GetString(DatabaseConstants.WordIndexColumnName));
			Logger.InfoFormat("Found Total {0} nodes in {1}.", result.Count, tableName);
			return result;
		}

		public int DeleteWord(string word)
		{
			int count = ExecuteNonQuery($"DELETE FROM {DatabaseConstants.WordListTableName} WHERE {DatabaseConstants.WordColumnName} = '{word}'");
			if (count > 0)
				Logger.Info($"Deleted '{word}' from database");
			return count;
		}

		public bool AddNode(string node, string tableName = null)
		{
			if (string.IsNullOrWhiteSpace(tableName))
				tableName = DatabaseConstants.EndWordListTableName;

			if (string.IsNullOrWhiteSpace(node))
				throw new ArgumentNullException("node");

			if (Convert.ToInt32(ExecuteScalar($"SELECT COUNT(*) FROM {tableName} WHERE {DatabaseConstants.WordIndexColumnName} = '{node[0]}';")) > 0)
				return false;

			ExecuteNonQuery($"INSERT INTO {tableName}({DatabaseConstants.WordIndexColumnName}) VALUES('{node[0]}')");
			return true;
		}

		public bool AddNode(string node, NodeFlags types)
		{
			bool result = false;
			if (types.HasFlag(NodeFlags.EndWord))
				result = AddNode(node, DatabaseConstants.EndWordListTableName) || result;
			if (types.HasFlag(NodeFlags.AttackWord))
				result = AddNode(node, DatabaseConstants.AttackWordListTableName) || result;
			if (types.HasFlag(NodeFlags.ReverseEndWord))
				result = AddNode(node, DatabaseConstants.ReverseEndWordListTableName) || result;
			if (types.HasFlag(NodeFlags.ReverseAttackWord))
				result = AddNode(node, DatabaseConstants.ReverseAttackWordListTableName) || result;
			if (types.HasFlag(NodeFlags.KkutuEndWord))
				result = AddNode(node, DatabaseConstants.KkutuEndWordListTableName) || result;
			if (types.HasFlag(NodeFlags.KkutuAttackWord))
				result = AddNode(node, DatabaseConstants.KkutuAttackWordListTableName) || result;
			return result;
		}

		public int DeleteNode(string node, string tableName = null)
		{
			if (string.IsNullOrWhiteSpace(tableName))
				tableName = DatabaseConstants.EndWordListTableName;

			int count = ExecuteNonQuery($"DELETE FROM {tableName} WHERE {DatabaseConstants.WordIndexColumnName} = '{node}'");
			if (count > 0)
				Logger.Info($"Deleted '{node}' from {tableName}");
			return count;
		}

		public int DeleteNode(string node, NodeFlags types)
		{
			int count = 0;
			if (types.HasFlag(NodeFlags.EndWord))
				count += DeleteNode(node, DatabaseConstants.EndWordListTableName);
			if (types.HasFlag(NodeFlags.AttackWord))
				count += DeleteNode(node, DatabaseConstants.AttackWordListTableName);
			if (types.HasFlag(NodeFlags.ReverseEndWord))
				count += DeleteNode(node, DatabaseConstants.ReverseEndWordListTableName);
			if (types.HasFlag(NodeFlags.ReverseAttackWord))
				count += DeleteNode(node, DatabaseConstants.ReverseAttackWordListTableName);
			if (types.HasFlag(NodeFlags.KkutuEndWord))
				count += DeleteNode(node, DatabaseConstants.KkutuEndWordListTableName);
			if (types.HasFlag(NodeFlags.KkutuAttackWord))
				count += DeleteNode(node, DatabaseConstants.KkutuAttackWordListTableName);
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

					int dbTotalCount = Convert.ToInt32(ExecuteScalar($"SELECT COUNT(*) FROM {DatabaseConstants.WordListTableName}"));
					Logger.InfoFormat("Database has Total {0} elements.", dbTotalCount);
					Logger.Info("Getting all elements from database..");
					int elementCount = 0;
					int DeduplicatedCount = 0;
					int RemovedCount = 0;
					int FixedCount = 0;
					var DeletionList = new List<string>();
					var WordFixList = new Dictionary<string, string>();
					var WordIndexCorrection = new Dictionary<string, string>();
					var ReverseWordIndexCorrection = new Dictionary<string, string>();
					var KkutuIndexCorrection = new Dictionary<string, string>();
					var FlagCorrection = new Dictionary<string, (int, int)>();

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
						using (CommonDatabaseReader reader = ExecuteReader($"SELECT * FROM {DatabaseConstants.WordListTableName} ORDER BY({DatabaseConstants.WordColumnName}) DESC", auxiliaryConnection))
						{
							while (reader.Read())
							{
								elementCount++;
								string content = reader.GetString(DatabaseConstants.WordColumnName);
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
								string currentWordIndex = reader.GetString(DatabaseConstants.WordIndexColumnName);
								if (correctWordIndex != currentWordIndex)
								{
									Logger.InfoFormat("Invaild Word Index; Will be fixed to '{0}'.", correctWordIndex);
									WordIndexCorrection.Add(content, currentWordIndex);
								}

								// Check ReverseWordIndex tag
								string correctReverseWordIndex = content.Last().ToString();
								string currentReverseWordIndex = reader.GetString(DatabaseConstants.ReverseWordIndexColumnName);
								if (correctReverseWordIndex != currentReverseWordIndex)
								{
									Logger.InfoFormat("Invaild Reverse Word Index; Will be fixed to '{0}'.", correctReverseWordIndex);
									ReverseWordIndexCorrection.Add(content, currentReverseWordIndex);
								}

								// Check KkutuIndex tag
								string correctKkutuIndex = content.Length > 2 ? content.Substring(0, 2) : content.First().ToString();
								string currentKkutuIndex = reader.GetString(DatabaseConstants.KkutuWordIndexColumnName);
								if (correctKkutuIndex != currentKkutuIndex)
								{
									Logger.InfoFormat("Invaild Kkutu Index; Will be fixed to '{0}'.", correctKkutuIndex);
									KkutuIndexCorrection.Add(content, currentKkutuIndex);
								}

								WordFlags correctFlags = Utils.GetFlags(content);
								int correctFlagsI = (int)correctFlags;
								int currentFlags = reader.GetInt32(DatabaseConstants.FlagsColumnName);
								if (correctFlagsI != currentFlags)
								{
									Logger.InfoFormat("Invaild flags; Will be fixed to '{0}'.", correctFlags);
									FlagCorrection.Add(content, (currentFlags, correctFlagsI));
								}
							}
						}

						// Start fixing
						foreach (string content in DeletionList)
						{
							Logger.InfoFormat("Removed '{0}' from database.", content);
							ExecuteNonQuery($"DELETE FROM {DatabaseConstants.WordListTableName} WHERE {DatabaseConstants.WordColumnName} = '" + content + "'");
							RemovedCount++;
						}

						foreach (var pair in WordFixList)
						{
							Logger.InfoFormat("Fixed {0} from '{1}' to '{2}'.", DatabaseConstants.WordColumnName, pair.Key, pair.Value);
							ExecuteNonQuery($"UPDATE {DatabaseConstants.WordListTableName} SET {DatabaseConstants.WordColumnName} = '{pair.Value}' WHERE {DatabaseConstants.WordColumnName} = '{pair.Key}';");
							FixedCount++;
						}

						foreach (var pair in WordIndexCorrection)
						{
							string correctWordIndex = pair.Key.First().ToString();
							Logger.InfoFormat("Fixed {0} of '{1}': from '{2}' to '{3}'.", DatabaseConstants.WordIndexColumnName, pair.Key, pair.Value, correctWordIndex);
							ExecuteNonQuery($"UPDATE {DatabaseConstants.WordListTableName} SET {DatabaseConstants.WordIndexColumnName} = '{correctWordIndex}' WHERE {DatabaseConstants.WordColumnName} = '{pair.Key}';");
							FixedCount++;
						}

						foreach (var pair in ReverseWordIndexCorrection)
						{
							string correctReverseWordIndex = pair.Value.Last().ToString();
							Logger.InfoFormat("Fixed {0} of '{1}': from '{2}' to '{3}'.", DatabaseConstants.ReverseWordIndexColumnName, pair.Key, pair.Value, correctReverseWordIndex);
							ExecuteNonQuery($"UPDATE {DatabaseConstants.WordListTableName} SET {DatabaseConstants.ReverseWordIndexColumnName} = '{correctReverseWordIndex}' WHERE {DatabaseConstants.WordColumnName} = '{pair.Key}';");
							FixedCount++;
						}

						foreach (var pair in KkutuIndexCorrection)
						{
							string content = pair.Key;
							string correctKkutuIndex = content.Length > 2 ? content.Substring(0, 2) : content.First().ToString();
							Logger.InfoFormat("Fixed {0} of '{1}': from '{2}' to '{3}'.", DatabaseConstants.KkutuWordIndexColumnName, content, pair.Value, correctKkutuIndex);
							ExecuteNonQuery($"UPDATE {DatabaseConstants.WordListTableName} SET kkutu_index = '{correctKkutuIndex}' WHERE {DatabaseConstants.WordColumnName} = '{content}';");
							FixedCount++;
						}

						foreach (var pair in FlagCorrection)
						{
							Logger.InfoFormat("Fixed {0} of '{1}': from {2} to {3}.", DatabaseConstants.FlagsColumnName, pair.Key, (WordFlags)pair.Value.Item1, (WordFlags)pair.Value.Item2);
							ExecuteNonQuery($"UPDATE {DatabaseConstants.WordListTableName} SET flags = {pair.Value.Item2} WHERE {DatabaseConstants.WordColumnName} = '{pair.Key}';");
							FixedCount++;
						}

						Logger.Info("Executing vacuum...");
						PerformVacuum();
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
				ExecuteNonQuery($"DELETE FROM {DatabaseConstants.WordListTableName} WHERE {DatabaseConstants.WordColumnName} = '{i}'");
			return result;
		}

		private static string GetIndexColumnName(CommonHandler.ResponsePresentedWord presentedWord, GameMode mode)
		{
			switch (mode)
			{
				case GameMode.First_and_Last:
					return DatabaseConstants.ReverseWordIndexColumnName;
				case GameMode.Kkutu:
					if (presentedWord.Content.Length > 1) // TODO: 세 글자용 인덱스도 만들기
						return DatabaseConstants.KkutuWordIndexColumnName;
					break;
			}
			return DatabaseConstants.WordIndexColumnName;
		}

		public List<PathFinder.PathObject> FindWord(CommonHandler.ResponsePresentedWord data, string missionChar, PathFinderFlags flags, WordPreference wordPreference, GameMode mode)
		{
			var result = new List<PathFinder.PathObject>();
			string query = CreateQuery(data, missionChar, flags, wordPreference, mode);
			//Logger.InfoFormat("Query: {0}", query);
			using (CommonDatabaseReader reader = ExecuteReader(query))
				while (reader.Read())
				{
					string word = reader.GetString(DatabaseConstants.WordColumnName).ToString().Trim();
					result.Add(new PathFinder.PathObject(word, (WordFlags)reader.GetInt32(DatabaseConstants.FlagsColumnName), !string.IsNullOrWhiteSpace(missionChar) && word.Any(c => c == missionChar.First())));
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
				orderCondition = $"({auxiliaryOrderCondition} LENGTH({DatabaseConstants.WordColumnName}))";
			else
				orderCondition = $"({GetCheckMissionCharFuncName()}({DatabaseConstants.WordColumnName}, '{missionChar}') + {auxiliaryOrderCondition} LENGTH({DatabaseConstants.WordColumnName}))";

			if (mode == GameMode.All)
				condition = auxiliaryCondition = "";

			return $"SELECT * FROM {DatabaseConstants.WordListTableName} {condition} {auxiliaryCondition} ORDER BY {orderCondition} DESC LIMIT {DatabaseConstants.QueryResultLimit}";
		}

		public bool AddWord(string word, WordFlags flags)
		{
			if (string.IsNullOrWhiteSpace(word))
				throw new ArgumentNullException(nameof(word));

			if (Convert.ToInt32(ExecuteScalar($"SELECT COUNT(*) FROM {DatabaseConstants.WordListTableName} WHERE {DatabaseConstants.WordColumnName} = '{word}';")) > 0)
				return false;

			ExecuteNonQuery($"INSERT INTO {DatabaseConstants.WordListTableName}({DatabaseConstants.WordIndexColumnName}, {DatabaseConstants.ReverseWordIndexColumnName}, {DatabaseConstants.KkutuWordIndexColumnName}, {DatabaseConstants.WordColumnName}, {DatabaseConstants.FlagsColumnName}) VALUES('{word.First()}', '{word.Last()}', '{(word.Length >= 2 ? word.Substring(0, 2) : word.First().ToString())}', '{word}', {((int)flags)})");
			return true;
		}

		protected void CheckTable()
		{
			if (!IsTableExists(DatabaseConstants.WordListTableName))
				MakeTable(DatabaseConstants.WordListTableName);
			else
			{
				const string BackwardCompat = "[Backward-compatibility] ";
				bool needToCleanUp = false;
				// For backward compatibility
				if (!IsColumnExists(DatabaseConstants.ReverseWordIndexColumnName))
				{
					TryExecuteNonQuery($"add {DatabaseConstants.ReverseWordIndexColumnName}", $"ALTER TABLE {DatabaseConstants.WordListTableName} ADD COLUMN {DatabaseConstants.ReverseWordIndexColumnName} CHAR(1) NOT NULL DEFAULT ' '");
					Logger.Warn($"{BackwardCompat}Added {DatabaseConstants.ReverseWordIndexColumnName} column");
				}

				if (!IsColumnExists(DatabaseConstants.KkutuWordIndexColumnName))
				{
					TryExecuteNonQuery($"add {DatabaseConstants.KkutuWordIndexColumnName}", $"ALTER TABLE {DatabaseConstants.WordListTableName} ADD COLUMN {DatabaseConstants.KkutuWordIndexColumnName} CHAR(2) NOT NULL DEFAULT ' '");
					Logger.Warn($"{BackwardCompat}Added {DatabaseConstants.KkutuWordIndexColumnName} column");
				}

				if (IsColumnExists(DatabaseConstants.IsEndwordColumnName))
				{
					try
					{
						if (!IsColumnExists(DatabaseConstants.FlagsColumnName))
						{
							ExecuteNonQuery($"ALTER TABLE {DatabaseConstants.WordListTableName} ADD COLUMN {DatabaseConstants.FlagsColumnName} SMALLINT NOT NULL DEFAULT 0");
							ExecuteNonQuery($"UPDATE {DatabaseConstants.WordListTableName} SET {DatabaseConstants.FlagsColumnName} = CAST({DatabaseConstants.IsEndwordColumnName} AS SMALLINT)");
							Logger.Warn($"{BackwardCompat}Converted '{DatabaseConstants.IsEndwordColumnName}' into {DatabaseConstants.FlagsColumnName} column.");
						}

						DropWordListColumn(DatabaseConstants.IsEndwordColumnName);
						needToCleanUp = true;

						Logger.Warn($"{BackwardCompat}Dropped {DatabaseConstants.IsEndwordColumnName} column as it is no longer used.");
					}
					catch (Exception ex)
					{
						Logger.Error($"{BackwardCompat}Failed to add {DatabaseConstants.FlagsColumnName} column", ex);
					}
				}

				if (!IsColumnExists(DatabaseConstants.SequenceColumnName))
				{
					try
					{
						AddSequenceColumnToWordList();
						Logger.Warn($"{BackwardCompat}Added sequence column");
						needToCleanUp = true;
					}
					catch (Exception ex)
					{
						Logger.Error($"{BackwardCompat}Failed to add sequence column", ex);
					}
				}

				string kkutuindextype = GetColumnType(DatabaseConstants.KkutuWordIndexColumnName);
				if (kkutuindextype.Equals("CHAR(2)", StringComparison.InvariantCultureIgnoreCase) || kkutuindextype.Equals("character", StringComparison.InvariantCultureIgnoreCase))
				{
					ChangeWordListColumnType(DatabaseConstants.KkutuWordIndexColumnName, "VARCHAR(2)");
					Logger.Warn($"{BackwardCompat}Changed type of '{DatabaseConstants.KkutuWordIndexColumnName}' from CHAR(2) to VARCHAR(2)");
					needToCleanUp = true;
				}

				if (needToCleanUp)
				{
					Logger.Warn($"Executing vacuum...");
					PerformVacuum();
				}
			}

			CreateIndex(DatabaseConstants.WordListTableName, DatabaseConstants.WordIndexColumnName);
			CreateIndex(DatabaseConstants.WordListTableName, DatabaseConstants.ReverseWordIndexColumnName);
			CreateIndex(DatabaseConstants.WordListTableName, DatabaseConstants.KkutuWordIndexColumnName);

			MakeTableIfNotExists(DatabaseConstants.EndWordListTableName);
			MakeTableIfNotExists(DatabaseConstants.ReverseEndWordListTableName);
			MakeTableIfNotExists(DatabaseConstants.KkutuEndWordListTableName);
			MakeTableIfNotExists(DatabaseConstants.AttackWordListTableName);
			MakeTableIfNotExists(DatabaseConstants.ReverseAttackWordListTableName);
			MakeTableIfNotExists(DatabaseConstants.KkutuAttackWordListTableName);
		}

		private void MakeTableIfNotExists(string tableName)
		{
			if (!IsTableExists(tableName))
				MakeTable(tableName);
		}

		private void CreateIndex(string tableName, string columnName)
		{
			ExecuteNonQuery($"CREATE INDEX IF NOT EXISTS {columnName} ON {tableName} ({columnName})");
		}

		protected void MakeTable(string tablename)
		{
			Logger.Info("Create Table : " + tablename);
			string columnOptions;
			switch (tablename)
			{
				case DatabaseConstants.WordListTableName:
					columnOptions = GetWordListColumnOptions();
					break;
				case DatabaseConstants.KkutuEndWordListTableName:
					columnOptions = $"{DatabaseConstants.WordIndexColumnName} VARCHAR(2) NOT NULL";
					break;
				default:
					columnOptions = $"{DatabaseConstants.WordIndexColumnName} CHAR(1) NOT NULL";
					break;
			}
			ExecuteNonQuery($"CREATE TABLE {tablename} ({columnOptions});");
		}

		private bool GetWordIndexColumnName(GameMode gameMode, out string str)
		{
			switch (gameMode)
			{
				case GameMode.Last_and_First:
				case GameMode.Middle_and_First:
					str = DatabaseConstants.WordIndexColumnName;
					break;
				case GameMode.First_and_Last:
					str = DatabaseConstants.ReverseWordIndexColumnName;
					break;
				case GameMode.Kkutu:
					str = DatabaseConstants.KkutuWordIndexColumnName;
					break;
				default:
					str = null;
					return false;
			}

			return true;
		}

		protected int TryExecuteNonQuery(string action, string query, IDisposable connection = null)
		{
			try
			{
				return ExecuteNonQuery(query, connection);
			}
			catch (Exception ex)
			{
				Logger.Error($"Failed to {action}", ex);
			}
			return -1;
		}

		protected object TryExecuteScalar(string action, string query, IDisposable connection = null)
		{
			try
			{
				return ExecuteScalar(query, connection);
			}
			catch (Exception ex)
			{
				Logger.Error($"Failed to {action}", ex);
			}
			return null;
		}

		protected CommonDatabaseReader TryExecuteReader(string action, string query, IDisposable connection = null)
		{
			try
			{
				return ExecuteReader(query, connection);
			}
			catch (Exception ex)
			{
				Logger.Error($"Failed to {action}", ex);
			}
			return null;
		}


		public abstract string GetDBInfo();

		protected abstract int ExecuteNonQuery(string query, IDisposable connection = null);

		protected abstract object ExecuteScalar(string query, IDisposable connection = null);

		protected abstract CommonDatabaseReader ExecuteReader(string query, IDisposable connection = null);

		protected abstract string GetCheckMissionCharFuncName();

		protected abstract int DeduplicateDatabase(IDisposable connection);

		protected abstract IDisposable OpenSecondaryConnection();

		protected abstract bool IsColumnExists(string columnName, string tableName = null, IDisposable connection = null);

		public abstract bool IsTableExists(string tablename, IDisposable connection = null);

		protected abstract string GetWordListColumnOptions();
		protected abstract string GetColumnType(string columnName, string tableName = null, IDisposable connection = null);

		protected abstract void AddSequenceColumnToWordList();

		protected abstract void ChangeWordListColumnType(string columnName, string newType, string tableName = null, IDisposable connection = null);

		protected abstract void DropWordListColumn(string columnName);

		protected abstract void PerformVacuum();
		public abstract void Dispose();

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
