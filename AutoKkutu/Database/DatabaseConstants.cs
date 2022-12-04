namespace AutoKkutu.Database;

public static class DatabaseConstants
{
	public const string DefaultSQLiteDatabaseName = "path.sqlite";

	// Table names

	/// <summary>
	/// 단어 목록 테이블 이름
	/// </summary>
	public const string WordTableName = "word_list";

	/// <summary>
	/// 한방 단어 목록 테이블 이름
	/// </summary>
	public const string EndNodeIndexTableName = "endword_list";

	/// <summary>
	/// 앞말잇기 한방 단어 목록 테이블 이름
	/// </summary>
	public const string ReverseEndNodeIndexTableName = "reverse_endword_list";

	/// <summary>
	/// 끄투 한방 단어 목록 테이블 이름
	/// </summary>
	public const string KkutuEndNodeIndexTableName = "kkutu_endword_list";

	/// <summary>
	/// 쿵쿵따 한방 단어 목록 테이블 이름
	/// </summary>
	public const string KKTEndNodeIndexTableName = "kkt_endword_list";

	/// <summary>
	/// 공격 단어 목록 테이블 이름
	/// </summary>
	public const string AttackNodeIndexTableName = "attackword_list";

	/// <summary>
	/// 앞말잇기 공격 단어 목록 테이블 이름
	/// </summary>
	public const string ReverseAttackNodeIndexTableName = "reverse_attackword_list";

	/// <summary>
	/// 끄투 공격 단어 목록 테이블 이름
	/// </summary>
	public const string KkutuAttackNodeIndexTableName = "kkutu_attackword_list";

	/// <summary>
	/// 쿵쿵따 공격 단어 목록 테이블 이름
	/// </summary>
	public const string KKTAttackNodeIndexTableName = "kkt_attackword_list";

	// Column names

	/// <summary>
	/// 각 단어 고유 시퀀스 열 이름
	/// </summary>
	public const string SequenceColumnName = "seq";

	/// <summary>
	/// 단어 열 이름
	/// </summary>
	public const string WordColumnName = "word";

	/// <summary>
	/// 단어 인덱스 열 이름
	/// </summary>
	public const string WordIndexColumnName = "word_index";

	/// <summary>
	/// 앞말잇기 단어 인덱스 열 이름
	/// </summary>
	public const string ReverseWordIndexColumnName = "reverse_word_index";

	/// <summary>
	/// 끄투 단어인덱스 열 이름
	/// </summary>
	public const string KkutuWordIndexColumnName = "kkutu_index";

	/// <summary>
	/// 단어 속성(플래그) 열 이름
	/// </summary>
	public const string FlagsColumnName = "flags";

	/// <summary>
	/// 한방 단어 여부 열 이름
	/// @deprecated: 단어 속성으로 대체됨
	/// </summary>
	public const string IsEndwordColumnName = "is_endword";

	public const string LoadFromLocalSQLite = "SQLite 데이터베이스 불러오기";

	public const int QueryResultLimit = 128;

	// https://wiki.postgresql.org/wiki/Deleting_duplicates
	public static readonly string DeduplicationQuery = $"DELETE FROM {WordTableName} WHERE seq IN (SELECT seq FROM (SELECT seq, ROW_NUMBER() OVER w as rnum FROM {WordTableName} WINDOW w AS (PARTITION BY word ORDER BY seq)) t WHERE t.rnum > 1);";

	// Error messages
	// TODO: Move to resources

	public const string ErrorConnect = "Failed to connect to the database";

	public const string ErrorIsTableExists = "Failed to check the existence of table '{0}'";

	public const string ErrorIsColumnExists = "Failed to check the existence of column '{0}' in table '{1}'";

	public const string ErrorGetColumnType = "Failed to get the data type of column '{0}' in table '{1}'";

	public const int MaxWordLength = 256;
	public const int MaxWordPriorityLength = 131072; // 256(Max db word length) * 256(Max mission char count per word) * 2(For correct result)
}
