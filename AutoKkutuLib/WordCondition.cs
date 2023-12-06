using System.Text;

namespace AutoKkutuLib;

/// <summary>
/// 단어 검색 조건을 나타냅니다
/// </summary>
public readonly struct WordCondition
{
	public static WordCondition Empty { get; } = new WordCondition("");

	/// <summary>
	/// 주 단어 조건 문자입니다.
	/// 빈 단어 조건일 경우, 이 속성은 빈 문자열(<c>""</c>)입니다.
	/// </summary>
	public string Char { get; }

	/// <summary>
	/// 보조 단어 조건 문자가 존재하는지의 여부
	/// </summary>
	public bool SubAvailable => !string.IsNullOrWhiteSpace(SubChar);

	/// <summary>
	/// 보조 단어 조건 문자 (두음법칙 등)
	/// </summary>
	public string SubChar { get; }

	/// <summary>
	/// 미션 글자를 나타냅니다. 만약 미션 글자 조건이 존재하지 않는다면 빈 문자열(<c>""</c>)입니다.
	/// </summary>
	public string MissionChar { get; }

	/// <summary>
	/// 쿵쿵따 모드에서 입력할 단어의 길이입니다.
	/// 반드시 2 또는 3 둘 중 하나여야 합니다.
	/// </summary>
	public int WordLength { get; }

	/// <summary>
	/// 단어 검색 조건이 정규 표현식을 포함하는지의 여부를 나타냅니다.
	/// </summary>
	public bool Regexp { get; }

	public WordCondition(string content, string substitution = "", string missionChar = "", int wordLength = 3, bool regexp = false)
	{
		Char = content;
		SubChar = substitution;
		MissionChar = missionChar;
		WordLength = wordLength;
		Regexp = regexp;
	}

	public override bool Equals(object? obj) => obj is WordCondition other
		&& string.Equals(Char, other.Char, StringComparison.OrdinalIgnoreCase)
		&& string.Equals(MissionChar, other.MissionChar, StringComparison.OrdinalIgnoreCase)
		&& Regexp == other.Regexp
		&& WordLength == other.WordLength
		&& (!SubAvailable || string.Equals(SubChar, other.SubChar, StringComparison.OrdinalIgnoreCase));

	/// <summary>
	/// 단어 검색 조건이 서로 비슷한지의 여부를 반환합니다. 단, 이 단어 조건이나 대상 단어 조건이 빈 단어 조건일 경우, 이 함수는 항상 <c>false</c>를 반환합니다.
	/// 단어 검색 조건이 비슷하다는 것은 서로 한 개 이상의 조건을 공유하고, 미션 단어가 동일하다는 것을 의미합니다.
	/// </summary>
	/// <remarks>
	/// 에시로, <c>{Char: 나, MissionChar: 마}</c> 와 <c>{Char: 라, SubChar: 나, MissionChar: 마}</c>는 서로 비슷한 단어 검색 조건입니다.
	/// </remarks>
	/// <param name="o"></param>
	/// <returns></returns>
	public bool IsSimilar(WordCondition? o)
	{
		if (o is not WordCondition other)
			return false;
		if (IsEmpty() || other.IsEmpty())
			return false;
		return MissionChar == other.MissionChar
			&& Regexp == other.Regexp
			&& WordLength == other.WordLength
			&& (string.Equals(Char, other.Char, StringComparison.OrdinalIgnoreCase)
				|| SubAvailable && string.Equals(SubChar, other.Char, StringComparison.OrdinalIgnoreCase)
				|| other.SubAvailable && string.Equals(Char, other.SubChar, StringComparison.OrdinalIgnoreCase)
				|| SubAvailable && other.SubAvailable && string.Equals(SubChar, other.SubChar, StringComparison.OrdinalIgnoreCase));
	}

	/// <summary>
	/// 이 단어 검색 조건이 빈 단어 검색 조건인지의 여부를 반환합니다.
	/// </summary>
	public bool IsEmpty() => string.IsNullOrEmpty(Char);

	public override int GetHashCode() => HashCode.Combine(Char, SubAvailable, SubChar, MissionChar, WordLength, Regexp);

	public override string ToString()
	{
		var builder = new StringBuilder();
		builder.Append("WordCondition");
		if (Regexp)
			builder.Append("(regex)");
		builder.Append("{Char: ").Append(Char);
		if (SubAvailable)
			builder.Append(", SubChar: ").Append(SubChar);
		if (!string.IsNullOrEmpty(MissionChar))
			builder.Append(", MissionChar: ").Append(MissionChar);
		if (WordLength != 3) // not default value
			builder.Append(", WordLength: ").Append(WordLength);
		return builder.Append('}').ToString();
	}

	public static bool operator ==(WordCondition left, WordCondition right) => left.Equals(right);

	public static bool operator !=(WordCondition left, WordCondition right) => !(left == right);
}
