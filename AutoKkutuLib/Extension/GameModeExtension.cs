using AutoKkutuLib.Hangul;

namespace AutoKkutuLib.Extension;

/// <summary>
/// 게임모드별로 수행될 수 있는 특수 기능들을 정의하는 확장 클래스입니다.
/// </summary>
public static class GameModeExtension
{
	/// <summary>
	/// 제시된 단어를 게임 모드에 맞는 단어 조건으로 변환하여 반환합니다. 만약 변환될 수 없다면 <c>null</c>을 반환합니다.
	/// </summary>
	/// <param name="gameMode">게임 모드</param>
	/// <param name="path">현재 제시된 단어</param>
	/// <param name="missionChar">미션 글자</param>
	/// <param name="initialLaw">두음법칙 적용 여부</param>
	public static WordCondition? ConvertWordToCondition(this GameMode gameMode, string path, string missionChar, bool initialLaw = true)
	{
		var node = gameMode.ConvertWordToTailNode(path);
		if (string.IsNullOrWhiteSpace(node))
			return null;

		var content = new WordCondition(node, missionChar: missionChar);
		if (initialLaw)
			content = InitialLaw.ApplyInitialLaw(content);

		return content;
	}

	/// <summary>
	/// 제시된 단어를 게임 모드에 맞는 TAIL 노드로 변환하여 반환합니다. 만약 변환될 수 없다면 <c>null</c>을 반환합니다.
	/// </summary>
	/// <param name="gameMode">게임 모드</param>
	/// <param name="path">제시된 단어</param>
	/// <exception cref="ArgumentException">제시된 단어가 공란일 때 발생.</exception>
	public static string? ConvertWordToTailNode(this GameMode gameMode, string path)
	{
		if (string.IsNullOrWhiteSpace(path))
			throw new ArgumentException("Parameter is null or blank", nameof(path));

		switch (gameMode)
		{
			case GameMode.LastAndFirst:
			case GameMode.KungKungTta:
			case GameMode.LastAndFirstFree:
				return path.GetLaFTailNode();

			case GameMode.FirstAndLast:
				return path.GetFaLTailNode();

			case GameMode.MiddleAndFirst:
				if (path.Length > 2 && path.Length % 2 == 1)
					return path.GetMaFTailNode();
				break;

			case GameMode.Kkutu:
				return path.GetKkutuTailNode();

			case GameMode.TypingBattle:
			case GameMode.All:
			case GameMode.Free:
				break;
		}

		return null;
	}

	/// <summary>
	/// 주어진 게임 모드가 자유 모드에 속하는지의 여부를 반환합니다.
	/// </summary>
	/// <param name="mode">게임 모드</param>
	public static bool IsFreeMode(this GameMode mode) => mode is GameMode.Free or GameMode.LastAndFirstFree;

	/// <summary>
	/// 주어진 게임 모드가 사용 단어 조건을 제시하지 않는 모드인지의 여부를 반환합니다.
	/// </summary>
	/// <param name="mode">게임 모드</param>
	public static bool IsConditionlessMode(this GameMode mode) => mode is GameMode.Free or GameMode.All;

	/// <summary>
	/// 주어진 게임 모드가 표시되는 이름을 반환합니다.
	/// </summary>
	/// <param name="gameMode">게임 모드</param>
	public static string? GameModeName(this GameMode gameMode) => gameMode switch
	{
		GameMode.LastAndFirst => I18n.GameMode_LastAndFirst,
		GameMode.FirstAndLast => I18n.GameMode_FirstAndLast,
		GameMode.MiddleAndFirst => I18n.GameMode_MiddleAndFirst,
		GameMode.Kkutu => I18n.GameMode_Kkutu,
		GameMode.KungKungTta => I18n.GameMode_KungKungTta,
		GameMode.TypingBattle => I18n.GameMode_TypingBattle,
		GameMode.All => I18n.GameMode_All,
		GameMode.AllKorean => I18n.GameMode_All_Korean,
		GameMode.AllEnglish => I18n.GameMode_All_English,
		GameMode.Free => I18n.GameMode_Free,
		GameMode.LastAndFirstFree => I18n.GameMode_LastAndFirstFree,
		GameMode.Hunmin => I18n.GameMode_HunMin,
		_ => null,
	};
}
