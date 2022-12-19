namespace AutoKkutuLib.Hangul;

public static class HangulCharExtension
{
	/// <summary>
	/// 주어진 문자가 한글이고, 종성을 가지고 있는지(밭침이 있는지)의 여부를 반환하는 함수.
	/// </summary>
	/// <param name="character">검사할 문자.</param>
	public static bool HasFinalConsonant(this char character)
	{
		var unicodeIndex = Convert.ToUInt16(character);
		return unicodeIndex.IsHangulSyllable() && (unicodeIndex - HangulConstants.HangulSyllablesOrigin) % 588 % 28 > 0 || unicodeIndex.IsHangulJamoJongseong();
	}

	public static bool IsHangulJamoChoseong(this ushort index) => index is >= HangulConstants.HangulJamoChoseongOrigin and <= HangulConstants.HangulJamoChoseongBound;

	public static bool IsHangulJamoChoseong(this char character) => Convert.ToUInt16(character).IsHangulJamoChoseong();

	public static bool IsHangulJamoJungseong(this ushort index) => index is >= HangulConstants.HangulJamoJungseongOrigin and <= HangulConstants.HangulJamoJungseongBound;

	public static bool IsHangulJamoJungseong(this char character) => Convert.ToUInt16(character).IsHangulJamoJungseong();

	public static bool IsHangulJamoJongseong(this ushort index) => index is >= HangulConstants.HangulJamoJongseongOrigin and <= HangulConstants.HangulJamoJongseongBound;

	public static bool IsHangulJamoJongseong(this char character) => Convert.ToUInt16(character).IsHangulJamoJongseong();

	public static bool IsHangulCompatibilityJamoConsonant(this ushort index) => index is >= HangulConstants.HangulCompatibilityJamoConsonantOrigin and <= HangulConstants.HangulCompatibilityJamoConsonantBound;

	public static bool IsHangulCompatibilityJamoConsonant(this char character) => Convert.ToUInt16(character).IsHangulCompatibilityJamoConsonant();

	public static bool IsHangulCompatibilityJamoVowel(this ushort index) => index is >= HangulConstants.HangulCompatibilityJamoVowelOrigin and <= HangulConstants.HangulCompatibilityJamoVowelBound;

	public static bool IsHangulCompatibilityJamoVowel(this char character) => Convert.ToUInt16(character).IsHangulCompatibilityJamoVowel();

	public static bool IsHangulSyllable(this ushort index) => index is >= HangulConstants.HangulSyllablesOrigin and <= HangulConstants.HangulSyllablesBound;

	public static bool IsHangulSyllable(this char character) => Convert.ToUInt16(character).IsHangulSyllable();

	public static bool IsHangul(this ushort index) => index.IsHangulJamoChoseong() || index.IsHangulJamoJungseong() || index.IsHangulJamoJongseong() || index.IsHangulCompatibilityJamoConsonant() || index.IsHangulCompatibilityJamoVowel() || index.IsHangulSyllable();

	public static bool IsHangul(this char character) => Convert.ToUInt16(character).IsHangul();
}
