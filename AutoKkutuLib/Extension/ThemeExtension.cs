using AutoKkutuLib.Database;

namespace AutoKkutuLib.Extension;
public static class ThemeExtension
{
	public static long[] ThemesToBitMasks(this IEnumerable<Theme> themes)
	{
		var bitmasks = new long[DatabaseConstants.ThemeColumnCount];
		foreach (var theme in themes)
			bitmasks[theme.BitMaskOrdinal] |= theme.BitMaskMask;

		return bitmasks;
	}
}
