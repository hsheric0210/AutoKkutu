namespace AutoKkutuLib.Hangul;

public struct HangulSplit
{
	public static HangulSplit EmptyNonHangul { get; } = NonHangul(' ');
	public static HangulSplit EmptyHangul { get; } = Hangul();

	public readonly bool IsHangul { get; }
	public char InitialConsonant { get; set; } = ' ';
	public char Medial { get; set; } = ' ';
	public char FinalConsonant { get; set; } = ' ';

	public bool HasInitialConsonant => !char.IsWhiteSpace(InitialConsonant);
	public bool HasMedial => !char.IsWhiteSpace(Medial);
	public bool HasFinalConsonant => !char.IsWhiteSpace(FinalConsonant);

	/// <summary>
	/// 주어진 문자에 대한 초성ㆍ중성ㆍ종성을 추출합니다.
	/// 원본 소스: https://plog2012.blogspot.com/2012/11/c.html
	/// </summary>
	/// <param name="character">초성ㆍ중성ㆍ종성을 추출할 문자입니다.</param>
	/// <returns>만약 입력된 문자가 한글이라면 추출된 초성ㆍ중성ㆍ종성 문자를 <c>HangulSplit</c> 에 넣어 반환하고, 한글이 아니라면 해당 문자를 <c>HangulSplit</c>의 초성 자리에 넣어서 반환합니다.</returns>
	public static HangulSplit Parse(char character)
	{
		char initial = ' ', medial = ' ', final = ' ';
		var hangul = true;
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

			initial = HangulConstants.InitialConsonantTable[initialIndex];
			medial = HangulConstants.MedialTable[medialIndex];
			final = HangulConstants.FinalConsonantTable[finalIndex];
		}
		else if (unicodeIndex.IsHangulCompatibilityJamoConsonant() || unicodeIndex.IsHangulJamoChoseong())
		{
			initial = character;
		}
		else if (unicodeIndex.IsHangulCompatibilityJamoVowel() || unicodeIndex.IsHangulJamoJungseong())
		{
			medial = character;
		}
		else if (unicodeIndex.IsHangulJamoJongseong())
		{
			final = character;
		}
		else
		{
			// 한글이 아니면... 일단 초성에 넣기.
			hangul = false;
			initial = character;
		}

		return new HangulSplit(hangul, initial, medial, final);
	}

	public static HangulSplit Hangul(char initial = ' ', char medial = ' ', char final = ' ') => new(true, initial, medial, final);

	public static HangulSplit NonHangul(char ch) => new(false, ch, ' ', ' ');

	private HangulSplit(bool isHangul, char initialConsonant, char medial, char finalConsonant)
	{
		IsHangul = isHangul;
		InitialConsonant = initialConsonant;
		Medial = medial;
		FinalConsonant = finalConsonant;
	}

	public IList<(ConsonantType, char)> Serialize()
	{
		var enumerable = new List<(ConsonantType, char)>(5);

		if (!IsHangul)
		{
			enumerable.Add((ConsonantType.None, InitialConsonant));
			return enumerable;
		}

		if (HasInitialConsonant)
			enumerable.Add((ConsonantType.Initial, InitialConsonant));

		if (HasMedial)
		{
			foreach (var medial in HangulCluster.Vowel.SplitCluster(Medial))
				enumerable.Add((ConsonantType.Medial, medial));
		}

		if (HasFinalConsonant)
		{
			foreach (var consonant in HangulCluster.Consonant.SplitCluster(FinalConsonant))
				enumerable.Add((ConsonantType.Final, consonant));
		}

		return enumerable;
	}

	/// <summary>
	/// 분리되어 있던 초성ㆍ중성ㆍ종성을 합쳐 하나의 한글 문자를 만듭니다.
	/// 원본 소스: https://plog2012.blogspot.com/2012/11/c.html
	/// </summary>
	/// <param name="splitted">분리된 한글</param>
	/// <returns>조합된 한글 문자</returns>
	/// <exception cref="ArgumentException"><paramref name="splitted"/>의 초성이 채워져 있지 않을 때 발생</exception>
	public char Merge() => IsHangul ? MergeHangul(InitialConsonant, Medial, FinalConsonant) : InitialConsonant;

	private static char MergeHangul(char initial, char medial, char final)
	{
		if (char.IsWhiteSpace(initial))
			return char.IsWhiteSpace(medial) ? final : medial;

		return char.IsWhiteSpace(medial)
			? initial
			: Convert.ToChar(HangulConstants.HangulSyllablesOrigin
					+ (HangulConstants.InitialConsonantTable.IndexOf(initial, StringComparison.Ordinal) * 21 + HangulConstants.MedialTable.IndexOf(medial, StringComparison.Ordinal))
					* 28
					+ HangulConstants.FinalConsonantTable.IndexOf(final, StringComparison.Ordinal));
	}

	public override bool Equals(object? obj) => obj is HangulSplit splitted && IsHangul == splitted.IsHangul && InitialConsonant == splitted.InitialConsonant && Medial == splitted.Medial && FinalConsonant == splitted.FinalConsonant;
	public override int GetHashCode() => HashCode.Combine(IsHangul, InitialConsonant, Medial, FinalConsonant);
	public static bool operator ==(HangulSplit left, HangulSplit right) => left.Equals(right);
	public static bool operator !=(HangulSplit left, HangulSplit right) => !(left == right);

	public override string ToString() => $"{Merge()}({InitialConsonant}{Medial}{FinalConsonant})";
}
