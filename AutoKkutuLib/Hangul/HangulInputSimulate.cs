namespace AutoKkutuLib.Hangul;

public static class HangulInputSimulate
{
	public static (bool, char, string) SimulateAppend(this string str, JamoType type, char ch)
	{
		if (str is null)
			throw new ArgumentNullException(nameof(str));
		HangulSplitted lastSplit = str.Length == 0 ? HangulSplitted.Empty : str.Last().Split();
		return lastSplit.IsHangul ? (true, ch, Combine(str, type, ch, lastSplit)) : (false, ch, str + ch);
	}

	// FIXME: 두벌식 입력 알고리즘은 현재 지원되지 않습니다 (도깨비불 현상을 아직 제대로 재현하지 못하였습니다)
	/*
	/// <summary>
	/// 세벌식 (자음-모음 구분) 입력 방식 - todo: 도깨비불 현상 재현
	/// </summary>
	private static string CombineDubeol(string str, JamoType appendCharType, char charToAppend, HangulSplitted lastSplit)
	{
		var result = charToAppend;
		var isFull = lastSplit.IsFull;
		switch (appendCharType)
		{
			case JamoType.Initial:
				if (lastSplit.InitialConsonant is null)
				{
					result = (lastSplit with
					{
						InitialConsonant = charToAppend
					}).Merge();
				}
				break;

			case JamoType.Medial:
				if (lastSplit.Medial is null)
				{
					result = (lastSplit with
					{
						Medial = charToAppend
					}).Merge();
				}
				break;

			case JamoType.Final:
				// 종성은 비어 있을 수도, 차 있을 수도 있기에 IsFull로 검사가 불가능하다.
				result = (char.IsWhiteSpace(lastSplit.FinalConsonant)
					? (lastSplit with
					{
						FinalConsonant = charToAppend
					})
					: (lastSplit with
					{
						FinalConsonant = HangulConsonantCluster.MergeCluster(lastSplit.FinalConsonant, charToAppend) // 자음군 조합
					})).Merge();
				return str[..^1] + result.ToString();
		}
		return (isFull ? str : str[..^1]) + result.ToString();
	}
	*/

	/// <summary>
	/// 세벌식 (초-중-종 구분) 입력 방식
	/// </summary>
	private static string Combine(string str, JamoType appendCharType, char charToAppend, HangulSplitted lastSplit)
	{
		var result = charToAppend;
		switch (appendCharType)
		{
			case JamoType.Initial:
				if (!lastSplit.HasInitialConsonant)
				{
					result = (lastSplit with
					{
						InitialConsonant = charToAppend
					}).Merge();
				}
				break;

			case JamoType.Medial:
				result = (lastSplit with
				{
					Medial = HangulCluster.Vowel.MergeCluster(lastSplit.Medial, charToAppend) // 합성 모음 조합
				}).Merge();
				break;

			case JamoType.Final:
				result = (lastSplit with
				{
					FinalConsonant = HangulCluster.Consonant.MergeCluster(lastSplit.FinalConsonant, charToAppend) // 자음군 조합
				}).Merge();
				return str[..^1] + result.ToString();
		}
		return (appendCharType == JamoType.Initial ? str : str[..^1]) + result.ToString();
	}
}
