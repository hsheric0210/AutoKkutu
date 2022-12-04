using System.Collections.Generic;

// Original source available at https://plog2012.blogspot.com/2012/11/c.html
namespace AutoKkutu.Utils.HangulProcessing
{
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
}
