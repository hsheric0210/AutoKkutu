using AutoKkutu.Databases;
using log4net;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using static AutoKkutu.Constants;

namespace AutoKkutu
{
	public abstract class CommonDatabase : IDisposable
	{
		private const string BackwardCompatibilityModule = "[Backward-compatibility] ";

		public static readonly ILog Logger = LogManager.GetLogger(nameof(CommonDatabase));
		
		public static EventHandler<DBImportEventArgs> ImportStart;
		public static EventHandler<DBImportEventArgs> ImportDone;
		public static EventHandler DBError;

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

		private static string GetIndexColumnName(FindWordInfo opts)
		{
			switch (opts.Mode)
			{
				case GameMode.First_and_Last:
					return DatabaseConstants.ReverseWordIndexColumnName;
				case GameMode.Kkutu:
					// TODO: 세 글자용 인덱스도 만들기
					ResponsePresentedWord word = opts.Word;
					if (word.Content.Length == 2 || word.CanSubstitution && word.Substitution.Length == 2)
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
					return;
				case GameMode.Middle_and_First:
					endWordFlag = (int)WordFlags.MiddleEndWord;
					attackWordFlag = (int)WordFlags.MiddleAttackWord;
					return;
				case GameMode.Kkutu:
					endWordFlag = (int)WordFlags.KkutuEndWord;
					attackWordFlag = (int)WordFlags.KkutuAttackWord;
					return;
			}
			endWordFlag = (int)WordFlags.EndWord;
			attackWordFlag = (int)WordFlags.AttackWord;
		}

		private PathObjectFlags GetPathObjectFlags(GetPathObjectFlagsInfo info, out int missionCharCount)
		{
			WordFlags wordFlags = info.WordFlags;
			PathObjectFlags pathFlags = PathObjectFlags.None;
			if (wordFlags.HasFlag(info.EndWordFlag))
				pathFlags |= PathObjectFlags.EndWord;
			if (wordFlags.HasFlag(info.AttackWordFlag))
				pathFlags |= PathObjectFlags.AttackWord;

			var missionChar = info.MissionChar;
			if (!string.IsNullOrWhiteSpace(missionChar))
			{
				missionCharCount = info.Word.Count(c => c == missionChar.First());
				if (missionCharCount > 0)
					pathFlags |= PathObjectFlags.MissionWord;
			}
			else
				missionCharCount = 0;
			return pathFlags;
		}

		public List<PathObject> FindWord(FindWordInfo info)
		{
			var result = new List<PathObject>();
			GetOptimalWordFlags(info.Mode, out int endWordFlag, out int attackWordFlag);
			string query = CreateQuery(info, endWordFlag, attackWordFlag);
			//Logger.InfoFormat("Query: {0}", query);
			using (CommonDatabaseReader reader = ExecuteReader(query))
				while (reader.Read())
				{
					string word = reader.GetString(DatabaseConstants.WordColumnName).ToString().Trim();
					result.Add(new PathObject(word, GetPathObjectFlags(new GetPathObjectFlagsInfo
					{
						Word = word,
						WordFlags = (WordFlags)reader.GetInt32(DatabaseConstants.FlagsColumnName),
						MissionChar = info.MissionChar,
						EndWordFlag = (WordFlags)endWordFlag,
						AttackWordFlag = (WordFlags)attackWordFlag
					}, out int missionCharCount), missionCharCount));
				}
			return result;
		}

		private string CreateQuery(FindWordInfo info, int endWordFlag, int attackWordFlag)
		{
			string condition;
			var data = info.Word;
			string indexColumnName = GetIndexColumnName(info);
			if (data.CanSubstitution)
				condition = $"WHERE ({indexColumnName} = '{data.Content}' OR {indexColumnName} = '{data.Substitution}')";
			else
				condition = $"WHERE {indexColumnName} = '{data.Content}'";

			var opt = new PreferenceInfo { PathFinderFlags = info.PathFinderFlags, WordPreference = info.WordPreference, Condition = "", OrderCondition = "" };

			// 한방 단어
			ApplyPreference(PathFinderFlags.USING_END_WORD, endWordFlag, DatabaseConstants.EndWordIndexPriority, ref opt);

			// 공격 단어
			ApplyPreference(PathFinderFlags.USING_ATTACK_WORD, attackWordFlag, DatabaseConstants.AttackWordIndexPriority, ref opt);

			// 미션 단어
			string orderCondition;
			if (string.IsNullOrWhiteSpace(info.MissionChar))
				orderCondition = $"({opt.OrderCondition} LENGTH({DatabaseConstants.WordColumnName}))";
			else
				orderCondition = $"({GetCheckMissionCharFuncName()}({DatabaseConstants.WordColumnName}, '{info.MissionChar}') + {opt.OrderCondition} LENGTH({DatabaseConstants.WordColumnName}))";

			if (info.Mode == GameMode.All)
				condition = opt.Condition = "";

			return $"SELECT * FROM {DatabaseConstants.WordListTableName} {condition} {opt.Condition} ORDER BY {orderCondition} DESC LIMIT {DatabaseConstants.QueryResultLimit}";
		}

