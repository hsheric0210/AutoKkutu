using System.Text;

namespace AutoKkutuLib;

/// <summary>
/// 단어 검색 조건을 나타냅니다
/// </summary>
public readonly struct WordCondition
{
	public static WordCondition Empty { get; } = new WordCondition("");

	/// <summary>
	/// 주 단어 조건 문자
	/// </summary>
	public string Char { get; }

	/// <summary>
	/// 보조 단어 조건 문자가 존재하는지의 여부
	/// </summary>
	public bool SubAvailable => !string.IsNullOrWhiteSpace(SubChar);

	/// <summary>
	/// 보조 단어 조건 문자 (두음법칙 등)
	/// </summary>
	public string? SubChar { get; }

	/// <summary>
	/// 미션 글자
	/// </summary>
	public string? MissionChar { get; }

	public WordCondition(string content, string? substitution = null, string? missionChar = null)
	{
		Char = content;
		SubChar = substitution;
		MissionChar = missionChar;
	}

	public override bool Equals(object? obj) => obj is WordCondition other
		&& string.Equals(Char, other.Char, StringComparison.OrdinalIgnoreCase)
		&& string.Equals(MissionChar, other.MissionChar, StringComparison.OrdinalIgnoreCase)
		&& (!SubAvailable || string.Equals(SubChar, other.SubChar, StringComparison.OrdinalIgnoreCase));

	/// <summary>
	/// 단어 검색 조건이 서로 비슷한지의 여부를 반환합니다.
	/// 단어 검색 조건이 비슷하다는 것은 서로 한 개 이상의 조건을 공유하고, 미션 단어가 동일하다는 것을 의미합니다.
	/// </summary>
	/// <remarks>
	/// 에시로, <c>{Char: 나, MissionChar: 마}</c> 와 <c>{Char: 라, SubChar: 나, MissionChar: 마}</c>는 서로 비슷한 단어 검색 조건입니다.
	/// </remarks>
	/// <param name="o"></param>
	/// <returns></returns>
	public bool IsSimilar(WordCondition? o)
	{
		return o is WordCondition other && MissionChar == other.MissionChar
		&& (string.Equals(Char, other.Char, StringComparison.OrdinalIgnoreCase)
			|| SubAvailable && string.Equals(SubChar, other.Char, StringComparison.OrdinalIgnoreCase)
			|| other.SubAvailable && string.Equals(Char, other.SubChar, StringComparison.OrdinalIgnoreCase)
			|| SubAvailable && other.SubAvailable && string.Equals(SubChar, other.SubChar, StringComparison.OrdinalIgnoreCase));
	}

	public override int GetHashCode() => HashCode.Combine(Char, SubAvailable, SubChar, MissionChar);

	public override string ToString()
	{
		var builder = new StringBuilder();
		builder.Append("WordCondition{Char: ").Append(Char);
		if (SubAvailable)
			builder.Append(", SubChar: ").Append(SubChar);
		if (!string.IsNullOrWhiteSpace(MissionChar))
			builder.Append(", MissionChar: ").Append(MissionChar);
		return builder.Append('}').ToString();
	}

	public static bool operator ==(WordCondition left, WordCondition right) => left.Equals(right);

	public static bool operator !=(WordCondition left, WordCondition right) => !(left == right);
}
