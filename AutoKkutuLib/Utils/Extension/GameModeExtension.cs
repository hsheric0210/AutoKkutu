using AutoKkutuLib.Constants;

namespace AutoKkutuLib.Utils.Extension;
public static class GameModeExtension
{
	public static string? ConvertToPresentedWord(this GameMode mode, string path)
	{
		if (string.IsNullOrWhiteSpace(path))
			throw new ArgumentException("Parameter is null or blank", nameof(path));

		switch (mode)
		{
			case GameMode.LastAndFirst:
			case GameMode.KungKungTta:
			case GameMode.LastAndFirstFree:
				return path.GetLaFTailNode();

			case GameMode.FirstAndLast:
				return path.GetFaLHeadNode();

			case GameMode.MiddleAndFirst:
				if (path.Length > 2 && path.Length % 2 == 1)
					return path.GetMaFTailNode();
				break;

			case GameMode.Kkutu:
				return path.GetKkutuTailNode();

			case GameMode.TypingBattle:
				break;

			case GameMode.All:
				break;

			case GameMode.Free:
				break;
		}

		return null;
	}

	public static bool IsFreeMode(this GameMode mode) => mode is GameMode.Free or GameMode.LastAndFirstFree;
}
