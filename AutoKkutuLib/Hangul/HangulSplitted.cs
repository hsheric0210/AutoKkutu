namespace AutoKkutuLib.Hangul;

public struct HangulSplitted
{
	public static HangulSplitted Empty { get; } = new HangulSplitted(false);
	public static HangulSplitted EmptyHangul { get; } = new HangulSplitted(true);

	public readonly bool IsHangul { get; }
	public char InitialConsonant { get; set; }
	public char Medial { get; set; }
	public char FinalConsonant { get; set; }

	public bool HasInitialConsonant => !char.IsWhiteSpace(InitialConsonant);
	public bool HasMedial => !char.IsWhiteSpace(Medial);
	public bool HasFinalConsonant => !char.IsWhiteSpace(FinalConsonant);

	public HangulSplitted(bool isHangul, char initialConsonant = ' ', char medial = ' ', char finalConsonant = ' ')
	{
		IsHangul = isHangul;
		InitialConsonant = initialConsonant;
		Medial = medial;
		FinalConsonant = finalConsonant;
	}

	public IList<(JamoType, char)> Serialize()
	{
		var enumerable = new List<(JamoType, char)>(3);

		if (HasInitialConsonant)
			enumerable.Add((JamoType.Initial, InitialConsonant));

		if (HasMedial)
		{
			foreach (var medial in HangulCluster.Vowel.SplitCluster(Medial))
				enumerable.Add((JamoType.Medial, medial));
		}

		if (HasFinalConsonant)
		{
			foreach (var consonant in HangulCluster.Consonant.SplitCluster(FinalConsonant))
				enumerable.Add((JamoType.Final, consonant));
		}

		return enumerable;
	}

	public override bool Equals(object? obj) => obj is HangulSplitted splitted && IsHangul == splitted.IsHangul && InitialConsonant == splitted.InitialConsonant && Medial == splitted.Medial && FinalConsonant == splitted.FinalConsonant;
	public override int GetHashCode() => HashCode.Combine(IsHangul, InitialConsonant, Medial, FinalConsonant);
	public static bool operator ==(HangulSplitted left, HangulSplitted right) => left.Equals(right);
	public static bool operator !=(HangulSplitted left, HangulSplitted right) => !(left == right);
}
