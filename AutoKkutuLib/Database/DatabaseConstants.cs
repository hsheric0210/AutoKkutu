namespace AutoKkutuLib.Database;

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

	public const string ThemeTableName = "theme";

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
	/// 끄투 단어 인덱스 열 이름
	/// </summary>
	public const string KkutuWordIndexColumnName = "kkutu_index";

	/// <summary>
	/// 단어 종류(명사, 부사, 감탄사, ...) 열 이름
	/// </summary>
	public const string TypeColumnName = "type";

	/// <summary>
	/// 단어 주제(일반, 교통, 음식, ...) 열 이름
	/// </summary>
	public const string ThemeColumn1Name = "theme_1";

	/// <summary>
	/// 단어 주제(일반, 교통, 음식, ...) 열 이름
	/// </summary>
	public const string ThemeColumn2Name = "theme_2";

	/// <summary>
	/// 단어 주제(일반, 교통, 음식, ...) 열 이름
	/// </summary>
	public const string ThemeColumn3Name = "theme_3";

	/// <summary>
	/// 단어 주제(일반, 교통, 음식, ...) 열 이름
	/// </summary>
	public const string ThemeColumn4Name = "theme_4";

	/// <summary>
	/// 단어 초성 열 이름
	/// </summary>
	public const string ChoseongColumnName = "choseong";

	/// <summary>
	/// 단어 뜻 열 이름
	/// </summary>
	public const string MeaningColumnName = "meaning";

	/// <summary>
	/// 단어 속성(플래그) 열 이름
	/// </summary>
	public const string FlagsColumnName = "flags";

	/// <summary>
	/// 한방 단어 여부 열 이름
	/// @deprecated: 단어 속성으로 대체됨
	/// </summary>
	public const string IsEndwordColumnName = "is_endword";

	public const string ThemeNameColumnName = "theme_name";

	public const string BitmaskOrdinalColumnName = "bitmask_ordinal";

	public const string BitmaskIndexColumnName = "bitmask_index";

	// FIXME: Move to resources
	public const string LoadFromLocalSQLite = "SQLite 데이터베이스 불러오기";

	public const string ErrorConnect = "Failed to connect to the database";

	public const string ErrorIsTableExists = "Failed to check the existence of table '{0}'";

	public const string ErrorIsColumnExists = "Failed to check the existence of column '{0}' in table '{1}'";

	public const string ErrorGetColumnType = "Failed to get the data type of column '{0}' in table '{1}'";

	public const int ThemeColumnCount = 4;
	public const int MaxWordLength = 256;
	public const int MaxWordPriorityLength = 131072; // 256(Max db word length) * 256(Max mission char count per word) * 2(For correct result)
}
