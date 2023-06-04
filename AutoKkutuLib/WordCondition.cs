﻿using System.Text;

namespace AutoKkutuLib;

/// <summary>
/// 단어 검색 조건을 나타냅니다
/// </summary>
public struct WordCondition
{
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

	public bool Equals(object? obj, bool checkMissionChar) => obj is WordCondition other
		&& string.Equals(Char, other.Char, StringComparison.OrdinalIgnoreCase)
		&& (!checkMissionChar || string.Equals(MissionChar, other.MissionChar, StringComparison.OrdinalIgnoreCase))
		&& (!SubAvailable || string.Equals(SubChar, other.SubChar, StringComparison.OrdinalIgnoreCase));

	public override int GetHashCode() => HashCode.Combine(Char, SubAvailable, SubChar, MissionChar);

	public override bool Equals(object? obj) => Equals(obj, true);

	public override string ToString()
	{
		var builder = new StringBuilder();
		builder.Append("WordCondition[Char='").Append(Char).Append('\'');
		if (SubAvailable)
			builder.Append(", SubChar='").Append(SubChar).Append('\'');
		if (!string.IsNullOrWhiteSpace(MissionChar))
			builder.Append(", MissionChar='").Append(MissionChar).Append('\'');
		return builder.Append(']').ToString();
	}
}