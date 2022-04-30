using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using AutoKkutu.Databases;
using log4net;
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
					string mysqlConnectionString = ((MySQLSection)config.GetSection("mysql")).ConnectionString;
					Logger.InfoFormat("MySQL selected: {0}", mysqlConnectionString);
					return new MySQLDatabase(mysqlConnectionString);
				case "mariadb":
					string mariadbConnectionString = ((MySQLSection)config.GetSection("mysql")).ConnectionString;
					Logger.InfoFormat("MariaDB selected: {0}", mariadbConnectionString);
					return new MariaDBDatabase(mariadbConnectionString);
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

		/// <summary>
		/// 데이터베이스로부터 노드 목록 데이터를 읽어들입니다.
		/// </summary>
		/// <param name="tableName">노드 목록 데이터를 읽어올 데이터베이스 테이블 이름</param>
		/// <returns>읽어온 노드 목록</returns>
		public List<string> GetNodeList(string tableName)
		{
			var result = new List<string>();

			using (CommonDatabaseReader reader = ExecuteReader($"SELECT * FROM {tableName}"))
				while (reader.Read())
					result.Add(reader.GetString(DatabaseConstants.WordIndexColumnName));
			Logger.InfoFormat("Found Total {0} nodes in {1}.", result.Count, tableName);
			return result;
		}

		/// <summary>
		/// 데이터베이스에서 단어를 삭제합니다.
		/// </summary>
		/// <param name="word">삭제할 단어</param>
		/// <returns>삭제된 단어의 총 갯수</returns>
		public int DeleteWord(string word)
		{
			int count = ExecuteNonQuery($"DELETE FROM {DatabaseConstants.WordListTableName} WHERE {DatabaseConstants.WordColumnName} = '{word}'");
			if (count > 0)
				Logger.Info($"Deleted '{word}' from database");
			return count;
		}

		/// <summary>
		/// 데이터베이스에 노드를 추가합니다.
		/// </summary>
		/// <param name="node">추가할 노드</param>
		/// <param name="tableName">노드를 추가할 데이터베이스 테이블의 이름</param>
		/// <returns>데이터베이스에 추가된 노드의 총 갯수</returns>
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

		/// <summary>
		/// 데이터베이스에 노드를 추가합니다.
		/// </summary>
		/// <param name="node">추가할 노드</param>
		/// <param name="types">추가할 노드의 속성들</param>
		/// <returns>데이터베이스에 추가된 노드의 총 갯수</returns>
		public bool AddNode(string node, NodeFlags types)
		{
			bool result = false;

			// 한방 단어
			if (types.HasFlag(NodeFlags.EndWord))
				result = AddNode(node, DatabaseConstants.EndWordListTableName) || result;

			// 공격 단어
			if (types.HasFlag(NodeFlags.AttackWord))
				result = AddNode(node, DatabaseConstants.AttackWordListTableName) || result;

			// 앞말잇기 한방 단어
			if (types.HasFlag(NodeFlags.ReverseEndWord))
				result = AddNode(node, DatabaseConstants.ReverseEndWordListTableName) || result;

			// 앞말잇기 공격 단어
			if (types.HasFlag(NodeFlags.ReverseAttackWord))
				result = AddNode(node, DatabaseConstants.ReverseAttackWordListTableName) || result;

			// 끄투 한방 단어
			if (types.HasFlag(NodeFlags.KkutuEndWord))
				result = AddNode(node, DatabaseConstants.KkutuEndWordListTableName) || result;

			// 끄투 공격 단어
			if (types.HasFlag(NodeFlags.KkutuAttackWord))
				result = AddNode(node, DatabaseConstants.KkutuAttackWordListTableName) || result;

			return result;
		}

		/// <summary>
		/// 데이터베이스에서 노드를 삭제합니다.
		/// </summary>
		/// <param name="node">삭제할 노드</param>
		/// <param name="tableName">노드를 삭제할 데이터베이스 테이블의 이름</param>
		/// <returns>데이터베이스에서 삭제된 노드의 총 갯수</returns>
		public int DeleteNode(string node, string tableName = null)
		{
			if (string.IsNullOrWhiteSpace(tableName))
				tableName = DatabaseConstants.EndWordListTableName;

			int count = ExecuteNonQuery($"DELETE FROM {tableName} WHERE {DatabaseConstants.WordIndexColumnName} = '{node}'");
			if (count > 0)
				Logger.Info($"Deleted '{node}' from {tableName}");
			return count;
		}

		/// <summary>
		/// 데이터베이스에서 노드를 삭제합니다.
		/// </summary>
		/// <param name="node">삭제할 노드</param>
		/// <param name="types">삭제할 노드의 속성들</param>
		/// <returns>데이터베이스에서 삭제된 노드의 총 갯수</returns>
		public int DeleteNode(string node, NodeFlags types)
		{
			int count = 0;
			
			// 한방 단어
			if (types.HasFlag(NodeFlags.EndWord))
				count += DeleteNode(node, DatabaseConstants.EndWordListTableName);

			// 공격 단어
			if (types.HasFlag(NodeFlags.AttackWord))
				count += DeleteNode(node, DatabaseConstants.AttackWordListTableName);

			// 앞말잇기 한방 단어
			if (types.HasFlag(NodeFlags.ReverseEndWord))
				count += DeleteNode(node, DatabaseConstants.ReverseEndWordListTableName);

			// 앞말잇기 공격 단어
			if (types.HasFlag(NodeFlags.ReverseAttackWord))
				count += DeleteNode(node, DatabaseConstants.ReverseAttackWordListTableName);
			
			// 끄투 한방 단어
			if (types.HasFlag(NodeFlags.KkutuEndWord))
				count += DeleteNode(node, DatabaseConstants.KkutuEndWordListTableName);
			
			// 끄투 공격 단어
			if (types.HasFlag(NodeFlags.KkutuAttackWord))
				count += DeleteNode(node, DatabaseConstants.KkutuAttackWordListTableName);

			return count;
		}

		/// <summary>
		/// 데이터베이스의 무결성을 검증하고, 문제를 발견하면 수정합니다.
		/// </summary>
		/// <param name="UseOnlineDB">온라인 검사(끄투 사전을 통한 검사)를 진행하는지의 여부</param>
		public void CheckDB(bool UseOnlineDB)
		{
			if (UseOnlineDB && string.IsNullOrWhiteSpace(JSEvaluator.EvaluateJS("document.getElementById('dict-output').style")))
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

					var watch = new Stopwatch();

					int dbTotalCount = Convert.ToInt32(ExecuteScalar($"SELECT COUNT(*) FROM {DatabaseConstants.WordListTableName}"));
					Logger.InfoFormat("Database has Total {0} elements.", dbTotalCount);

					int CurrentElementIndex = 0, DeduplicatedCount = 0, RemovedCount = 0, FixedCount = 0;

					var DeletionList = new List<string>();
					Dictionary<string, string> WordFixList = new Dictionary<string, string>(), WordIndexCorrection = new Dictionary<string, string>(), ReverseWordIndexCorrection = new Dictionary<string, string>(), KkutuIndexCorrection = new Dictionary<string, string>();
					var FlagCorrection = new Dictionary<string, (int, int)>();

					Logger.Info("Opening auxiliary SQLite connection...");
					using (var auxiliaryConnection = OpenSecondaryConnection())
					{
						// Deduplicate
						DeduplicateDatabaseAndGetCount(auxiliaryConnection, ref DeduplicatedCount);

						// Refresh node lists
						RefreshNodeLists();

						// Check for errors
						using (CommonDatabaseReader reader = ExecuteReader($"SELECT * FROM {DatabaseConstants.WordListTableName} ORDER BY({DatabaseConstants.WordColumnName}) DESC", auxiliaryConnection))
						{
							Logger.Info("Searching problems...");

							watch.Start();
							while (reader.Read())
							{
								CurrentElementIndex++;
								string content = reader.GetString(DatabaseConstants.WordColumnName);
								Logger.InfoFormat("Total {0} of {1} ('{2}')", dbTotalCount, CurrentElementIndex, content);

								// Check word validity
								if (IsInvalid(content))
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
								CheckIndexColumn(reader, DatabaseConstants.WordIndexColumnName, Utils.GetLaFHeadNode, WordIndexCorrection);

								// Check ReverseWordIndex tag
								CheckIndexColumn(reader, DatabaseConstants.ReverseWordIndexColumnName, Utils.GetFaLHeadNode, ReverseWordIndexCorrection);

								// Check KkutuIndex tag
								CheckIndexColumn(reader, DatabaseConstants.KkutuWordIndexColumnName, Utils.GetKkutuHeadNode, KkutuIndexCorrection);

								// Check Flags
								CheckFlagsColumn(reader, FlagCorrection);
							}
							watch.Stop();
							Logger.InfoFormat("Done searching problems. Took {0}ms.", watch.ElapsedMilliseconds);
						}

						watch.Restart();

						// Start fixing
						DeleteElements(DeletionList, ref RemovedCount);
						FixIndex(WordFixList, DatabaseConstants.WordColumnName, null, ref FixedCount);
						FixIndex(WordIndexCorrection, DatabaseConstants.WordIndexColumnName, Utils.GetLaFHeadNode, ref FixedCount);
						FixIndex(ReverseWordIndexCorrection, DatabaseConstants.ReverseWordIndexColumnName, Utils.GetFaLHeadNode, ref FixedCount);
						FixIndex(KkutuIndexCorrection, DatabaseConstants.KkutuWordIndexColumnName, Utils.GetKkutuHeadNode, ref FixedCount);
						FixFlag(FlagCorrection, ref FixedCount);

						watch.Stop();
						Logger.InfoFormat("Done fixing problems. Took {0}ms.", watch.ElapsedMilliseconds);

						ExecuteVacuum();
					}

					Logger.InfoFormat("Database check completed: Total {0} / Removed {1} / Fixed {2}.", dbTotalCount, RemovedCount, FixedCount);

					if (DBJobDone != null)
						DBJobDone(null, new DBJobArgs("데이터베이스 무결성 검증", $"{RemovedCount} 개 항목 제거됨 / {FixedCount} 개 항목 수정됨"));
				}
				catch (Exception ex)
				{
					Logger.Error($"Exception while checking database", ex);
				}
			});
		}

		/// <summary>
		/// (지원되는 DBMS에 한해) Vacuum 작업을 실행합니다.
		/// </summary>
		private void ExecuteVacuum()
		{
			var watch = new Stopwatch();
			Logger.Info("Executing vacuum...");
			watch.Restart();
			PerformVacuum();
			watch.Stop();
			Logger.InfoFormat("Vacuum took {0}ms.", watch.ElapsedMilliseconds);
		}

		private void FixFlag(Dictionary<string, (int, int)> FlagCorrection, ref int FixedCount)
		{
			foreach (var pair in FlagCorrection)
			{
				Logger.InfoFormat("Fixed {0} of '{1}': from {2} to {3}.", DatabaseConstants.FlagsColumnName, pair.Key, (WordFlags)pair.Value.Item1, (WordFlags)pair.Value.Item2);
				ExecuteNonQuery($"UPDATE {DatabaseConstants.WordListTableName} SET flags = {pair.Value.Item2} WHERE {DatabaseConstants.WordColumnName} = '{pair.Key}';");
				FixedCount++;
			}
		}

		private void FixIndex(Dictionary<string, string> WordIndexCorrection, string indexColumnName, Func<string, string> correctIndexSupplier, ref int FixedCount)
		{
			foreach (var pair in WordIndexCorrection)
			{
				string correctWordIndex;
				if (correctIndexSupplier == null)
				{
					correctWordIndex = pair.Value;
					Logger.InfoFormat("Fixed {0}: from '{1}' to '{2}'.", indexColumnName, pair.Key, correctWordIndex);
				}
				else
				{
					correctWordIndex = correctIndexSupplier(pair.Key);
					Logger.InfoFormat("Fixed {0} of '{1}': from '{2}' to '{3}'.", indexColumnName, pair.Key, pair.Value, correctWordIndex);
				}
				ExecuteNonQuery($"UPDATE {DatabaseConstants.WordListTableName} SET {indexColumnName} = '{correctWordIndex}' WHERE {DatabaseConstants.WordColumnName} = '{pair.Key}';");
				FixedCount++;
			}
		}

		private void DeleteElements(IEnumerable<string> DeletionList, ref int RemovedCount)
		{
			foreach (string content in DeletionList)
			{
				Logger.InfoFormat("Removed '{0}' from database.", content);
				ExecuteNonQuery($"DELETE FROM {DatabaseConstants.WordListTableName} WHERE {DatabaseConstants.WordColumnName} = '" + content + "'");
				RemovedCount++;
			}
		}

		private static void CheckFlagsColumn(CommonDatabaseReader reader, Dictionary<string, (int, int)> FlagCorrection)
		{
			string content = reader.GetString(DatabaseConstants.WordColumnName);
			WordFlags correctFlags = Utils.GetFlags(content);
			int _correctFlags = (int)correctFlags;
			int currentFlags = reader.GetInt32(DatabaseConstants.FlagsColumnName);
			if (_correctFlags != currentFlags)
			{
				Logger.InfoFormat("Invaild flags; Will be fixed to '{0}'.", correctFlags);
				FlagCorrection.Add(content, (currentFlags, _correctFlags));
			}
		}

		private static void CheckIndexColumn(CommonDatabaseReader reader, string indexColumnName, Func<string, string> correctIndexSupplier, Dictionary<string, string> toBeCorrectedTo)
		{
			string content = reader.GetString(DatabaseConstants.WordColumnName);
			string correctWordIndex = correctIndexSupplier(content);
			string currentWordIndex = reader.GetString(indexColumnName);
			if (correctWordIndex != currentWordIndex)
			{
				Logger.InfoFormat("Invaild '{0}' column; Will be fixed to '{1}'.", indexColumnName, correctWordIndex);
				toBeCorrectedTo.Add(content, currentWordIndex);
			}
		}

		/// <summary>
		/// 단어가 올바른 단어인지, 유효하지 않은 문자를 포함하고 있진 않은지 검사합니다.
		/// </summary>
		/// <param name="content">검사할 단어</param>
		/// <returns>단어가 유효할 시 false, 그렇지 않을 경우 true</returns>
		private static bool IsInvalid(string content) => content.Length == 1 || int.TryParse(content[0].ToString(), out int _) || content[0] == '[' || content[0] == ')' || content[0] == '-' || content[0] == '.' || content.Contains(" ") || content.Contains(":");

		/// <summary>
		/// 단어 노드 목록들(한방 단어 노드 목록, 공격 단어 노드 목록 등)을 데이터베이스로부터 다시 로드합니다.
		/// </summary>
		private static void RefreshNodeLists()
		{
			var watch = new Stopwatch();
			watch.Start();
			Logger.Info("Updating node lists...");
			try
			{
				PathFinder.UpdateNodeLists();
				watch.Stop();
				Logger.InfoFormat("Done refreshing node lists. Took {0}ms.", watch.ElapsedMilliseconds);
			}
			catch (Exception ex)
			{
				Logger.Error($"Failed to refresh node lists", ex);
			}
		}

		private void DeduplicateDatabaseAndGetCount(IDisposable auxiliaryConnection, ref int DeduplicatedCount)
		{
			var watch = new Stopwatch();
			watch.Start();
			Logger.Info("Deduplicating entries...");
			try
			{
				DeduplicatedCount = DeduplicateDatabase(auxiliaryConnection);
				watch.Stop();
				Logger.InfoFormat("Removed {0} duplicate entries. Took {1}ms.", DeduplicatedCount, watch.ElapsedMilliseconds);
			}
			catch (Exception ex)
			{
				Logger.Error("Deduplication failed", ex);
			}
		}

		public void LoadFromExternalSQLite(string fileName) => SQLiteDatabaseHelper.LoadFromExternalSQLite(this, fileName);

		/// <summary>
		/// 끄투 사전 기능을 이용하여 단어가 해당 서버의 데이터베이스에 존재하는지 검사합니다.
		/// </summary>
		/// <param name="word">검사할 단어</param>
		/// <returns>해당 단어가 서버에서 사용할 수 있는지의 여부</returns>
		private bool CheckElementOnline(string word)
		{
			bool result = DatabaseManagement.KkutuOnlineDictCheck(word.Trim());
			if (!result)
				ExecuteNonQuery($"DELETE FROM {DatabaseConstants.WordListTableName} WHERE {DatabaseConstants.WordColumnName} = '{word}'");
			return result;
		}

		private static string GetIndexColumnName(CommonHandler.ResponsePresentedWord presentedWord, GameMode mode)
		{
			switch (mode)
			{
				case GameMode.First_and_Last:
					return DatabaseConstants.ReverseWordIndexColumnName;
				case GameMode.Kkutu:
					// TODO: 세 글자용 인덱스도 만들기
					if (presentedWord.Content.Length == 2 || presentedWord.CanSubstitution && presentedWord.Substitution.Length == 2)
						return DatabaseConstants.KkutuWordIndexColumnName;
					break;
			}
			return DatabaseConstants.WordIndexColumnName;
		}

		public void GetOptimalWordFlags(GameMode mode, out int endWordFlag, out int attackWordFlag)
		{
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
			}
			endWordFlag = (int)WordFlags.EndWord;
			attackWordFlag = (int)WordFlags.AttackWord;
		}

		public PathObjectFlags GetPathObjectFlags(string word, WordFlags wordFlags, int endWordFlag, int attackWordFlag, string missionChar, out int missionCharCount)
		{
			PathObjectFlags pathFlags = PathObjectFlags.None;
			if (wordFlags.HasFlag((WordFlags)endWordFlag))
				pathFlags |= PathObjectFlags.EndWord;
			if (wordFlags.HasFlag((WordFlags)attackWordFlag))
				pathFlags |= PathObjectFlags.AttackWord;

			missionCharCount = word.Count(c => c == missionChar.First());
			if (!string.IsNullOrWhiteSpace(missionChar) && missionCharCount > 0)
				pathFlags |= PathObjectFlags.MissionWord;
			return pathFlags;
		}

		public List<PathFinder.PathObject> FindWord(CommonHandler.ResponsePresentedWord data, string missionChar, PathFinderFlags findFlags, WordPreference wordPreference, GameMode mode)
		{
			var result = new List<PathFinder.PathObject>();
			GetOptimalWordFlags(mode, out int endWordFlag, out int attackWordFlag);
			string query = CreateQuery(data, missionChar, findFlags, wordPreference, mode, endWordFlag, attackWordFlag);
			//Logger.InfoFormat("Query: {0}", query);
			using (CommonDatabaseReader reader = ExecuteReader(query))
				while (reader.Read())
				{
					string word = reader.GetString(DatabaseConstants.WordColumnName).ToString().Trim();
					result.Add(new PathFinder.PathObject(word, GetPathObjectFlags(word, (WordFlags)reader.GetInt32(DatabaseConstants.FlagsColumnName), endWordFlag, attackWordFlag, missionChar, out int missionCharCount), missionCharCount));
				}
			return result;
		}

		private string CreateQuery(CommonHandler.ResponsePresentedWord data, string missionChar, PathFinderFlags flags, WordPreference wordPreference, GameMode mode, int endWordFlag, int attackWordFlag)
		{
			string indexColumnName = GetIndexColumnName(data, mode);
			string condition;
			if (data.CanSubstitution)
				condition = $"WHERE ({indexColumnName} = '{data.Content}' OR {indexColumnName} = '{data.Substitution}')";
			else
				condition = $"WHERE {indexColumnName} = '{data.Content}'";

			var opt = new PreferenceOptimize { FinderFlags = flags, Preference = wordPreference, Condition = "", OrderCondition = "" };

			// 한방 단어
			ApplyPreference(PathFinderFlags.USING_END_WORD, endWordFlag, DatabaseConstants.EndWordIndexPriority, ref opt);

			// 공격 단어
			ApplyPreference(PathFinderFlags.USING_ATTACK_WORD, attackWordFlag, DatabaseConstants.AttackWordIndexPriority, ref opt);

			// 미션 단어
			string orderCondition;
			if (string.IsNullOrWhiteSpace(missionChar))
				orderCondition = $"({opt.OrderCondition} LENGTH({DatabaseConstants.WordColumnName}))";
			else
				orderCondition = $"({GetCheckMissionCharFuncName()}({DatabaseConstants.WordColumnName}, '{missionChar}') + {opt.OrderCondition} LENGTH({DatabaseConstants.WordColumnName}))";

			if (mode == GameMode.All)
				condition = opt.Condition = "";

			return $"SELECT * FROM {DatabaseConstants.WordListTableName} {condition} {opt.Condition} ORDER BY {orderCondition} DESC LIMIT {DatabaseConstants.QueryResultLimit}";
		}

		private struct PreferenceOptimize
		{
			public PathFinderFlags FinderFlags;
			public WordPreference Preference;
			public string Condition;
			public string OrderCondition;
		}

		private static void ApplyPreference(PathFinderFlags targetFlag, int flag, int targetPriority, ref PreferenceOptimize opt)
		{
			if (!opt.FinderFlags.HasFlag(targetFlag))
				opt.Condition += $"AND (flags & {flag} = 0)";
			else if (opt.Preference == WordPreference.ATTACK_DAMAGE)
				opt.OrderCondition += $"(CASE WHEN (flags & {flag} != 0) THEN {targetPriority} ELSE 0 END) +";
		}

		public bool AddWord(string word, WordFlags flags)
		{
			if (string.IsNullOrWhiteSpace(word))
				throw new ArgumentNullException(nameof(word));

			if (Convert.ToInt32(ExecuteScalar($"SELECT COUNT(*) FROM {DatabaseConstants.WordListTableName} WHERE {DatabaseConstants.WordColumnName} = '{word}';")) > 0)
				return false;

			ExecuteNonQuery($"INSERT INTO {DatabaseConstants.WordListTableName}({DatabaseConstants.WordIndexColumnName}, {DatabaseConstants.ReverseWordIndexColumnName}, {DatabaseConstants.KkutuWordIndexColumnName}, {DatabaseConstants.WordColumnName}, {DatabaseConstants.FlagsColumnName}) VALUES('{Utils.GetLaFHeadNode(word)}', '{Utils.GetFaLHeadNode(word)}', '{Utils.GetKkutuHeadNode(word)}', '{word}', {((int)flags)})");
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

		public abstract string GetDBType();

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
