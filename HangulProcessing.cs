using System;
using System.Collections.Generic;
using System.Linq;

// Copyright PLOG (@ plog2012.blogspot.com) All rights reserved
// Original source available at https://plog2012.blogspot.com/2012/11/c.html
namespace AutoKkutu
{
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
			char result = ch;
			if (lastSplit?.IsHangul == true)
			{
				bool isFull = lastSplit.IsFull;
				switch (type)
				{
					case JamoType.Initial:
						if (lastSplit.InitialConsonant is null)
						{
							result = Merge(lastSplit with
							{
								InitialConsonant = ch
							});
						}

						break;

					case JamoType.Medial:
						if (lastSplit.Medial is null)
						{
							result = Merge(lastSplit with
							{
								Medial = ch
							});
						}
						break;

					case JamoType.Final:
						// 종성은 비어 있을 수도, 차 있을 수도 있기에 IsFull로 검사가 불가능하다.
						if (char.IsWhiteSpace(lastSplit.FinalConsonant)) // 종성이 비어 있을 경우
						{
							result = Merge(lastSplit with
							{
								FinalConsonant = ch
							});
						}
						else // 종성이 비어 있지 않을 경우
						{
							result = Merge(lastSplit with
							{
								FinalConsonant = MergeConsonantCluster(lastSplit.FinalConsonant, ch) // 자음군 조합
							});
						}
						return str[..^1] + result.ToString();
				}
				return (isFull ? str : str[..^1]) + result.ToString();
			}
			return str + ch;
		}

		/// <summary>
		/// 주어진 문자에 대한 초성ㆍ중성ㆍ종성을 추출합니다.
		/// </summary>
		/// <param name="character">초성ㆍ중성ㆍ종성을 추출할 문자입니다.</param>
		/// <returns>만약 입력된 문자가 한글이라면 추출된 초성ㆍ중성ㆍ종성을 <c>HangulSplitted</c> 의 형태로 반환하고, 한글이 아니라면 null을 반환합니다.</returns>
		public static HangulSplitted SplitConsonants(this char character)
		{
			int initialIndex, medialIndex, finalIndex;
			ushort unicodeIndex = Convert.ToUInt16(character);
			if (unicodeIndex.IsHangulSyllable())
			{
				int delta = unicodeIndex - HangulConstants.HangulSyllablesOrigin;
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
			ushort unicodeIndex = Convert.ToUInt16(character);
			return unicodeIndex.IsHangulSyllable() && (unicodeIndex - HangulConstants.HangulSyllablesOrigin) % 588 % 28 > 0 || unicodeIndex.IsHangulJamoJongseong();
		}

		/// <summary>
		/// 오직 초성만을 추출하는 용도에만 특화된 최적화된 함수.
		/// </summary>
		/// <param name="character">초성을 추출할 문자.</param>
		/// <returns>만약 입력된 문자가 한글이라면 추출된 초성, 한글이 아니라면 원 문자를 그대로 반환합니다.</returns>
		public static char ExtractInitialConsonant(this char character)
		{
			ushort unicodeIndex = Convert.ToUInt16(character);
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

		public static bool IsHangul(this ushort index) => IsHangulJamoChoseong(index) || IsHangulJamoJungseong(index) || IsHangulJamoJongseong(index) || IsHangulCompatibilityJamoConsonant(index) || IsHangulCompatibilityJamoVowel(index) || IsHangulSyllable(index);

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
					char ch = filtered[0];
					foreach (char consonant in filtered.Skip(1))
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

	public sealed record HangulSplitted(bool IsHangul, char? InitialConsonant = null, char? Medial = null, char FinalConsonant = ' ')
	{
		public bool IsFull => InitialConsonant is not null && Medial is not null;

		public IList<(JamoType, char)> Serialize()
		{
			var enumerable = new List<(JamoType, char)>(3);
			if (InitialConsonant is not null)
				enumerable.Add((JamoType.Initial, (char)InitialConsonant));
			if (Medial is not null)
				enumerable.Add((JamoType.Medial, (char)Medial));
			if (!char.IsWhiteSpace(FinalConsonant))
			{
				foreach (var consonant in FinalConsonant.SplitConsonantCluster())
					enumerable.Add((JamoType.Final, consonant));
			}

			return enumerable;
		}
	}

	// TODO: 중세 자음, 모음 지원
	public static class HangulConstants
	{
		/// <summary>
		/// 초성
		/// </summary>
		public static readonly string InitialConsonantTable = "ㄱㄲㄴㄷㄸㄹㅁㅂㅃㅅㅆㅇㅈㅉㅊㅋㅌㅍㅎ";

		/// <summary>
		/// 중성
		/// </summary>
		public static readonly string MedialTable = "ㅏㅐㅑㅒㅓㅔㅕㅖㅗㅘㅙㅚㅛㅜㅝㅞㅟㅠㅡㅢㅣ";

		/// <summary>
		/// 종성
		/// </summary>
		public static readonly string FinalConsonantTable = " ㄱㄲㄳㄴㄵㄶㄷㄹㄺㄻㄼㄽㄾㄿㅀㅁㅂㅄㅅㅆㅇㅈㅊㅋㅌㅍㅎ";

		/// <summary>
		/// 유니코드 'Hangul Jamo' 초성 시작 위치
		/// </summary>
		public const ushort HangulJamoChoseongOrigin = 0x1100;

		/// <summary>
		/// 유니코드 'Hangul Jamo' 초성 끝 위치
		/// </summary>
		public const ushort HangulJamoChoseongBound = 0x115F;

		/// <summary>
		/// 유니코드 'Hangul Jamo' 중성 시작 위치
		/// </summary>
		public const ushort HangulJamoJungseongOrigin = 0x1160;

		/// <summary>
		/// 유니코드 'Hangul Jamo' 중성 끝 위치
		/// </summary>
		public const ushort HangulJamoJungseongBound = 0x11A7;

		/// <summary>
		/// 유니코드 'Hangul Jamo' 종성 시작 위치
		/// </summary>
		public const ushort HangulJamoJongseongOrigin = 0x11A8;

		/// <summary>
		/// 유니코드 'Hangul Jamo' 종성 끝 위치
		/// </summary>
		public const ushort HangulJamoJongseongBound = 0x11FF;

		/// <summary>
		/// 유니코드 'Hangul Compatibility Jamo' 자음 시작 위치
		/// </summary>
		public const ushort HangulCompatibilityJamoConsonantOrigin = 0x3131;

		/// <summary>
		/// 유니코드 'Hangul Compatibility Jamo' 자음 끝 위치
		/// </summary>
		public const ushort HangulCompatibilityJamoConsonantBound = 0x314E;

		/// <summary>
		/// 유니코드 'Hangul Compatibility Jamo' 모음 시작 위치
		/// </summary>
		public const ushort HangulCompatibilityJamoVowelOrigin = 0x314F;

		/// <summary>
		/// 유니코드 'Hangul Compatibility Jamo' 모음 끝 위치
		/// </summary>
		public const ushort HangulCompatibilityJamoVowelBound = 0x3163;

		/// <summary>
		/// 유니코드 'Hangul Syllables' 시작 위치
		/// </summary>
		public const ushort HangulSyllablesOrigin = 0xAC00;

		/// <summary>
		/// 유니코드 'Hangul Syllables' 끝 위치
		/// </summary>
		public const ushort HangulSyllablesBound = 0xD79F;

		/// <summary>
		/// 겹자음 조합 변환 테이블
		/// </summary>
		public static IDictionary<char, IDictionary<char, char>> ConsonantClusterTable
		{
			get;
		} = new Dictionary<char, IDictionary<char, char>>()
		{
			{ 'ㄱ', new Dictionary<char, char>()
			{
				{ 'ㄱ', 'ㄲ' },
				{ 'ㅅ', 'ㄳ' }
			}
			},
			{ 'ㄴ', new Dictionary<char, char>()
			{
				{ 'ㅈ', 'ㄵ' },
				{ 'ㅎ', 'ㄶ' }
			}
			},
			{ 'ㄷ', new Dictionary<char, char>()
			{
				{ 'ㄷ', 'ㄸ' }
			} },
			{ 'ㄹ', new Dictionary<char, char>()
			{
				{ 'ㄱ', 'ㄺ' },
				{ 'ㅁ', 'ㄻ' },
				{ 'ㅂ', 'ㄼ' },
				{ 'ㅅ', 'ㄽ' },
				{ 'ㅌ', 'ㄾ' },
				{ 'ㅍ', 'ㄿ' },
				{ 'ㅎ', 'ㅀ' }
			} },
			{ 'ㅂ', new Dictionary<char, char>()
			{
				{ 'ㅂ', 'ㅃ' },
				{ 'ㅅ', 'ㅄ' }
			}
			},
			{ 'ㅅ', new Dictionary<char, char>()
			{
				{ 'ㅅ', 'ㅆ' }
			}
			},
			{ 'ㅈ', new Dictionary<char, char>()
			{
				{ 'ㅈ', 'ㅉ' }
			}
			}
		};

		/// <summary>
		/// 겹자음 조합 역변환 테이블
		/// </summary>
		public static IDictionary<char, IList<char>> InverseConsonantClusterTable
		{
			get;
		} = new Dictionary<char, IList<char>>()
		{
			{ 'ㄳ', new List<char>() { 'ㄱ', 'ㅅ' } },
			{ 'ㄵ', new List<char>() { 'ㄴ', 'ㅈ' } },
			{ 'ㄶ', new List<char>() { 'ㄴ', 'ㅎ' } },
			{ 'ㄺ', new List<char>() { 'ㄹ', 'ㄱ' } },
			{ 'ㄻ', new List<char>() { 'ㄹ', 'ㅁ' } },
			{ 'ㄼ', new List<char>() { 'ㄹ', 'ㅂ' } },
			{ 'ㄽ', new List<char>() { 'ㄹ', 'ㅅ' } },
			{ 'ㄾ', new List<char>() { 'ㄹ', 'ㅌ' } },
			{ 'ㄿ', new List<char>() { 'ㄹ', 'ㅍ' } },
			{ 'ㅀ', new List<char>() { 'ㄹ', 'ㅎ' } },
			{ 'ㅄ', new List<char>() { 'ㅂ', 'ㅅ' } }
		};
	}

	public enum JamoType
	{
		/// <summary>
		/// 초성 또는 영어 알파벳
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
}