		private static void ApplyPreference(PathFinderFlags targetFlag, int flag, int targetPriority, ref PreferenceInfo opt)
		{
			if (!opt.PathFinderFlags.HasFlag(targetFlag))
				opt.Condition += $"AND (flags & {flag} = 0)";
			else if (opt.WordPreference == WordPreference.ATTACK_DAMAGE)
				opt.OrderCondition += $"(CASE WHEN (flags & {flag} != 0) THEN {targetPriority} ELSE 0 END) +";
		}

		public bool AddWord(string word, WordFlags flags)
		{
			if (string.IsNullOrWhiteSpace(word))
				throw new ArgumentNullException(nameof(word));

			if (Convert.ToInt32(ExecuteScalar($"SELECT COUNT(*) FROM {DatabaseConstants.WordListTableName} WHERE {DatabaseConstants.WordColumnName} = '{word}';")) > 0)
				return false;

			ExecuteNonQuery($"INSERT INTO {DatabaseConstants.WordListTableName}({DatabaseConstants.WordIndexColumnName}, {DatabaseConstants.ReverseWordIndexColumnName}, {DatabaseConstants.KkutuWordIndexColumnName}, {DatabaseConstants.WordColumnName}, {DatabaseConstants.FlagsColumnName}) VALUES('{DatabaseUtils.GetLaFHeadNode(word)}', '{DatabaseUtils.GetFaLHeadNode(word)}', '{DatabaseUtils.GetKkutuHeadNode(word)}', '{word}', {((int)flags)})");
			return true;
		}

		protected void CheckTable()
		{
			// Create node tables
			foreach (var tableName in new string[] { DatabaseConstants.EndWordListTableName, DatabaseConstants.AttackWordListTableName, DatabaseConstants.ReverseEndWordListTableName, DatabaseConstants.ReverseAttackWordListTableName, DatabaseConstants.KkutuEndWordListTableName, DatabaseConstants.KkutuAttackWordListTableName })
				MakeTableIfNotExists(tableName);

			// Create word list table
			if (!IsTableExists(DatabaseConstants.WordListTableName))
				MakeTable(DatabaseConstants.WordListTableName);
			else
			{
				bool needToCleanUp = false;

				// Backward compatibility features
				AddInexistentColumns();
				needToCleanUp = DropIsEndWordColumn();
				needToCleanUp = AddSequenceColumn() || needToCleanUp;
				needToCleanUp = UpdateKkutuIndexDataType() || needToCleanUp;

				if (needToCleanUp)
				{
					Logger.Warn($"Executing vacuum...");
					PerformVacuum();
				}
			}

			// Create indexes
			foreach (var columnName in new string[] { DatabaseConstants.WordIndexColumnName, DatabaseConstants.ReverseWordIndexColumnName, DatabaseConstants.KkutuWordIndexColumnName })
				CreateIndex(DatabaseConstants.WordListTableName, columnName);
		}

		private bool UpdateKkutuIndexDataType()
		{
			string kkutuindextype = GetColumnType(DatabaseConstants.KkutuWordIndexColumnName);
			if (kkutuindextype.Equals("CHAR(2)", StringComparison.InvariantCultureIgnoreCase) || kkutuindextype.Equals("character", StringComparison.InvariantCultureIgnoreCase))
			{
				ChangeWordListColumnType(DatabaseConstants.KkutuWordIndexColumnName, "VARCHAR(2)");
				Logger.Warn($"{BackwardCompatibilityModule}Changed type of '{DatabaseConstants.KkutuWordIndexColumnName}' from CHAR(2) to VARCHAR(2)");
				return true;
			}

			return false;
		}

