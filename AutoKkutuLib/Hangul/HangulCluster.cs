using System.Collections.Immutable;

namespace AutoKkutuLib.Hangul;

/// <summary>
/// 자음군 분리 및 합치기를 위한 유틸리티 클래스
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
	/// 분리되어 있던 자음군 조합
	/// (예시: ['ㄱ', 'ㅅ']->'ㄳ')
	/// </summary>
	internal static char MergeCluster(this HangulCluster clusterType, params char?[] splitted)
	{
		if (splitted is null)
			throw new ArgumentNullException(nameof(splitted));

		switch (splitted.Length)
		{
			case 0:
				return ' ';
			case 1:
				return splitted[0] ?? ' ';
			default:
				var filtered = splitted.Where(_ch => _ch is char ch && !char.IsWhiteSpace(ch)).Select(c => (char)c!).ToArray();
				// TODO: 어두자음군 지원
				var ch = filtered[0];
				foreach (var consonant in filtered.Skip(1))
				{
					if (!GetCompositionTable(clusterType).TryGetValue(ch, out IImmutableDictionary<char, char>? combination) || !combination.TryGetValue(consonant, out ch))
						throw new InvalidOperationException($"Unsupported consonant cluster combination: {ch} + {consonant}");
				}

				return ch;
		}
	}

	/// <summary>
	/// 자음군을 분리
	/// (예시: 'ㄺ'->['ㄹ', 'ㄱ'])
	/// </summary>
	internal static IImmutableList<char> SplitCluster(this HangulCluster clusterType, char combined)
	{
		return GetDecompositionTable(clusterType).TryGetValue(combined, out IImmutableList<char>? consonants)
			? consonants
			: ImmutableList.Create(combined);
	}
}
