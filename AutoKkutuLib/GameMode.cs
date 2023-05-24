namespace AutoKkutuLib;

public enum GameMode
{
	None = 0,
	/// <summary>
	/// 끝말잇기
	/// </summary>
	LastAndFirst,

	/// <summary>
	/// 앞말잇기
	/// </summary>
	FirstAndLast,

	/// <summary>
	/// 가운뎃말잇기
	/// </summary>
	MiddleAndFirst,

	/// <summary>
	/// 끄투
	/// </summary>
	Kkutu,

	/// <summary>
	/// 쿵쿵따
	/// </summary>
	KungKungTta,

	/// <summary>
	/// 타자 대결
	/// </summary>
	TypingBattle,

	/// <summary>
	/// 전체
	/// </summary>
	All,

	/// <summary>
	/// 자유
	/// </summary>
	Free,

	/// <summary>
	/// 자유 끝말잇기
	/// </summary>
	LastAndFirstFree
}
