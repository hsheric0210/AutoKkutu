// Original source available at https://plog2012.blogspot.com/2012/11/c.html

using System.Collections.Immutable;

namespace AutoKkutuLib.Hangul;

internal static class HangulConstants
{
	/// <summary>
	/// 초성
	/// </summary>
	internal static readonly string InitialConsonantTable = "ㄱㄲㄴㄷㄸㄹㅁㅂㅃㅅㅆㅇㅈㅉㅊㅋㅌㅍㅎ";

	/// <summary>
	/// 중성
	/// </summary>
	internal static readonly string MedialTable = "ㅏㅐㅑㅒㅓㅔㅕㅖㅗㅘㅙㅚㅛㅜㅝㅞㅟㅠㅡㅢㅣ";


	/// <summary>
	/// 종성
	/// </summary>
	internal static readonly string FinalConsonantTable = " ㄱㄲㄳㄴㄵㄶㄷㄹㄺㄻㄼㄽㄾㄿㅀㅁㅂㅄㅅㅆㅇㅈㅊㅋㅌㅍㅎ";

	/// <summary>
	/// 쌍자음 (SHIFT키로 입력 가능한 자음)
	/// </summary>
	internal static readonly string SsangJaeum = "ㄲㄸㅃㅆㅉ";

	/// <summary>
	/// SHIFT키로 입력 가능한 모음 (쌍자음과는 달리, 이는 딱히 칭해 부르는 이름이 없음)
	/// </summary>
	internal static readonly string ShiftMedial = "ㅒㅖ";

	internal static readonly string FinalConsonantBlacklist = "ㄸㅃㅉ";

	/// <summary>
	/// 유니코드 'Hangul Jamo' 초성 시작 위치
	/// </summary>
	internal const ushort HangulJamoChoseongOrigin = 0x1100;

	/// <summary>
	/// 유니코드 'Hangul Jamo' 초성 끝 위치
	/// </summary>
	internal const ushort HangulJamoChoseongBound = 0x115F;

	/// <summary>
	/// 유니코드 'Hangul Jamo' 중성 시작 위치
	/// </summary>
	internal const ushort HangulJamoJungseongOrigin = 0x1160;

	/// <summary>
	/// 유니코드 'Hangul Jamo' 중성 끝 위치
	/// </summary>
	internal const ushort HangulJamoJungseongBound = 0x11A7;

	/// <summary>
	/// 유니코드 'Hangul Jamo' 종성 시작 위치
	/// </summary>
	internal const ushort HangulJamoJongseongOrigin = 0x11A8;

	/// <summary>
	/// 유니코드 'Hangul Jamo' 종성 끝 위치
	/// </summary>
	internal const ushort HangulJamoJongseongBound = 0x11FF;

	/// <summary>
	/// 유니코드 'Hangul Compatibility Jamo' 자음 시작 위치
	/// </summary>
	internal const ushort HangulCompatibilityJamoConsonantOrigin = 0x3131;

	/// <summary>
	/// 유니코드 'Hangul Compatibility Jamo' 자음 끝 위치
	/// </summary>
	internal const ushort HangulCompatibilityJamoConsonantBound = 0x314E;

	/// <summary>
	/// 유니코드 'Hangul Compatibility Jamo' 모음 시작 위치
	/// </summary>
	internal const ushort HangulCompatibilityJamoVowelOrigin = 0x314F;

	/// <summary>
	/// 유니코드 'Hangul Compatibility Jamo' 모음 끝 위치
	/// </summary>
	internal const ushort HangulCompatibilityJamoVowelBound = 0x3163;

	/// <summary>
	/// 유니코드 'Hangul Syllables' 시작 위치
	/// </summary>
	internal const ushort HangulSyllablesOrigin = 0xAC00;

	/// <summary>
	/// 유니코드 'Hangul Syllables' 끝 위치
	/// </summary>
	internal const ushort HangulSyllablesBound = 0xD79F;

	/// <summary>
	/// 자음군 조합 조합 테이블
	/// </summary>
	internal static IImmutableDictionary<char, IImmutableDictionary<char, char>> ConsonantClusterCompositionTable { get; }

	/// <summary>
	/// 자음군 조합 분해 테이블
	/// </summary>
	internal static IImmutableDictionary<char, IImmutableList<char>> ConsonantClusterDecompositionTable { get; }

	/// <summary>
	/// 합성 모음 조합 테이블
	/// </summary>
	internal static IImmutableDictionary<char, IImmutableDictionary<char, char>> VowelClusterCompositionTable { get; }

	/// <summary>
	/// 자음군 조합 분해 테이블
	/// </summary>
	internal static IImmutableDictionary<char, IImmutableList<char>> VowelClusterDecompositionTable { get; }

