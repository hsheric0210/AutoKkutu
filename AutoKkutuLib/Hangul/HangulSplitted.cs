namespace AutoKkutuLib.Hangul;

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
			foreach (var consonant in FinalConsonant.SplitCluster())
				enumerable.Add((JamoType.Final, consonant));
		}

		return enumerable;
	}
}
