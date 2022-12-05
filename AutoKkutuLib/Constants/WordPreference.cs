using System;
using System.ComponentModel;

namespace AutoKkutuLib.Constants;

[TypeConverter(typeof(WordPreferenceTypeConverter))]
public class WordPreference
{
	private readonly WordCategories[] Attributes;

	public WordPreference() : this(Array.Empty<WordCategories>())
	{
	}

	public WordPreference(WordCategories[] attributes)
	{
		Attributes = attributes;
	}
	public WordCategories[] GetAttributes() => Attributes;

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