	/// <summary>
	/// Creates a new <see cref="ImmutableDictionary"/> with the given key/value pairs.
	/// https://stackoverflow.com/a/66274927
	/// </summary>
	private static ImmutableDictionary<K, V> CreateImmDict<K, V>(params (K key, V value)[] items) where K : notnull
	{
		var builder = ImmutableDictionary.CreateBuilder<K, V>();
		foreach (var (key, value) in items)
			builder.Add(key, value);
		return builder.ToImmutable();
	}

	static HangulConstants()
	{
		// 주의: 쌍자음의 경우 SHIFT로 입력하는 것이 더 빠르기에 분해/조합 테이블에 넣지 말 것!

		var ccC = ImmutableDictionary.CreateBuilder<char, IImmutableDictionary<char, char>>();
		ccC['ㅂ'] = CreateImmDict(('ㅅ', 'ㅄ'));
		ccC['ㄱ'] = CreateImmDict(('ㅅ', 'ㄳ'));
		ccC['ㄴ'] = CreateImmDict(
			('ㅈ', 'ㄵ'),
			('ㅎ', 'ㄶ')
		);
		ccC['ㄹ'] = CreateImmDict(
			('ㄱ', 'ㄺ'),
			('ㅁ', 'ㄻ'),
			('ㅂ', 'ㄼ'),
			('ㅅ', 'ㄽ'),
			('ㅌ', 'ㄾ'),
			('ㅍ', 'ㄿ'),
			('ㅎ', 'ㅀ')
		);
		ConsonantClusterCompositionTable = ccC.ToImmutable();

		var ccDec = ImmutableDictionary.CreateBuilder<char, IImmutableList<char>>();
		ccDec['ㄳ'] = ImmutableList.Create('ㄱ', 'ㅅ');
		ccDec['ㄵ'] = ImmutableList.Create('ㄴ', 'ㅈ');
		ccDec['ㄶ'] = ImmutableList.Create('ㄴ', 'ㅎ');
		ccDec['ㄺ'] = ImmutableList.Create('ㄹ', 'ㄱ');
		ccDec['ㄻ'] = ImmutableList.Create('ㄹ', 'ㅁ');
		ccDec['ㄼ'] = ImmutableList.Create('ㄹ', 'ㅂ');
		ccDec['ㄽ'] = ImmutableList.Create('ㄹ', 'ㅅ');
		ccDec['ㄾ'] = ImmutableList.Create('ㄹ', 'ㅌ');
		ccDec['ㄿ'] = ImmutableList.Create('ㄹ', 'ㅍ');
		ccDec['ㅀ'] = ImmutableList.Create('ㄹ', 'ㅎ');
		ccDec['ㅄ'] = ImmutableList.Create('ㅂ', 'ㅅ');
		ConsonantClusterDecompositionTable = ccDec.ToImmutable();

		var vcC = ImmutableDictionary.CreateBuilder<char, IImmutableDictionary<char, char>>();
		vcC['ㅗ'] = CreateImmDict(
			('ㅏ', 'ㅘ'),
			('ㅐ', 'ㅙ'),
			('ㅣ', 'ㅚ')
		);
		vcC['ㅜ'] = CreateImmDict(
			('ㅓ', 'ㅝ'),
			('ㅔ', 'ㅞ'),
			('ㅣ', 'ㅟ')
		);
		vcC['ㅡ'] = CreateImmDict(('ㅣ', 'ㅢ'));
		VowelClusterCompositionTable = vcC.ToImmutable();

		var vcDec = ImmutableDictionary.CreateBuilder<char, IImmutableList<char>>();
		vcDec['ㅘ'] = ImmutableList.Create('ㅗ', 'ㅏ');
		vcDec['ㅙ'] = ImmutableList.Create('ㅗ', 'ㅐ');
		vcDec['ㅚ'] = ImmutableList.Create('ㅗ', 'ㅣ');
		vcDec['ㅝ'] = ImmutableList.Create('ㅜ', 'ㅓ');
		vcDec['ㅞ'] = ImmutableList.Create('ㅜ', 'ㅔ');
		vcDec['ㅟ'] = ImmutableList.Create('ㅜ', 'ㅣ');
		vcDec['ㅢ'] = ImmutableList.Create('ㅡ', 'ㅣ');
		VowelClusterDecompositionTable = vcDec.ToImmutable();
	}
}

public enum ConsonantType
{
	/// <summary>
	/// 초성, 중성, 종성에 포함되지 않는 문자 (영어 알파벳 등)를 나타냅니다.
	/// </summary>
	None,

	/// <summary>
	/// 초성
	/// </summary>
	Initial,

	/// <summary>
	/// 중성
	/// </summary>
	Medial,

	/// <summary>
	/// 종성
	/// </summary>
	Final
}

public enum JamoType
{
	/// <summary>
	/// 한글 자음, 모음에 포함되지 않는 문자 (영어 알파벳 등)를 나타냅니다.
	/// </summary>
	None,

	/// <summary>
	/// 한글 자음
	/// </summary>
	Consonant,

	/// <summary>
	/// 한글 모음
	/// </summary>
	Medial
}