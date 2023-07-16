namespace AutoKkutuLib.Extension;

/// <summary>
/// 단어에서 노드를 추출하는 기능을 추가하는 확장 클래스입니다.
/// </summary>
public static class WordToNodeExtension
{
	/// <summary>
	/// 끝말잇기 단어 <paramref name="word"/>의 HEAD 노드/인덱스를 추출합니다.
	/// </summary>
	/// <remarks>
	/// 대표적인 예시로, '안녕하세요'의 경우 '안'을 반환합니다.
	/// </remarks>
	public static string GetLaFHeadNode(this string word) => word == null ? throw new ArgumentNullException(nameof(word)) : word[0].ToString();

	/// <summary>
	/// 앞말잇기 단어 <paramref name="word"/>의 HEAD 노드/인덱스를 추출합니다.
	/// </summary>
	/// <remarks>
	/// 대표적인 예시로, '안녕하세요'의 경우 '요'를 반환합니다.
	/// </remarks>
	public static string GetFaLHeadNode(this string word) => word == null ? throw new ArgumentNullException(nameof(word)) : word.Last().ToString();

	/// <summary>
	/// 끄투 단어 <paramref name="word"/>의 HEAD 노드/인덱스를 추출합니다.
	/// </summary>
	/// <remarks>
	/// 대표적인 예시로, '안녕하세요'의 경우 '안녕'를 반환하고, '가나다'의 경우 '가'를 반환합니다.
	/// </remarks>
	public static string GetKkutuHeadNode(this string word)
	{
		if (word == null)
			throw new ArgumentNullException(nameof(word));

		return word.Length >= 4 ? word[..2] : word.Length >= 3 ? word[0].ToString() : "";
	}

	/// <summary>
	/// 끝말잇기 단어 <paramref name="word"/>의 TAIL 노드/인덱스를 반환합니다.
	/// </summary>
	/// <remarks>
	/// 대표적인 예시로, '안녕하세요'의 경우 '요'를 반환합니다.
	/// </remarks>
	public static string GetLaFTailNode(this string word) => word.GetFaLHeadNode();

	/// <summary>
	/// 앞말잇기 단어 <paramref name="word"/>의 TAIL 노드/인덱스
	/// </summary>
	/// <remarks>
	/// 대표적인 예시로, '안녕하세요'의 경우 '안'를 반환합니다.
	/// </remarks>
	public static string GetFaLTailNode(this string word) => word.GetLaFHeadNode();

	/// <summary>
	/// 끄투 단어 <paramref name="word"/>의 TAIL 노드/인덱스
	/// </summary>
	/// <remarks>
	/// 대표적인 예시로, '안녕하세요'의 경우 '세요'를 반환하고, '가나다'의 경우 '다'를 반환합니다.
	/// </remarks>
	public static string GetKkutuTailNode(this string word)
	{
		return word == null
			? throw new ArgumentNullException(nameof(word))
			: word.Length >= 4 ? word.Substring(word.Length - 2, 2) : word.Last().ToString();
	}

	/// <summary>
	/// 가운뎃말잇기 단어 <paramref name="word"/>의 TAIL 노드/인덱스를 반환합니다.
	/// 만약 글자 수가 짝수여서 중간 글자가 2개가 나올 경우, 그 중 앞의 것을 반환합니다.
	/// </summary>
	/// <remarks>
	/// 대표적인 예시로, '안녕하세요'의 경우 '하'를 반환하고, '가나다라'의 경우 '나'를 반환합니다.
	/// </remarks>
	public static string GetMaFTailNode(this string word) => word == null ? throw new ArgumentNullException(nameof(word)) : word[(word.Length - 1) / 2].ToString();
}
