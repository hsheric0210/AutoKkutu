using System;
using System.ComponentModel;

namespace AutoKkutu.Constants
{
	[TypeConverter(typeof(WordPreferenceTypeConverter))]
	public class WordPreference
	{
		private readonly WordType[] Attributes;

		public WordPreference() : this(Array.Empty<WordType>())
		{
		}

		public WordPreference(WordType[] attributes)
		{
			Attributes = attributes;
		}
		public WordType[] GetAttributes() => Attributes;

		public static string GetName(WordType attr)
		{
			string name = "";
			if (attr.HasFlag(WordType.EndWord))
				name += "한방";
			else if (attr.HasFlag(WordType.AttackWord))
				name += "공격";

			if (attr.HasFlag(WordType.MissionWord))
			{
				if (string.IsNullOrEmpty(name))
					name = "미션";
				else
					name += " 미션";
			}

			return string.IsNullOrEmpty(name) ? "일반 단어" : $"{name} 단어";
		}

		public static WordType[] GetDefault() => new WordType[]
			{
				WordType.EndWord | WordType.MissionWord,
				WordType.EndWord,
				WordType.AttackWord | WordType.MissionWord,
				WordType.AttackWord,
				WordType.MissionWord,
				WordType.None
			};
	}
}
