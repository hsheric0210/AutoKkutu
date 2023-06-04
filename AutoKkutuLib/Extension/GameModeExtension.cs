using AutoKkutuLib.Hangul;

namespace AutoKkutuLib.Extension;
public static class GameModeExtension
{
	public static WordCondition? ConvertWordToCondition(this GameMode gameMode, string path, string? missionChar, bool initialLaw = true)
	{
		var node = gameMode.ConvertWordToTailNode(path);
		if (string.IsNullOrWhiteSpace(node))
			return null;

		var content = new WordCondition(node, missionChar: missionChar);
		if (initialLaw)
			content = InitialLaw.ApplyInitialLaw(content);

		return content;
	}

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

	public static bool IsFreeMode(this GameMode mode) => mode is GameMode.Free or GameMode.LastAndFirstFree;

	public static string? GameModeName(this GameMode gameMode) => gameMode switch
	{
		GameMode.LastAndFirst => I18n.GameMode_LastAndFirst,
		GameMode.FirstAndLast => I18n.GameMode_FirstAndLast,
		GameMode.MiddleAndFirst => I18n.GameMode_MiddleAndFirst,
		GameMode.Kkutu => I18n.GameMode_Kkutu,
		GameMode.KungKungTta => I18n.GameMode_KungKungTta,
		GameMode.TypingBattle => I18n.GameMode_TypingBattle,
		GameMode.All => I18n.GameMode_All,
		GameMode.Free => I18n.GameMode_Free,
		GameMode.LastAndFirstFree => I18n.GameMode_LastAndFirstFree,
		_ => null,
	};
}
