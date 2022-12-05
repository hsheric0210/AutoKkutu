using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoKkutuLib.Utils.Hangul;

public static class HangulProcessing
{
	public static char Merge(char initial, char? medial, char final)
	{
		// 중성 없이는 종성도 없고, 조합도 없다
		if (medial == null)
			return initial;
		return Convert.ToChar(HangulConstants.HangulSyllablesOrigin + (HangulConstants.InitialConsonantTable.IndexOf(initial, StringComparison.Ordinal) * 21 + HangulConstants.MedialTable.IndexOf((char)medial, StringComparison.Ordinal)) * 28 + HangulConstants.FinalConsonantTable.IndexOf(final, StringComparison.Ordinal));
	}

	public static char Merge(HangulSplitted splitted)
	{
		if (splitted is null)
			throw new ArgumentNullException(nameof(splitted));
		if (splitted.InitialConsonant is null)
			throw new ArgumentException("Initial consonant is null", nameof(splitted));
		if (!splitted.IsHangul)
			return splitted.FinalConsonant;
		return Merge((char)splitted.InitialConsonant, splitted.Medial, splitted.FinalConsonant);
	}

	public static string AppendChar(this string str, JamoType type, char ch)
	{
		if (str is null)
			throw new ArgumentNullException(nameof(str));
		HangulSplitted? lastSplit = str.Length == 0 ? null : str.Last().SplitConsonants();
		var result = ch;
		if (lastSplit?.IsHangul == true)
			return CombineHangulSebeol(str, type, ch, lastSplit);
		return str + ch;
	}

	private static string CombineHangulDubeol(string str, JamoType appendCharType, char charToAppend, HangulSplitted lastSplit)
	{
		var result = charToAppend;
		var isFull = lastSplit.IsFull;
		if (appendCharType == JamoType.Medial)
		{
		}
		switch (appendCharType)
		{
			case JamoType.Initial:
				if (lastSplit.InitialConsonant is null)
				{
					result = Merge(lastSplit with
					{
						InitialConsonant = charToAppend
					});
				}

				break;

			case JamoType.Medial:
				if (lastSplit.Medial is null)
				{
					result = Merge(lastSplit with
					{
						Medial = charToAppend
					});
				}

				break;

			case JamoType.Final:
				// 종성은 비어 있을 수도, 차 있을 수도 있기에 IsFull로 검사가 불가능하다.
				result = char.IsWhiteSpace(lastSplit.FinalConsonant)
					? Merge(lastSplit with
					{
						FinalConsonant = charToAppend
					})
					: Merge(lastSplit with
					{
						FinalConsonant = MergeConsonantCluster(lastSplit.FinalConsonant, charToAppend) // 자음군 조합
					});
				return str[..^1] + result.ToString();
		}
		return (isFull ? str : str[..^1]) + result.ToString();
	}

	private static string CombineHangulSebeol(string str, JamoType appendCharType, char charToAppend, HangulSplitted lastSplit)
	{
		var result = charToAppend;
		var isFull = lastSplit.IsFull;
		switch (appendCharType)
		{
			case JamoType.Initial:
				if (lastSplit.InitialConsonant is null)
				{
					result = Merge(lastSplit with
					{
						InitialConsonant = charToAppend
					});
				}

				break;

			case JamoType.Medial:
				if (lastSplit.Medial is null)
				{
					result = Merge(lastSplit with
					{
						Medial = charToAppend
					});
				}

				break;

			case JamoType.Final:
				// 종성은 비어 있을 수도, 차 있을 수도 있기에 IsFull로 검사가 불가능하다.
				result = char.IsWhiteSpace(lastSplit.FinalConsonant)
					? Merge(lastSplit with
					{
						FinalConsonant = charToAppend
					})
					: Merge(lastSplit with
					{
						FinalConsonant = MergeConsonantCluster(lastSplit.FinalConsonant, charToAppend) // 자음군 조합
					});
				return str[..^1] + result.ToString();
		}
		return (isFull ? str : str[..^1]) + result.ToString();
	}

	/// <summary>
	/// 주어진 문자에 대한 초성ㆍ중성ㆍ종성을 추출합니다.
	/// Original source available at https://plog2012.blogspot.com/2012/11/c.html
	/// </summary>
	/// <param name="character">초성ㆍ중성ㆍ종성을 추출할 문자입니다.</param>
	/// <returns>만약 입력된 문자가 한글이라면 추출된 초성ㆍ중성ㆍ종성을 <c>HangulSplitted</c> 의 형태로 반환하고, 한글이 아니라면 null을 반환합니다.</returns>
	public static HangulSplitted SplitConsonants(this char character)
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

	/// <summary>
	/// 주어진 문자가 한글이고, 종성을 가지고 있는지(밭침이 있는지)의 여부를 반환하는 함수.
	/// </summary>
	/// <param name="character">검사할 문자.</param>
	public static bool HasFinalConsonant(this char character)
	{
		var unicodeIndex = Convert.ToUInt16(character);
		return unicodeIndex.IsHangulSyllable() && (unicodeIndex - HangulConstants.HangulSyllablesOrigin) % 588 % 28 > 0 || unicodeIndex.IsHangulJamoJongseong();
	}

	/// <summary>
	/// 오직 초성만을 추출하는 용도에만 특화된 최적화된 함수.
	/// </summary>
	/// <param name="character">초성을 추출할 문자.</param>
	/// <returns>만약 입력된 문자가 한글이라면 추출된 초성, 한글이 아니라면 원 문자를 그대로 반환합니다.</returns>
	public static char ExtractInitialConsonant(this char character)
	{
		var unicodeIndex = Convert.ToUInt16(character);
		if (unicodeIndex.IsHangulSyllable())
			return HangulConstants.InitialConsonantTable[(unicodeIndex - HangulConstants.HangulSyllablesOrigin) / 588];
		// 자모는 굳이 처리해야 할 필요 X
		return character;
	}

	public static string ExtractInitialConsonant(this string str) => string.Concat(str.Select(c => c.ExtractInitialConsonant()));

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

	public static char MergeConsonantCluster(params char[] consonants)
	{
		if (consonants is null)
			throw new ArgumentNullException(nameof(consonants));

		switch (consonants.Length)
		{
			case 0:
				return ' ';
			case 1:
				return consonants[0];
			default:
				var filtered = consonants.Where(ch => !char.IsWhiteSpace(ch)).ToArray();
				// TODO: 어두자음군 지원
				var ch = filtered[0];
				foreach (var consonant in filtered.Skip(1))
				{
					if (!HangulConstants.ConsonantClusterTable.TryGetValue(ch, out IDictionary<char, char>? combination) || !combination.TryGetValue(consonant, out ch))
						throw new InvalidOperationException($"Unsupported combination: {ch} + {consonant}");
				}

				return ch;
		}
	}

	public static IList<char> SplitConsonantCluster(this char consonantCluster)
	{
		if (HangulConstants.InverseConsonantClusterTable.TryGetValue(consonantCluster, out IList<char>? consonants))
			return consonants;
		return new List<char>() { consonantCluster };
	}
}
