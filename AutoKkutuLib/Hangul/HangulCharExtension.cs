namespace AutoKkutuLib.Hangul;

internal static class HangulCharExtension
{
	/// <summary>
	/// 주어진 문자가 한글이고, 종성을 가지고 있는지(밭침이 있는지)의 여부를 반환하는 함수.
	/// </summary>
	/// <param name="character">검사할 문자.</param>
	internal static bool HasFinalConsonant(this char character)
	{
		var unicodeIndex = Convert.ToUInt16(character);
		return unicodeIndex.IsHangulSyllable() && (unicodeIndex - HangulConstants.HangulSyllablesOrigin) % 588 % 28 > 0 || unicodeIndex.IsHangulJamoJongseong();
	}

	internal static bool IsHangulJamoChoseong(this ushort index) => index is >= HangulConstants.HangulJamoChoseongOrigin and <= HangulConstants.HangulJamoChoseongBound;

	internal static bool IsHangulJamoChoseong(this char character) => Convert.ToUInt16(character).IsHangulJamoChoseong();

	internal static bool IsHangulJamoJungseong(this ushort index) => index is >= HangulConstants.HangulJamoJungseongOrigin and <= HangulConstants.HangulJamoJungseongBound;

	internal static bool IsHangulJamoJungseong(this char character) => Convert.ToUInt16(character).IsHangulJamoJungseong();

	internal static bool IsHangulJamoJongseong(this ushort index) => index is >= HangulConstants.HangulJamoJongseongOrigin and <= HangulConstants.HangulJamoJongseongBound;

	internal static bool IsHangulJamoJongseong(this char character) => Convert.ToUInt16(character).IsHangulJamoJongseong();

	internal static bool IsHangulCompatibilityJamoConsonant(this ushort index) => index is >= HangulConstants.HangulCompatibilityJamoConsonantOrigin and <= HangulConstants.HangulCompatibilityJamoConsonantBound;

	internal static bool IsHangulCompatibilityJamoConsonant(this char character) => Convert.ToUInt16(character).IsHangulCompatibilityJamoConsonant();

	internal static bool IsHangulCompatibilityJamoVowel(this ushort index) => index is >= HangulConstants.HangulCompatibilityJamoVowelOrigin and <= HangulConstants.HangulCompatibilityJamoVowelBound;

	internal static bool IsHangulCompatibilityJamoVowel(this char character) => Convert.ToUInt16(character).IsHangulCompatibilityJamoVowel();

	internal static bool IsHangulSyllable(this ushort index) => index is >= HangulConstants.HangulSyllablesOrigin and <= HangulConstants.HangulSyllablesBound;

	internal static bool IsHangulSyllable(this char character) => Convert.ToUInt16(character).IsHangulSyllable();

	internal static bool IsHangul(this ushort index) => index.IsHangulJamoChoseong() || index.IsHangulJamoJungseong() || index.IsHangulJamoJongseong() || index.IsHangulCompatibilityJamoConsonant() || index.IsHangulCompatibilityJamoVowel() || index.IsHangulSyllable();

	internal static bool IsHangul(this char character) => Convert.ToUInt16(character).IsHangul();

	internal static bool IsSsangJaeum(this char character) => HangulConstants.SsangJaeum.Contains(character);

	internal static bool IsShiftMedial(this char character) => HangulConstants.ShiftMedial.Contains(character);

	internal static bool IsBlacklistedFromFinalConsonant(this char character) => HangulConstants.FinalConsonantBlacklist.Contains(character);
}
