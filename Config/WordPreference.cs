namespace AutoKkutu.Constants
{
	public class WordPreference
	{
		private readonly WordAttributes[] Attributes;

		public WordPreference() : this(GetDefault())
		{
		}

		public WordPreference(WordAttributes[] attributes)
		{
			Attributes = attributes;
		}
		public WordAttributes[] GetAttributes() => Attributes;

		public static string GetName(WordAttributes attr)
		{
			string name = "";
			if (attr.HasFlag(WordAttributes.EndWord))
				name += "한방";
			else if (attr.HasFlag(WordAttributes.AttackWord))
				name += "공격";

			if (attr.HasFlag(WordAttributes.MissionWord))
			{
				if (string.IsNullOrEmpty(name))
					name = "미션";
				else
					name += " 미션";
			}

			return string.IsNullOrEmpty(name) ? "일반 단어" : $"{name} 단어";
		}

		private static WordAttributes[] GetDefault()
		{
			return new WordAttributes[]
			{
				WordAttributes.EndWord | WordAttributes.MissionWord,
				WordAttributes.EndWord,
				WordAttributes.AttackWord | WordAttributes.MissionWord,
				WordAttributes.AttackWord,
				WordAttributes.MissionWord,
				WordAttributes.None
			};
		}
	}
}