		private bool AddSequenceColumn()
		{
			if (!IsColumnExists(DatabaseConstants.SequenceColumnName))
			{
				try
				{
					AddSequenceColumnToWordList();
					Logger.Warn($"{BackwardCompatibilityModule}Added sequence column");
					return true;
				}
				catch (Exception ex)
				{
					Logger.Error($"{BackwardCompatibilityModule}Failed to add sequence column", ex);
				}
			}

			return false;
		}

		private bool DropIsEndWordColumn()
		{
			if (IsColumnExists(DatabaseConstants.IsEndwordColumnName))
			{
				try
				{
					if (!IsColumnExists(DatabaseConstants.FlagsColumnName))
					{
						ExecuteNonQuery($"ALTER TABLE {DatabaseConstants.WordListTableName} ADD COLUMN {DatabaseConstants.FlagsColumnName} SMALLINT NOT NULL DEFAULT 0");
						ExecuteNonQuery($"UPDATE {DatabaseConstants.WordListTableName} SET {DatabaseConstants.FlagsColumnName} = CAST({DatabaseConstants.IsEndwordColumnName} AS SMALLINT)");
						Logger.Warn($"{BackwardCompatibilityModule}Converted '{DatabaseConstants.IsEndwordColumnName}' into {DatabaseConstants.FlagsColumnName} column.");
					}

					DropWordListColumn(DatabaseConstants.IsEndwordColumnName);
					Logger.Warn($"{BackwardCompatibilityModule}Dropped {DatabaseConstants.IsEndwordColumnName} column as it is no longer used.");
					return true;
				}
				catch (Exception ex)
				{
					Logger.Error($"{BackwardCompatibilityModule}Failed to add {DatabaseConstants.FlagsColumnName} column", ex);
				}
			}

			return false;
		}

		private void AddInexistentColumns()
		{
			if (!IsColumnExists(DatabaseConstants.ReverseWordIndexColumnName))
			{
				TryExecuteNonQuery($"add {DatabaseConstants.ReverseWordIndexColumnName}", $"ALTER TABLE {DatabaseConstants.WordListTableName} ADD COLUMN {DatabaseConstants.ReverseWordIndexColumnName} CHAR(1) NOT NULL DEFAULT ' '");
				Logger.Warn($"{BackwardCompatibilityModule}Added {DatabaseConstants.ReverseWordIndexColumnName} column");
			}

			if (!IsColumnExists(DatabaseConstants.KkutuWordIndexColumnName))
			{
				TryExecuteNonQuery($"add {DatabaseConstants.KkutuWordIndexColumnName}", $"ALTER TABLE {DatabaseConstants.WordListTableName} ADD COLUMN {DatabaseConstants.KkutuWordIndexColumnName} CHAR(2) NOT NULL DEFAULT ' '");
				Logger.Warn($"{BackwardCompatibilityModule}Added {DatabaseConstants.KkutuWordIndexColumnName} column");
			}
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

		public abstract int ExecuteNonQuery(string query, IDisposable connection = null);

		public abstract object ExecuteScalar(string query, IDisposable connection = null);

		public abstract CommonDatabaseReader ExecuteReader(string query, IDisposable connection = null);

		protected abstract string GetCheckMissionCharFuncName();

		public abstract int DeduplicateDatabase(IDisposable connection);

		public abstract IDisposable OpenSecondaryConnection();

		protected abstract bool IsColumnExists(string columnName, string tableName = null, IDisposable connection = null);

		public abstract bool IsTableExists(string tablename, IDisposable connection = null);

		protected abstract string GetWordListColumnOptions();
		
		protected abstract string GetColumnType(string columnName, string tableName = null, IDisposable connection = null);

		protected abstract void AddSequenceColumnToWordList();

		protected abstract void ChangeWordListColumnType(string columnName, string newType, string tableName = null, IDisposable connection = null);

		protected abstract void DropWordListColumn(string columnName);

		public abstract void PerformVacuum();

		public abstract void Dispose();

		private struct PreferenceInfo
		{
			public PathFinderFlags PathFinderFlags;
			public WordPreference WordPreference;
			public string Condition;
			public string OrderCondition;
		}

		private struct GetPathObjectFlagsInfo
		{
			public string Word;
			public string MissionChar;
			public WordFlags WordFlags;
			public WordFlags EndWordFlag;
			public WordFlags AttackWordFlag;
		}
	}

	public class DBImportEventArgs : EventArgs
	{
		public string Name;
		public string Result;

		public DBImportEventArgs(string name) => Name = name;

		public DBImportEventArgs(string name, string result)
		{
			Name = name;
			Result = result;
		}
	}
}
