namespace AutoKkutuLib;
public static class EnumValuesHolder
{
	public static WordCategories[] GetWordPreferenceValues() => (WordCategories[])Enum.GetValues(typeof(WordCategories));

	public static GameMode[] GetGameModeValues() => (GameMode[])Enum.GetValues(typeof(GameMode));
}
