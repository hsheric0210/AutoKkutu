using System.ComponentModel;

namespace AutoKkutuLib;

/// <summary>
/// 단어 선호도 순위 목록을 나타냅니다.
/// 목록 상 맨 위에 있는 단어 조건부터 우선하여 검색됩니다.
/// </summary>
[TypeConverter(typeof(WordPreferenceTypeConverter))]
public sealed class WordPreference : IEquatable<WordPreference?>
{
	private readonly WordCategories[] attributes;

	public WordPreference() : this(Array.Empty<WordCategories>())
	{
	}

	public WordPreference(WordCategories[] attributes) => this.attributes = attributes;
	public WordCategories[] GetAttributes() => attributes;

	public override bool Equals(object? obj) => Equals(obj as WordPreference);

	public bool Equals(WordPreference? other) => other is not null && attributes.SequenceEqual(other.attributes);

	public override int GetHashCode()
	{
		var hash = new HashCode();
		foreach (var category in attributes)
			hash.Add(category);
		return hash.ToHashCode();
	}

	public static bool operator ==(WordPreference? left, WordPreference? right) => EqualityComparer<WordPreference>.Default.Equals(left, right);
	public static bool operator !=(WordPreference? left, WordPreference? right) => !(left == right);

	// static utility methods

	public static string GetName(WordCategories attr)
	{
		var name = "";
		if (attr.HasFlag(WordCategories.EndWord))
			name += "한방";
		else if (attr.HasFlag(WordCategories.AttackWord))
			name += "공격";

		if (attr.HasFlag(WordCategories.MissionWord))
		{
			if (string.IsNullOrEmpty(name))
				name = "미션";
			else
				name += " 미션";
		}

		return string.IsNullOrEmpty(name) ? "일반 단어" : $"{name} 단어";
	}

	public static WordCategories[] GetDefault() => new WordCategories[]
		{
			WordCategories.EndWord | WordCategories.MissionWord,
			WordCategories.EndWord,
			WordCategories.AttackWord | WordCategories.MissionWord,
			WordCategories.AttackWord,
			WordCategories.MissionWord,
			WordCategories.None
		};
}
