using System;
using System.Linq;

namespace AutoKkutuLib.Utils.Extension;

public static class WordNodeExtension
{
	/// <summary>
	/// 끝말잇기 단어 <paramref name="word"/>의 HEAD 노드/인덱스
	/// </summary>
	public static string GetLaFHeadNode(this string word)
	{
		if (word == null)
			throw new ArgumentNullException(nameof(word));

		return word[0].ToString();
	}

	/// <summary>
	/// 앞말잇기 단어 <paramref name="word"/>의 HEAD 노드/인덱스
	/// </summary>
	public static string GetFaLHeadNode(this string word)
	{
		if (word == null)
			throw new ArgumentNullException(nameof(word));

		return word.Last().ToString();
	}

	/// <summary>
	/// 끄투 단어 <paramref name="word"/>의 HEAD 노드/인덱스
	/// </summary>
	public static string GetKkutuHeadNode(this string word)
	{
		if (word == null)
			throw new ArgumentNullException(nameof(word));

		if (word.Length >= 4)
			return word[..2];
		return word.Length >= 3 ? word[0].ToString() : "";
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
		if (word == null)
			throw new ArgumentNullException(nameof(word));

		return word.Length >= 4 ? word.Substring(word.Length - 3, 2) : word.Last().ToString();
	}

	/// <summary>
	/// 가운뎃말잇기 단어 <paramref name="word"/>의 TAIL 노드/인덱스
	/// </summary>
	public static string GetMaFTailNode(this string word)
	{
		if (word == null)
			throw new ArgumentNullException(nameof(word));

		return word[(word.Length - 1) / 2].ToString();
	}
}
