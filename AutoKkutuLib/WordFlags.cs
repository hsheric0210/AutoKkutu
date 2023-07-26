namespace AutoKkutuLib;

/// <summary>
/// 데이터베이스의 'flags' 행에 해당하는 비스마스크 플래그를 나타냅니다.
/// </summary>
[Flags]
public enum WordFlags
{
	None = 0,

	/// <summary>
	/// 한방 단어
	/// </summary>
	EndWord = 1 << 0,

	/// <summary>
	/// 공격 단어
	/// </summary>
	AttackWord = 1 << 1,

	/// <summary>
	/// 앞말잇기 한방 단어
	/// </summary>
	ReverseEndWord = 1 << 2,

	/// <summary>
	/// 앞말잇기 공격 단어
	/// </summary>
	ReverseAttackWord = 1 << 3,

	/// <summary>
	/// 가운뎃말잇기 한방 단어
	/// </summary>
	MiddleEndWord = 1 << 4,

	/// <summary>
	/// 가운뎃말잇기 공격 단어
	/// </summary>
	MiddleAttackWord = 1 << 5,

	/// <summary>
	/// 끄투 한방 단어
	/// </summary>
	KkutuEndWord = 1 << 6,

	/// <summary>
	/// 끄투 공격 단어
	/// </summary>
	KkutuAttackWord = 1 << 7,

	/// <summary>
	/// 쿵쿵따에서 사용 가능한 단어인지 여부
	/// </summary>
	KKT3 = 1 << 8,

	/// <summary>
	/// 쿵쿵따-2323에서 사용 가능한 단어인지 여부
	/// </summary>
	KKT2 = 1 << 9,

	/// <summary>
	/// 쿵쿵따 한방 단어
	/// </summary>
	KKTEndWord = 1 << 10,

	/// <summary>
	/// 한국어 단어
	/// </summary>
	Korean = 1 << 11,

	/// <summary>
	/// 영어 단어
	/// </summary>
	English = 1 << 12,

	/// <summary>
	/// 쿵쿵따 공격 단어
	/// </summary>
	KKTAttackWord = 1 << 13,

	/// <summary>
	/// 외래어
	/// </summary>
	/// <remarks>
	/// 주의: 이 플래그는 데이터베이스 검증 작업을 통해 갱신되지 않습니다.
	/// 단어 추가 당시에 직접 지정해 주거나, 국어 사전 가져오기 기능 등을 통해서만 갱신이 가능합니다.
	/// </remarks>
	LoanWord = 1 << 14,

	/// <summary>
	/// 어인정 단어
	/// </summary>
	Injeong = 1 << 15,

	/// <summary>
	/// 방언
	/// </summary>
	/// <remarks>
	/// 주의: 이 플래그는 데이터베이스 검증 작업을 통해 갱신되지 않습니다.
	/// 단어 추가 당시에 직접 지정해 주거나, 국어 사전 가져오기 기능 등을 통해서만 갱신이 가능합니다.
	/// </remarks>
	Dialect = 1 << 16,

	/// <summary>
	/// 옛말
	/// </summary>
	/// <remarks>
	/// 주의: 이 플래그는 데이터베이스 검증 작업을 통해 갱신되지 않습니다.
	/// 단어 추가 당시에 직접 지정해 주거나, 국어 사전 가져오기 기능 등을 통해서만 갱신이 가능합니다.
	/// </remarks>
	DeadLang = 1 << 17,

	/// <summary>
	/// 문화어
	/// </summary>
	/// <remarks>
	/// 주의: 이 플래그는 데이터베이스 검증 작업을 통해 갱신되지 않습니다.
	/// 단어 추가 당시에 직접 지정해 주거나, 국어 사전 가져오기 기능 등을 통해서만 갱신이 가능합니다.
	/// </remarks>
	Munhwa = 1 << 18
}
