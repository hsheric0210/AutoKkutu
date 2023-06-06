namespace AutoKkutuLib.Hangul;

internal static class HangulConsonantExtension
{
	internal static char Merge(this HangulSplitted splitted)
	{
		if (!splitted.HasInitialConsonant)
			throw new ArgumentException("At least initial consonant must not be empty", nameof(splitted));
		return splitted.IsHangul ? Merge(splitted.InitialConsonant, splitted.Medial, splitted.FinalConsonant) : splitted.InitialConsonant;
	}

	internal static char Merge(char initial, char medial, char final)
	{
		// 중성 없이는 종성도 없고, 조합도 없다
		return char.IsWhiteSpace(medial)
			? initial
			: Convert.ToChar(HangulConstants.HangulSyllablesOrigin
					+ (HangulConstants.InitialConsonantTable.IndexOf(initial, StringComparison.Ordinal) * 21 + HangulConstants.MedialTable.IndexOf(medial, StringComparison.Ordinal))
					* 28
					+ HangulConstants.FinalConsonantTable.IndexOf(final, StringComparison.Ordinal));
	}

	/// <summary>
	/// 주어진 문자에 대한 초성ㆍ중성ㆍ종성을 추출합니다.
	/// 원본 소스는 이곳에서 확인할 수 있습니다: https://plog2012.blogspot.com/2012/11/c.html
	/// </summary>
	/// <param name="character">초성ㆍ중성ㆍ종성을 추출할 문자입니다.</param>
	/// <returns>만약 입력된 문자가 한글이라면 추출된 초성ㆍ중성ㆍ종성 문자를 <c>HangulSplitted</c> 에 넣어 반환하고, 한글이 아니라면 해당 문자를 <c>HangulSplitted</c>의 초성 자리에 넣어서 반환합니다.</returns>
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
			return new HangulSplitted(true, initialConsonant: character);
		}
		else if (unicodeIndex.IsHangulCompatibilityJamoVowel() || unicodeIndex.IsHangulJamoJungseong())
		{
			return new HangulSplitted(true, medial: character);
		}
		else if (unicodeIndex.IsHangulJamoJongseong())
		{
			return new HangulSplitted(true, finalConsonant: character);
		}

		// 한글이 아니면... 일단 초성에 넣기.
		return new HangulSplitted(false, initialConsonant: character);
	}
}
