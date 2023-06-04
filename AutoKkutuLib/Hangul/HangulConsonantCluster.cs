namespace AutoKkutuLib.Hangul;

/// <summary>
/// 자음군 분리 및 합치기를 위한 유틸리티 클래스
/// </summary>
internal static class HangulConsonantCluster
{
	/// <summary>
	/// 분리되어 있던 자음군을 합칩니다.
	/// (예시: ['ㄱ', 'ㅅ']->'ㄳ')
	/// </summary>
	internal static char MergeCluster(params char[] consonants)
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
					if (!HangulConstants.ConsonantClusterCompositionTable.TryGetValue(ch, out IDictionary<char, char>? combination) || !combination.TryGetValue(consonant, out ch))
						throw new InvalidOperationException($"Unsupported combination: {ch} + {consonant}");
				}

				return ch;
		}
	}

	/// <summary>
	/// 자음군을 분리합니다.
	/// (예시: 'ㄺ'->['ㄹ', 'ㄱ'])
	/// </summary>
	internal static IList<char> SplitCluster(this char consonantCluster)
	{
		return HangulConstants.ConsonantClusterDecompositionTable.TryGetValue(consonantCluster, out IList<char>? consonants)
			? consonants
			: new List<char>() { consonantCluster };
	}
}
