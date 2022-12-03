using System;

namespace AutoKkutu.Constants
{
	public class PresentedWord
	{
		public string Content
		{
			get;
		}

		public bool CanSubstitution
		{
			get;
		}

		public string? Substitution
		{
			get;
		}

		public PresentedWord(string content, bool canSubsitution, string substituation = "")
		{
			Content = content;
			CanSubstitution = canSubsitution;
			if (!CanSubstitution)
				return;
			Substitution = substituation;
		}

		public override bool Equals(object? obj) => obj is PresentedWord other
			&& string.Equals(Content, other.Content, StringComparison.OrdinalIgnoreCase)
			&& Substitution == other.Substitution
			&& (!CanSubstitution || string.Equals(Substitution, other.Substitution, StringComparison.OrdinalIgnoreCase));

		public override int GetHashCode() => HashCode.Combine(Content, CanSubstitution, Substitution);
	}
}
