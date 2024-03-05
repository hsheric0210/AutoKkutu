using System.Diagnostics.CodeAnalysis;

namespace AutoKkutuLib.Database.Helper;
public class ThemeManager
{
	private readonly ISet<Theme> themes;

	public ThemeManager(ISet<Theme> themes) => this.themes = themes;

	public bool TryGetById(string id, [NotNullWhen(true)] out Theme? theme)
	{
		theme = themes.First(t => t.Name.Equals(id, StringComparison.OrdinalIgnoreCase));
		return theme != null;
	}

	public IList<Theme> ParseThemes(string themeString, string delimiter = "|")
	{
		var pieces = themeString.Split(delimiter, StringSplitOptions.RemoveEmptyEntries);
		var themeList = new List<Theme>(pieces.Length);
		foreach (var themeElement in pieces)
		{
			if (TryGetById(themeElement, out var theme))
				themeList.Add(theme);
		}

		return themeList;
	}

	public IList<Theme> BitMasksToThemes(long[] themeBitmasks)
	{
		if (themeBitmasks.Length != DatabaseConstants.ThemeColumnCount)
			throw new ArgumentException("Theme bitmask count must be " + DatabaseConstants.ThemeColumnCount);

		var themeList = new List<Theme>();
		foreach (var theme in themes)
		{
			if ((themeBitmasks[theme.BitMaskOrdinal] & theme.BitMaskMask) != 0)
				themeList.Add(theme);
		}

		return themeList;
	}
}
