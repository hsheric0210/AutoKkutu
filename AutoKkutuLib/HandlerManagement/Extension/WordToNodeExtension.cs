namespace AutoKkutuLib.HandlerManagement.Extension;

public static class WordToNodeExtension
{
	/// <summary>
	/// 끝말잇기 단어 <paramref name="word"/>의 HEAD 노드/인덱스
	/// </summary>
	public static string GetLaFHeadNode(this string word) => word == null ? throw new ArgumentNullException(nameof(word)) : word[0].ToString();

	/// <summary>
	/// 앞말잇기 단어 <paramref name="word"/>의 HEAD 노드/인덱스
	/// </summary>
	public static string GetFaLHeadNode(this string word) => word == null ? throw new ArgumentNullException(nameof(word)) : word.Last().ToString();

	/// <summary>
	/// 끄투 단어 <paramref name="word"/>의 HEAD 노드/인덱스
	/// </summary>
	public static string GetKkutuHeadNode(this string word)
	{
		if (word == null)
			throw new ArgumentNullException(nameof(word));

		return word.Length >= 4 ? word[..2] : word.Length >= 3 ? word[0].ToString() : "";
	}

	/// <summary>
	/// 끝말잇기 단어 <paramref name="word"/>의 TAIL 노드/인덱스
	/// </summary>
	public static string GetLaFTailNode(this string word) => word.GetFaLHeadNode();

	/// <summary>
	/// 앞말잇기 단어 <paramref name="word"/>의 TAIL 노드/인덱스
	/// </summary>
	public static string GetFaLTailNode(this string word) => word.GetLaFHeadNode();

	/// <summary>
	/// 끄투 단어 <paramref name="word"/>의 TAIL 노드/인덱스
	/// </summary>
	public static string GetKkutuTailNode(this string word)
	{
		return word == null
			? throw new ArgumentNullException(nameof(word))
			: word.Length >= 4 ? word.Substring(word.Length - 3, 2) : word.Last().ToString();
	}

	/// <summary>
	/// 가운뎃말잇기 단어 <paramref name="word"/>의 TAIL 노드/인덱스
	/// </summary>
	public static string GetMaFTailNode(this string word) => word == null ? throw new ArgumentNullException(nameof(word)) : word[(word.Length - 1) / 2].ToString();
}
