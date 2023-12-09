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
	/// 한국어 전체
	/// </summary>
	AllKorean,

	/// <summary>
	/// 영어 전체
	/// </summary>
	AllEnglish,

	/// <summary>
	/// 자유
	/// </summary>
	Free,

	/// <summary>
	/// 자유 끝말잇기
	/// </summary>
	LastAndFirstFree,

	/// <summary>
	/// 훈민정음
	/// </summary>
	Hunmin
}

public enum GameImplMode
{
	None = 0,
	/// <summary>
	/// 끝말잇기, 끄투, 쿵쿵따, 앞말잇기, 가운뎃말잇기, 자유, 자유 끝말잇기, 전체 등
	/// </summary>
	Classic,

	/// <summary>
	/// 타자 대결
	/// </summary>
	TypingBattle,

	/// <summary>
	/// 훈민정음
	/// </summary>
	Hunmin
}

public static class GameImplModeExtension
{
	public static GameImplMode ToGameImplMode(this GameMode mode)
	{
		return mode switch
		{
			GameMode.LastAndFirst
			or GameMode.FirstAndLast
			or GameMode.MiddleAndFirst
			or GameMode.Kkutu
			or GameMode.KungKungTta
			or GameMode.All
			or GameMode.Free
			or GameMode.LastAndFirstFree => GameImplMode.Classic,
			GameMode.TypingBattle => GameImplMode.TypingBattle,
			GameMode.Hunmin => GameImplMode.Hunmin,
			_ => GameImplMode.None,
		};
	}
}