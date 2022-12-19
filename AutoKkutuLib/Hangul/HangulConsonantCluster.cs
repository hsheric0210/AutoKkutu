namespace AutoKkutuLib.Hangul;

public static class HangulConsonantCluster
{
	/// <summary>
	/// 자음군 합치기
	/// </summary>
	public static char MergeCluster(params char[] consonants)
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
	/// 자음군 분리
	/// </summary>
	public static IList<char> SplitCluster(this char consonantCluster)
	{
		return HangulConstants.ConsonantClusterDecompositionTable.TryGetValue(consonantCluster, out IList<char>? consonants)
			? consonants
			: new List<char>() { consonantCluster };
	}
}
