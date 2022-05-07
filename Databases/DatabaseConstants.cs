namespace AutoKkutu.Databases
{
	public static class DatabaseConstants
	{
		public const string DatabaseFileName = "path.sqlite";

		public const string WordListTableName = "word_list";

		public const string EndWordListTableName = "endword_list";

		public const string ReverseEndWordListTableName = "reverse_endword_list";

		public const string KkutuEndWordListTableName = "kkutu_endword_list";

		public const string AttackWordListTableName = "attackword_list";

		public const string ReverseAttackWordListTableName = "reverse_attackword_list";

		public const string KkutuAttackWordListTableName = "kkutu_attackword_list";

		public const string SequenceColumnName = "seq";

		public const string WordColumnName = "word";

		public const string WordIndexColumnName = "word_index";

		public const string ReverseWordIndexColumnName = "reverse_word_index";

		public const string KkutuWordIndexColumnName = "kkutu_index";

		public const string FlagsColumnName = "flags";

		// @deprecated
		public const string IsEndwordColumnName = "is_endword";

		public const string LoadFromLocalSQLite = "SQLite 데이터베이스 불러오기";

		public const int QueryResultLimit = 128;

		// https://wiki.postgresql.org/wiki/Deleting_duplicates
		public static readonly string DeduplicationQuery = $"DELETE FROM {WordListTableName} WHERE seq IN (SELECT seq FROM (SELECT seq, ROW_NUMBER() OVER w as rnum FROM {WordListTableName} WINDOW w AS (PARTITION BY word ORDER BY seq)) t WHERE t.rnum > 1);";

		public const string ErrorConnect = "Failed to connect to the database";

		public const string ErrorIsTableExists = "Failed to check the existence of table '{0}'";

		public const string ErrorIsColumnExists = "Failed to check the existence of column '{0}' in table '{1}'";

		public const string ErrorGetColumnType = "Failed to get the data type of column '{0}' in table '{1}'";

		public const int MaxWordLength = 256;
		public const int MaxWordPlusMissionLength = 131072; // 256(Max db word length) * 256(Max mission char count per word)
	}
}
