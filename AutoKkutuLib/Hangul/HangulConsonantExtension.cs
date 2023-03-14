namespace AutoKkutuLib.Hangul;

internal static class HangulConsonantExtension
{
	internal static char Merge(this HangulSplitted splitted)
	{
		if (splitted is null)
			throw new ArgumentNullException(nameof(splitted));
		if (splitted.InitialConsonant is null)
			throw new ArgumentException("Initial consonant is null", nameof(splitted));
		return !splitted.IsHangul
			? splitted.FinalConsonant
			: Merge((char)splitted.InitialConsonant, splitted.Medial, splitted.FinalConsonant);
	}

	internal static char Merge(char initial, char? medial, char final)
	{
		// 중성 없이는 종성도 없고, 조합도 없다
		return medial == null
			? initial
			: Convert.ToChar(HangulConstants.HangulSyllablesOrigin + (HangulConstants.InitialConsonantTable.IndexOf(initial, StringComparison.Ordinal) * 21 + HangulConstants.MedialTable.IndexOf((char)medial, StringComparison.Ordinal)) * 28 + HangulConstants.FinalConsonantTable.IndexOf(final, StringComparison.Ordinal));
	}

	/// <summary>
	/// 주어진 문자에 대한 초성ㆍ중성ㆍ종성을 추출합니다.
	/// Original source available at https://plog2012.blogspot.com/2012/11/c.html
	/// </summary>
	/// <param name="character">초성ㆍ중성ㆍ종성을 추출할 문자입니다.</param>
	/// <returns>만약 입력된 문자가 한글이라면 추출된 초성ㆍ중성ㆍ종성을 <c>HangulSplitted</c> 의 형태로 반환하고, 한글이 아니라면 null을 반환합니다.</returns>
	public static HangulSplitted Split(this char character)
	{
		int initialIndex, medialIndex, finalIndex;
		var unicodeIndex = Convert.ToUInt16(character);
		if (unicodeIndex.IsHangulSyllable())
		{
			var delta = unicodeIndex - HangulConstants.HangulSyllablesOrigin;
			initialIndex = delta / 588;
			delta %= 588;
			medialIndex = delta / 28;
			delta %= 28;
			finalIndex = delta;
			return new HangulSplitted(true, HangulConstants.InitialConsonantTable[initialIndex], HangulConstants.MedialTable[medialIndex], HangulConstants.FinalConsonantTable[finalIndex]);
		}
		else if (unicodeIndex.IsHangulCompatibilityJamoConsonant() || unicodeIndex.IsHangulJamoChoseong())
		{
			return new HangulSplitted(true, character);
		}
		else if (unicodeIndex.IsHangulCompatibilityJamoVowel() || unicodeIndex.IsHangulJamoJungseong())
		{
			return new HangulSplitted(true, Medial: character);
		}
		else if (unicodeIndex.IsHangulJamoJongseong())
		{
			return new HangulSplitted(true, FinalConsonant: character);
		}

		return new HangulSplitted(false, character);
	}
}
