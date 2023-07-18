using System.Collections.Immutable;

namespace AutoKkutuLib.Hangul;

/// <summary>
/// 자음군 / 합성 모음 분리 및 합치기를 위한 유틸리티 클래스
/// </summary>
internal enum HangulCluster
{
	/// <summary>
	/// 자음군
	/// (예시: ㄳ, ㄾ, ㄶ)
	/// </summary>
	Consonant,

	/// <summary>
	/// 합성 모음
	/// (예시: ㅟ, ㅞ, ㅢ)
	/// </summary>
	Vowel
}

/// <summary>
/// Can't even add a static method to enum
/// https://stackoverflow.com/a/25993744
/// </summary>
internal static class HangulClusterExtension
{
	private static IImmutableDictionary<char, IImmutableDictionary<char, char>> GetCompositionTable(this HangulCluster type)
		=> type switch
		{
			HangulCluster.Consonant => HangulConstants.ConsonantClusterCompositionTable,
			HangulCluster.Vowel => HangulConstants.VowelClusterCompositionTable,
			_ => throw new ArgumentException("Unknown cluster composition type: " + type)
		};

	private static IImmutableDictionary<char, IImmutableList<char>> GetDecompositionTable(this HangulCluster type)
		=> type switch
		{
			HangulCluster.Consonant => HangulConstants.ConsonantClusterDecompositionTable,
			HangulCluster.Vowel => HangulConstants.VowelClusterDecompositionTable,
			_ => throw new ArgumentException("Unknown cluster decomposition type: " + type)
		};

	/// <summary>
	/// 분리되어 있던 자음군/합성 모음 조합
	/// (자음군 조합 예시: ['ㄱ', 'ㅅ']->'ㄳ')
	/// (합성 모음 조합 예시: ['ㅜ', 'ㅔ']->'ㅞ')
	/// </summary>
	internal static bool TryMergeCluster(this HangulCluster clusterType, char first, char second, out char result)
	{
		if (char.IsWhiteSpace(first))
		{
			result = second;
			return true;
		}
		if (char.IsWhiteSpace(second))
		{
			result = first;
			return true;
		}

		if (clusterType.GetCompositionTable().TryGetValue(first, out var combination) && combination.TryGetValue(second, out result))
			return true;

		result = ' ';
		return false;
	}

	/// <summary>
	/// 자음군/합성 모음 분해
	/// (자음군 분해 예시: 'ㄺ'->['ㄹ', 'ㄱ'])
	/// (합성 모음 분해 예시: 'ㅟ'->['ㅜ', 'ㅣ'])
	/// </summary>
	internal static IImmutableList<char> SplitCluster(this HangulCluster clusterType, char combined)
		=> clusterType.GetDecompositionTable().TryGetValue(combined, out var consonants) ? consonants : ImmutableList.Create(combined);
}
