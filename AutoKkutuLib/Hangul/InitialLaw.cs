namespace AutoKkutuLib.Hangul;
/// <summary>
/// JJoriping의 원 구현: https://github.com/JJoriping/KKuTu/blob/a2c240bc31fe2dea31d26fb1cf7625b4645556a6/Server/lib/Game/games/classic.js#L531
/// </summary>
public static class InitialLaw
{
	private static readonly ISet<char> rieul2Nieun = new HashSet<char>() { 'ㅏ', 'ㅐ', 'ㅗ', 'ㅚ', 'ㅜ', 'ㅡ' };
	private static readonly ISet<char> rieul2Ieung = new HashSet<char>() { 'ㅑ', 'ㅕ', 'ㅖ', 'ㅛ', 'ㅠ', 'ㅣ' };
	private static readonly ISet<char> nieun2Ieung = new HashSet<char>() { 'ㅕ', 'ㅛ', 'ㅠ', 'ㅣ' };

	/// <summary>
	/// (만약 가능하다면) 단어 조건에 두음법칙을 적용합니다.
	/// </summary>
	/// <param name="condition">(두음법칙이 적용되지 않은) 단어 조건</param>
	/// <returns>두음법칙이 적용된 단어 조건</returns>
	public static WordCondition ApplyInitialLaw(WordCondition condition)
	{
		if (condition.SubAvailable || string.IsNullOrEmpty(condition.Char))
			return condition;

		var applied = true;
		var split = HangulSplit.Parse(condition.Char[0]);
		if (!split.IsHangul || !split.HasMedial)
			return condition;

		if (split.InitialConsonant == 'ㄹ')
		{
			if (rieul2Nieun.Contains(split.Medial))
				split = split with { InitialConsonant = 'ㄴ' };
			else if (rieul2Ieung.Contains(split.Medial))
				split = split with { InitialConsonant = 'ㅇ' };
			else
				applied = false;
		}
		else if (split.InitialConsonant == 'ㄴ' && nieun2Ieung.Contains(split.Medial))
		{
			split = split with { InitialConsonant = 'ㅇ' };
		}
		else
		{
			applied = false;
		}

		if (!applied)
			return condition;

		return new WordCondition(condition.Char, split.Merge() + (condition.Char.Length > 1 ? condition.Char[1..] : ""), condition.MissionChar);
	}
}
